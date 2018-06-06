using System.Threading.Tasks;

namespace Http2Sharp.Cli
{
    internal static class Program
    {
        private static async Task Main()
        {
            using (var server = new HttpServer<TestServer>())
            {
                await server.StartListen();
            }
        }
    }
}
