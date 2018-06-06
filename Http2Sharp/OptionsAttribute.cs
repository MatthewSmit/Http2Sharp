using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class OptionsAttribute : MethodAttribute
    {
        public OptionsAttribute([NotNull] string path)
            : base("OPTIONS", path)
        {
        }
    }
}