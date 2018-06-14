namespace Http2Sharp.Http2
{
    internal enum ErrorCode
    {
        /// <summary>
        /// The associated condition is not a result of an error.
        /// For example, a GOAWAY might include this code to indicate graceful shutdown of a connection.
        /// </summary>
        None = 0x00,
        /// <summary>
        /// The endpoint detected an unspecific protocol error.
        /// This error is for use when a more specific error code is not available.
        /// </summary>
        Protocol = 0x01,
        /// <summary>
        /// The endpoint encountered an unexpected internal error.
        /// </summary>
        Internal = 0x02,
        /// <summary>
        /// The endpoint detected that its peer violated the flow-control protocol.
        /// </summary>
        FlowControl = 0x03,
        /// <summary>
        /// The endpoint sent a SETTINGS frame but did not receive a response in a timely manner.
        /// </summary>
        SettingsTimeout = 0x04,
        /// <summary>
        /// The endpoint received a frame after a stream was half-closed.
        /// </summary>
        StreamClosed = 0x05,
        /// <summary>
        /// The endpoint received a frame with an invalid size.
        /// </summary>
        FrameSize = 0x06,
        /// <summary>
        /// The endpoint refused the stream prior to performing any application processing.
        /// </summary>
        RefusedStream = 0x07,
        /// <summary>
        /// Used by the endpoint to indicate that the stream is no longer needed.
        /// </summary>
        Cancel = 0x08,
        /// <summary>
        /// The endpoint is unable to maintain the header compression context for the connection.
        /// </summary>
        Compression = 0x09,
        /// <summary>
        /// The connection established in response to a CONNECT request was reset or abnormally closed.
        /// </summary>
        Connect = 0x0A,
        /// <summary>
        /// The endpoint detected that its peer is exhibiting a behavior that might be generating excessive load.
        /// </summary>
        EnhanceYourCalm = 0x0B,
        /// <summary>
        /// The underlying transport has properties that do not meet minimum security requirements.
        /// </summary>
        InadequateSecurity = 0x0C,
        /// <summary>
        /// The endpoint requires that HTTP/1.1 be used instead of HTTP/2.
        /// </summary>
        Http11Required = 0x0D,
    }
}