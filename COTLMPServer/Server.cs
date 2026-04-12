/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define COTLMP server class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMPServer.Messages;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains the classes/structs/enums for the server
 */
namespace COTLMPServer
{
    /**
     * @brief
     * This class implements the functionality of a server
     *
     * @field Port
     * The port on which the server is running on
     *
     * The port that the server is running on
     * 
     * @field ServerStopped
     * The event that is invoked when the server is stopped
     * 
     * @field running
     * 1 if server is running, 0 if not
     * 
     * @field disposedValue
     * Whether if the Server object was disposed
     * 
     * @field client
     * The UdpClient object that the server uses for listening
     * 
     * @field token
     * The user-provided cancellation token that is canceled to stop the server
     * 
     * @field logger
     * The user-provided logger
     */
    public sealed class Server : IDisposable
    {
        public readonly int Port;
        public event EventHandler<ServerStoppedArgs> ServerStopped;

        private volatile int running;
        private volatile bool disposedValue;
        private readonly UdpClient client;
        private readonly CancellationToken token;
        private readonly ILogger logger;
        private readonly string gameVersion;
        private readonly ConcurrentDictionary<IPEndPoint, Player> players;
        private readonly SemaphoreSlim sendLock;
        private readonly bool[] ids;
        private readonly object idLock;

        /**
         * @brief
         * The Server class constructor
         * 
         * @param[in] port
         * The port that the server should listen on. 0 for any ephemeral port.
         * 
         * @param[in] cancellationToken
         * The cancellation token to stop the server. Null if none.
         * 
         * @param[in] logger
         * The logger that the server should use. Null if none.
         */
        public Server(string ver, int maxPlayers, int port = 0, CancellationToken? cancellationToken = null, ILogger log = null)
        {
            client = new UdpClient(port);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                client.Client.IOControl(-1744830452, new byte[] { 0 }, null);
            logger = log;
            running = 0;
            players = new ConcurrentDictionary<IPEndPoint, Player>();
            Port = (client.Client.LocalEndPoint as IPEndPoint)?.Port ?? 0;
            gameVersion = ver;
            if (cancellationToken == null)
                token = CancellationToken.None;
            else
                token = cancellationToken.Value;
            sendLock = new SemaphoreSlim(1, 1);
            ids = new bool[maxPlayers];
            idLock = new object();
        }

        private async Task PlayerHeartBeat(Player plr, IPEndPoint endpoint)
        {
            try
            {
                while (!plr.Cancellation.Token.IsCancellationRequested)
                {
                    bool disconnect = false;
                    lock (plr.Lock)
                    {
                        if (plr.Lag)
                        {
                            disconnect = true;
                        }
                        plr.Lag = true;
                    }
                    if (disconnect)
                    {
                        await DisconnectPlayer(endpoint, "Timed out");
                        break;
                    }
                    await Task.Delay(15000, plr.Cancellation.Token);
                }
            }
            finally
            {
                plr.Cancellation.Dispose();
            }
        }

        private async Task Send(IPEndPoint endPoint, byte[] data)
        {
            await sendLock.WaitAsync();
            try
            {
                await client.SendAsync(data, data.Length, endPoint);
            } // don't catch anything, let the caller do it
            finally
            {
                sendLock.Release();
            }
        }

        private async Task DisconnectPlayer(IPEndPoint endPoint, string message = null)
        {
            Message msg = new Message(MessageType.Disconnect, 1, message == null ? null : Encoding.UTF8.GetBytes(message));
            if (players.TryRemove(endPoint, out var removed))
            {
                logger?.LogInfo($"{endPoint} ({removed.Username}) disconnected: {message ?? "No reason provided"}");
                removed.Cancellation.Cancel();
                msg.Sequence = removed.Sequence;
                lock (idLock)
                {
                    ids[removed.ID] = false;
                }
            }
            await Send(endPoint, msg.Serialize());
        }

