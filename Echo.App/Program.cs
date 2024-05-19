using System;
using System.Threading.Tasks;

namespace Echo
{
    class Program
    {
        static async Task Main(string[] args) {
            string url = "http://localhost:8080/";

            WebSocketServer server  = new WebSocketServer(url);

            Console.WriteLine("Press <ESC> key to stop the server...\n");
            while(Console.ReadKey().Key != ConsoleKey.Escape) 
            {
                await server.StartAsync();
            }
            await server.StopAsync();
        }
    }
}