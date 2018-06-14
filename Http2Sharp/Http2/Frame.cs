using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Http2Sharp.Http2
{
    internal sealed class Frame : IReader
    {
        public int Length;
        public FrameType Type;
        public FrameFlags Flags;
        public int Identifier;
        [CanBeNull] public byte[] Data;
        public int Pointer { get; private set; }

        public Frame()
        {
        }

        public Frame(FrameType type, FrameFlags flags, int identifier)
        {
            Length = 0;
            Type = type;
            Flags = flags;
            Identifier = identifier;
            Data = default;
        }

        public Frame(int length, FrameType type, FrameFlags flags, int identifier)
        {
            Length = length;
            Type = type;
            Flags = flags;
            Identifier = identifier;
            Data = new byte[length];
        }

        public Frame(FrameType type, FrameFlags flags, int identifier, [NotNull] byte[] data)
        {
            Length = data.Length;
            Type = type;
            Flags = flags;
            Identifier = identifier;
            Data = data;
        }

        /// <inheritdoc />
        public byte Peek()
        {
            if (Pointer + 1 > Length)
            {
                throw new IndexOutOfRangeException();
            }

            Debug.Assert(Data != null, nameof(Data) + " != null");

            return Data[Pointer];
        }

        /// <inheritdoc />
        public byte ReadByte()
        {
            if (Pointer + 1 > Length)
            {
                throw new IndexOutOfRangeException();
            }

            Debug.Assert(Data != null, nameof(Data) + " != null");

            var value = Data[Pointer];
            Pointer += 1;
            return value;
        }

        public ushort ReadUInt16()
        {
            if (Pointer + 2 > Length)
            {
                throw new IndexOutOfRangeException();
            }

            Debug.Assert(Data != null, nameof(Data) + " != null");

            var value = (ushort)(Data[Pointer + 1] | (Data[Pointer] << 8));
            Pointer += 2;
            return value;
        }

        public uint ReadUInt32()
        {
            if (Pointer + 4 > Length)
            {
                throw new IndexOutOfRangeException();
            }

            Debug.Assert(Data != null, nameof(Data) + " != null");

            var value = Data[Pointer + 3] | ((uint)Data[Pointer + 2] << 8) | ((uint)Data[Pointer + 1] << 16) | ((uint)Data[Pointer] << 24);
            Pointer += 4;
            return value;
        }

        /// <inheritdoc />
        public ReadOnlySpan<byte> ReadSpan(int length)
        {
            if (Pointer + length > Length)
            {
                throw new IndexOutOfRangeException();
            }

            Debug.Assert(Data != null, nameof(Data) + " != null");

            var span = new ReadOnlySpan<byte>(Data, Pointer, length);
            Pointer += length;
            return span;
        }

        public void Write(uint value)
        {
            if (Pointer + 4 > Length)
            {
                throw new IndexOutOfRangeException();
            }

            Debug.Assert(Data != null, nameof(Data) + " != null");

            Data[Pointer + 0] = (byte)(value >> 24);
            Data[Pointer + 1] = (byte)(value >> 16);
            Data[Pointer + 2] = (byte)(value >> 8);
            Data[Pointer + 3] = (byte)value;
            Pointer += 4;
        }

        public void Write(int value)
        {
            Write((uint)value);
        }
    }
}