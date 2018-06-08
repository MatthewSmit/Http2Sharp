using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal static class UriExtensions
    {
        private static readonly Uri baseUri = new Uri("http://localhost");

        [NotNull]
        public static IReadOnlyList<(string, string)> SplitQueries([NotNull] this Uri uri)
        {
            var query = uri.Query;
            if (query.Length == 0)
            {
                return Array.Empty<(string, string)>();
            }

            return query.Substring(1).Split('&').Select(part =>
            {
                var splitPart = part.Split('=');
                if (splitPart.Length != 2)
                {
                    throw new ArgumentException();
                }
                return (splitPart[0], splitPart[1]);
            }).ToList();
        }

        [NotNull]
        public static Uri MakeAbsolute(this Uri uri)
        {
            return new Uri(baseUri, uri);
        }
    }
}
