using Xunit;

namespace Http2Sharp
{
    public sealed class HttpPathTest
    {
        [Fact]
        public void TestEmptyPath()
        {
            var path = new HttpPath("/");
            Assert.Equal(0, path.Segments.Count);
        }
    }
}
