using System;

namespace Http2Sharp
{
    internal sealed class HttpException : Exception
    {
        public HttpException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; }
    }
}