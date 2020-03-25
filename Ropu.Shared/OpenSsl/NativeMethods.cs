using System;
using System.Runtime.InteropServices;

namespace Ropu.Shared.OpenSsl
{
    public static class NativeMethods
    {
        public const int EVP_CTRL_AEAD_GET_TAG = 0x10;
        public const int EVP_CTRL_AEAD_SET_TAG = 0x11;

        [DllImport("ssl")]
        public static extern IntPtr EVP_CIPHER_CTX_new();

        [DllImport("ssl")]
        public static extern IntPtr EVP_aes_256_gcm();

        [DllImport("ssl")]
        public static extern int EVP_EncryptInit(
            IntPtr cipherContext,
            IntPtr cipher, 
            IntPtr key,
            IntPtr iv);

        [DllImport("ssl")]
        public static extern int EVP_EncryptInit_ex(
            IntPtr cipherContext,
            IntPtr cipher, 
            IntPtr impl,
            byte[]? key,
            ref byte iv);

        [DllImport("ssl")]
        public static extern int EVP_DecryptInit_ex(
            IntPtr cipherContext,
            IntPtr cipher, 
            IntPtr impl,
            byte[]? key,
            ref byte iv);

        [DllImport("ssl")]
        public static extern int EVP_EncryptUpdate(IntPtr context, ref byte output, out int outl, ref byte input, int inputLength);

        [DllImport("ssl")]
        public static extern int EVP_EncryptFinal_ex(IntPtr context, ref byte output, out int outl);

        [DllImport("ssl")]
        public static extern int EVP_DecryptFinal_ex(IntPtr context, ref byte output, out int outl);

        [DllImport("ssl")]
        public static extern int EVP_CIPHER_CTX_ctrl(IntPtr context, int type, int arg, ref byte ptr);

        [DllImport("ssl")]
        public static extern void EVP_CIPHER_CTX_free(IntPtr context);

        [DllImport("ssl")]
        public static extern int EVP_DecryptUpdate(IntPtr context, ref byte output, out int outputLength, ref byte input, int inputLength);
    }
}