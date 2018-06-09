using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal sealed class HttpRequest
    {
        public HttpRequest(Method method, [NotNull] Uri target, [NotNull] IEnumerable<(string, string)> clientHeaders)
        {
            Method = method;
            Target = target;
            Queries = target.SplitQueries();

            foreach (var (headerName, headerValue) in clientHeaders)
            {
                if (string.Equals("Content-Type", headerName, StringComparison.InvariantCultureIgnoreCase))
                {
                    ContentType = headerValue;
                }
            }
        }

        public Method Method { get; }
        [NotNull]
        public Uri Target { get; }
        [CanBeNull]
        public string ContentType { get; }
        [NotNull]
        public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();
        [NotNull]
        public IReadOnlyList<(string, string)> Queries { get; }
        [CanBeNull]
        public byte[] Body { get; set; }
    }
}