using System.Collections.Generic;
using Ropu.Web.Models;

namespace Ropu.Web.Services
{
    public interface IGroupsService
    {
        IEnumerable<IGroup> Groups
        {
            get;
        }

        (bool, string) AddGroup(string name, GroupType groupType);

        IGroup Get(uint groupId);
        (bool result, string message) Edit(Group group);
    }
}