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
        public Server(string ver, int port = 0, CancellationToken? cancellationToken = null, ILogger log = null)
        {
            client = new UdpClient(port);
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
        }

        private async Task PlayerHeartBeat(Player plr)
        {

        }

        /**
         * @brief
         * Main server logic
         * 
         * @remarks
         * Only one instance of this method can run at a time
         */
        public async Task Run(int maxPlayers)
        {
            if (disposedValue)
                throw new ObjectDisposedException(nameof(Server));

            if(Interlocked.CompareExchange(ref running, 1, 0) != 0)
            {
                return;
            }

            var args = new ServerStoppedArgs(ServerStopReason.NormalShutdown, "");
            CancellationTokenRegistration registration = token.Register(client.Dispose);
            logger?.LogInfo("Started server at port " + Port + "!");

            // id tracking
            bool[] ids = new bool[maxPlayers];

            // pre-serialize these static messages to not serialize them each time we need to send them
            byte[] disconnectBytes = new Message(MessageType.Disconnect, Encoding.UTF8.GetBytes("Client sent invalid data")).Serialize();
            byte[] fullBytes = new Message(MessageType.Handshake, new HandshakeServer(new HandshakeServer.Player[0], "The server is full!").Serialize()).Serialize();
            byte[] pongBytes = new Message(MessageType.Ping).Serialize();
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    UdpReceiveResult result = await client.ReceiveAsync();

                    try
                    {
                        Message message = Message.Deserialize(result.Buffer);

                        switch(message.Type)
                        {
                            case MessageType.Handshake:
                                if(players.ContainsKey(result.RemoteEndPoint))
                                    throw new InvalidDataException();

                                var data = HandshakeClient.Deserialize(message.Data);

                                if (data.Username.Length > 35 || data.GameVersion.Length > 30)
                                    throw new InvalidDataException();

                                if (!data.GameVersion.Equals(gameVersion))
                                {
                                    byte[] rejectBytes = new Message(MessageType.Handshake,
                                        new HandshakeServer(new HandshakeServer.Player[0],
                                        $"Game version mismatch! server {gameVersion} client {data.GameVersion}").Serialize()
                                        ).Serialize();
                                    await client.SendAsync(rejectBytes, rejectBytes.Length, result.RemoteEndPoint);
                                    break;
                                }

                                int id = -1;

                                for (int i = 0; i < ids.Length; ++i)
                                {
                                    if (!ids[i])
                                    {
                                        id = i;
                                        ids[i] = true;
                                        break;
                                    }
                                }

                                if (id == -1)
                                {
                                    await client.SendAsync(fullBytes, fullBytes.Length, result.RemoteEndPoint);
                                    break;
                                }


                                HandshakeServer.Player[] pubPlayers = players.Values.Select(HandshakeServer.Player.FromInternal).ToArray();

                                byte[] acceptBytes = new Message(
                                    MessageType.Handshake,
                                    new HandshakeServer(
                                        pubPlayers,
                                        id:id
                                    ).Serialize()
                                    ).Serialize();

                                await client.SendAsync(acceptBytes, acceptBytes.Length, result.RemoteEndPoint);

                                var player = new Player(
                                    id,
                                    data.Skin,
                                    data.Username, 
                                    "Base Biome 1", // the main cult
                                    new PlayerState(PlayerState.State.Idle, 0, 0, false, 0, new Vector3()),
                                    CancellationTokenSource.CreateLinkedTokenSource(token));
                                players.TryAdd(result.RemoteEndPoint, player);

                                _ = PlayerHeartBeat(player);

                                break;

                            case MessageType.Disconnect:
                                players.TryRemove(result.RemoteEndPoint, out var remove);
                                if (remove != null)
                                {
                                    logger?.LogInfo(remove.Username + " left the game");
                                    remove.Cancellation.Cancel();
                                    ids[remove.ID] = false;
                                }
                                break;

                            case MessageType.Ping:
                                await client.SendAsync(pongBytes, pongBytes.Length, result.RemoteEndPoint);
                                break;
                        }
                    }
                    catch(Exception e) when (e is InvalidDataException || e is ArgumentNullException)
                    {
                        players.TryRemove(result.RemoteEndPoint, out var removed);
                        if (removed != null)
                            ids[removed.ID] = false;
                        logger?.LogInfo(result.RemoteEndPoint + " (" + (removed?.Username ?? "no username") + ") disconnected: invalid data sent");
                        removed?.Cancellation.Cancel();
                        try
                        {
                            await client.SendAsync(disconnectBytes, disconnectBytes.Length, result.RemoteEndPoint);
                        }
                        catch { }
                    }
                    catch (SocketException e)
                    {
                        players.TryRemove(result.RemoteEndPoint, out var removed);
                        if (removed != null)
                            ids[removed.ID] = false;
                        logger?.LogInfo(result.RemoteEndPoint + " (" + (removed?.Username ?? "no username") + ") disconnected: " + e.Message);
                        removed?.Cancellation.Cancel();
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
