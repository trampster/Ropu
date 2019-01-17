using System;
using Ropu.Client;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var jitterBuffer = new JitterBuffer(2,10);

            var audioData = new ushort[120];
            var audioDataMemory = audioData.AsMemory();

            ushort sequenceNumber = 3;
            while(true)
            {
                jitterBuffer.AddAudio(42, sequenceNumber, audioDataMemory);
                jitterBuffer.GetNext();
                sequenceNumber++;
            }
        }
    }
}
