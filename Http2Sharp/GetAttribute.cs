using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class GetAttribute : MethodAttribute
    {
        public GetAttribute([NotNull] string path)
            : base(Method.Get, path)
        {
        }
    }
}