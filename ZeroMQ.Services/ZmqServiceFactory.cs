using System.Collections.Generic;

namespace ZeroMQ.Services
{
    public class ZmqServiceFactory : List<IZmqService>
    {
        public ZmqServiceFactory(params IZmqService[] services) : base(services)
        {
        }

        public virtual IZmqService GetHandler(byte[] message)
        {
            foreach (var service in this)
            {
                if (service.CanProcess(message))
                    return service;
            }
            return null;
        }

        public virtual IZmqMessageFormatter GetMessageFormatter(byte[] message)
        {
            return GetHandler(message).MessageFormatter;
        }
    }
}
