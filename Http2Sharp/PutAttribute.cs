using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PutAttribute : MethodAttribute
    {
        public PutAttribute([NotNull] string path)
            : base(Method.Put, path)
        {
        }
    }
}