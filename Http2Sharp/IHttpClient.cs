using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Http2Sharp
{
    public interface IHttpClient : IDisposable
    {
        Task SendResponseAsync([NotNull] HttpResponse response);

        [NotNull]
        string RemoteEndPoint { get; }
        [NotNull]
        Stream Stream { get; }
        [NotNull]
        HttpStream HttpStream { get; }
    }
}
