using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echo 
{
    /// <summary>
    /// Represents a WebSocket server.
    /// </summary>
    public class WebSocketServer
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, ConnectedClient> _connectedClients = new Dictionary<string, ConnectedClient>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class with the specified URL.
        /// </summary>
        /// <param name="url">The URL on which the WebSocket server will listen for incoming requests.</param>
        public WebSocketServer(string url)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the WebSocket serve and listens for incoming requests.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("WebSocket server started.");
            Console.WriteLine("=========================");

            foreach (string prefix in _listener.Prefixes)
            {
                Console.WriteLine($"Listening to incoming WS requests on {prefix}");
            }


            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    /// <summary>
                    /// Get the HTTP listener context for the incoming request.
                    /// </summary>
                    HttpListenerContext context = await _listener.GetContextAsync();
                    
                    Console.WriteLine($"Request: {context.Request.HttpMethod} {context.Request.Headers["Host"]} {context.Request.Headers["Upgrade"]} {context.Request.Headers["Sec-WebSocket-Key"]} {context.Request.Headers["Sec-WebSocket-Version"]}\n");

                    if (context.Request.IsWebSocketRequest)
                    {
                        await ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Stops the WebSocket server asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
            await Task.Delay(1000);
            _listener.Stop();
            Console.WriteLine("WebSocket server stopped.");
        }

        /// <summary>
        /// Processes the WebSocket request asynchronously.
        /// </summary>
        /// <param name="context">The HTTP listener context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            HttpListenerWebSocketContext? webSocketContext = null;

            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);

                var webSocket = webSocketContext.WebSocket;
                Console.WriteLine($"WebSocket state: {webSocket.State}");

                ConnectedClient connectedClient = await HandleClientConnection(webSocket);

                var buffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription ?? string.Empty, CancellationToken.None);
                    Console.WriteLine("WebSocket connection closed.");
                }
                else
                {
                    while (!result.CloseStatus.HasValue)
                    {
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var receivedMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                            Console.WriteLine($"Received message: {receivedMessage}");

                            var sendBuffer = System.Text.Encoding.UTF8.GetBytes(receivedMessage);
                            await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                            Console.WriteLine($"Sent message back: {receivedMessage}");
                        }

                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription ?? string.Empty, CancellationToken.None);
                            Console.WriteLine("WebSocket connection closed.");
                            break;
                        }
                    }
                }

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Console.WriteLine("WebSocket connection closed.");

                RemoveConnectedClient(connectedClient.ClientId);

            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"WebSocket processing failed: {ex.Message}");
                if (webSocketContext != null)
                {
                    webSocketContext.WebSocket.Abort();
                }
            }
        }

        /// <summary>
        /// Create a new connectedClient instance and add it to the _connectedClients dictionary
        /// </summary>
        /// <param name="webSocket">An instance of a WebSocket</param>
        /// <returns>An instance of connected client instance</returns>
        private async Task<ConnectedClient> HandleClientConnection(WebSocket webSocket)
        {
            string clientId = Guid.NewGuid().ToString();
            var connectedClient = new ConnectedClient(clientId, webSocket); 
            _connectedClients.Add(clientId, connectedClient);

            var welcomeBuffer = System.Text.Encoding.UTF8.GetBytes("Welcome to Echo Chat!");
            await webSocket.SendAsync(new ArraySegment<byte>(welcomeBuffer), WebSocketMessageType.Text, true, CancellationToken.None);

            return connectedClient;
        }

        /// <summary>
        /// Remove specified connected client from _connectedClient dictionary
        /// </summary>
        /// <param name="clientId">The ID of the connected client to be removed</param>
        private void RemoveConnectedClient(string clientId)
        {
            if(_connectedClients.ContainsKey(clientId))
            {
                _connectedClients.Remove(clientId);
                Console.WriteLine($"Client {clientId} removed from the connected clients.");
            }
        }
    }
}