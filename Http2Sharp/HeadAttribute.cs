using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class HeadAttribute : MethodAttribute
    {
        public HeadAttribute([NotNull] string path)
            : base("HEAD", path)
        {
        }
    }
}