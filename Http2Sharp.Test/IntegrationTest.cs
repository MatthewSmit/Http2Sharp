using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Xunit;

namespace Http2Sharp.Test
{
    public sealed class IntegrationTest : IDisposable
    {
        private readonly HttpServer server = new HttpServer(new TestServer());
        private readonly Task serverTask;

        public IntegrationTest()
        {
            server.Port = 8080;
            serverTask = server.StartListenAsync();
        }

        public void Dispose()
        {
            server.Dispose();
            serverTask.Wait();
        }

        [Fact]
        public void TestGetEmpty()
        {
            var result = SendRequest("GET", "http://localhost:8080/");
            Assert.Equal("Hello World", result);
        }

        [NotNull]
        private static string SendRequest([NotNull] string method, [NotNull] string uri)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = method;
            using (var response = request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException()))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
