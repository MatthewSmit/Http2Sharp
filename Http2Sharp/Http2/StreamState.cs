namespace Http2Sharp.Http2
{
    internal enum StreamState
    {
        Idle,
        HeadersExpectingContinuation,
        Open,
        HalfClosedRemote,
        Reset,
        Closed
    }
}
