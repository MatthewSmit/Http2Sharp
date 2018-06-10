using System;
using System.IO;
using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class LimitedStream : Stream
    {
        [NotNull] private readonly Stream baseStream;
        private long position;

        public LimitedStream([NotNull] Stream baseStream, long length)
        {
            this.baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            Length = length;

            if (baseStream.CanSeek && baseStream.Length < baseStream.Position + length)
            {
                throw new ArgumentException("Must have enough data left to read");
            }
        }

        /// <inheritdoc />
        public override void Flush()
        {
            baseStream.Flush();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var amountToRead = (int)Math.Min(Length - position, count);
            var read = baseStream.Read(buffer, offset, amountToRead);
            position += read;
            return read;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public override bool CanRead => baseStream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length { get; }

        /// <inheritdoc />
        public override long Position
        {
            get => position;
            set => throw new InvalidOperationException();
        }
    }
}
