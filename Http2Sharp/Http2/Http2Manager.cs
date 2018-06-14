using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using JetBrains.Annotations;
using NLog;

namespace Http2Sharp.Http2
{
    internal sealed class Http2Manager : IDisposable
    {
        private enum SettingsParameter
        {
            /// <summary>
            /// Allows the sender to inform the remote endpoint of the maximum size of the header compression table used to decode header blocks, in octets.
            /// The encoder can select any size equal to or less than this value by using signaling specific to the header compression format inside a header block.
            /// The initial value is 4,096 octets.
            /// </summary>
            HeaderTableSize = 0x0001,
            /// <summary>
            /// This setting can be used to disable server push.
            /// An endpoint MUST NOT send a PUSH_PROMISE frame if it receives this parameter set to a value of 0.
            /// An endpoint that has both set this parameter to 0 and had it acknowledged MUST treat the receipt of a PUSH_PROMISE frame as a connection error of type PROTOCOL_ERROR.
            /// The initial value is 1, which indicates that server push is permitted. Any value other than 0 or 1 MUST be treated as a connection error of type PROTOCOL_ERROR.
            /// </summary>
            EnablePush = 0x0002,
            /// <summary>
            /// Indicates the maximum number of concurrent streams that the sender will allow.
            /// This limit is directional: it applies to the number of streams that the sender permits the receiver to create.
            /// Initially, there is no limit to this value. It is recommended that this value be no smaller than 100, so as to not unnecessarily limit parallelism.
            /// A value of 0 for SETTINGS_MAX_CONCURRENT_STREAMS SHOULD NOT be treated as special by endpoints.
            /// A zero value does prevent the creation of new streams; however, this can also happen for any limit that is exhausted with active streams.
            /// Servers SHOULD only set a zero value for short durations; if a server does not wish to accept requests, closing the connection is more appropriate.
            /// </summary>
            MaximumConcurrentStreams = 0x0003,
            /// <summary>
            /// Indicates the sender's initial window size (in octets) for stream-level flow control. The initial value is 216-1 (65,535) octets.
            /// This setting affects the window size of all streams.
            /// Values above the maximum flow-control window size of 2^31-1 MUST be treated as a connection error of type FLOW_CONTROL_ERROR.
            /// </summary>
            InitialWindowSize = 0x0004,
            /// <summary>
            /// Indicates the size of the largest frame payload that the sender is willing to receive, in octets.
            /// The initial value is 214 (16,384) octets.
            /// The value advertised by an endpoint MUST be between this initial value and the maximum allowed frame size (224-1 or 16,777,215 octets), inclusive.
            /// Values outside this range MUST be treated as a connection error of type PROTOCOL_ERROR.
            /// </summary>
            MaximumFrameSize = 0x0005,
            /// <summary>
            /// This advisory setting informs a peer of the maximum size of header list that the sender is prepared to accept, in octets.
            /// The value is based on the uncompressed size of header fields, including the length of the name and value in octets plus an overhead of 32 octets for each header field.
            /// For any given request, a lower limit than what is advertised MAY be enforced. The initial value of this setting is unlimited.
            /// </summary>
            MaximumHeaderListSize = 0x0006,
        }

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [NotNull] public static readonly IReadOnlyList<byte> Http2Header = new byte[]
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

        [NotNull] private readonly Func<HttpRequest, Task> processClient;
        [NotNull] private readonly string remoteEndPoint;
        [NotNull] private readonly TaskFactory taskFactory;
        [NotNull] private readonly Task outgoingTask;
        [NotNull] private readonly BufferBlock<Frame> outgoingFrames = new BufferBlock<Frame>();
        [NotNull] private readonly Stream baseStream;
        private bool readSettings;

        private uint headerTableSize = 4096;
        private bool enablePush = true;
        private uint maximumConcurrentStreams = uint.MaxValue;
        private uint initialWindowSize = 0xFFFF;
        private uint maximumHeaderListSize = uint.MaxValue;
        private int lastSuccessfulId;

        private readonly Dictionary<int, Http2Stream> streams = new Dictionary<int, Http2Stream>();

