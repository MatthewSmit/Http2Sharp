using System;
using System.Threading.Tasks;

namespace Http2Sharp
{
    public interface IHttpListener : IDisposable
    {
        Task StartListenAsync(TaskFactory taskFactory, Func<HttpRequest, Task> processClient);
    }
}
