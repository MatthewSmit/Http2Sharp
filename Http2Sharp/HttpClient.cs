using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Optional;
using Optional.Unsafe;

namespace Http2Sharp
{
    internal sealed class HttpClient : IHttpClient
    {
        private const string METHOD = "(?<method>GET|HEAD|POST|PUT|DELETE|CONNECT|OPTIONS|TRACE|PATCH)";
        private const string PCHAR = @"([a-zA-Z0-9\-\._~@:!$&'()\*\+,;=]|%[a-fA-F0-9][a-fA-F0-9])";
        private const string SEGMENT = PCHAR + "*";
        private const string QUERY = "(" + PCHAR + "|/|\\?)*";
        private const string ABSOLUTE_PATH = "(/" + SEGMENT + ")+";
        private const string ORIGIN_FORM = ABSOLUTE_PATH + @"(\?" + QUERY + ")?";
        private const string REQUEST_TARGET = "(?<target>" + ORIGIN_FORM + ")";
        private const string HTTP_VERSION_REGEX = @"(?<version>HTTP/\d\.\d)";

        private const string FIELD_NAME = @"[!#$%&'\*\+\-\.^_`\|~a-zA-Z0-9]+";
        private const string FIELD_VALUE = @".+?";

        private static readonly Regex requestLineRegex = new Regex($"^{METHOD} {REQUEST_TARGET} {HTTP_VERSION_REGEX}$");
        private static readonly Regex headerLineRegex = new Regex($@"^(?<name>{FIELD_NAME}):\s*(?<value>{FIELD_VALUE})\s*$");

        private const string HTTP_VERSION = "HTTP/1.1";

        [NotNull] private readonly HttpReader reader;
        private readonly List<(string, string)> headers = new List<(string, string)>();
        private readonly TcpClient client;
        private readonly Stream stream;

        private Option<long> contentLength;

        public HttpClient([NotNull] TcpClient client, [NotNull] Stream stream)
        {
            this.client = client;
            this.stream = stream;
            reader = new HttpReader(stream);
        }

        public HttpClient([NotNull] TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
            reader = new HttpReader(stream);
        }

        public void Dispose()
        {
            stream.Dispose();
            client.Dispose();
        }

        /// <inheritdoc />
        public async Task ReadHeadersAsync()
        {
            var startLine = await reader.ReadLineAsync().ConfigureAwait(false);
            var startLineMatch = requestLineRegex.Match(startLine);

            if (!startLineMatch.Success)
            {
                throw new HttpException("Invalid start line: " + startLine, HttpStatusCode.BadRequest);
            }

            Method = ParseMethod(startLineMatch.Groups["method"].Value);
            Target = startLineMatch.Groups["target"].Value;
            Version = startLineMatch.Groups["version"].Value;

            while (true)
            {
                var headerLine = await reader.ReadLineAsync().ConfigureAwait(false);
                if (headerLine.Length == 0)
                {
                    break;
                }

                var headerLineMatch = headerLineRegex.Match(headerLine);
                if (!headerLineMatch.Success)
                {
                    throw new HttpException("Invalid header line: " + headerLine, HttpStatusCode.BadRequest);
                }

                var name = headerLineMatch.Groups["name"].Value.ToUpperInvariant();
                var value = headerLineMatch.Groups["value"].Value;

                ProcessSpecialHeader(name, value);
                headers.Add((name, value));
            }
        }

        private void ProcessSpecialHeader([NotNull] string name, [NotNull] string value)
        {
            if (string.Equals(name, "Transfer-Encoding", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new NotImplementedException();
            }
            else if (string.Equals(name, "Content-Length", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
                {
                    throw new HttpException("Invalid Content-Length", HttpStatusCode.BadRequest);
                }

                contentLength = Option.Some(result);
            }
        }

        private static Method ParseMethod([NotNull] string value)
        {
            switch (value)
            {
                case "GET":
                    return Method.Get;
                case "HEAD":
                    return Method.Head;
                case "POST":
                    return Method.Post;
                case "PUT":
                    return Method.Put;
                case "DELETE":
                    return Method.Delete;
                case "CONNECT":
                    return Method.Connect;
                case "OPTIONS":
                    return Method.Options;
                case "TRACE":
                    return Method.Trace;
                case "PATCH":
                    return Method.Patch;
                default:
                    throw new ArgumentOutOfRangeException("Unknown or invalid HTTP method " + value);
            }
        }

        /// <inheritdoc />
        [ItemCanBeNull]
        public async Task<byte[]> ReadBodyAsync()
        {
            if (RequiresBody())
            {
                // TODO: Read Transfer-Encoding first

                if (contentLength.HasValue)
                {
                    var contentLengthValue = contentLength.ValueOrFailure();
                    if (contentLengthValue == 0)
                    {
                        return null;
                    }

                    var data = new byte[contentLengthValue];
                    await reader.ReadAsync(data, 0, data.Length).ConfigureAwait(false);
                    return data;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return null;
        }

        private bool RequiresBody()
        {
            switch (Method)
            {
                case Method.Get:
                case Method.Options:
                    return contentLength.HasValue || GetHeader("Transfer-Encoding") != null;

                case Method.Head:
                case Method.Delete:
                case Method.Trace:
                    return false;

                case Method.Post:
                case Method.Put:
                case Method.Connect:
                case Method.Patch:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException("Unknown or invalid HTTP method " + Method);
            }
        }

        /// <inheritdoc />
        public async Task SendResponseAsync([NotNull] HttpResponse response)
        {
            var result = new StringBuilder();
            result.Append(HTTP_VERSION + " " + (int)response.StatusCode + " " + response.StatusCodeReason + "\r\n");
            foreach (var (headerName, headerValue) in response.Headers)
            {
                result.Append(headerName + ": " + headerValue + "\r\n");
            }

            result.Append("\r\n");

            var headersData = Encoding.UTF8.GetBytes(result.ToString());

            await stream.WriteAsync(headersData, 0, headersData.Length).ConfigureAwait(false);
            if (response.Data != null)
            {
                await stream.WriteAsync(response.Data, 0, response.Data.Length).ConfigureAwait(false);
            }
        }

        private string GetHeader(string headerName)
        {
            foreach (var (name, value) in headers)
            {
                if (string.Equals(headerName, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return value;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public string RemoteEndPoint => client.Client.RemoteEndPoint.ToString();

        /// <inheritdoc />
        public Method Method { get; private set; }

        /// <inheritdoc />
        public string Target { get; private set; }

        /// <inheritdoc />
        public string Version { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<(string, string)> Headers => headers;
    }
}
