namespace Ropu.Web.Models
{
    public interface IGroup
    {
        uint Id {get;}

        string Name {get;}

        string ImageHash
        {
            get;
        }

        GroupType GroupType {get;}
    }
}