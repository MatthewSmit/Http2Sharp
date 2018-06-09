using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Http2Sharp
{
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
            await EnsureBlockAsync().ConfigureAwait(false);
            if (blockSize - blockPointer >= length)
            {
                Array.Copy(block, blockPointer, data, offset, length);
                blockPointer += length;
            }
            else
            {
                throw new NotImplementedException();
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
                        if (blockPointer + 1 >= blockSize)
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            if (block[blockPointer + 1] != '\n')
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                blockPointer += 2;
                                return result.ToString();
                            }
                        }
                    }
                    else if (value >= 0x80)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        result.Append((char)value);
                    }
                }

                throw new NotImplementedException();
            }
        }

        private async Task EnsureBlockAsync()
        {
            if (blockPointer >= blockSize)
            {
                blockSize = await stream.ReadAsync(block, 0, BLOCK_SIZE).ConfigureAwait(false);
                blockPointer = 0;
            }
        }
    }
}