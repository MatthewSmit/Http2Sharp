using System;
using System.Net;
using System.Runtime.Serialization;

namespace Http2Sharp
{
    [Serializable]
    public sealed class HttpException : Exception
    {
        public HttpException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpException(string message, HttpStatusCode statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        private HttpException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public HttpStatusCode StatusCode { get; }
    }
}
