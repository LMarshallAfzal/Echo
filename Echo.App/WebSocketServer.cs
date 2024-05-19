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

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
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

                // TODO: handle the WebSocket connection


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