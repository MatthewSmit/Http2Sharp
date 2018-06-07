using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PatchAttribute : MethodAttribute
    {
        public PatchAttribute([NotNull] string path)
            : base(Method.Patch, path)
        {
        }
    }
}