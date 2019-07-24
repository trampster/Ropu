namespace Ropu.Web.Models
{
    public interface IGroup
    {
        uint Id {get;}

        string Name {get;}

        GroupType GroupType {get;}
    }
}