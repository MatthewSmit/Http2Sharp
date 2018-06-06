using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class TraceAttribute : MethodAttribute
    {
        public TraceAttribute([NotNull] string path)
            : base("TRACE", path)
        {
        }
    }
}