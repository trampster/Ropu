using System;
using System.Security.Cryptography;
using System.Threading;

namespace Ropu.Shared
{
    public class AesCounterMode
    {
        static ThreadLocal<byte[]> _counterBuf = new ThreadLocal<byte[]>(() => new byte[16]);

        static ThreadLocal<byte[]> _blockInput = new ThreadLocal<byte[]>(() => new byte[16]);

        static ThreadLocal<byte[]> _blockOutput = new ThreadLocal<byte[]>(() => new byte[16]);

        #nullable disable
        byte[] CounterBuf => _counterBuf.Value;
        byte[] BlockInput => _blockInput.Value;
        byte[] BlockOutput => _blockOutput.Value;
        #nullable enable

        public void TransformBlock(Span<byte> iv, Span<byte> input, ICryptoTransform aesTransform, int sequenceNumber, byte blockIndex, Span<byte> output)
        {
            // turn counter into an array
            var counterBuf = CounterBuf;
            counterBuf[0] =  (byte)(blockIndex & 0xFF);
            counterBuf[12] = (byte)(sequenceNumber << 24);
            counterBuf[13] = (byte)(sequenceNumber << 16);
            counterBuf[14] = (byte)(sequenceNumber << 8);
            counterBuf[15] = (byte)sequenceNumber;

            var blockInput = BlockInput;
            var blockOutput = BlockOutput;

            // xor iv with counter
            Xor(iv, counterBuf, blockInput);

            // encrypt with key
            aesTransform.TransformBlock(blockInput, 0, 16, blockOutput, 0);

            // xor with input
            Xor(blockOutput, input, output);
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