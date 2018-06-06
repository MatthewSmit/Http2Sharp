using JetBrains.Annotations;

namespace Http2Sharp
{
    public sealed class ConnectAttribute : MethodAttribute
    {
        public ConnectAttribute([NotNull] string path)
            : base("CONNECT", path)
        {
        }
    }
}