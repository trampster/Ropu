using System.Collections.Generic;
using Ropu.Shared.WebModels;

namespace Ropu.Shared.Groups
{
    public class Group : IGroup
    {

        public Group()
        {
            Name = "";
            ImageHash = "";
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

        byte[]? _image;
        public byte[]? Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
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