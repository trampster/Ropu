using System;
using System.Threading;
using Ropu.Shared.WebModels;

namespace Ropu.Shared
{
    public class CachedEncryptionKey
    {
        int _packetCounter = 0;

        public CachedEncryptionKey(EncryptionKey key)
        {
            Key = key;
        }

        EncryptionKey Key
        {
            get;
        }

        public int KeyId => Key.KeyId;

        public bool isTodaysKey()
        {
            bool result = Key.Date.Date == DateTime.UtcNow.Date;
            return result;
        }

        public uint GetPacketCounter()
        {
            return (uint)Interlocked.Increment(ref _packetCounter);
        }
        
        byte[]? _keyMaterialArray;

        public byte[] GetKeyMaterial()
        {
            if(_keyMaterialArray == null)
            {
                _keyMaterialArray = HexConverter.FromHex(Key.KeyMaterial);
            }
            return _keyMaterialArray;
        }

        AesGcmEncryption? _aesGcmEncryption;

        public AesGcmEncryption Encryption
        {
            get
            {
                if(_aesGcmEncryption == null)
                {
                    _aesGcmEncryption = new AesGcmEncryption(GetKeyMaterial());
                }
                return _aesGcmEncryption;
            }
        }
    }
}