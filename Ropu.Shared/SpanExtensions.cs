
using System;
using System.Net;

namespace Ropu.Shared
{
    public static class SpanExtensions
    {
        public static uint ParseUint(this Span<byte> data)
        {
            return (uint)(
                (data[0] << 24) +
                (data[1] << 16) +
                (data[2] << 8) +
                data[3]); 
        }

        public static ushort ParseUshort(this Span<byte> data)
        {
            return (ushort)(
                (data[0] << 8) +
                (data[1])); 
        }

        public static IPEndPoint ParseIPEndPoint(this Span<byte> data)
        {
            var address = new IPAddress(data);
            ushort port = data.Slice(4).ParseUshort();
            return new IPEndPoint(address, port);
        }
    }
}