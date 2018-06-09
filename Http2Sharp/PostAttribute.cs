using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    /// <summary>
    /// A route handling the POST HTTP method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PostAttribute : MethodAttribute
    {
        public PostAttribute([NotNull] string path)
            : base(Method.Post, path)
        {
        }
    }
}
