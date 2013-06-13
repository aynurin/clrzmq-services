
namespace ZeroMQ.Services
{
    public interface IZmqService
    {
        bool IsRunning { set; }
        IZmqMessageFormatter MessageFormatter { get; }
        void Starting(ZmqSocket server);
        object Receive(object message);
        bool CanProcess(byte[] message);
    }
}
