using System;
using System.Net;

namespace Ropu.Shared
{
    public static class BufferExtensions
    {
        public static void WriteUint(this byte[] buffer, uint value, int start)
        {
            buffer[start]     = (byte)((value & 0xFF000000) >> 24);
            buffer[start + 1] = (byte)((value & 0x00FF0000) >> 16);
            buffer[start + 2] = (byte)((value & 0x0000FF00) >> 8);
            buffer[start + 3] = (byte) (value & 0x000000FF);
        }

        public static void WriteUshort(this byte[] buffer, ushort value, int start)
        {
            buffer[start]     = (byte)((value & 0x0000FF00) >> 8);
            buffer[start + 1] = (byte) (value & 0x000000FF);
        }

        public static void WriteEndPoint(this byte[] buffer, IPEndPoint endPoint, int start)
        {
            endPoint.Address.TryWriteBytes(buffer.AsSpan(start), out int bytesWritten);
            buffer.WriteUshort((ushort)endPoint.Port, start + bytesWritten);
        }
    }
}