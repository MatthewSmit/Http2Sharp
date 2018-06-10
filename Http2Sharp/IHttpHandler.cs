using JetBrains.Annotations;

namespace Http2Sharp
{
    public interface IHttpHandler
    {
        bool CanHandle([NotNull] HttpRequest request);
        [NotNull] HttpResponse HandleRequest([NotNull] HttpRequest request);
    }
}