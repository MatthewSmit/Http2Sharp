using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    /// <summary>
    /// A route handling the PUT HTTP method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PutAttribute : MethodAttribute
    {
        public PutAttribute([NotNull] string path)
            : base(Method.Put, path)
        {
        }
    }
}
