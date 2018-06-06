using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodAttribute : Attribute
    {
        protected MethodAttribute([NotNull] string method, [NotNull] string path)
        {
            Method = method;
            Path = path;
        }

        [NotNull]
        public string Method { get; set; }

        [NotNull]
        public string Path { get; set; }
    }
}