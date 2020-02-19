using System;
using System.Threading.Tasks;

namespace Ropu.Client
{
    public interface IMediaClient : IMediaPacketParser, IDisposable
    {
        uint? Talker
        {
            set;
        }

        Task StartSendingAudio(ushort groupId);

        void StopSendingAudio();

        Task PlayAudio();
    }
}