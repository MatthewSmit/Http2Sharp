using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal sealed class Route
    {
        [CanBeNull] private Route parent;
        private readonly RouteMethod[] routeMethods = new RouteMethod[Enum.GetValues(typeof(Method)).Length];
        private readonly IList<(Regex, Route)> children = new List<(Regex, Route)>();

        public Route()
        {
        }

        private Route(Route parent)
        {
            this.parent = parent;
        }

        [NotNull]
        public Route CreateRoute([NotNull] HttpPath path)
        {
            var currentRoute = this;
            foreach (var segment in path.SegmentRegex)
            {
                var newRoute = currentRoute.FindRoute(segment);

                if (newRoute == null)
                {
                    newRoute = new Route(currentRoute);
                    currentRoute.children.Add((segment, newRoute));
                }

                currentRoute = newRoute;
            }

            return currentRoute;
        }

        private Route FindRoute(Regex segment)
        {
            foreach (var (regex, route) in children)
            {
                if (string.Equals(segment.ToString(), regex.ToString()))
                {
                    return route;
                }
            }

            return null;
        }

        [CanBeNull]
        public Route GetRoute(string segment, IDictionary<string, string> parameters)
        {
            foreach (var (regex, route) in children)
            {
                var match = regex.Match(segment);
                if (match.Success)
                {
                    var names = regex.GetGroupNames();
                    foreach (var name in names)
                    {
                        if (!char.IsDigit(name[0]))
                        {
                            var value = match.Groups[name].Value;
                            parameters[name] = value;
                        }
                    }

                    return route;
                }
            }

            return null;
        }

        public void SetMethod(Method method, RouteMethod routeMethod)
        {
            routeMethods[(int)method] = routeMethod;
        }

        [Pure]
        public RouteMethod GetMethod(Method method)
        {
            return routeMethods[(int)method];
        }
    }
}