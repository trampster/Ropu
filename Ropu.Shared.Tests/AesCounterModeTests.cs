using NUnit.Framework;
using System.Security.Cryptography;
using Ropu.Shared;

namespace Ropu.Shared.Tests
{
    public class AesCounterModeTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void RoundTrip()
        {
            
            var aesCounterMode = new AesCounterMode();
            byte[] iv = new byte[]{0xa7,0xc8,0x77,0x4c,0x92,0x32,0x3d,0x1d,0x44,0x81,0x01,0xaf,0xd0,0x82,0xea,0x17};
            byte[] plainText = new byte[]{0x8a,0x07,0xd2,0xe7,0x46,0x4a,0x0e,0x11,0xc4,0x12,0x01,0x13,0xc1,0x68,0xf9,0x89};
            byte[] key = new byte[]
            {
                0xe9,0xf9,0x49,0x75,0x6b,0x80,0xca,0x96,0x79,0xaf,0x0e,0xb6,0xf1,0x7c,0x29,0x57,
                0x22,0x5f,0x67,0x5b,0x5e,0xf6,0x96,0xe7,0xab,0x3e,0x7f,0x54,0xfe,0xc1,0x65,0x6c
            };
            
            AesCryptoServiceProvider provider = new AesCryptoServiceProvider();
            provider.Key = key;
            provider.Mode = CipherMode.ECB;
            var transform = provider.CreateEncryptor();

            byte[] cipherText = new byte[16];

            aesCounterMode.TransformBlock(iv, plainText, transform, 42,  3, cipherText);

            byte[] roundTripResult = new byte[16];
            aesCounterMode.TransformBlock(iv, cipherText, transform, 42, 3, roundTripResult);

            CollectionAssert.AreEqual(plainText, roundTripResult);
        }
    }
}