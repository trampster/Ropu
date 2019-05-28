using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Ropu.Web.Services
{
    public class PasswordHasher
    {
        readonly RandomNumberGenerator _rng;
        const int _iterCount = 1000;
        const int _saltSize =  128 / 8;
        const int _desiredSubKeySize = 256 / 8;

        public PasswordHasher()
        {
            _rng = RandomNumberGenerator.Create();
        }
        public string HashPassword(string password)
        {
            var hash = HashPassword(password, _rng, KeyDerivationPrf.HMACSHA256, _iterCount, _saltSize, _desiredSubKeySize);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyHash(string password, string hash)
        {
            return VerifyHashedPassword(Convert.FromBase64String(hash), password);
        }

        static byte[] HashPassword(string password, RandomNumberGenerator rng, KeyDerivationPrf prf, int iterCount, int saltSize, int desiredSubKeySize)
        {
            byte[] salt = new byte[saltSize];
            rng.GetBytes(salt);
            byte[] subkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, desiredSubKeySize);

            var outputBytes = new byte[13 + salt.Length + subkey.Length];
            var outputSpan = outputBytes.AsSpan();
            outputSpan[0] = 0x01; // format marker
            WriteNetworkByteOrder(outputSpan.Slice(1), (uint)prf);
            WriteNetworkByteOrder(outputSpan.Slice(5), (uint)iterCount);
            WriteNetworkByteOrder(outputSpan.Slice(9), (uint)saltSize);
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);
            return outputBytes;
        }

        static void WriteNetworkByteOrder(Span<byte> span, uint value)
        {
            span[0] = (byte)(value >> 24);
            span[1] = (byte)(value >> 16);
            span[2] = (byte)(value >> 8);
            span[3] = (byte)(value >> 0);
        }

        static bool VerifyHashedPassword(byte[] hashedPassword, string password)
        {
            int iterCount = default(int);

            try
            {
                var hashSpan = hashedPassword.AsSpan();
                // Read header information
                KeyDerivationPrf prf = (KeyDerivationPrf)ReadNetworkByteOrder(hashSpan.Slice(1));
                iterCount = (int)ReadNetworkByteOrder(hashSpan.Slice(5));
                int saltLength = (int)ReadNetworkByteOrder(hashSpan.Slice(9));

                // Read the salt: must be >= 128 bits
                if (saltLength < 128 / 8)
                {
                    return false;
                }
                byte[] salt = new byte[saltLength];
                Buffer.BlockCopy(hashedPassword, 13, salt, 0, salt.Length);

                // Read the subkey (the rest of the payload): must be >= 128 bits
                int subkeyLength = hashedPassword.Length - 13 - salt.Length;
                if (subkeyLength < 128 / 8)
                {
                    return false;
                }
                byte[] expectedSubkey = new byte[subkeyLength];
                Buffer.BlockCopy(hashedPassword, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

                // Hash the incoming password and verify it
                byte[] actualSubkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, subkeyLength);
                return ByteArraysEqual(actualSubkey, expectedSubkey);
            }
            catch
            {
                // This should never occur except in the case of a malformed payload, where
                // we might go off the end of the array. Regardless, a malformed payload
                // implies verification failed.
                return false;
            }
        }

        static uint ReadNetworkByteOrder(Span<byte> buffer)
        {
            return ((uint)(buffer[0]) << 24)
                | ((uint)(buffer[1]) << 16)
                | ((uint)(buffer[2]) << 8)
                | ((uint)(buffer[3]));
        }

        static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (var i = 0; i < a.Length; i++)
            {
                if(a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}