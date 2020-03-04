using NUnit.Framework;
using System.Security.Cryptography;
using Ropu.Shared;

namespace Ropu.Shared.Tests
{
    public class AesGcmEncryptionTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void RoundTrip()
        {
            byte[] plainText = new byte[]{0x8a,0x07,0xd2,0xe7,0x46,0x4a,0x0e,0x11,0xc4,0x12,0x01,0x13,0xc1,0x68,0xf9,0x89};
            byte[] key = new byte[]
            {
                0xe9,0xf9,0x49,0x75,0x6b,0x80,0xca,0x96,0x79,0xaf,0x0e,0xb6,0xf1,0x7c,0x29,0x57,
                0x22,0x5f,0x67,0x5b,0x5e,0xf6,0x96,0xe7,0xab,0x3e,0x7f,0x54,0xfe,0xc1,0x65,0x6c
            };

            var aesCounterMode = new AesGcmEncryption(key);

            byte[] cipherText = new byte[16];
            byte[] tag = new byte[12];

            aesCounterMode.Encrypt(plainText, 42, cipherText, tag);

            byte[] roundTripResult = new byte[16];
            aesCounterMode.Decrypt(cipherText, 42, roundTripResult, tag);

            CollectionAssert.AreEqual(plainText, roundTripResult);
        }
    }
}