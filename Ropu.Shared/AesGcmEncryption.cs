using System;

namespace Ropu.Shared
{
    public class AesGcmEncryption
    {
        IAesGcm _aesGcm;

        [ThreadStatic]
        static byte[] _nounceBuffer = new byte[0];
        [ThreadStatic]
        static bool _threadInitialized = false;
        readonly byte[] _key;

        public AesGcmEncryption(byte[] key, IAesGcm aesGcm)
        {
            _key = key;
            _aesGcm = aesGcm;
        }

        public void Encrypt(Span<byte> input, int packetCounter, Span<byte> output, Span<byte> tag)
        {
            if (!_threadInitialized)
            {
                _nounceBuffer = new byte[12];
            }
            // turn counter into an array
            _nounceBuffer[8] = (byte)(packetCounter << 24);
            _nounceBuffer[9] = (byte)(packetCounter << 16);
            _nounceBuffer[10] = (byte)(packetCounter << 8);
            _nounceBuffer[11] = (byte)packetCounter;

            _aesGcm.Encrypt(_nounceBuffer, input, output, tag);
        }

        public void Encrypt(Span<byte> input, Span<byte> output, Span<byte> tag, int packetCounter)
        {
            Encrypt(input, packetCounter, output, tag);
        }

        public void Decrypt(Span<byte> input, int packetCounter, Span<byte> output, Span<byte> tag)
        {
            if (!_threadInitialized)
            {
                _nounceBuffer = new byte[12];
            }

            // turn counter into an array
            _nounceBuffer[8] = (byte)(packetCounter << 24);
            _nounceBuffer[9] = (byte)(packetCounter << 16);
            _nounceBuffer[10] = (byte)(packetCounter << 8);
            _nounceBuffer[11] = (byte)packetCounter;

            _aesGcm.Decrypt(_nounceBuffer, input, tag, output);
        }

        public byte[] Key => _key;

    }
}