        public Http2Manager(IServerConfiguration serverConfiguration,
            [NotNull] string remoteEndPoint,
            [NotNull] TaskFactory taskFactory,
            [NotNull] Stream baseStream,
            [NotNull] Func<HttpRequest, Task> processClient)
        {
            ServerConfiguration = serverConfiguration;
            this.remoteEndPoint = remoteEndPoint;
            this.taskFactory = taskFactory;
            this.baseStream = baseStream;
            this.processClient = processClient;

            outgoingTask = taskFactory.StartNew(OutgoingHandlerAsync);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            outgoingFrames.Complete();
            outgoingTask.Wait();
            baseStream.Dispose();
        }

        private async Task OutgoingHandlerAsync()
        {
            while (await outgoingFrames.OutputAvailableAsync().ConfigureAwait(false))
            {
                var frame = outgoingFrames.Receive();
                SendFrameInternal(frame);
            }
        }

        public void ProcessFrame()
        {
            try
            {
                var frame = ReadFrame();

                if (readSettings)
                {
                    switch (frame.Type)
                    {
                        case FrameType.Data:
                            ReadDataFrame(frame);
                            break;

                        case FrameType.Headers:
                            ReadHeadersFrame(frame);
                            break;

                        case FrameType.Priority:
                            ReadPriorityFrame(frame);
                            break;

                        case FrameType.ResetStream:
                            ReadResetStreamFrame(frame);
                            break;

                        case FrameType.Settings:
                            ReadSettingsFrame(frame, false);
                            break;

                        case FrameType.PushPromise:
                            ReadPushPromiseFrame(frame);
                            break;

                        case FrameType.Ping:
                            ReadPingFrame(frame);
                            break;

                        case FrameType.GoAway:
                            ReadGoAwayFrame(frame);
                            break;

                        case FrameType.WindowUpdate:
                            ReadWindowUpdateFrame(frame);
                            break;

                        case FrameType.Continuation:
                            ReadContinuationFrame(frame);
                            break;

                        default:
                            // Ignore and discard
                            break;
                    }
                }
                else if (frame.Type == FrameType.Settings)
                {
                    ReadSettingsFrame(frame, true);
                    SendSettingsFrame();
                    SendSettingsAcknowledge();
                    readSettings = true;
                }
                else
                {
                    throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
                }

                if (frame.Identifier != 0)
                {
                    lastSuccessfulId = frame.Identifier;
                }
            }
            catch (StreamException e)
            {
                Debug.Assert(e.StreamIdentifier != 0);
                var frame = new Frame(4, FrameType.ResetStream, 0, e.StreamIdentifier);
                frame.Write((uint)e.ErrorCode);
                SendFrame(frame);

                var stream = GetStream(e.StreamIdentifier);
                stream.State = StreamState.Reset;
            }
            catch (ConnectionException e)
            {
                var frame = new Frame(8, FrameType.GoAway, 0, 0);
                frame.Write(e.StreamIdentifier);
                frame.Write((uint)e.ErrorCode);
                SendFrame(frame);
                Running = false;
            }
        }

        [NotNull]
        private Frame ReadFrame()
        {
            var headerData = new byte[9];
            if (baseStream.Read(headerData, 0, headerData.Length) != headerData.Length)
            {
                throw new IOException();
            }

            var frame = new Frame
            {
                Length = headerData[2] | (headerData[1] << 8) | (headerData[0] << 16),
                Type = (FrameType)headerData[3],
                Flags = (FrameFlags)headerData[4],
                Identifier = headerData[8] | (headerData[7] << 8) | (headerData[6] << 16) | ((headerData[5] & 0x7F) << 24)
            };

            logger.Log(LogLevel.Debug, CultureInfo.InvariantCulture, "Recieved frame. Type: {0}, Flags: {1}, Identifier: {2}, Length: 0x{3:X}",
                frame.Type,
                frame.Flags,
                frame.Identifier,
                frame.Length);

            if (frame.Length > MaximumFrameSize)
            {
                var isFatal = frame.Identifier == 0 ||
                              frame.Type == FrameType.Headers ||
                              frame.Type == FrameType.Continuation ||
                              frame.Type == FrameType.PushPromise ||
                              frame.Type == FrameType.Settings;
                throw isFatal ?
                    (Exception)new ConnectionException(frame.Identifier, ErrorCode.FrameSize) :
                    new StreamException(frame.Identifier, ErrorCode.FrameSize);
            }

            frame.Data = new byte[frame.Length];

            if (baseStream.Read(frame.Data, 0, frame.Length) != frame.Length)
            {
                throw new IOException();
            }

            return frame;
        }

