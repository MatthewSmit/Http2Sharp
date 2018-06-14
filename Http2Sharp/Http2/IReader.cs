using System;

namespace Http2Sharp.Http2
{
    internal interface IReader
    {
        byte Peek();
        byte ReadByte();
        ReadOnlySpan<byte> ReadSpan(int length);
    }
}
