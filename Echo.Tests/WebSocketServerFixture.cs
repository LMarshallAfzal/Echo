using System;
using System.Net;
using System.Net.Sockets;
using Echo;

public class WebSocketServerFixture: IDisposable
{
    public WebSocketServer Server { get; private set; }
    public string ServerUrl { get; private set; }

    public WebSocketServerFixture()
    {
        int port = GetAvailablePort();
        ServerUrl = $"http://localhost:{port}/";
        Server = new(ServerUrl);
    }

    public void Dispose()
    {
        Server.StopAsync().GetAwaiter().GetResult();
    }

    private int GetAvailablePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}