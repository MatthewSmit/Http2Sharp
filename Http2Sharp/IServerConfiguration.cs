using JetBrains.Annotations;

namespace Http2Sharp
{
    public interface IServerConfiguration
    {
        void AddServerHeaders([NotNull] HttpResponse response);
    }
}