using System;

namespace Ropu.Client.JitterBuffer
{
    public interface IJitterBuffer
    {
        void AddAudio(uint userId, ushort sequenceNumber, Span<byte> audioData);

        (AudioData?, bool) GetNext(Action streamFinished, Action streamStart);

        /// <summary>
        /// Set by client to indicate who has the floor.
        /// If set the jitter buffer should only accept packets from this user id
        /// </summary>
        uint? Talker
        {
            set;
        } 
    }
}