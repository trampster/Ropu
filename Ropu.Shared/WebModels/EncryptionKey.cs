using System;

namespace Ropu.Shared.WebModels
{
    public class EncryptionKey
    {
        public EncryptionKey()
        {
            KeyMaterial = "";
        }

        public int KeyId
        {
            get;
            set;
        }

        public DateTime Date
        {
            get;
            set;
        }

        public string KeyMaterial
        {
            get;
            set;
        }

        byte[]? _keyMaterialArray;

        public byte[] GetKeyMaterial()
        {
            if(_keyMaterialArray == null)
            {
                _keyMaterialArray = HexConverter.FromHex(KeyMaterial);
            }
            return _keyMaterialArray;
        }
    }
}