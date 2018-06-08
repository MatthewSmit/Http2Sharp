using JetBrains.Annotations;

namespace Http2Sharp.Test
{
    [Router]
    internal sealed class TestServer
    {
        [NotNull]
        [Get("/")]
        public HttpResponse Main()
        {
            return HttpResponse.Send("Hello World");
        }

        [NotNull]
        [Get("/echo")]
        public HttpResponse Echo([Query] [NotNull] string value)
        {
            return HttpResponse.Send(value);
        }
    }
}