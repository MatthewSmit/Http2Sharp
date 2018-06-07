using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Http2Sharp
{
    public sealed class HttpResponse
    {
        [NotNull] private readonly Dictionary<string, string> headers = new Dictionary<string, string>();

        private HttpResponse(int statusCode, [CanBeNull] string mimeType, [CanBeNull] byte[] data)
        {
            StatusCode = statusCode;

            if (mimeType != null && data == null ||
                mimeType == null && data != null)
            {
                throw new ArgumentException();
            }

            if (mimeType != null)
            {
                headers["Content-Type"] = mimeType;
            }

            if (data != null)
            {
                headers["Content-Length"] = data.Length.ToString();
            }
            Data = data;
        }

        [NotNull]
        public static HttpResponse Send<T>([NotNull] T value)
        {
            return Status(200, value);
        }

        [NotNull]
        public static HttpResponse Status(int statusCode)
        {
            return new HttpResponse(statusCode, null, null);
        }

        [NotNull]
        public static HttpResponse Status<T>(int statusCode, T value)
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

        public int StatusCode { get; }

        [NotNull]
        public string StatusCodeReason
        {
            get
            {
                //TODO
                switch (StatusCode)
                {
                    case 200:
                        return "OK";
                    default:
                        return "TODO";
                }
            }
        }
    }
}
