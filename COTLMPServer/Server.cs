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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
            await sendLock.WaitAsync(token);
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
                logger?.LogInfo($"{removed.Username} ({endPoint}) disconnected: {message ?? "No reason provided"}");
                removed.Cancellation.Cancel();
                msg.Sequence = removed.Sequence;
                lock (idLock)
                {
                    ids[removed.ID] = false;
                }
            }
            await Send(endPoint, msg.Serialize());
            await SendToBiome(removed.Biome, MessageType.PlayerLeft, BitConverter.GetBytes(BitConverter.IsLittleEndian ? removed.ID : ReverseEndianness(removed.ID)), null);
        }

        private static uint ReverseEndianness(uint val)
        {
            return ((val & 0x000000FFU) << 24 |
                    (val & 0x0000FF00U) << 8 |
                    (val & 0x00FF0000U) >> 8 |
                    (val & 0xFF000000U) >> 24);
        }

        private async Task SendToBiome(string name, MessageType type, byte[] data, Player except)
        {
            var pairs = players.ToArray().Where(p => p.Value.Biome == name);

            Message msg = new Message(type, 0, data);
            var tasks = new List<Task>();
            foreach(var pair in pairs)
            {
                if (pair.Value == except)
                    continue;

                byte[] bytes;
                lock (pair.Value.Lock)
                {
                    msg.Sequence = pair.Value.Sequence++;
                    bytes = msg.Serialize();
                }
                tasks.Add(Send(pair.Key, bytes));
            }
            await Task.WhenAll(tasks);
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
                                lock (plr.Lock)
                                {
                                    plr.Sequence = message.Sequence + 1;
                                    plr.Lag = false;
                                }
                        }
                        else if (message.Type != MessageType.Ping && message.Type != MessageType.Handshake)
                            continue;

                        switch (message.Type)
                        {
                            case MessageType.Handshake:
                                if (message.Sequence != 1 || plr != null)
                                    throw new InvalidDataException();

                                var data = HandshakeClient.Deserialize(message.Data);

                                if (data.GameVersion != gameVersion)
                                {
                                    await DisconnectPlayer(result.RemoteEndPoint, $"Game version mismatch! server {gameVersion} client {data.GameVersion}");
                                    break;
                                }

                                uint id = 9999;

                                lock (idLock)
                                {
                                    for (uint i = 0; i < ids.Length; ++i)
                                    {
                                        if (!ids[i])
                                        {
                                            id = i;
                                            ids[i] = true;
                                            break;
                                        }
                                    }
                                }

                                if (id == 9999)
                                {
                                    await DisconnectPlayer(result.RemoteEndPoint, "The server is full!");
                                    break;
                                }

                                byte[] acceptBytes = new Message(
                                    MessageType.Handshake,
                                    2,
                                    (BitConverter.IsLittleEndian) ? BitConverter.GetBytes(id) : BitConverter.GetBytes(id).Reverse().ToArray()
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

                                logger?.LogInfo($"{player.Username} ({result.RemoteEndPoint}) joined the game");

                                break;

                            case MessageType.Transition:
                                {
                                    plr.Biome = Encoding.UTF8.GetString(message.Data);
                                    var pairs = players.ToArray().Where(p => p.Value.Biome == plr.Biome);

                                    var msg = new Message(MessageType.StateUpdate, 0, null);
                                    byte[] localInfo = PlayerInfo.FromInternal(plr).Serialize();

                                    var tasks = new List<Task>();
                                    foreach (var pair in pairs)
                                    {
                                        byte[] bytes;
                                        lock (plr.Lock)
                                        {
                                            msg.Sequence = plr.Sequence++;
                                            msg.Data = PlayerInfo.FromInternal(pair.Value).Serialize();
                                            bytes = msg.Serialize();
                                        }
                                        tasks.Add(Send(result.RemoteEndPoint, bytes));

                                        lock (pair.Value.Lock)
                                        {
                                            msg.Sequence = plr.Sequence++;
                                            msg.Data = localInfo;
                                            bytes = msg.Serialize();
                                        }
                                        tasks.Add(Send(pair.Key, bytes));
                                    }
                                    await Task.WhenAll(tasks);
                                }
                                break;

                            case MessageType.PositionUpdate:
                                {
                                    Vector3.Deserialize(message.Data, 0, out _); // to check the format
                                    byte[] bytes = new byte[sizeof(uint) + Vector3.SerializedSize];
                                    Array.Copy(BitConverter.GetBytes(BitConverter.IsLittleEndian ? plr.ID : ReverseEndianness(plr.ID)), bytes, sizeof(uint));
                                    Array.Copy(message.Data, 0, bytes, sizeof(uint), Vector3.SerializedSize);

                                    await SendToBiome(plr.Biome, MessageType.PositionUpdate, bytes, plr);
                                }
                                break;

                            case MessageType.Disconnect:
                                await DisconnectPlayer(result.RemoteEndPoint, "Disconnected");
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
