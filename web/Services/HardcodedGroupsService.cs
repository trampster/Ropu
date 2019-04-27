using System.Collections.Generic;
using Ropu.Web.Models;

namespace Ropu.Web.Services
{
    public class HardcodedGroupsService : IGroupsService
    {
        readonly IGroup[] _groups = new[]
        {
            new Group()
            {
                Name = "Avengers",
                Id = 4242
            },
            new Group()
            {
                Name = "Justice L",
                Id = 4243
            }
        };

        public IEnumerable<IGroup> Groups
        {
            get
            {
                return _groups;
            }
        }
    }
}