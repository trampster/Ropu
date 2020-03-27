using NUnit.Framework;
using System.Security.Cryptography;
using Ropu.Shared;
using Ropu.Shared.OpenSsl;
using System;

namespace Ropu.Shared.Tests
{
    public class AesGcmOpenSslTests
    {
        IAesGcm _aesGcm;
        byte[] _key;
        byte[] _plainText;

        [SetUp]
        public void Setup()
        {
            _key = new byte[]
            {
                0xee, 0xbc, 0x1f, 0x57, 0x48, 0x7f, 0x51, 0x92, 0x1c, 0x04, 0x65, 0x66,
                0x5f, 0x8a, 0xe6, 0xd1, 0x65, 0x8b, 0xb2, 0x6d, 0xe6, 0xf8, 0xa0, 0x69,
                0xa3, 0x52, 0x02, 0x93, 0xa5, 0x72, 0x07, 0x8f
            };
            _plainText = new byte[]{0xf5, 0x6e, 0x87, 0x05, 0x5b, 0xc3, 0x2d, 0x0e, 0xeb, 0x31, 0xb2, 0xea, 0xcc, 0x2b, 0xf2, 0xa5};

            _aesGcm = new AesGcmOpenSsl(_key);
        }

        [TearDown]
        public void TearDown()
        {
            _aesGcm.Dispose();
        }

        [Test]
        public void EncryptSameResultAsDotNetStandard()
        {
            byte[] plainText = new byte[]{0xf5, 0x6e, 0x87, 0x05, 0x5b, 0xc3, 0x2d, 0x0e, 0xeb, 0x31, 0xb2, 0xea, 0xcc, 0x2b, 0xf2, 0xa5};
            var nonce = new byte[12] {0x99, 0xaa, 0x3e, 0x68, 0xed, 0x81, 0x73, 0xa0, 0xee, 0xd0, 0x66, 0x84};

            byte[] cipherText = new byte[16];
            byte[] tag = new byte[16];
            _aesGcm.Encrypt(nonce, plainText, cipherText, tag);

            using(var aesGcmDotnetStandard = new AesGcm(_key))
            {
                byte[] expectedCipherText = new byte[16];
                byte[] expectedTag = new byte[16];
                aesGcmDotnetStandard.Encrypt(nonce, plainText, expectedCipherText, expectedTag);

                CollectionAssert.AreEqual(expectedCipherText, cipherText);
                CollectionAssert.AreEqual(expectedTag, tag);
            }
        }

        [Test]
        public void DecryptSameGetsCorrectPlainText()
        {
            byte[] cipherText = new byte[] {247,38,68,19,168,76,14,124,213,54,134,126,185,242,23,54};
            byte[] tag = new byte[] {108,122,83,229,85,19,162,11,241,78,126,173,82,38,62,74};
            var nonce = new byte[12] {0x99, 0xaa, 0x3e, 0x68, 0xed, 0x81, 0x73, 0xa0, 0xee, 0xd0, 0x66, 0x84};

            byte[] plainText = new byte[16];
            _aesGcm.Decrypt(nonce, cipherText, tag, plainText);

            CollectionAssert.AreEqual(_plainText, plainText);
        }
    }
}