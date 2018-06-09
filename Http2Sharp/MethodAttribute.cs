using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [MeansImplicitUse]
    public class MethodAttribute : Attribute
    {
        protected MethodAttribute(Method method, [NotNull] string path)
        {
            Method = method;
            Path = path;
        }

        public Method Method { get; set; }

        [NotNull]
        public string Path { get; set; }
    }
}
