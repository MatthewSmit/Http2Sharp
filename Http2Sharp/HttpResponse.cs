using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Http2Sharp
{
    public sealed class HttpResponse : IDisposable
    {
        [NotNull] private static readonly IReadOnlyDictionary<HttpStatusCode, string> statusCodeReasons = new Dictionary<HttpStatusCode, string>
        {
            // 1XX Informational
            { HttpStatusCode.Continue, "Continue" },
            { HttpStatusCode.SwitchingProtocols, "Switching Protocols" },

            // 2XX Success
            { HttpStatusCode.OK, "OK" },
            { HttpStatusCode.Created, "Created" },
            { HttpStatusCode.Accepted, "Accepted" },
            { HttpStatusCode.NonAuthoritativeInformation, "Non-Authoritative Information" },
            { HttpStatusCode.NoContent, "No Content" },
            { HttpStatusCode.ResetContent, "Reset Content" },
            { HttpStatusCode.PartialContent, "Partial Content" },

            // 3XX Redirection
            { HttpStatusCode.MultipleChoices, "Multiple Choices" },
            { HttpStatusCode.MovedPermanently, "Moved Permanently" },
            { HttpStatusCode.Found, "Found" },
            { HttpStatusCode.SeeOther, "See Other" },
            { HttpStatusCode.NotModified, "Not Modified" },
            { HttpStatusCode.UseProxy, "Use Proxy" },
            { HttpStatusCode.TemporaryRedirect, "Temporary Redirect" },

            // 4XX Client errors
            { HttpStatusCode.BadRequest, "Bad Request" },
            { HttpStatusCode.Unauthorized, "Unauthorized" },
            { HttpStatusCode.PaymentRequired, "Payment Required" },
            { HttpStatusCode.Forbidden, "Forbidden" },
            { HttpStatusCode.NotFound, "Not Found" },
            { HttpStatusCode.MethodNotAllowed, "Method Not Allowed" },
            { HttpStatusCode.NotAcceptable, "Not Acceptable" },
            { HttpStatusCode.ProxyAuthenticationRequired, "Proxy Authentication Required" },
            { HttpStatusCode.RequestTimeout, "Request Timeout" },
            { HttpStatusCode.Conflict, "Conflict" },
            { HttpStatusCode.Gone, "Gone" },
            { HttpStatusCode.LengthRequired, "Length Required" },
            { HttpStatusCode.PreconditionFailed, "Precondition Failed" },
            { HttpStatusCode.RequestEntityTooLarge, "Payload Too Large" },
            { HttpStatusCode.RequestUriTooLong, "URI Too Long" },
            { HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type" },
            { HttpStatusCode.RequestedRangeNotSatisfiable, "Range Not Satisfiable" },
            { HttpStatusCode.ExpectationFailed, "Expectation Failed" },
            { HttpStatusCode.UpgradeRequired, "Upgrade Required" },

            // 5XX Server errors
            { HttpStatusCode.InternalServerError, "Internal Server Error" },
            { HttpStatusCode.NotImplemented, "Not Implemented" },
            { HttpStatusCode.BadGateway, "Bad Gateway" },
            { HttpStatusCode.ServiceUnavailable, "Service Unavailable" },
            { HttpStatusCode.GatewayTimeout, "Gateway Timeout" },
            { HttpStatusCode.HttpVersionNotSupported, "HTTP Version Not Supported" },
        };

        [NotNull] private readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        private readonly bool leaveStreamOpen;

        private HttpResponse(HttpStatusCode statusCode, [CanBeNull] string mimeType, [CanBeNull] byte[] data)
            : this(statusCode, mimeType, data == null ? null : new MemoryStream(data), false)
        {
        }

        private HttpResponse(HttpStatusCode statusCode, [CanBeNull] string mimeType, [CanBeNull] Stream dataStream, bool leaveStreamOpen)
        {
            StatusCode = statusCode;

            if (mimeType != null && dataStream == null)
            {
                throw new ArgumentException("Must provide data if a mime type is provided");
            }

            headers["date"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);

            if (mimeType != null)
            {
                headers["content-type"] = mimeType;
            }

            if (dataStream != null)
            {
                headers["content-length"] = dataStream.Length.ToString(CultureInfo.InvariantCulture);
            }
            DataStream = dataStream;
            this.leaveStreamOpen = leaveStreamOpen;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!leaveStreamOpen)
            {
                DataStream?.Dispose();
            }
        }

        public bool ContainsHeader([NotNull] string name)
        {
            return headers.ContainsKey(name);
        }

        [NotNull]
        public static HttpResponse Send<T>([NotNull] T value)
        {
            return Status(HttpStatusCode.OK, value);
        }

        [NotNull]
        public static HttpResponse SendFile([NotNull] string filePath, [CanBeNull] string mimeType = null)
        {
            var fileInfo = new FileInfo(filePath);
            if (mimeType == null)
            {
                mimeType = MimeTypes.FindFromExtension(fileInfo.Extension);
            }

            return new HttpResponse(HttpStatusCode.OK, mimeType, fileInfo.OpenRead(), false);
        }

        [NotNull]
        public static HttpResponse Status(HttpStatusCode statusCode)
        {
            return new HttpResponse(statusCode, null, null);
        }

        [NotNull]
        public static HttpResponse Status<T>(HttpStatusCode statusCode, T value)
        {
            if (value is string valueString)
            {
                return new HttpResponse(statusCode, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes(valueString));
            }

            var valueData = JsonConvert.SerializeObject(value);
            return new HttpResponse(statusCode, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(valueData));
        }

        [NotNull]
        public IReadOnlyDictionary<string, string> Headers => headers;

        [CanBeNull]
        public Stream DataStream { get; }

        public HttpStatusCode StatusCode { get; }

        [NotNull]
        public string StatusCodeReason
        {
            get
            {
                if (statusCodeReasons.TryGetValue(StatusCode, out var value))
                {
                    return value;
                }

                return StatusCode.ToString();
            }
        }

        public void AddHeader([NotNull] string headerName, [NotNull] string headerValue)
        {
            if (headerName == null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            headers[headerName.ToLowerInvariant()] = headerValue;
        }
    }
}
