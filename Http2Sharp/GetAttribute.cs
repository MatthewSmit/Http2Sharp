using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    /// <summary>
    /// A route handling the GET HTTP method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class GetAttribute : MethodAttribute
    {
        public GetAttribute([NotNull] string path)
            : base(Method.Get, path)
        {
        }
    }
}
