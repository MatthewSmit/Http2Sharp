using System;

namespace Http2Sharp.Http2
{
    internal sealed class StreamException : Exception
    {
        public StreamException(int streamIdentifier, ErrorCode errorCode)
        {
            StreamIdentifier = streamIdentifier;
            ErrorCode = errorCode;
        }

        public int StreamIdentifier { get; }
        public ErrorCode ErrorCode { get; }
    }
}