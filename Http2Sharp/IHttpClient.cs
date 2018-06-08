using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Http2Sharp
{
    public interface IHttpClient : IDisposable
    {
        Task ReadHeadersAsync();
        Task<object> ReadBodyAsync();

        Task SendResponseAsync(HttpResponse response);

        string RemoteEndPoint { get; }
        Method Method { get; }
        string Target { get; }
        string Version { get; }
        IReadOnlyList<(string, string)> Headers { get; }
    }
}