        /**
         * @brief
         * Main server logic
         * 
         * @remarks
         * Only one instance of this method can run at a time
         */
        public async Task Run()
        {
            if (disposedValue)
                throw new ObjectDisposedException(nameof(Server));

            if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            {
                return;
            }

            var args = new ServerStoppedArgs(ServerStopReason.NormalShutdown, "");
            CancellationTokenRegistration registration = token.Register(client.Dispose);
            logger?.LogInfo("Started server at port " + Port + "!");

            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    UdpReceiveResult result = await client.ReceiveAsync();

                    if (result.Buffer.Length > 2500)
                    {
                        await DisconnectPlayer(result.RemoteEndPoint);
                    }

                    try
                    {
                        Message message = Message.Deserialize(result.Buffer);

                        if (players.TryGetValue(result.RemoteEndPoint, out var plr))
                        {
                            if (message.Sequence < plr.Sequence && message.Type != MessageType.Disconnect)
                                continue;
                            if (message.Sequence >= plr.Sequence)
                                lock(plr.Lock)
                                {
                                    plr.Sequence = message.Sequence + 1;
                                    plr.Lag = false;
                                }
                        }


                        switch (message.Type)
                        {
                            case MessageType.Handshake:
                                if (message.Sequence != 0 || players.ContainsKey(result.RemoteEndPoint))
                                    throw new InvalidDataException();

                                var data = HandshakeClient.Deserialize(message.Data);

                                if (data.Username.Length > 35 || data.GameVersion.Length > 30)
                                    throw new InvalidDataException();

                                if (!data.GameVersion.Equals(gameVersion))
                                {
                                    await DisconnectPlayer(result.RemoteEndPoint, $"Game version mismatch! server {gameVersion} client {data.GameVersion}");
                                    break;
                                }

                                int id = -1;

                                lock (idLock)
                                {
                                    for (int i = 0; i < ids.Length; ++i)
                                    {
                                        if (!ids[i])
                                        {
                                            id = i;
                                            ids[i] = true;
                                            break;
                                        }
                                    }
                                }

                                if (id == -1)
                                {
                                    await DisconnectPlayer(result.RemoteEndPoint, "The server is full!");
                                    break;
                                }


                                HandshakeServer.Player[] pubPlayers = players.Values.Select(HandshakeServer.Player.FromInternal).ToArray();

                                byte[] acceptBytes = new Message(
                                    MessageType.Handshake,
                                    1,
                                    new HandshakeServer(
                                        pubPlayers,
                                        id: id
                                    ).Serialize()
                                    ).Serialize();

                                try
                                {
                                    await Send(result.RemoteEndPoint, acceptBytes);
                                }
                                catch
                                {
                                    lock (idLock)
                                        ids[id] = false;
                                    break;
                                }

                                var player = new Player(
                                    id,
                                    data.Skin,
                                    data.Username,
                                    "Base Biome 1", // the main cult
                                    new PlayerState(PlayerState.State.Idle, 0, 0, false, 0, new Vector3()),
                                    CancellationTokenSource.CreateLinkedTokenSource(token));
                                if (!players.TryAdd(result.RemoteEndPoint, player))
                                {
                                    lock (idLock)
                                        ids[id] = false;
                                    await DisconnectPlayer(result.RemoteEndPoint, "The server is full!");
                                    player.Cancellation.Dispose();
                                    break;
                                }

                                _ = PlayerHeartBeat(player, result.RemoteEndPoint);

                                break;

                            case MessageType.Disconnect:
                                if (players.TryRemove(result.RemoteEndPoint, out var remove))
                                {
                                    logger?.LogInfo(remove.Username + " left the game");
                                    remove.Cancellation.Cancel();
                                    lock (idLock)
                                        ids[remove.ID] = false;
                                }
                                break;

                            case MessageType.Ping:
                                uint seq = plr?.Sequence ?? 0;
                                await Send(result.RemoteEndPoint, new Message(MessageType.Ping, seq).Serialize());
                                break;
                        }
                    }
                    catch (Exception e) when (e is InvalidDataException || e is ArgumentNullException)
                    {
                        try
                        {
                            await DisconnectPlayer(result.RemoteEndPoint, "Client sent invalid data");
                        }
                        catch { }
                    }
                    catch (SocketException e)
                    {
                        if (players.TryRemove(result.RemoteEndPoint, out var removed))
                        {
                            removed.Cancellation.Cancel();
                            lock (idLock)
                                ids[removed.ID] = false;
                            logger?.LogInfo($"{result.RemoteEndPoint} ({removed.Username}) disconnected: {e.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger?.LogInfo("Stopping server...");
            }
            catch (ObjectDisposedException) when (token.IsCancellationRequested)
            {
                logger?.LogInfo("Stopping server...");
            }
            catch (Exception e)
            {
                logger?.LogFatal(e.ToString());
                args.Reason = ServerStopReason.Error;
                args.What = e.ToString();
            }
            finally
            {
                registration.Dispose();
                client.Dispose();
                ServerStopped?.Invoke(this, args);
            }
        }

        /**
         * @brief
         * Dispose of unmanaged resources
         * 
         * @param[in] disposing
         * Whether if the Dispose() method was called manually
         */
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }
                disposedValue = true;
            }
        }

        /**
         * @brief
         * The server destructor
         */
        ~Server()
        {
            Dispose(false);
        }

        /**
         * @brief
         * Dispose of unmanaged resources 
         */
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

/* EOF */
