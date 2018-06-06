using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class PatchAttribute : MethodAttribute
    {
        public PatchAttribute([NotNull] string path)
            : base("PATCH", path)
        {
        }
    }
}