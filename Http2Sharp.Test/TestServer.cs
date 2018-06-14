using JetBrains.Annotations;

namespace Http2Sharp
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

        [NotNull]
        [Get("/echo2/{value}")]
        public HttpResponse Echo2([Param] [NotNull] string value)
        {
            return HttpResponse.Send(value);
        }

        [NotNull]
        [Post("/post")]
        public HttpResponse Post([Body] [NotNull] User user)
        {
            return HttpResponse.Send(user);
        }
    }
}