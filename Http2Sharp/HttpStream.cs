using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Http2Sharp.Http2;
using JetBrains.Annotations;

namespace Http2Sharp
{
    public class HttpStream : Stream
    {
        public const int BLOCK_SIZE = 4096;

        [NotNull] private readonly Stream stream;

        private readonly byte[] block = new byte[BLOCK_SIZE];
        private int blockPointer;
        private int blockSize;

        public HttpStream([NotNull] Stream stream)
        {
            if (!stream.CanRead || !stream.CanWrite)
            {
                throw new ArgumentException();
            }

            this.stream = stream;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                stream.Dispose();
            }
        }

        /// <inheritdoc />
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = count;
            while (remaining > 0)
            {
                EnsureBlock();

                var dataAvaliable = blockSize - blockPointer;
                var toRead = Math.Min(dataAvaliable, remaining);
                Array.Copy(block, blockPointer, buffer, offset, toRead);
                blockPointer += toRead;
                offset += toRead;
                remaining -= toRead;
            }

            return count - remaining;
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var remaining = count;
            while (remaining > 0)
            {
                await EnsureBlockAsync(cancellationToken).ConfigureAwait(false);

                var dataAvaliable = blockSize - blockPointer;
                var toRead = Math.Min(dataAvaliable, remaining);
                Array.Copy(block, blockPointer, buffer, offset, toRead);
                blockPointer += toRead;
                offset += toRead;
                remaining -= toRead;
            }

            return count - remaining;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public bool ReadIsHttp2()
        {
            if (blockSize != 0 || blockPointer != 0)
            {
                throw new InvalidOperationException("Can only check for HTTP/2 as the first operation.");
            }

            var http2Header = Http2Manager.Http2Header;

            EnsureBlock();
            if (blockSize < http2Header.Count)
            {
                return false;
            }

            for (var i = 0; i < http2Header.Count; i++)
            {
                if (block[i] != http2Header[i])
                {
                    return false;
                }
            }

            blockPointer += http2Header.Count;
            return true;
        }

        public virtual string ReadLine()
        {
            var result = new StringBuilder();
            while (true)
            {
                EnsureBlock();

                for (; blockPointer < blockSize; blockPointer++)
                {
                    var value = block[blockPointer];
                    if (value == '\r')
                    {
                        blockPointer += 1;
                        if (blockPointer >= blockSize)
                        {
                            EnsureBlock();
                        }

                        if (block[blockPointer] != '\n')
                        {
                            throw new HttpException("Missing a \\r\\n in the http line", HttpStatusCode.BadRequest);
                        }

                        blockPointer += 1;
                        return result.ToString();
                    }

                    if (value >= 0x80)
                    {
                        throw new HttpException("Invalid HTTP data.", HttpStatusCode.BadRequest);
                    }

                    result.Append((char)value);
                }
            }
        }

        public virtual async Task<string> ReadLineAsync()
        {
            var result = new StringBuilder();
            while (true)
            {
                await EnsureBlockAsync(CancellationToken.None).ConfigureAwait(false);

                for (; blockPointer < blockSize; blockPointer++)
                {
                    var value = block[blockPointer];
                    if (value == '\r')
                    {
                        blockPointer += 1;
                        if (blockPointer >= blockSize)
                        {
                            await EnsureBlockAsync(CancellationToken.None).ConfigureAwait(false);
                        }

                        if (block[blockPointer] != '\n')
                        {
                            throw new HttpException("Missing a \\r\\n in the http line", HttpStatusCode.BadRequest);
                        }

                        blockPointer += 1;
                        return result.ToString();
                    }

                    if (value >= 0x80)
                    {
                        throw new HttpException("Invalid HTTP data.", HttpStatusCode.BadRequest);
                    }

                    result.Append((char)value);
                }
            }
        }

        private void EnsureBlock()
        {
            if (blockPointer >= blockSize)
            {
                blockSize = stream.Read(block, 0, BLOCK_SIZE);
                if (blockSize == 0)
                {
                    throw new IOException("No data avaliable to read.");
                }
                blockPointer = 0;
            }
        }

        private async Task EnsureBlockAsync(CancellationToken cancellationToken)
        {
            if (blockPointer >= blockSize)
            {
                blockSize = await stream.ReadAsync(block, 0, BLOCK_SIZE, cancellationToken).ConfigureAwait(false);
                if (blockSize == 0)
                {
                    throw new IOException("No data avaliable to read.");
                }
                blockPointer = 0;
            }
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

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
