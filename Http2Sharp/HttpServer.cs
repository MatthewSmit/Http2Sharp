using System;
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
        private RouteManager routeManager;

        public HttpServer()
        {
            baseRouter = new T();
            routeManager = new RouteManager(baseRouter);
        }

        public void Dispose()
        {
            listener?.Stop();
        }

        public async Task StartListen()
        {
            var endPoint = new IPEndPoint(Address, Port);
            listener = new TcpListener(endPoint);
            listener.Start();
            logger.Info("Starting http server on " + endPoint);

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                logger.Info("Client connected from " + client.Client.RemoteEndPoint);

                var httpClient = new HttpClient(client);
                ProcessClient(httpClient);
            }
        }

        private async Task ProcessClient([NotNull] HttpClient httpClient)
        {
            try
            {
                await httpClient.ReadHeaders();
                logger.Info($"Message from {httpClient.Client.Client.RemoteEndPoint} for {httpClient.Method} {httpClient.Target}");

                var target = new HttpUri(httpClient.Target);
                var (method, parameters) = routeManager.GetRoute(httpClient.Method, target);
                var response = method.Invoke(parameters, target.Queries, await httpClient.ReadBody());
                await httpClient.SendResponse(response);
            }
            catch (HttpException e)
            {
                await httpClient.SendResponse(HttpResponse.Status(e.StatusCode));
            }
            catch (TargetInvocationException e)
            {
                logger.Error(e, "Uncaught exception when running route handler");
                // TODO: Filter message when not in debug mode
                await httpClient.SendResponse(HttpResponse.Status(500, e.ToString()));
            }
            catch (Exception e)
            {
                logger.Error(e, "Uncaught exception when processing connection");
                await httpClient.SendResponse(HttpResponse.Status(500, e.ToString()));
                throw;
            }
            finally
            {
                httpClient.Client.Dispose();
            }
        }

        public IPAddress Address { get; set; } = IPAddress.Loopback;
        public ushort Port { get; set; } = 80;
    }
}
