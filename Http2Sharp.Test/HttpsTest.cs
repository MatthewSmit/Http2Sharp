using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using JetBrains.Annotations;
using Xunit;

namespace Http2Sharp
{
    public sealed class HttpsTest : IDisposable
    {
        private readonly HttpServer server = new HttpServer();

        public HttpsTest()
        {
            server.AddListener(new HttpsListener(server, IPAddress.Loopback, 8081, new X509Certificate2("key.pfx")));
            server.AddHandler(new GenericHttpHandler(new TestServer()));
            server.StartListen();
        }

        public void Dispose()
        {
            server.Dispose();
        }

        [Fact]
        public void TestTls()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var result = SendRequest("GET", "https://localhost:8081/");
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
