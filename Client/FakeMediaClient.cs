using System;
using System.Threading.Tasks;

namespace Ropu.Client
{
    public class FakeMediaClient : IMediaClient
    {
        public uint? Talker { set{} }

        public void Dispose()
        {
        }

        public void ParseMediaPacketGroupCall(Span<byte> data)
        {
        }

        public async Task PlayAudio()
        {
            await Task.Delay(-1);
        }

        public async Task StartSendingAudio(ushort groupId)
        {
            await Task.Delay(-1);
        }

        public void StopSendingAudio()
        {
        }
    }
}