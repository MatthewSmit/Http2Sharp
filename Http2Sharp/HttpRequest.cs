using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Optional;
using Optional.Unsafe;

namespace Http2Sharp
{
    public sealed class HttpRequest
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

        private HttpRequest(IHttpClient client, string protocol, Method method, [NotNull] string target, [NotNull] string version, [NotNull] IReadOnlyList<(string, string)> headers)
        {
            Client = client;
            Method = method;
            Version = version;
            Headers = headers;

            Uri uriTarget = null;

            foreach (var (headerName, headerValue) in headers)
            {
                switch (headerName)
                {
                    case "host":
                        uriTarget = new Uri(protocol + "://" + headerValue + target);
                        break;

                    case "content-type":
                        ContentType = headerValue;
                        break;

                    case "content-length":
                        if (!long.TryParse(headerValue, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
                        {
                            throw new HttpException("Invalid Content-Length", HttpStatusCode.BadRequest);
                        }

                        ContentLength = Option.Some(result);
                        break;
                }
            }

            if (uriTarget == null)
            {
                uriTarget = new Uri(protocol + "://localhost" + target);
            }

            Target = uriTarget;
            Queries = uriTarget.SplitQueries();
        }

        [ItemNotNull]
        public static async Task<HttpRequest> FromHttpClientAsync(string protocol, [NotNull] IHttpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var stream = client.Stream;
            var startLine = await stream.ReadLineAsync().ConfigureAwait(false);
            var startLineMatch = requestLineRegex.Match(startLine);

            if (!startLineMatch.Success)
            {
                throw new HttpException("Invalid start line: " + startLine, HttpStatusCode.BadRequest);
            }

            var method = ParseMethod(startLineMatch.Groups["method"].Value);
            var target = startLineMatch.Groups["target"].Value;
            var version = startLineMatch.Groups["version"].Value;
            var headers = await ReadHeadersAsync(stream).ConfigureAwait(false);
            return new HttpRequest(client, protocol, method, target, version, headers);
        }

        [ItemNotNull]
        private static async Task<IReadOnlyList<(string, string)>> ReadHeadersAsync([NotNull] HttpStream stream)
        {
            var headers = new List<(string, string)>();
            while (true)
            {
                var headerLine = await stream.ReadLineAsync().ConfigureAwait(false);
                if (headerLine.Length == 0)
                {
                    return headers;
                }

                var headerLineMatch = headerLineRegex.Match(headerLine);
                if (!headerLineMatch.Success)
                {
                    throw new HttpException("Invalid header line: " + headerLine, HttpStatusCode.BadRequest);
                }

                var name = headerLineMatch.Groups["name"].Value.ToLowerInvariant();
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
                    throw new ArgumentOutOfRangeException("Unknown or invalid HTTP method " + value);
            }
        }

        [NotNull]
        public Stream GetBodyStream()
        {
            if (ContentLength.HasValue)
            {
                var contentLength = ContentLength.ValueOrFailure();
                return new LimitedStream(Client.Stream, contentLength);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public Method Method { get; }
        [NotNull]
        public Uri Target { get; }
        [NotNull]
        public string Version { get; }

        [CanBeNull]
        public string ContentType { get; }
        public Option<long> ContentLength { get; }

        [NotNull]
        public IReadOnlyList<(string, string)> Headers { get; }
        [NotNull]
        public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();
        [NotNull]
        public IEnumerable<(string, string)> Queries { get; }

        public IHttpClient Client { get; }
    }
}
