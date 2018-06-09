using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    /// <summary>
    /// A route handling the DELETE HTTP method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class DeleteAttribute : MethodAttribute
    {
        public DeleteAttribute([NotNull] string path)
            : base(Method.Delete, path)
        {
        }
    }
}
