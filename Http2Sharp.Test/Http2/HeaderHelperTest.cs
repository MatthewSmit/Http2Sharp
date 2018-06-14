using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using Xunit;

namespace Http2Sharp.Http2
{
    public sealed class HeaderHelperTest
    {
        private sealed class MemoryReader : IReader
        {
            [NotNull] private readonly byte[] data;
            private int pointer;

            public MemoryReader([NotNull] byte[] data)
            {
                this.data = data;
                pointer = 0;
            }

            /// <inheritdoc />
            public byte Peek()
            {
                return data[pointer];
            }

            /// <inheritdoc />
            public byte ReadByte()
            {
                return data[pointer++];
            }

            /// <inheritdoc />
            public ReadOnlySpan<byte> ReadSpan(int length)
            {
                return new ReadOnlySpan<byte>(data, pointer, length);
            }
        }

        [Fact]
        public void TestIntegerDecoding1()
        {
            var reader = new MemoryReader(new byte[]
            {
                0b0000_1010
            });
            var headerHelper = new HeaderHelper(reader);

            Assert.Equal(10, headerHelper.ReadInteger(5));
        }

        [Fact]
        public void TestIntegerDecoding2()
        {
            var reader = new MemoryReader(new byte[]
            {
                0b0001_1111,
                0b1001_1010,
                0b0000_1010,
            });
            var headerHelper = new HeaderHelper(reader);

            Assert.Equal(1337, headerHelper.ReadInteger(5));
        }

        [Fact]
        public void TestIntegerDecoding3()
        {
            var reader = new MemoryReader(new byte[]
            {
                0b0010_1010,
            });
            var headerHelper = new HeaderHelper(reader);

            Assert.Equal(42, headerHelper.ReadInteger(8));
        }

        [Fact]
        public void TestRoundTrip()
        {
            var response = HttpResponse.Send(42);
            var frames = HeaderHelper.GenerateFrames(response, 1, int.MaxValue, false);
            var frame = frames.Single();

            Assert.NotNull(frame.Data);
            var headerHelper = new HeaderHelper(new MemoryReader(frame.Data));
            var (headerName, headerValue) = headerHelper.ReadValue();
            Assert.Equal(":status", headerName);
            Assert.Equal("200", headerValue);
        }
    }
}
