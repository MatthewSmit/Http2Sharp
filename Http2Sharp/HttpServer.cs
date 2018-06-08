using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NLog;

namespace Http2Sharp
{
    public sealed class HttpServer : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private TcpListener listener;
        private readonly RouteManager routeManager;
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        public HttpServer([NotNull] object baseRouter)
        {
            routeManager = new RouteManager(baseRouter);
        }

        public void Dispose()
        {
            cancellation.Cancel();
            listener?.Stop();
        }

        public Task StartListenAsync()
        {
            var taskFactory = new TaskFactory(cancellation.Token);
            var endPoint = new IPEndPoint(Address, Port);
            listener = new TcpListener(endPoint);
            listener.Start();
            logger.Info(CultureInfo.CurrentCulture, "Starting http server on {0}", endPoint);

            return taskFactory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var client = listener.AcceptTcpClient();
                        logger.Info(CultureInfo.CurrentCulture, "Client connected from {0}", client.Client.RemoteEndPoint);

                        ProcessClientAsync(client);
                    }
                    catch (SocketException e)
                    {
                        // This exception occurs when the socket is stopped from another thread.
                        if (e.SocketErrorCode == SocketError.Interrupted)
                        {
                            return;
                        }
                        throw;
                    }
                }
            }, cancellation.Token);
        }

        private async Task ProcessClientAsync([NotNull] TcpClient client)
        {
            using (var httpClient = new HttpClient(client))
            {
                try
                {
                    await httpClient.ReadHeadersAsync().ConfigureAwait(false);
                    logger.Info(CultureInfo.CurrentCulture, "Message from {0} for {1} {2}", httpClient.Client.Client.RemoteEndPoint, httpClient.Method, httpClient.Target);

                    var target = new Uri(httpClient.Target, UriKind.Relative).MakeAbsolute();
                    var (method, parameters) = routeManager.GetRoute(httpClient.Method, target);
                    var queries = target.SplitQueries();
                    var response = method.Invoke(parameters, queries, await httpClient.ReadBodyAsync().ConfigureAwait(false));
                    await httpClient.SendResponseAsync(response).ConfigureAwait(false);
                }
                catch (HttpException e)
                {
                    await httpClient.SendResponseAsync(HttpResponse.Status(e.StatusCode)).ConfigureAwait(false);
                }
                catch (TargetInvocationException e)
                {
                    logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when running route handler");
                    // TODO: Filter message when not in debug mode
                    await httpClient.SendResponseAsync(HttpResponse.Status(500, e.ToString())).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when processing connection");
                    await httpClient.SendResponseAsync(HttpResponse.Status(500, e.ToString())).ConfigureAwait(false);
                    throw;
                }
            }
        }

        public IPAddress Address { get; set; } = IPAddress.Loopback;
        public int Port { get; set; } = 80;
    }
}
