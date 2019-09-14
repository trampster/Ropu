using System.Collections.Generic;
using Ropu.Shared.WebModels;

namespace Ropu.Web.Models
{
    public class RedisGroup : IGroup
    {
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