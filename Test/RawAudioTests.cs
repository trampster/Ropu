using NUnit.Framework;
using Ropu.Client;
using System;
using System.Linq;

namespace Ropu.Tests.Client
{
    public class RawCodecTests
    {
        RawCodec _rawCodec;
        
        [SetUp]
        public void Setup()
        {
            _rawCodec = new RawCodec();
        }

        [Test]
        public void EncodeDecode_WithData_DecodesToOriginal()
        {
            // arrange
            short[] audio = Enumerable.Range(200, 160).Select(u => (short)u).ToArray();
            byte[] encoded = new byte[320];

            


            // act
            _rawCodec.Encode(audio, encoded.AsSpan());
            var audioData = new AudioData();
            audioData.Data = encoded.AsSpan();
            short[] decoded = new short[160];
            _rawCodec.Decode(audioData, decoded);

            // assert
            SpanAssert.AreEqual(audio.AsSpan(), decoded.AsSpan());
        }

    }
}