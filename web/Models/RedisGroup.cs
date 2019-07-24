using System.Collections.Generic;

namespace Ropu.Web.Models
{
    public enum GroupType
    {
        Open, //anyone can join
        Invite, //you have to be invited
    }

    public class RedisGroup : IGroup
    {
        public uint Id
        {
            get;
            set;
        }

        public string Name
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