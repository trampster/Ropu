namespace Ropu.Web.Services
{
    public interface IImageService
    {
        byte[]? Get(string hash);
        string DefaultUserImageHash
        {
            get;
        }
        string DefaultGroupImageHash
        {
            get;
        }

        string Add(byte[] imageBytes);
    }
}