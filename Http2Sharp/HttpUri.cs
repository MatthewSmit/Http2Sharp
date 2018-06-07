using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class HttpUri
    {
        private readonly List<string> pathSegments = new List<string>();
        private readonly List<(string, string)> queries = new List<(string, string)>();

        public HttpUri([NotNull] string path)
        {
            if (path[0] != '/')
            {
                throw new ArgumentException();
            }

            var i = 1;
            ProcessPath(ref i, path);
        }

        private void ProcessPath(ref int i, [NotNull] string path)
        {
            var builder = new StringBuilder();
            for (; i < path.Length; i++)
            {
                switch (path[i])
                {
                    case '/':
                        pathSegments.Add(builder.ToString());
                        builder.Clear();
                        break;
                    case '%':
                        throw new NotImplementedException();
                    case '-':
                    case '.':
                    case '_':
                    case '@':
                    case ':':
                    case '!':
                    case '$':
                    case '&':
                    case '\'':
                    case '(':
                    case ')':
                    case '*':
                    case '+':
                    case ',':
                    case ';':
                    case '=':
                        builder.Append(path[i]);
                        break;
                    case '?':
                        if (builder.Length > 0)
                        {
                            pathSegments.Add(builder.ToString());
                        }

                        ProcessQuery(ref i, path);

                        return;
                    default:
                        if (char.IsLetterOrDigit(path[i]))
                        {
                            builder.Append(path[i]);
                            break;
                        }

                        throw new ArgumentException();
                }
            }

            if (builder.Length > 0)
            {
                pathSegments.Add(builder.ToString());
            }
        }

        private void ProcessQuery(ref int i, [NotNull] string path)
        {
            // TODO: Improve?

            queries.AddRange(path.Substring(i + 1).Split('&').Select(delegate(string x)
            {
                var parts = x.Split('=');
                if (parts.Length != 2)
                {
                    throw new NotImplementedException();
                }

                return (parts[0], parts[1]);
            }));
        }

        [NotNull]
        public static implicit operator HttpUri([NotNull] string path)
        {
            return new HttpUri(path);
        }

        public IEnumerable<string> PathSegments => pathSegments;
        public IList<(string, string)> Queries => queries;
    }
}