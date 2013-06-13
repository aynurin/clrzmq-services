using System;
using System.Net;
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

        public void BindService()
        {
            // ZMQ Context, server socket
            using (var context = ZmqContext.Create())
            using (var server = context.CreateSocket(SocketType.REP))
            {
                server.Bind(EndpointString());

                _service.IsRunning = true;
                try
                {
                    SetRunning(server);
                    _service.Starting(server);

                    while (true)
                    {
                        // Wait for next request from client
                        var frm = server.ReceiveFrame();
                        object msg = MessageFormatter == null ? frm.Buffer : MessageFormatter.Decode(frm.Buffer);
                        var result = _service.Receive(msg);
                        if (result != null)
                        {
                            byte[] data = MessageFormatter == null
                                              ? (result as byte[])
                                              : MessageFormatter.Encode(result);
                            if (data != null)
                            {
                                // Send reply back to client
                                server.SendFrame(new Frame(data));
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

        private string EndpointString()
        {
            string ip = _endPoint.Address == IPAddress.Any ? "*" : _endPoint.Address.ToString();
            return "tcp://" + ip + ":" + _endPoint.Port;
        }

        public void Dispose()
        {
            if (_hostingThread != null && _hostingThread.IsAlive)
                _hostingThread.Abort();
        }
    }
}