        private void ReadDataFrame([NotNull] Frame frame)
        {
            if (frame.Identifier == 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            var stream = TryGetStream(frame.Identifier);

            if (stream == null)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            switch (stream.State)
            {
                case StreamState.Idle:
                case StreamState.HeadersExpectingContinuation:
                case StreamState.HalfClosedRemote:
                    throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
                case StreamState.Open:
                    break;
                case StreamState.Reset:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var endStream = frame.Flags.HasFlag(FrameFlags.EndStream);
            var padded = frame.Flags.HasFlag(FrameFlags.Padded);

            byte paddingLength = 0;

            if (paddingLength > frame.Length)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            if (endStream)
            {
                stream.State = StreamState.HalfClosedRemote;
            }

            if (padded)
            {
                paddingLength = frame.ReadByte();
            }

            stream.TwoWayStream.CopyRead(frame.ReadSpan(frame.Length - paddingLength - frame.Pointer));

            if (endStream)
            {
                stream.TwoWayStream.CloseRead();
            }
        }

        private void ReadHeadersFrame([NotNull] Frame frame)
        {
            if (frame.Identifier == 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            var endStream = frame.Flags.HasFlag(FrameFlags.EndStream);
            var endHeaders = frame.Flags.HasFlag(FrameFlags.EndHeaders);
            var padded = frame.Flags.HasFlag(FrameFlags.Padded);
            var priority = frame.Flags.HasFlag(FrameFlags.Priority);

            var stream = GetStream(frame.Identifier);

            switch (stream.State)
            {
                case StreamState.Idle:
                    break;
                case StreamState.HeadersExpectingContinuation:
                    throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
                case StreamState.Open:
                    if (!endStream)
                    {
                        throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
                    }

                    if (!endHeaders)
                    {
                        throw new NotImplementedException();
                    }
                    stream.State = StreamState.HalfClosedRemote;
                    stream.TwoWayStream.CloseRead();
                    // TODO: Read chunked headers
                    return;
                case StreamState.HalfClosedRemote:
                case StreamState.Reset:
                case StreamState.Closed:
                    throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            byte paddingLength = 0;
            StreamState nextState;

            if (endStream)
            {
                nextState = StreamState.HalfClosedRemote;
            }
            else if (endHeaders)
            {
                nextState = StreamState.Open;
            }
            else
            {
                nextState = StreamState.HeadersExpectingContinuation;
            }

            if (padded)
            {
                paddingLength = frame.ReadByte();
            }

            if (priority)
            {
                var streamDependancy = frame.ReadUInt32();
                var exclusive = streamDependancy & 0x80000000;
                streamDependancy &= 0x7FFFFFFF;
                var weight = frame.ReadByte();

                // TODO
            }

            if (paddingLength + frame.Pointer > frame.Length)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            try
            {
                var headerHelper = new HeaderHelper(frame);
                var readAllStatus = false;
                while (frame.Pointer < frame.Length - paddingLength)
                {
                    var (headerName, headerValue) = headerHelper.ReadValue();

                    switch (headerName)
                    {
                        case ":authority":
                            if (readAllStatus)
                            {
                                throw new StreamException(frame.Identifier, ErrorCode.Protocol);
                            }
                            stream.Authority = headerValue;
                            break;

                        case ":method":
                            if (readAllStatus)
                            {
                                throw new StreamException(frame.Identifier, ErrorCode.Protocol);
                            }
                            stream.Method = headerValue;
                            break;

                        case ":path":
                            if (readAllStatus)
                            {
                                throw new StreamException(frame.Identifier, ErrorCode.Protocol);
                            }
                            stream.Path = headerValue;
                            break;

                        case ":scheme":
                            if (readAllStatus)
                            {
                                throw new StreamException(frame.Identifier, ErrorCode.Protocol);
                            }
                            stream.Scheme = headerValue;
                            break;

                        case ":status":
                            throw new StreamException(frame.Identifier, ErrorCode.Protocol);

                        default:
                            if (headerName.StartsWith(":", StringComparison.InvariantCulture))
                            {
                                throw new StreamException(frame.Identifier, ErrorCode.Protocol);
                            }
                            readAllStatus = true;
                            stream.Headers.Add((headerName, headerValue));
                            break;
                    }
                }
            }
            catch (CompressionException e)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Compression, e);
            }

            stream.State = nextState;

            if (endHeaders)
            {
                taskFactory.StartNew(() => stream.SendHeaders(processClient));
            }
        }

        private void ReadPriorityFrame([NotNull] Frame frame)
        {
            if (frame.Identifier == 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            if (frame.Length != 5)
            {
                throw new StreamException(frame.Identifier, ErrorCode.FrameSize);
            }

            var stream = GetStream(frame.Identifier);
            if (stream.State == StreamState.HeadersExpectingContinuation)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            var streamDependancy = frame.ReadUInt32();
            var exclusive = streamDependancy & 0x80000000;
            streamDependancy &= 0x7FFFFFFF;
            var weight = frame.ReadByte();

            // TODO: Priority
        }

        private void ReadResetStreamFrame([NotNull] Frame frame)
        {
            if (frame.Identifier == 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            if (frame.Length != 4)
            {
                throw new StreamException(frame.Identifier, ErrorCode.FrameSize);
            }

            var stream = TryGetStream(frame.Identifier);
            if (stream == null || stream.State == StreamState.Idle)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            var errorCode = (ErrorCode)frame.ReadUInt32();
            stream.State = StreamState.Closed;
            logger.Log(LogLevel.Info, CultureInfo.InvariantCulture, "Recieved reset stream for {0} because {1}", frame.Identifier, errorCode);
        }

        private void ReadSettingsFrame([NotNull] Frame frame, bool forceNoAcknowledge)
        {
            if (frame.Identifier != 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            var acknowledge = frame.Flags.HasFlag(FrameFlags.Acknowledge);

            if (acknowledge && frame.Length != 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.FrameSize);
            }

            if (frame.Length % 6 != 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.FrameSize);
            }

            if (!acknowledge)
            {
                for (var i = 0; i < frame.Length; i += 6)
                {
                    var identifier = (SettingsParameter)frame.ReadUInt16();
                    var value = frame.ReadUInt32();

                    switch (identifier)
                    {
                        case SettingsParameter.HeaderTableSize:
                            headerTableSize = value;
                            break;
                        case SettingsParameter.EnablePush:
                            if (value >= 2)
                            {
                                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
                            }

                            enablePush = value != 0;
                            break;
                        case SettingsParameter.MaximumConcurrentStreams:
                            maximumConcurrentStreams = value;
                            break;
                        case SettingsParameter.InitialWindowSize:
                            if (value > int.MaxValue)
                            {
                                throw new ConnectionException(frame.Identifier, ErrorCode.FlowControl);
                            }
                            initialWindowSize = value;
                            break;
                        case SettingsParameter.MaximumFrameSize:
                            if (value < 0x4000 || value > 0x00FFFFFF)
                            {
                                throw new ConnectionException(frame.Identifier, ErrorCode.FlowControl);
                            }
                            MaximumFrameSize = (int)value;
                            break;
                        case SettingsParameter.MaximumHeaderListSize:
                            maximumHeaderListSize = value;
                            break;
                    }
                }
            }

            if (!acknowledge && !forceNoAcknowledge)
            {
                SendSettingsAcknowledge();
            }
        }

        private void ReadPushPromiseFrame([NotNull] Frame frame)
        {
            throw new NotImplementedException();
        }

        private void ReadPingFrame([NotNull] Frame frame)
        {
            if (frame.Identifier != 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            if (frame.Length != 8)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.FrameSize);
            }
            
            var acknowledge = frame.Flags.HasFlag(FrameFlags.Acknowledge);
            if (!acknowledge)
            {
                SendPingAcknowledge(frame);
            }
        }

        private void ReadGoAwayFrame([NotNull] Frame frame)
        {
            if (frame.Identifier != 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            if (frame.Length < 8)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.FrameSize);
            }

            frame.ReadUInt32(); // Last frame identifier
            var errorCode = (ErrorCode)frame.ReadUInt32();
            logger.Log(LogLevel.Info, CultureInfo.InvariantCulture, "Recieved go away because {0}", errorCode);
            Running = false;
        }

        private void ReadWindowUpdateFrame([NotNull] Frame frame)
        {
            if (frame.Length != 4)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            var increment = frame.ReadUInt32() & 0x7FFFFFFF;

            if (frame.Identifier == 0)
            {
                // TODO
            }
            else
            {
                var stream = TryGetStream(frame.Identifier);
                if (stream == null || stream.State == StreamState.Idle)
                {
                    throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
                }
            }
        }

        private void ReadContinuationFrame([NotNull] Frame frame)
        {
            if (frame.Identifier == 0)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            var stream = TryGetStream(frame.Identifier);
            if (stream == null || stream.State != StreamState.HeadersExpectingContinuation)
            {
                throw new ConnectionException(frame.Identifier, ErrorCode.Protocol);
            }

            throw new NotImplementedException();
        }

        private void SendSettingsFrame()
        {
            SendFrame(new Frame(FrameType.Settings, 0, 0));
        }

        private void SendSettingsAcknowledge()
        {
            SendFrame(new Frame(FrameType.Settings, FrameFlags.Acknowledge, 0));
        }

        private void SendPingAcknowledge([NotNull] Frame pingFrame)
        {
            Debug.Assert(pingFrame.Data != null, "pingFrame.Data != null");
            SendFrame(new Frame(FrameType.Ping, FrameFlags.Acknowledge, 0, pingFrame.Data));
        }

        public void SendFrame([NotNull] Frame frame)
        {
            logger.Log(LogLevel.Debug, CultureInfo.InvariantCulture, "Sending frame. Type: {0}, Flags: {1}, Identifier: {2}, Length: 0x{3:X}",
                frame.Type,
                frame.Flags,
                frame.Identifier,
                frame.Length);

            if (!outgoingFrames.Post(frame))
            {
                throw new NotImplementedException();
            }
        }

        private void SendFrameInternal([NotNull] Frame frame)
        {
            var headerData = WriteHeader(frame.Length, frame.Type, frame.Flags, frame.Identifier);
            baseStream.Write(headerData, 0, headerData.Length);
            if (frame.Length > 0)
            {
                baseStream.Write(frame.Data, 0, frame.Length);
            }
        }

        [NotNull]
        private Http2Stream GetStream(int frameIdentifier)
        {
            if (streams.TryGetValue(frameIdentifier, out var value))
            {
                return value;
            }

            value = new Http2Stream(this, remoteEndPoint, frameIdentifier);
            streams.Add(frameIdentifier, value);
            return value;
        }

        [CanBeNull]
        private Http2Stream TryGetStream(int frameIdentifier)
        {
            streams.TryGetValue(frameIdentifier, out var value);
            return value;
        }

        private static byte[] WriteHeader(int length, FrameType frameType, FrameFlags flags, int identifier)
        {
            var headerData = new byte[9];

            if (length > 0x00FFFFFF)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (identifier < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            headerData[0] = (byte)(length >> 16);
            headerData[1] = (byte)(length >> 8);
            headerData[2] = (byte)length;
            headerData[3] = (byte)frameType;
            headerData[4] = (byte)flags;
            headerData[5] = (byte)(identifier >> 24);
            headerData[6] = (byte)(identifier >> 16);
            headerData[7] = (byte)(identifier >> 8);
            headerData[8] = (byte)identifier;

            return headerData;
        }

        public int MaximumFrameSize { get; private set; } = 0x4000;
        public IServerConfiguration ServerConfiguration { get; }
        public bool Running { get; set; } = true;
    }
}
