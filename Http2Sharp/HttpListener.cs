using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
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
        public Task StartListenAsync([NotNull] TaskFactory taskFactory, [NotNull] Func<IHttpClient, Task> processClient)
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

                        processClient(CreateClient(client));
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
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when processing connection");
                        client?.Dispose();
                    }
                }
            }, taskFactory.CancellationToken);
        }

        [NotNull]
        protected virtual IHttpClient CreateClient([NotNull] TcpClient client)
        {
            return new HttpClient(ServerConfiguration, client);
        }

        [NotNull]
        public IServerConfiguration ServerConfiguration { get; }
    }
}
