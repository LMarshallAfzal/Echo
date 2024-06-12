using System;
using System.Net;
using System.Net.WebSockets;


namespace Echo 
{
    public class ConnectedClient 
    {
        public string ClientId { get; set; }
        public WebSocket WebSocket { get; set; }

        public ConnectedClient(string clientId, WebSocket webSocket)
        {
            ClientId = clientId;
            WebSocket = webSocket;
        }
    }
}