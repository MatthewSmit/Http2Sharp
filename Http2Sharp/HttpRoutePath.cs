using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Http2Sharp
{
    internal sealed class HttpRoutePath : HttpPath
    {
        private readonly IList<Regex> regexes = new List<Regex>();

        public HttpRoutePath([NotNull] string path)
        {
            var segments = SegmentsList;

            var i = 0;
            if (path[0] == '/')
            {
                i = 1;
            }

            var builder = new StringBuilder();
            var regexBuilder = new StringBuilder();
            for (; i < path.Length; i++)
            {
                switch (path[i])
                {
                    case '/':
                        segments.Add(builder.ToString());
                        regexes.Add(new Regex("^" + regexBuilder + "$"));
                        builder.Clear();
                        regexBuilder.Clear();
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
                        regexBuilder.Append('\\');
                        regexBuilder.Append(path[i]);
                        break;

                    case '{':
                        ReadParameter(ref i, path, builder, regexBuilder);
                        break;

                    default:
                        if (char.IsLetterOrDigit(path[i]))
                        {
                            builder.Append(path[i]);
                            regexBuilder.Append(path[i]);
                            break;
                        }

                        throw new ArgumentException();
                }
            }

            if (builder.Length > 0)
            {
                segments.Add(builder.ToString());
                regexes.Add(new Regex("^" + regexBuilder + "$"));
            }
        }

        private static void ReadParameter(ref int i, [NotNull] string path, [NotNull] StringBuilder builder, [NotNull] StringBuilder regexBuilder)
        {
            builder.Append(path[i]);
            i++;

            var nameStart = i;
            string name = null;
            for (; i < path.Length; i++)
            {
                builder.Append(path[i]);
                if (path[i] == '}')
                {
                    name = path.Substring(nameStart, i - nameStart);
                    i++;
                    regexBuilder.Append("(?<" + name + ">[^/]+)");
                    return;
                }

                if (path[i] == ':')
                {
                    name = path.Substring(nameStart, i - nameStart);
                    i++;
                    break;
                }
            }

            if (name == null)
            {
                throw new NotImplementedException();
            }

            regexBuilder.Append("(?<" + name + ">");
            for (; i < path.Length; i++)
            {
                //TODO: Validate regex properly
                builder.Append(path[i]);
                if (path[i] == '}')
                {
                    regexBuilder.Append(")");
                    return;
                }

                regexBuilder.Append(path[i]);
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override IEnumerable<Regex> SegmentRegex => regexes;
    }
}
