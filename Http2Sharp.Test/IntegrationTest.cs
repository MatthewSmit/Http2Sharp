using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Xunit;

namespace Http2Sharp.Test
{
    public sealed class IntegrationTest : IDisposable
    {
        private const string BASE_URL = "http://localhost:8080";

        private readonly HttpServer server = new HttpServer(new TestServer());

        public IntegrationTest()
        {
            server.AddListener(new HttpListener(server, IPAddress.Loopback, 8080));
            server.StartListen();
        }

        public void Dispose()
        {
            server.Dispose();
        }

        [Fact]
        public void TestGetEmpty()
        {
            var result = SendRequest("GET", BASE_URL + "/");
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void TestGetEcho()
        {
            var result = SendRequest("GET", BASE_URL + "/echo?value=TEST");
            Assert.Equal("TEST", result);
        }

        [Fact]
        public void TestBad()
        {
            var exception = Record.Exception(() =>
            {
                SendRequest("GET", BASE_URL + "/not-found");
            });
            Assert.IsType<WebException>(exception);

            var response = (HttpWebResponse)((WebException)exception).Response;
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public void TestPost()
        {
            var result = SendRequest("POST", BASE_URL + "/post", new User
            {
                Gender = Gender.Male,
                DateOfBirth = DateTime.Now,
                Email = "test@test.test",
                Name = "Name Name",
                Username = "Username"
            });
            var userResult = JsonConvert.DeserializeObject<User>(result);
            Assert.Equal(Gender.Male, userResult.Gender);
            Assert.Equal("test@test.test", userResult.Email);
            Assert.Equal("Name Name", userResult.Name);
            Assert.Equal("Username", userResult.Username);
        }

        [NotNull]
        private static string SendRequest([NotNull] string method, [NotNull] string uri, [CanBeNull] object body = default)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = method;
            if (body != null)
            {
                var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }

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
