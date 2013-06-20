using System;
using System.Net;
using System.Text;
using System.Threading;

namespace ZeroMQ.Services
{
    public class ZmqServiceHost : IDisposable
    {
        private readonly IPEndPoint _endPoint;
        private readonly IZmqService _service;
        private readonly ZmqServiceFactory _serviceFactory;
        private readonly Thread _hostingThread;

        public IZmqMessageFormatter MessageFormatter { get; set; }

        public ZmqServiceHost(IPEndPoint endPoint, IZmqService service)
        {
            _endPoint = endPoint;
            _service = service;
            _hostingThread = new Thread(BindService);
        }

        public ZmqServiceHost(IPEndPoint endPoint, ZmqServiceFactory serviceFactory)
        {
            _endPoint = endPoint;
            _serviceFactory = serviceFactory;
            _hostingThread = new Thread(BindService);
        }

        public void Open()
        {
            _hostingThread.Start();
        }

        private void BindService()
        {
            // ZMQ Context, server socket
            using (var context = ZmqContext.Create())
            using (var server = context.CreateSocket(SocketType.REP))
            {
                server.Bind(_endPoint.AsString());

                try
                {
                    SetRunning(server);

                    while (true)
                    {
                        // Wait for next request from client
                        int messageSize;
                        byte[] buffer = server.Receive(null, TimeSpan.FromSeconds(10), out messageSize);
                        if (server.ReceiveStatus == ReceiveStatus.TryAgain)
                            continue;
                        if (buffer == null || messageSize == 0)
                        {
                            Thread.Yield();
                            continue;
                        }

                        if (server.ReceiveStatus == ReceiveStatus.Received)
                        {
                            object msg = _service.MessageFormatter == null ? buffer : _service.MessageFormatter.Decode(buffer);

                            var result = _service.Receive(msg);
                            if (result != null)
                            {
                                byte[] data = _service.MessageFormatter == null
                                                  ? (result as byte[])
                                                  : _service.MessageFormatter.Encode(result);

                                // Send reply back to client
                                if (data != null)
                                    server.Send(data);
                            }
                        }
                    }
                }
                finally
                {
                    _service.IsRunning = false;
                }
            }
        }

        private void SetRunning(ZmqSocket socket, IZmqService svc = null)
        {
            if (svc != null)
            {
                svc.Starting(socket);
                svc.IsRunning = true;
            }
            else if (_service != null)
                SetRunning(socket, _service);
            else if (_serviceFactory != null)
                foreach (var service in _serviceFactory)
                    SetRunning(socket, service);
        }

        public void Dispose()
        {
            if (_hostingThread != null && _hostingThread.IsAlive)
                _hostingThread.Abort();
        }
    }
}
