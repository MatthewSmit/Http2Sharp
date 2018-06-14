using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Http2Sharp.Http2
{
    internal sealed class Http2Stream : IHttpClient
    {
        private Task task;
        private readonly Http2Manager manager;
        private readonly string remoteEndPoint;
        private readonly int frameIdentifier;

        public Http2Stream(Http2Manager manager, string remoteEndPoint, int frameIdentifier)
        {
            this.manager = manager;
            this.remoteEndPoint = remoteEndPoint;
            this.frameIdentifier = frameIdentifier;
        }

        public void SendHeaders([NotNull] Func<HttpRequest, Task> processClient)
        {
            var request = new HttpRequest(this, Scheme, HttpRequest.ParseMethod(Method), Path, "", Headers);
            task = processClient(request);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            TwoWayStream.Dispose();
            task.Wait();
        }

        /// <inheritdoc />
        public async Task SendResponseAsync(HttpResponse response)
        {
            manager.ServerConfiguration.AddServerHeaders(response);
            var frames = HeaderHelper.GenerateFrames(response, frameIdentifier, manager.MaximumFrameSize, response.DataStream == null);
            foreach (var frame in frames)
            {
                manager.SendFrame(frame);
            }

            frames = GenerateDataFrames(response);
            foreach (var frame in frames)
            {
                manager.SendFrame(frame);
            }
        }

        [NotNull]
        private IEnumerable<Frame> GenerateDataFrames([NotNull] HttpResponse response)
        {
            if (response.DataStream == null)
            {
                return Array.Empty<Frame>();
            }

            if (response.DataStream.CanSeek)
            {
                // TODO: Maximum frame size
                var frames = new Frame[1];
                var data = new byte[response.DataStream.Length];
                if (response.DataStream.Read(data, 0, data.Length) != data.Length)
                {
                    throw new NotImplementedException();
                }
                frames[0] = new Frame(FrameType.Data, FrameFlags.EndStream, frameIdentifier, data);
                return frames;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public StreamState State { get; set; } = StreamState.Idle;

        public string Authority { get; set; }
        public string Method { get; set; }
        public string Scheme { get; set; }
        public string Path { get; set; }
        public List<(string, string)> Headers { get; } = new List<(string, string)>();

        /// <inheritdoc />
        public string RemoteEndPoint => remoteEndPoint + "/" + frameIdentifier;

        /// <inheritdoc />
        public Stream Stream => TwoWayStream;
        /// <inheritdoc />
        public HttpStream HttpStream => throw new NotImplementedException();
        public TwoWayStream TwoWayStream { get; } = new TwoWayStream();
    }
}