namespace Ropu.Shared.WebModels
{
    public interface IGroup
    {
        ushort Id {get;}

        string Name {get;}

        string ImageHash
        {
            get;
        }

        GroupType GroupType {get;}
    }
}