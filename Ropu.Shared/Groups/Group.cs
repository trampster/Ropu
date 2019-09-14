using System.Collections.Generic;
using Ropu.Shared.WebModels;

namespace Ropu.Shared.Groups
{
    public class Group : IGroup
    {

        public Group()
        {
        }

        public string Name
        {
            get;
            set;
        }

        public ushort Id
        {
            get;
            set;
        }

        public byte[] Image
        {
            get;
            set;
        }

        public string ImageHash
        {
            get;
            set;
        }

        public GroupType GroupType
        {
            get;
            set;
        }
    }
}