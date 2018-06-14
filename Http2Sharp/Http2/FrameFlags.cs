using System;

namespace Http2Sharp.Http2
{
    [Flags]
    internal enum FrameFlags : byte
    {
        None = 0,
        Acknowledge = 0x01,
        EndStream = 0x01,
        EndHeaders = 0x04,
        Padded = 0x08,
        Priority = 0x20,
    }
}