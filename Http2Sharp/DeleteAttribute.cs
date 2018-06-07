using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class DeleteAttribute : MethodAttribute
    {
        public DeleteAttribute([NotNull] string path)
            : base(Method.Delete, path)
        {
        }
    }
}