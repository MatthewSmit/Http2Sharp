using System;
using JetBrains.Annotations;

namespace Http2Sharp
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RouterAttribute : Attribute
    {
        public RouterAttribute()
        {
            Path = "/";
        }

        public RouterAttribute([NotNull] string path)
        {
            Path = path;
        }

        [NotNull]
        public string Path { get; set; }
    }
}
