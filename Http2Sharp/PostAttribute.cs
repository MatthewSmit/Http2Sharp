using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class PostAttribute : MethodAttribute
    {
        public PostAttribute([NotNull] string path)
            : base("POST", path)
        {
        }
    }
}