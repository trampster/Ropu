using System;
using System.Security.Cryptography;

namespace Ropu.Shared
{
    public class AesCounterMode
    {
        [ThreadStatic]
        static byte[] _counterBuf = new byte[16];
        [ThreadStatic]
        static byte[] _blockInput = new byte[16];
        [ThreadStatic]
        static byte[] _blockOutput = new byte[16];

        public void TransformBlock(Span<byte> iv, Span<byte> input, ICryptoTransform aesTransform, int sequenceNumber, byte blockIndex, Span<byte> output)
        {
            // turn counter into an array
            _counterBuf[0] =  (byte)(blockIndex & 0xFF);
            _counterBuf[12] = (byte)(sequenceNumber << 24);
            _counterBuf[13] = (byte)(sequenceNumber << 16);
            _counterBuf[14] = (byte)(sequenceNumber << 8);
            _counterBuf[15] = (byte)sequenceNumber;

            // xor iv with counter
            Xor(iv, _counterBuf, _blockInput);

            // encrypt with key
            aesTransform.TransformBlock(_blockInput, 0, 16, _blockOutput, 0);

            // xor with input
            Xor(_blockOutput, input, output);
        }

        void Xor(Span<byte> a, Span<byte> b, Span<byte> output)
        {
            for(int index = 0; index < a.Length; index++)
            {
                output[index] = (byte)(a[index] ^ b[index]);
            }
        }
    }
}