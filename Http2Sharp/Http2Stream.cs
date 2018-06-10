using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal sealed class Http2Stream : HttpStream
    {
        public static readonly IReadOnlyList<byte> Http2Header = new byte[]
        {
            0x50,
            0x52,
            0x49,
            0x20,
            0x2a,
            0x20,
            0x48,
            0x54,
            0x54,
            0x50,
            0x2f,
            0x32,
            0x2e,
            0x30,
            0x0d,
            0x0a,
            0x0d,
            0x0a,
            0x53,
            0x4d,
            0x0d,
            0x0a,
            0x0d,
            0x0a
        };

        public Http2Stream([NotNull] Stream stream)
            : base(stream)
        {
        }

        /// <inheritdoc />
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override void EnsureBlock()
        {
            throw new NotImplementedException();
        }

        /// <param name="cancellationToken"></param>
        /// <inheritdoc />
        protected override Task EnsureBlockAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => throw new NotImplementedException();

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => throw new NotImplementedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
