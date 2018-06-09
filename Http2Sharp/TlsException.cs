using System;

namespace Http2Sharp
{
    public sealed class TlsException : Exception
    {
        public TlsException()
        {
        }

        public TlsException(string message)
            : base(message)
        {
        }

        public TlsException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}