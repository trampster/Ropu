using Ropu.Client;

namespace RopuForms.Services
{
    public class FormsClientSettings : IClientSettings
    {
        public uint? UserId { get; set; }
        public bool FakeMedia { get; set; }
        public string FileMediaSource { get; set; }
    }
}
