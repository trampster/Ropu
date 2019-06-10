namespace Ropu.Web.Services
{
    public interface IImageService
    {
        byte[] Get(string hash);
        string DefaultUserImageHash
        {
            get;
        }
    }
}