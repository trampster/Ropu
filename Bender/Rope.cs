namespace Ropu.Bender
{
    public class Rope
    {
        public string Name
        {
            get;
            init;
        } = "";

        public string Folder
        {
            get;
            init;
        } = "";

        public string Command
        {
            get;
            init;
        } = "";

        public string Args
        {
            get;
            init;
        } = "";

        public ArgParam? ArgParam
        {
            get;
            init;
        } = null;
    }
}