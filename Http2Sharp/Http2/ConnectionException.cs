using System;

namespace Http2Sharp.Http2
{
    internal sealed class ConnectionException : Exception
    {
        public ConnectionException(int streamIdentifier, ErrorCode errorCode)
        {
            StreamIdentifier = streamIdentifier;
            ErrorCode = errorCode;
        }

        public ConnectionException(int streamIdentifier, ErrorCode errorCode, Exception innerException) :
            base("", innerException)
        {
            StreamIdentifier = streamIdentifier;
            ErrorCode = errorCode;
        }

        public int StreamIdentifier { get; }
        public ErrorCode ErrorCode { get; }
    }
}