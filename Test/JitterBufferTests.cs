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
        public void GetNext_BufferEmpty_ReturnsSilence()
        {
            // arrange
            // act
            var audio = _jitterBuffer.GetNext();

            // assert
            SpanAssert.AreEqual(new ushort[160].AsSpan(), audio.Span);
        }

        int GetBufferDelay()
        {
            var data = new ushort[160];
            data[0] = 4242;
            _jitterBuffer.AddAudio(12, 2, data.AsMemory());
            int size = 0;
            while(true)
            {
                size++;
                var audioOut = _jitterBuffer.GetNext().Span;
                if(audioOut[0] == 4242)
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