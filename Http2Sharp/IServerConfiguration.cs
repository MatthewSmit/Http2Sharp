using JetBrains.Annotations;

namespace Http2Sharp
{
    public interface IServerConfiguration
    {
        [NotNull]
        string ServerName { get; }
    }
}