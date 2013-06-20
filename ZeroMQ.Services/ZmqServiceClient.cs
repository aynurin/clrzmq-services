using System;
using System.Net;
using System.Threading;

namespace ZeroMQ.Services
{
    public class ZmqServiceClient : IDisposable
    {
        private readonly IPEndPoint _endpoint;
        private readonly ZmqContext _context;
        private readonly ZmqSocket _socket;
        private readonly IZmqMessageFormatter _messageFormatter;

        public ZmqServiceClient(IPEndPoint endpoint, IZmqMessageFormatter messageFormatter = null)
        {
            _messageFormatter = messageFormatter;
            _endpoint = endpoint;
            _context = ZmqContext.Create();
            _socket = _context.CreateSocket(SocketType.REQ);
            _socket.Connect(endpoint.AsString());
        }

        public object Send(object message)
        {
            return Send(message, TimeSpan.MaxValue);
        }

        public object Send(object message, TimeSpan waitForReply)
        {
            var bytes = _messageFormatter == null ? (byte[])message : _messageFormatter.Encode(message);
            _socket.Send(bytes, bytes.Length, SocketFlags.None, TimeSpan.FromSeconds(10));

            if (waitForReply != TimeSpan.Zero)
            {
                int messageSize;
                var data = _socket.Receive(null, waitForReply, out messageSize);
                return _messageFormatter == null ? data : _messageFormatter.Decode(data);
            }
            return null;
        }

        public void Dispose()
        {
            _socket.Dispose();
            _context.Dispose();
        }
    }
}
