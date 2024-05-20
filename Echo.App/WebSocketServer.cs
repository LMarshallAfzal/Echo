using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echo 
{
    public class WebSocketServer
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        public WebSocketServer(string url)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _cancellationTokenSource = new CancellationTokenSource();
        }

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
                    HttpListenerContext context = await _listener.GetContextAsync();
                    
                    // Console.WriteLine($"Request Headers:");
                    // foreach (string headerName in context.Request.Headers.AllKeys)
                    // {
                    //     Console.WriteLine($"{headerName}: {context.Request.Headers[headerName]}");
                    // }
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
                // finally {
                //     Console.WriteLine($"Request: {context.Request.HttpMethod} {context.Request.UserHostName} {context.Request.UserAgent}");
                // }
            }
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
            await Task.Delay(1000);
            _listener.Stop();
            Console.WriteLine("WebSocket server stopped.");
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
                return;
            }

            HttpListenerWebSocketContext? webSocketContext = null;
            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);

                var webSocket = webSocketContext.WebSocket;
                Console.WriteLine($"WebSocket state: {webSocket.State}");

                // TODO: handle the WebSocket connection
                var buffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.CloseStatus.HasValue)
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var recievedMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Recieved message: {recievedMessage}");

                        var sendBuffer = System.Text.Encoding.UTF8.GetBytes(recievedMessage);
                        await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        Console.WriteLine($"Sent message back: {recievedMessage}");
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        Console.WriteLine("WebSocket connection closed.");
                        break;
                    }

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
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
    }
}