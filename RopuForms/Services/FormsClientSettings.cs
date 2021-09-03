using Ropu.Client;

namespace RopuForms.Services
{
    public class FormsClientSettings : IClientSettings
    {
        public uint? UserId { get; set; }
        public bool FakeMedia { get; set; }
        public string FileMediaSource { get; set; }
        public string WebAddress { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
