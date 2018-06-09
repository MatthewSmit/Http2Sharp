using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Http2Sharp
{
    /// <summary>
    /// Handles reading both line information and raw bytes from a stream.
    /// </summary>
    internal sealed class HttpReader
    {
        private const int BLOCK_SIZE = 1024;

        private readonly Stream stream;

        private readonly byte[] block = new byte[BLOCK_SIZE];
        private int blockPointer;
        private int blockSize;

        public HttpReader(Stream stream)
        {
            this.stream = stream;
        }

        public async Task ReadAsync(byte[] data, int offset, int length)
        {
            var remaining = length;
            while (remaining > 0)
            {
                await EnsureBlockAsync().ConfigureAwait(false);

                var dataAvaliable = blockSize - blockPointer;
                var toRead = Math.Min(dataAvaliable, remaining);
                Array.Copy(block, blockPointer, data, offset, toRead);
                blockPointer += toRead;
                offset += toRead;
                remaining -= toRead;
            }
        }

        public async Task<string> ReadLineAsync()
        {
            var result = new StringBuilder();
            while (true)
            {
                await EnsureBlockAsync().ConfigureAwait(false);

                for (; blockPointer < blockSize; blockPointer++)
                {
                    var value = block[blockPointer];
                    if (value == '\r')
                    {
                        blockPointer += 1;
                        if (blockPointer >= blockSize)
                        {
                            await EnsureBlockAsync().ConfigureAwait(false);
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
                        throw new IOException("Invalid HTTP data.");
                    }

                    result.Append((char)value);
                }
            }
        }

        private async Task EnsureBlockAsync()
        {
            if (blockPointer >= blockSize)
            {
                blockSize = await stream.ReadAsync(block, 0, BLOCK_SIZE).ConfigureAwait(false);
                if (blockSize == 0)
                {
                    throw new IOException("No data avaliable to read.");
                }
                blockPointer = 0;
            }
        }
    }
}