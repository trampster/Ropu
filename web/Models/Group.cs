namespace Ropu.Web.Models
{
    public class Group : IGroup
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

        public GroupType GroupType
        {
            get;
            set;
        }
    }
}