using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Xunit;

namespace Http2Sharp.Test
{
    public sealed class HttpReaderTest
    {
        [Fact]
        public async Task TestSmallBytes()
        {
            var data = GenerateData(128);
            using (var stream = new MemoryStream(data))
            {
                var reader = new HttpReader(stream);
                var result = new byte[128];
                await reader.ReadAsync(result, 0, 128);
                Assert.True(VerifyData(result));
            }
        }

        [Fact]
        public async Task TestLargeBytes()
        {
            var data = GenerateData(2048);
            using (var stream = new MemoryStream(data))
            {
                var reader = new HttpReader(stream);
                var result = new byte[2048];
                await reader.ReadAsync(result, 0, 2048);
                Assert.True(VerifyData(result));
            }
        }

        [Fact]
        public async Task TestSmallText()
        {
            var data = GenerateLine(128);
            using (var stream = new MemoryStream(data))
            {
                var reader = new HttpReader(stream);
                var result = await reader.ReadLineAsync();
                Assert.Equal(126, result.Length);
                Assert.True(VerifyData(result));
            }
        }

        [Fact]
        public async Task TestLotsLines()
        {
            using (var stream = new MemoryStream())
            {
                for (var i = 0; i < 100; i++)
                {
                    var data = GenerateLine(128);
                    stream.Write(data, 0, data.Length);
                }

                stream.Position = 0;

                var reader = new HttpReader(stream);
                for (var i = 0; i < 100; i++)
                {
                    var result = await reader.ReadLineAsync();
                    Assert.Equal(126, result.Length);
                    Assert.True(VerifyData(result));
                }
            }
        }

        [Fact]
        public async Task TestLongLine()
        {
            var data = GenerateLine(2048);
            using (var stream = new MemoryStream(data))
            {
                var reader = new HttpReader(stream);
                var result = await reader.ReadLineAsync();
                Assert.Equal(2046, result.Length);
                Assert.True(VerifyData(result));
            }
        }

        [Fact]
        public async Task TestBreakOnBoundary()
        {
            var data = GenerateLine(1025);
            using (var stream = new MemoryStream(data))
            {
                var reader = new HttpReader(stream);
                var result = await reader.ReadLineAsync();
                Assert.Equal(1023, result.Length);
                Assert.True(VerifyData(result));
            }
        }

        [Fact]
        public async Task TestLargeMixedData()
        {
            using (var stream = new MemoryStream())
            {
                var data = GenerateData(128);
                stream.Write(data, 0, data.Length);

                data = GenerateLine(128);
                stream.Write(data, 0, data.Length);

                data = GenerateData(2048);
                stream.Write(data, 0, data.Length);

                data = GenerateLine(2048);
                stream.Write(data, 0, data.Length);

                stream.Position = 0;

                var reader = new HttpReader(stream);
                var resultData = new byte[128];
                await reader.ReadAsync(resultData, 0, 128);
                Assert.True(VerifyData(resultData));

                var resultText = await reader.ReadLineAsync();
                Assert.Equal(126, resultText.Length);
                Assert.True(VerifyData(resultText));

                resultData = new byte[2048];
                await reader.ReadAsync(resultData, 0, 2048);
                Assert.True(VerifyData(resultData));

                resultText = await reader.ReadLineAsync();
                Assert.Equal(2046, resultText.Length);
                Assert.True(VerifyData(resultText));
            }
        }

        [NotNull]
        private static byte[] GenerateData(int size)
        {
            var data = new byte[size];
            for (var i = 0; i < size; i++)
            {
                data[i] = (byte)(i & 0xFF);
            }
            return data;
        }

        [NotNull]
        private static byte[] GenerateLine(int size)
        {
            var line = new byte[size];
            for (var i = 0; i < size - 2; i++)
            {
                line[i] = (byte)('a' + i % 26);
            }

            line[size - 2] = (byte)'\r';
            line[size - 1] = (byte)'\n';
            return line;
        }

        private static bool VerifyData([NotNull] IReadOnlyList<byte> data)
        {
            for (var i = 0; i < data.Count; i++)
            {
                if (data[i] != (i & 0xFF))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool VerifyData([NotNull] string result)
        {
            for (var i = 0; i < result.Length; i++)
            {
                if (result[i] != 'a' + i % 26)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
