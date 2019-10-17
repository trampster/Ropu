using System.Collections.Generic;

namespace Ropu.Shared.WebModels
{
    public class EntitiesKeys
    {
        public EntitiesKeys()
        {
            Keys = new List<EncryptionKey>();
        }

        public uint UserOrGroupId
        {
            get;
            set;
        }

        public List<EncryptionKey> Keys
        {
            get;
            set;
        }
    }
}