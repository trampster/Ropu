using JsonSrcGen;

namespace Ropu.Client
{
    public interface IClientSettings
    {
        uint? UserId{get;set;}

        bool FakeMedia
        {
            get;
            set;
        }

        string? FileMediaSource
        {
            get;
            set;
        }

        string? WebAddress
        {
            get;
            set;
        }

        string? Email
        {
            get;
            set;
        }

        [JsonIgnore]
        string? Password
        {
            get;
            set;
        }
    }
}