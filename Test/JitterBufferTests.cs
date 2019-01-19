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

        [Test]
        public void AddAudio_NewBuffer_ComesOutOfterBufferMinSize()
        {
            // arrange
            var data = new ushort[160];
            for(int index = 0; index < data.Length; index++)
            {
                data[index] = (ushort)index;
            }

            // act
            _jitterBuffer.AddAudio(12, 2, data.AsMemory());
            _jitterBuffer.GetNext();
            _jitterBuffer.GetNext();
            var result = _jitterBuffer.GetNext();

            // assert
            SpanAssert.AreEqual(data.AsSpan(), result.Span);
        }
    }
}