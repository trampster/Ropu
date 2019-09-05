using System;
using System.Collections.Generic;
using Ropu.Web.Models;

namespace Ropu.Web.Services
{
    public interface IGroupsService
    {
        event EventHandler<(string name, ushort groupId)> NameChanged;

        IEnumerable<IGroup> Groups
        {
            get;
        }

        (bool, string) AddGroup(string name, GroupType groupType);

        IGroup Get(uint groupId);

        (bool result, string message) Edit(Group group);
        
        (bool result, string message) Delete(uint group);
    }
}