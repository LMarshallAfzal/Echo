using System;
using System.Threading.Tasks;

namespace Echo
{
    class Program
    {
        static async Task Main(string[] args) {
            string url = "http://localhost:8080/";

            WebSocketServer server  = new WebSocketServer(url);
            await server.StartAsync();

            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.Stop();
        }
    }
}