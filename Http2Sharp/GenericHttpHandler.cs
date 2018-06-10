using JetBrains.Annotations;

namespace Http2Sharp
{
    public class GenericHttpHandler : IHttpHandler
    {
        private readonly RouteManager routeManager;

        public GenericHttpHandler([NotNull] object baseRouter)
        {
            routeManager = new RouteManager(baseRouter);
        }

        /// <inheritdoc />
        public bool CanHandle(HttpRequest request)
        {
            //TODO
            return true;
        }

        /// <inheritdoc />
        public HttpResponse HandleRequest(HttpRequest request)
        {
            var route = routeManager.GetRoute(request);
            return route.Invoke(request);
        }
    }
}