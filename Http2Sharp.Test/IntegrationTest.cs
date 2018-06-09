using System;
using System.IO;
using System.Net;
using JetBrains.Annotations;
using Xunit;

namespace Http2Sharp.Test
{
    public sealed class IntegrationTest : IDisposable
    {
        private readonly HttpServer server = new HttpServer(new TestServer());

        public IntegrationTest()
        {
            server.AddListener(new HttpListener(IPAddress.Loopback, 8080));
            server.StartListen();
        }

        public void Dispose()
        {
            server.Dispose();
        }

        [Fact]
        public void TestGetEmpty()
        {
            var result = SendRequest("GET", "http://localhost:8080/");
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void TestGetEcho()
        {
            var result = SendRequest("GET", "http://localhost:8080/echo?value=TEST");
            Assert.Equal("TEST", result);
        }

        [Fact]
        public void TestBad()
        {
            var exception = Record.Exception(() =>
            {
                SendRequest("GET", "http://localhost:8080/not-found");
            });
            Assert.IsType<WebException>(exception);

            var response = (HttpWebResponse)((WebException)exception).Response;
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
