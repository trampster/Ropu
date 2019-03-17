using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Client.JitterBuffer;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

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