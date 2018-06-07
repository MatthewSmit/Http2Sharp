using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal sealed class HttpClient : IDisposable
    {
        private const string METHOD = "(?<method>GET|HEAD|POST|PUT|DELETE|CONNECT|OPTIONS|TRACE|PATCH)";
        private const string PCHAR = @"([a-zA-Z0-9\-\._~@:!$&'()\*\+,;=]|%[a-fA-F0-9][a-fA-F0-9])";
        private const string SEGMENT = PCHAR + "*";
        private const string QUERY = "(" + PCHAR + "|/|\\?)*";
        private const string ABSOLUTE_PATH = "(/" + SEGMENT + ")+";
        private const string ORIGIN_FORM = ABSOLUTE_PATH + @"(\?" + QUERY + ")?";
//        private const string ABSOLUTE_FORM = @"";
//        private const string AUTHORITY_FORM = @"";
//        private const string ASTERISK_FORM = @"\*";
        private const string REQUEST_TARGET = "(?<target>" + ORIGIN_FORM/* + "|" + ABSOLUTE_FORM + "|" + AUTHORITY_FORM + "|" + ASTERISK_FORM*/ + ")";
        private const string HTTP_VERSION_REGEX = @"(?<version>HTTP/\d\.\d)";

        private const string FIELD_NAME = @"[!#$%&'\*\+\-\.^_`\|~a-zA-Z0-9]+";
        private const string FIELD_VALUE = @".+?";

        private static readonly Regex requestLineRegex = new Regex($"^{METHOD} {REQUEST_TARGET} {HTTP_VERSION_REGEX}$");
        private static readonly Regex headerLineRegex = new Regex($@"^(?<name>{FIELD_NAME}):\s*(?<value>{FIELD_VALUE})\s*$");

        private const string HTTP_VERSION = "HTTP/1.1";

        [NotNull] private readonly StreamReader reader;
        private readonly List<(string, string)> headers = new List<(string, string)>();

        public HttpClient([NotNull] TcpClient client)
        {
            Client = client;
            reader = new StreamReader(client.GetStream());
        }

        public void Dispose()
        {
            reader.Dispose();
            Client.Dispose();
        }

        public async Task ReadHeadersAsync()
        {
            var startLine = await reader.ReadLineAsync().ConfigureAwait(false);
            var startLineMatch = requestLineRegex.Match(startLine);

            if (!startLineMatch.Success)
            {
                throw new HttpException("Invalid start line: " + startLine, 400);
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
                    throw new HttpException("Invalid header line: " + headerLine, 400);
                }

                var name = headerLineMatch.Groups["name"].Value.ToUpperInvariant();
                var value = headerLineMatch.Groups["value"].Value;
                headers.Add((name, value));
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
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task SendResponseAsync([NotNull] HttpResponse httpResponse)
        {
            var result = new StringBuilder();
            result.Append(HTTP_VERSION + " " + httpResponse.StatusCode + " " + httpResponse.StatusCodeReason + "\r\n");
            foreach (var (headerName, headerValue) in httpResponse.Headers)
            {
                result.Append(headerName + ": " + headerValue + "\r\n");
            }

            result.Append("\r\n");

            var headersData = Encoding.UTF8.GetBytes(result.ToString());

            var stream = Client.GetStream();
            await stream.WriteAsync(headersData, 0, headersData.Length).ConfigureAwait(false);
            if (httpResponse.Data != null)
            {
                await stream.WriteAsync(httpResponse.Data, 0, httpResponse.Data.Length).ConfigureAwait(false);
            }
        }

        public async Task<object> ReadBodyAsync()
        {
            switch (Method)
            {
                case Method.Get:
                case Method.Options:
                    if (GetHeader("Content-Length") == null && GetHeader("Transfer-Encoding") == null)
                    {
                        return null;
                    }
                    break;
                case Method.Head:
                case Method.Delete:
                case Method.Trace:
                    return null;
                case Method.Post:
                case Method.Put:
                case Method.Connect:
                case Method.Patch:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return await reader.ReadToEndAsync().ConfigureAwait(false);
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

        public TcpClient Client { get; }

        public IReadOnlyList<(string, string)> Headers => headers;

        public Method Method { get; private set; }

        public string Target { get; private set; }

        public string Version { get; private set; }
    }
}