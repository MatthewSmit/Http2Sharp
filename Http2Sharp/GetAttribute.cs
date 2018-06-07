using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class GetAttribute : MethodAttribute
    {
        public GetAttribute([NotNull] string path)
            : base(Method.Get, path)
        {
        }
    }
}