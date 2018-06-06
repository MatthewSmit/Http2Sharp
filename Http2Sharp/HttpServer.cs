using System;
using System.Threading.Tasks;

namespace Http2Sharp
{
    public class HttpServer<T> : IDisposable
        where T : class, new()
    {
        public HttpServer()
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task StartListen()
        {
            throw new NotImplementedException();
        }
    }
}
