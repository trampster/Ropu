using Ropu.Shared.WebModels;

namespace Ropu.Web.Models
{
    public class Group : IGroup
    {
        public Group()
        {
            Name = "";
            ImageHash = "";
        }

        public ushort Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public GroupType GroupType
        {
            get;
            set;
        }

        public string ImageHash
        {
            get;
            set;
        }
    }
}