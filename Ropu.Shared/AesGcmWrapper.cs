using System;
using System.Security.Cryptography;

namespace Ropu.Shared
{
    public class AesGcmWrapper : IAesGcm
    {
        readonly AesGcm _aesGcm;

        public AesGcmWrapper(byte[] key)
        {
            _aesGcm = new AesGcm(key);
        }

        public void Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext)
        {
            _aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        public void Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag)
        {
            _aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }
    }
}
