using System.Threading.Tasks;

namespace Http2Sharp
{
    internal interface IBlockReader
    {
        Task ReadAsync(byte[] data, int offset, int length);
    }
}
