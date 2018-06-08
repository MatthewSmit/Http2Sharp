using JetBrains.Annotations;

namespace Http2Sharp.Test
{
    [Router]
    internal sealed class TestServer
    {
        [NotNull]
        [Get("/")]
        public HttpResponse GetMain()
        {
            return HttpResponse.Send("Hello World");
        }
    }
}