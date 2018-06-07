using Xunit;

namespace Http2Sharp.Test
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
