using System;
using Ropu.Client;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var jitterBuffer = new JitterBuffer(2,10);



            ushort sequenceNumber = 3;
            while(true)
            {
                var audioData = new ushort[120];
                for(int index = 0; index < audioData.Length; index++)
                {
                    audioData[index] = sequenceNumber;
                }

                jitterBuffer.AddAudio(42, sequenceNumber, audioData.AsMemory());
                var outAudio = jitterBuffer.GetNext();
                ushort firstSample = outAudio.Span[0];
                Console.Out.WriteLine($"Out {firstSample}");
                sequenceNumber++;
            }
        }
    }
}
