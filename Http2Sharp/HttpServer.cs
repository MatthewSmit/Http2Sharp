using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NLog;

namespace Http2Sharp
{
    public sealed class HttpServer : IDisposable, IServerConfiguration
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private bool listening;
        private readonly IList<IHttpListener> listeners = new List<IHttpListener>();
        private readonly IList<IHttpHandler> handlers = new List<IHttpHandler>();
        private Task[] listenerTasks;

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private TaskFactory taskFactory;

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

        public void AddHandler(IHttpHandler handler)
        {
            handlers.Add(handler);
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

        private async Task ProcessClientAsync([NotNull] HttpRequest request)
        {
            try
            {
                logger.Info(CultureInfo.CurrentCulture, "Message from {0} for {1} {2}", request.Client.RemoteEndPoint, request.Method.ToString().ToUpperInvariant(), request.Target);

                foreach (var handler in handlers)
                {
                    if (handler.CanHandle(request))
                    {
                        using (var response = handler.HandleRequest(request))
                        {
                            await request.Client.SendResponseAsync(response).ConfigureAwait(false);
                        }

                        return;
                    }
                }

                await request.Client.SendResponseAsync(HttpResponse.Status(HttpStatusCode.BadRequest)).ConfigureAwait(false);
            }
            catch (HttpException e)
            {
                await request.Client.SendResponseAsync(HttpResponse.Status(e.StatusCode)).ConfigureAwait(false);
            }
            catch (TargetInvocationException e)
            {
                logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when running route handler");
                // TODO: Filter message when not in debug mode
                await request.Client.SendResponseAsync(HttpResponse.Status(HttpStatusCode.InternalServerError, e.ToString())).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Error(e, CultureInfo.CurrentCulture, "Uncaught exception when processing connection");
                // TODO: Filter message when not in debug mode
                await request.Client.SendResponseAsync(HttpResponse.Status(HttpStatusCode.InternalServerError, e.ToString())).ConfigureAwait(false);
                throw;
            }
        }

        /// <inheritdoc />
        public string ServerName => "HTTP2#/0.1";
    }
}
