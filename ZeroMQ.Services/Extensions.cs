using System.Net;

namespace ZeroMQ.Services
{
    static class Extensions
    {
        public static string AsString(this IPEndPoint endpoint)
        {
            string ip = endpoint.Address == IPAddress.Any ? "*" : endpoint.Address.ToString();
            return "tcp://" + ip + ":" + endpoint.Port;
        }
    }
}
