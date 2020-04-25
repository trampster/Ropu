using System;
using Ropu.AesGcm;

namespace Ropu.Shared.OpenSsl
{
    public class AesGcmOpenSslWrapper : IAesGcm
    {
        readonly AesGcmOpenSsl _aesGcmOpenSsl;
        public AesGcmOpenSslWrapper(byte[] key)
        {
            _aesGcmOpenSsl = new AesGcmOpenSsl(key);
        }

        public void Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag)
        {
            _aesGcmOpenSsl.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        public void Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext)
        {
            _aesGcmOpenSsl.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        #region IDisposable Support
        bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _aesGcmOpenSsl.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}