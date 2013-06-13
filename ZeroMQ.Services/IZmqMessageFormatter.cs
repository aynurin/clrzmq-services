
namespace ZeroMQ.Services
{
    public interface IZmqMessageFormatter
    {
        object Decode(byte[] buffer);
        byte[] Encode(object message);
    }
}
