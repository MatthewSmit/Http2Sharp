using System;
using System.Collections.Generic;
using System.Globalization;
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

        private bool listening;
        private readonly IList<IHttpListener> listeners = new List<IHttpListener>();
        private Task[] listenerTasks;

        private readonly RouteManager routeManager;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private TaskFactory taskFactory;

        public HttpServer([NotNull] object baseRouter)
        {
            routeManager = new RouteManager(baseRouter);
        }

        public void Dispose()
        {
            cancellation.Cancel();
            foreach (var listener in listeners)
            {
                listener.Dispose();
            }
        }

        public void AddListener(IHttpListener listener)
        {
            if (listening)
            {
                throw new InvalidOperationException("Cannot modify listeners without ");
            }

            listeners.Add(listener);
        }

        public void StartListen()
        {
            taskFactory = new TaskFactory(cancellation.Token);
            logger.Info(CultureInfo.CurrentCulture, "Starting http server");

            if (listeners.Count == 0)
            {
                throw new InvalidOperationException("Cannot listen without any listeners");
            }

            listening = true;
            listenerTasks = new Task[listeners.Count];
            for (var i = 0; i < listeners.Count; i++)
            {
                var listener = listeners[i];
                listenerTasks[i] = listener.StartListenAsync(taskFactory, ProcessClientAsync);
            }
        }

        public void WaitForAll()
        {
            Task.WaitAll(listenerTasks);
        }

        private async Task ProcessClientAsync([NotNull] IHttpClient client)
        {
            using (client)
            {
                try
                {
                    await client.ReadHeadersAsync().ConfigureAwait(false);
                    logger.Info(CultureInfo.CurrentCulture, "Message from {0} for {1} {2}", client.RemoteEndPoint, client.Method, client.Target);

                    var target = new Uri(client.Target, UriKind.Relative).MakeAbsolute();
                    var (method, parameters) = routeManager.GetRoute(client.Method, target);
                    var queries = target.SplitQueries();
                    var response = method.Invoke(parameters, queries, await client.ReadBodyAsync().ConfigureAwait(false));
                    await client.SendResponseAsync(response).ConfigureAwait(false);
                }
                catch (HttpException e)
                {
                    await client.SendResponseAsync(HttpResponse.Status(e.StatusCode)).ConfigureAwait(false);
                }
                catch (TargetInvocationException e)
                {
                    logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when running route handler");
                    // TODO: Filter message when not in debug mode
                    await client.SendResponseAsync(HttpResponse.Status(500, e.ToString())).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when processing connection");
                    await client.SendResponseAsync(HttpResponse.Status(500, e.ToString())).ConfigureAwait(false);
                    throw;
                }
            }
        }
    }
}
