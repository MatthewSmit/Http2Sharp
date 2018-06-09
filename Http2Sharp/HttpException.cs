using System;
using System.Net;

namespace Http2Sharp
{
    public sealed class HttpException : Exception
    {
        public HttpException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}