using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Http2Sharp
{
    public class HttpPath
    {
        protected HttpPath()
        {
        }

        public HttpPath([NotNull] string path)
        {
            var segments = SegmentsList;

            if (path[0] != '/')
            {
                throw new ArgumentException();
            }

            var builder = new StringBuilder();
            for (var i = 1; i < path.Length; i++)
            {
                switch (path[i])
                {
                    case '/':
                        throw new NotImplementedException();
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
                segments.Add(builder.ToString());
            }
        }

        protected List<string> SegmentsList { get; } = new List<string>();
        public IReadOnlyList<string> Segments => SegmentsList;
        public virtual IEnumerable<Regex> SegmentRegex
        {
            get
            {
                foreach (var segment in SegmentsList)
                {
                    throw new NotImplementedException();
                    yield return null;
                }
            }
        }
    }
}