using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal sealed class HttpClient : IHttpClient
    {
        private const string HTTP_VERSION = "HTTP/1.1";

        [NotNull] private readonly IServerConfiguration serverConfiguration;
        [NotNull] private readonly HttpStream httpStream;
        [NotNull] private readonly EndPoint remoteEndPoint;

        public HttpClient([NotNull] IServerConfiguration serverConfiguration, [NotNull] HttpStream httpStream, [NotNull] EndPoint remoteEndPoint)
        {
            this.serverConfiguration = serverConfiguration;
            this.httpStream = httpStream;
            this.remoteEndPoint = remoteEndPoint;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            httpStream.Dispose();
        }

        /// <inheritdoc />
        public async Task SendResponseAsync(HttpResponse response)
        {
            serverConfiguration.AddServerHeaders(response);

            var result = new StringBuilder();
            result.Append(HTTP_VERSION + " " + (int)response.StatusCode + " " + response.StatusCodeReason + "\r\n");
            foreach (var (headerName, headerValue) in response.Headers)
            {
                result.Append(headerName + ": " + headerValue + "\r\n");
            }

            result.Append("\r\n");

            var headersData = Encoding.UTF8.GetBytes(result.ToString());

            await httpStream.WriteAsync(headersData, 0, headersData.Length).ConfigureAwait(false);
            if (response.DataStream != null)
            {
                await response.DataStream.CopyToAsync(httpStream).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public string RemoteEndPoint => remoteEndPoint.ToString();

        /// <inheritdoc />
        public Stream Stream => httpStream;

        /// <inheritdoc />
        public HttpStream HttpStream => httpStream;
    }
}
