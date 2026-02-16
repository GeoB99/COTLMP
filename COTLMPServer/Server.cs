/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define COTLMP server class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMPServer.Messages;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
        public Server(int port = 0, CancellationToken? cancellationToken = null, ILogger log = null)
        {
            client = new UdpClient(port);
            logger = log;
            running = 0;
            Port = (client.Client.LocalEndPoint as IPEndPoint)?.Port ?? 0;
            if (cancellationToken == null)
                token = CancellationToken.None;
            else
                token = cancellationToken.Value;
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
                throw new ObjectDisposedException("Tried to use disposed Server object");

            if(Interlocked.CompareExchange(ref running, 1, 0) != 0)
            {
                return;
            }

            var args = new ServerStoppedArgs(ServerStopReason.NormalShutdown, "");
            CancellationTokenRegistration registration = token.Register(client.Dispose);
            logger?.LogInfo("Started server!");
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    UdpReceiveResult result = await client.ReceiveAsync();

                    Message message;
                    try
                    {
                        message = Message.Deserialize(result.Buffer);
                    }
                    catch(InvalidDataException)
                    {
                        // TODO: disconnect the user
                        continue;
                    }

                    switch(message.Type)
                    {
                        case MessageType.Test:
                            logger?.LogInfo("Test message!");
                            break;
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
                client.Dispose();
                registration.Dispose();
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
