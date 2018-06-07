using System;
using System.Collections.Generic;
using System.Linq;
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

        private HttpResponse BadRoute()
        {
            return HttpResponse.Status(404);
        }

        [NotNull]
        [Pure]
        public (RouteMethod, Dictionary<string, string>) GetRoute(Method method, [NotNull] HttpUri route)
        {
            var currentRoute = baseRoute;
            var parameters = new Dictionary<string, string>();

            foreach (var segment in route.PathSegments)
            {
                var childRoute = currentRoute.GetRoute(segment, parameters);
                if (childRoute == null)
                {
                    return (badRoute, parameters);
                }

                currentRoute = childRoute;
            }

            var routeMethod = currentRoute.GetMethod(method);
            return (routeMethod ?? badRoute, parameters);
        }
    }
}