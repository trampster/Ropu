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
    }
}