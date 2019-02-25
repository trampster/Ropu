using NUnit.Framework;
using Ropu.Client;
using System;
using System.Linq;

namespace Ropu.Tests.Client
{
    public class JitterBufferTests
    {
        JitterBuffer _jitterBuffer;
        
        [SetUp]
        public void Setup()
        {
            _jitterBuffer = new JitterBuffer(2,10);
        }

        [Test]
        public void GetNext_BufferEmpty_ReturnsEmptyData()
        {
            // arrange
            var data = new byte[320];
            data[0] = 42;
            _jitterBuffer.AddAudio(12, 2, data.AsSpan());
            _jitterBuffer.GetNext(() => {}); // an empty packet
            _jitterBuffer.GetNext(() => {}); // the one we set, now buffer should be empty
            _jitterBuffer.GetNext(() => {}); // will repeat last packet


            // act
            var audio = _jitterBuffer.GetNext(() => {});

            // assert
            SpanAssert.AreEqual(new byte[0].AsSpan(), audio.Data);
        }

        int GetBufferDelay()
        {
            var data = new byte[320];
            data[0] = 42;
            _jitterBuffer.AddAudio(12, 2, data.AsSpan());
            int size = 0;
            while(true)
            {
                size++;
                var audioOut = _jitterBuffer.GetNext(() => {}).Data;
                if(audioOut.Length > 0 && audioOut[0] == 42)
                {
                    return size;
                }
            }
        }

        [Test]
        public void AddAudio_NewBuffer_ComesOutOfterBufferMinSize()
        {
            // arrange
            // act
            var bufferSize = GetBufferDelay();

            // assert
            Assert.That(bufferSize, Is.EqualTo(2));
        }
    }
}