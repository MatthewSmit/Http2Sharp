using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Http2Sharp
{
    public sealed class HttpResponse
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

        private HttpResponse(HttpStatusCode statusCode, [CanBeNull] string mimeType, [CanBeNull] byte[] data)
        {
            StatusCode = statusCode;

            if (mimeType != null && data == null ||
                mimeType == null && data != null)
            {
                throw new ArgumentException("Must provide a mime type if and only if providing data");
            }

            if (mimeType != null)
            {
                headers["Content-Type"] = mimeType;
            }

            if (data != null)
            {
                headers["Content-Length"] = data.Length.ToString(CultureInfo.InvariantCulture);
            }
            Data = data;
        }

        [NotNull]
        public static HttpResponse Send<T>([NotNull] T value)
        {
            return Status(HttpStatusCode.OK, value);
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
        public byte[] Data { get; }

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
    }
}
