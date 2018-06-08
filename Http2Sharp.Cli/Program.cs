using System.Net;

namespace Http2Sharp.Cli
{
    internal static class Program
    {
        private static void Main()
        {
            using (var server = new HttpServer(new TestServer()))
            {
                server.AddListener(new HttpListener(IPAddress.Loopback, 8080));
                server.StartListen();
                server.WaitForAll();
            }
        }
    }
}
