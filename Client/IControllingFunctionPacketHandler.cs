using Ropu.Shared;

namespace Ropu.Client
{
    public interface IControllingFunctionPacketHandler
    {
        void RegistrationResponseReceived(Codec codec, ushort bitrate);
    }
}