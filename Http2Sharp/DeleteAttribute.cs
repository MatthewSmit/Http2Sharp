using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class DeleteAttribute : MethodAttribute
    {
        public DeleteAttribute([NotNull] string path)
            : base("DELETE", path)
        {
        }
    }
}