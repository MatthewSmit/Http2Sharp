using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    /// <summary>
    /// A route handling the PATCH HTTP method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PatchAttribute : MethodAttribute
    {
        public PatchAttribute([NotNull] string path)
            : base(Method.Patch, path)
        {
        }
    }
}
