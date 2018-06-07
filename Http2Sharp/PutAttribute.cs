using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class PutAttribute : MethodAttribute
    {
        public PutAttribute([NotNull] string path)
            : base(Method.Put, path)
        {
        }
    }
}