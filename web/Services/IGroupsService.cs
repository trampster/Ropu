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
    }
}