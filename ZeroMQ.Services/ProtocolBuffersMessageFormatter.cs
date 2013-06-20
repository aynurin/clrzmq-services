using System;
using System.IO;

namespace ZeroMQ.Services
{
    public class ProtocolBuffersMessageFormatter : IZmqMessageFormatter
    {
        private readonly Func<byte[], Type> _typeResolver;

        public ProtocolBuffersMessageFormatter(Func<byte[], Type> typeResolver)
        {
            _typeResolver = typeResolver;
        }

        public object Decode(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return null;
            using (var s = new MemoryStream(buffer))
                return ProtoBuf.Serializer.NonGeneric.Deserialize(_typeResolver(buffer), s);
        }

        public byte[] Encode(object message)
        {
            using (var s = new MemoryStream())
            {
                ProtoBuf.Serializer.NonGeneric.Serialize(s, message);
                s.Flush();
                return s.ToArray();
            }
        }
    }
}
