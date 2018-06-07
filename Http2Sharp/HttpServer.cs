using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NLog;

namespace Http2Sharp
{
    public sealed class HttpServer<T> : IDisposable
        where T : class, new()
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private TcpListener listener;
        private readonly T baseRouter;
        private readonly RouteManager routeManager;

        public HttpServer()
        {
            baseRouter = new T();
            routeManager = new RouteManager(baseRouter);
        }

        public void Dispose()
        {
            listener?.Stop();
        }

        public async Task StartListenAsync()
        {
            var endPoint = new IPEndPoint(Address, Port);
            listener = new TcpListener(endPoint);
            listener.Start();
            logger.Info(CultureInfo.CurrentCulture, "Starting http server on {0}", endPoint);

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                logger.Info(CultureInfo.CurrentCulture, "Client connected from {0}", client.Client.RemoteEndPoint);

                ProcessClientAsync(client);
            }
        }

        private async Task ProcessClientAsync([NotNull] TcpClient client)
        {
            using (var httpClient = new HttpClient(client))
            {
                try
                {
                    await httpClient.ReadHeadersAsync().ConfigureAwait(false);
                    logger.Info(CultureInfo.CurrentCulture, "Message from {0} for {1} {2}", httpClient.Client.Client.RemoteEndPoint, httpClient.Method, httpClient.Target);

                    var target = new HttpUri(httpClient.Target);
                    var (method, parameters) = routeManager.GetRoute(httpClient.Method, target);
                    var response = method.Invoke(parameters, target.Queries, await httpClient.ReadBodyAsync().ConfigureAwait(false));
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
