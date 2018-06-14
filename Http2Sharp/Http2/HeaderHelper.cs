using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace Http2Sharp.Http2
{
    internal struct HeaderHelper
    {
        private static readonly IReadOnlyDictionary<string, int> staticMapping = new Dictionary<string, int>
        {
            {"accept-charset", 16},
            {"accept-encoding", 16},
            {"accept-language", 17},
            {"accept-ranges", 18},
            {"accept", 19},
            {"access-control-allow-origin", 20},
            {"age", 21},
            {"allow", 22},
            {"authorization", 23},
            {"cache-control", 24},
            {"content-disposition", 25},
            {"content-encoding", 26},
            {"content-language", 27},
            {"content-length", 28},
            {"content-location", 29},
            {"content-range", 30},
            {"content-type", 31},
            {"cookie", 32},
            {"date", 33},
            {"etag", 34},
            {"expect", 35},
            {"expires", 36},
            {"from", 37},
            {"host", 38},
            {"if-match", 39},
            {"if-modified-since", 40},
            {"if-none-match", 41},
            {"if-range", 42},
            {"if-unmodified-since", 43},
            {"last-modified", 44},
            {"link", 45},
            {"location", 46},
            {"max-forwards", 47},
            {"proxy-authenticate", 48},
            {"proxy-authorization", 49},
            {"range", 50},
            {"referer", 51},
            {"refresh", 52},
            {"retry-after", 53},
            {"server", 54},
            {"set-cookie", 55},
            {"strict-transport-security", 56},
            {"transfer-encoding", 57},
            {"user-agent", 58},
            {"vary", 59},
            {"via", 60},
            {"www-authenticate", 61},
        };
        private static readonly IReadOnlyList<(string, string)> staticHeaderTable = new[]
        {
            (null, null),
            (":authority", string.Empty),
            (":method", "GET"),
            (":method", "POST"),
            (":path", "/"),
            (":path", "/index.html"),
            (":scheme", "http"),
            (":scheme", "https"),
            (":status", "200"),
            (":status", "204"),
            (":status", "206"),
            (":status", "304"),
            (":status", "400"),
            (":status", "404"),
            (":status", "500"),
            ("accept-charset", string.Empty),
            ("accept-encoding", "gzip, deflate"),
            ("accept-language", string.Empty),
            ("accept-ranges", string.Empty),
            ("accept", string.Empty),
            ("access-control-allow-origin", string.Empty),
            ("age", string.Empty),
            ("allow", string.Empty),
            ("authorization", string.Empty),
            ("cache-control", string.Empty),
            ("content-disposition", string.Empty),
            ("content-encoding", string.Empty),
            ("content-language", string.Empty),
            ("content-length", string.Empty),
            ("content-location", string.Empty),
            ("content-range", string.Empty),
            ("content-type", string.Empty),
            ("cookie", string.Empty),
            ("date", string.Empty),
            ("etag", string.Empty),
            ("expect", string.Empty),
            ("expires", string.Empty),
            ("from", string.Empty),
            ("host", string.Empty),
            ("if-match", string.Empty),
            ("if-modified-since", string.Empty),
            ("if-none-match", string.Empty),
            ("if-range", string.Empty),
            ("if-unmodified-since", string.Empty),
            ("last-modified", string.Empty),
            ("link", string.Empty),
            ("location", string.Empty),
            ("max-forwards", string.Empty),
            ("proxy-authenticate", string.Empty),
            ("proxy-authorization", string.Empty),
            ("range", string.Empty),
            ("referer", string.Empty),
            ("refresh", string.Empty),
            ("retry-after", string.Empty),
            ("server", string.Empty),
            ("set-cookie", string.Empty),
            ("strict-transport-security", string.Empty),
            ("transfer-encoding", string.Empty),
            ("user-agent", string.Empty),
            ("vary", string.Empty),
            ("via", string.Empty),
            ("www-authenticate", string.Empty),
        };

        private IList<(string, string)> dynamicTable;
        private IList<Frame> frames;
        private byte[] buffer;
        private int pointer;
        private int frameIdentifier;

        private readonly IReader reader;

        public HeaderHelper([NotNull] IReader reader)
            : this()
        {
            this.reader = reader;
        }

        public (string, string) ReadValue()
        {
            var header = reader.Peek();
            string headerName;
            string headerValue;

            if ((header & 0x80) != 0)
            {
                var index = ReadInteger(7);
                if (index == 0)
                {
                    throw new CompressionException();
                }

                (headerName, headerValue) = GetHeaderValue(index);
            }
            else if ((header & 0x40) != 0)
            {
                var index = ReadInteger(6);
                if (index == 0)
                {
                    headerName = ReadHeaderString();
                }
                else
                {
                    (headerName, _) = GetHeaderValue(index);
                }
                headerValue = ReadHeaderString();

                AddToTable(headerName, headerValue);
            }
            else if ((header & 0x20) != 0)
            {
                var maxSize = ReadInteger(5);
                throw new NotImplementedException();
            }
            else
            {
                var index = ReadInteger(4);
                if (index == 0)
                {
                    headerName = ReadHeaderString();
                }
                else
                {
                    (headerName, _) = GetHeaderValue(index);
                }
                headerValue = ReadHeaderString();
            }

            if (headerName.Any(char.IsUpper))
            {
                throw new CompressionException();
            }

            return (headerName, headerValue);
        }

        private void AddToTable([NotNull] string headerName, [NotNull] string headerValue)
        {
            if (dynamicTable == null)
            {
                dynamicTable = new List<(string, string)>();
            }

            dynamicTable.Add((headerName, headerValue));
            //TODO: Limit size of table
        }

        private (string headerName, string headerValue) GetHeaderValue(int index)
        {
            if (index >= staticHeaderTable.Count)
            {
                index -= staticHeaderTable.Count;
                return dynamicTable[dynamicTable.Count - index - 1];
            }

            return staticHeaderTable[index];
        }

        internal int ReadInteger(int prefixBits)
        {
            var mask = (1 << prefixBits) - 1;
            var result = reader.ReadByte() & mask;
            if (result != mask)
            {
                return result;
            }

            var m = 0;
            while (true)
            {
                var value = reader.ReadByte();
                result += (value & 0x7F) << m;
                m += 7;
                if ((value & 0x80) == 0)
                {
                    return result;
                }
            }
        }

        private string ReadHeaderString()
        {
            var huffmanEncoded = (reader.Peek() & 0x80) != 0;
            var stringLength = ReadInteger(7);

            var span = reader.ReadSpan(stringLength);

            if (huffmanEncoded)
            {
                return HuffmanDecoder.ReadString(span);
            }

            return Encoding.UTF8.GetString(span);
        }

        private IEnumerable<Frame> GenerateFramesImpl([NotNull] HttpResponse response, int frameIdentifier, int maxSize, bool lastFrame)
        {
            this.frameIdentifier = frameIdentifier;
            frames = new List<Frame>();
            buffer = new byte[1024];
            GenerateStatus(response.StatusCode);

            foreach (var (headerName, headerValue) in response.Headers)
            {
                if (staticMapping.TryGetValue(headerName, out var index))
                {
                    WriteHeader(index, headerValue);
                }
                else
                {
                    WriteHeader(headerName, headerValue);
                }
            }

            if (frames.Count == 0)
            {
                AddHeaderFrame(true, lastFrame);
            }
            else
            {
                throw new NotImplementedException();
            }

            return frames;
        }

        private void AddHeaderFrame(bool isLast, bool lastFrame)
        {
            var frame = new Frame
            {
                Length = pointer,
                Type = FrameType.Headers,
                Flags = (isLast ? FrameFlags.EndHeaders : FrameFlags.None) |
                        (lastFrame ? FrameFlags.EndStream : FrameFlags.None),
                Identifier = frameIdentifier,
                Data = buffer
            };
            frames.Add(frame);
        }

        private void GenerateStatus(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                    buffer[pointer++] = 0x80 | 8;
                    break;
                case HttpStatusCode.NoContent:
                    buffer[pointer++] = 0x80 | 9;
                    break;
                case HttpStatusCode.PartialContent:
                    buffer[pointer++] = 0x80 | 10;
                    break;
                case HttpStatusCode.NotModified:
                    buffer[pointer++] = 0x80 | 11;
                    break;
                case HttpStatusCode.BadRequest:
                    buffer[pointer++] = 0x80 | 12;
                    break;
                case HttpStatusCode.NotFound:
                    buffer[pointer++] = 0x80 | 13;
                    break;
                case HttpStatusCode.InternalServerError:
                    buffer[pointer++] = 0x80 | 14;
                    break;
                default:
                    WriteHeader(":status", ((int)statusCode).ToString(CultureInfo.InvariantCulture));
                    break;
            }
        }

        private void WriteHeader(int index, string value)
        {
            if (index == 0 || index > 0x3F)
            {
                throw new NotImplementedException();
            }

            buffer[pointer++] = (byte)(0x40 | index);

            var valueLength = Encoding.UTF8.GetByteCount(value);
            if (valueLength + 5 + pointer > buffer.Length)
            {
                throw new NotImplementedException();
            }

            WriteString(value);
        }

        private void WriteHeader([NotNull] string name, [NotNull] string value)
        {
            buffer[pointer++] = 0;
            var nameLength = Encoding.UTF8.GetByteCount(name);
            var valueLength = Encoding.UTF8.GetByteCount(value);
            if (nameLength + valueLength + 10 + pointer > buffer.Length)
            {
                throw new NotImplementedException();
            }

            WriteString(name);
            WriteString(value);
        }

        private void WriteString(string value)
        {
            // TODO: Huffman encoding
            var data = Encoding.UTF8.GetBytes(value);
            if (data.Length > 0x40)
            {
                throw new NotImplementedException();
            }

            buffer[pointer++] = (byte)data.Length;
            Array.Copy(data, 0, buffer, pointer, data.Length);
            pointer += data.Length;
        }

        [NotNull]
        public static IEnumerable<Frame> GenerateFrames([NotNull] HttpResponse response, int frameIdentifier, int maxSize, bool lastFrame)
        {
            return new HeaderHelper().GenerateFramesImpl(response, frameIdentifier, maxSize, lastFrame);
        }
    }
}