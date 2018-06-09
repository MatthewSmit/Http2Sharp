using System;
using System.Linq;
using System.Net;
using System.Reflection;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal sealed class RouteManager
    {
        private readonly Route baseRoute = new Route();
        private readonly RouteMethod badRoute;

        public RouteManager([NotNull] object baseRouter)
        {
            var routerType = baseRouter.GetType();
            var routerAttribute = routerType.GetCustomAttribute<RouterAttribute>();
            if (routerAttribute == null)
            {
                throw new ArgumentException("Invalid router class, requires a RouterAttribute.", nameof(baseRouter));
            }

            var path = new HttpPath(routerAttribute.Path);
            var routerRoute = baseRoute.CreateRoute(path);

            foreach (var method in routerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetCustomAttribute<MethodAttribute>() != null))
            {
                //TODO: Maybe have any return type and convert?
                if (method.ReturnType != typeof(HttpResponse))
                {
                    throw new InvalidOperationException();
                }

                foreach (var methodAttribute in method.GetCustomAttributes<MethodAttribute>())
                {
                    var routePath = new HttpRoutePath(methodAttribute.Path);
                    var methodRoute = routerRoute.CreateRoute(routePath);
                    var routeMethod = new RouteMethod(baseRouter, method);
                    methodRoute.SetMethod(methodAttribute.Method, routeMethod);
                }
            }

            badRoute = new RouteMethod(this, GetType().GetMethod("BadRoute", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
        }

        [NotNull]
        [UsedImplicitly]
        private HttpResponse BadRoute()
        {
            return HttpResponse.Status(HttpStatusCode.NotFound);
        }

        [NotNull]
        public RouteMethod GetRoute([NotNull] HttpRequest request)
        {
            var currentRoute = baseRoute;
            var routeSegments = request.Target.Segments;

            if (routeSegments.Length == 0 || routeSegments[0] != "/")
            {
                throw new ArgumentException("Not a valid http route");
            }

            for (var i = 1; i < routeSegments.Length; i++)
            {
                var segment = routeSegments[i];
                if (segment.EndsWith('/'))
                {
                    segment = segment.Remove(segment.Length - 1);
                }
                var childRoute = currentRoute.GetRoute(segment, request.Parameters);
                if (childRoute == null)
                {
                    return badRoute;
                }

                currentRoute = childRoute;
            }

            var routeMethod = currentRoute.GetMethod(request.Method);
            return routeMethod ?? badRoute;
        }
    }
}