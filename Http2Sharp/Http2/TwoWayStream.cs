using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Http2Sharp.Http2
{
    internal sealed class TwoWayStream : Stream
    {
        private struct Message
        {
            public byte[] Data;
            public int Pointer;
            public int Length;
        }

        private const int BUFFER_SIZE = 4096;

        private readonly BufferBlock<Message> readBuffer = new BufferBlock<Message>();
        private Message currentMessage;

        /// <inheritdoc />
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (currentMessage.Length == currentMessage.Pointer)
            {
                try
                {
                    currentMessage = readBuffer.Receive();
                }
                catch (InvalidOperationException e)
                {
                    if (e.Source == "System.Threading.Tasks.Dataflow")
                    {
                        return 0;
                    }
                    throw;
                }
            }

            var toCopy = Math.Min(count, currentMessage.Length - currentMessage.Pointer);
            Array.Copy(currentMessage.Data, currentMessage.Pointer, buffer, offset, toCopy);
            currentMessage.Pointer += toCopy;
            return toCopy;
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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

        public void CopyRead(ReadOnlySpan<byte> data)
        {
            var length = data.Length;
            var start = 0;
            while (length > 0)
            {
                var toCopy = Math.Min(length, BUFFER_SIZE);
                var message = new Message
                {
                    // TODO: Cache this
                    Data = new byte[BUFFER_SIZE],
                    Length = toCopy,
                    Pointer = 0
                };
                data.Slice(start, toCopy).CopyTo(new Span<byte>(message.Data, 0, toCopy));
                start += toCopy;
                length -= toCopy;
                if (!readBuffer.Post(message))
                {
                    throw new NotImplementedException();
                }
            }
        }

        public void CloseRead()
        {
            readBuffer.Complete();
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => throw new InvalidOperationException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }
    }
}