using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Http2Sharp.Http2;
using JetBrains.Annotations;
using NLog;

namespace Http2Sharp
{
    public class HttpListener : IHttpListener
    {
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IPEndPoint endPoint;
        private TcpListener listener;

        public HttpListener([NotNull] IServerConfiguration serverConfiguration, [NotNull] IPAddress address, int port)
        {
            ServerConfiguration = serverConfiguration;
            endPoint = new IPEndPoint(address, port);
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            listener?.Stop();
        }

        /// <inheritdoc />
        public Task StartListenAsync([NotNull] TaskFactory taskFactory, [NotNull] Func<HttpRequest, Task> processClient)
        {
            if (taskFactory == null)
            {
                throw new ArgumentNullException(nameof(taskFactory));
            }

            if (processClient == null)
            {
                throw new ArgumentNullException(nameof(processClient));
            }

            listener = new TcpListener(endPoint);
            listener.Start();
            Logger.Info(CultureInfo.CurrentCulture, "Listening at {0}", endPoint);

            return taskFactory.StartNew(() =>
            {
                while (true)
                {
                    TcpClient client = null;
                    try
                    {
                        client = listener.AcceptTcpClient();
                        Logger.Info(CultureInfo.CurrentCulture, "Client connected to {0} from {1}", endPoint, client.Client.RemoteEndPoint);
                    }
                    catch (SocketException e)
                    {
                        // This exception occurs when the socket is stopped from another thread.
                        if (e.SocketErrorCode == SocketError.Interrupted)
                        {
                            return;
                        }

                        Logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when processing connection");
                        client?.Dispose();
                        continue;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when processing connection");
                        client?.Dispose();
                        continue;
                    }

                    taskFactory.StartNew(async () =>
                    {
                        var httpStream = await CreateClientStreamAsync(client).ConfigureAwait(false);
                        if (httpStream.ReadIsHttp2())
                        {
                            using (var http2Manager = new Http2Manager(ServerConfiguration, client.Client.RemoteEndPoint.ToString(), taskFactory, httpStream, processClient))
                            {
                                while (http2Manager.Running)
                                {
                                    http2Manager.ProcessFrame();
                                }
                            }
                        }
                        else
                        {
                            using (httpStream)
                            {
                                var request = await HttpRequest.FromHttpClientAsync(Protocol, new HttpClient(ServerConfiguration, httpStream, client.Client.RemoteEndPoint)).ConfigureAwait(false);
                                await processClient(request).ConfigureAwait(false);
                            }
                        }
                    });
                }
            }, taskFactory.CancellationToken);
        }

        [ItemNotNull]
        protected async virtual Task<HttpStream> CreateClientStreamAsync([NotNull] TcpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            return new HttpStream(client.GetStream());
        }

        [NotNull]
        public IServerConfiguration ServerConfiguration { get; }

        [NotNull]
        public virtual string Protocol => "http";
    }
}
