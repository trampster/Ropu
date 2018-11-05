using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class ControllingFunctionClient : IControlPacketParser
    {
        readonly ProtocolSwitch _protocolSwitch;
        IControllingFunctionPacketHandler _controllingFunctionHandler;

        public ControllingFunctionClient(ProtocolSwitch protocolSwitch)
        {
            _protocolSwitch = protocolSwitch;
            _protocolSwitch.SetControlPacketParser(this);
        }

        public void SetControllingFunctionHandler(IControllingFunctionPacketHandler controllingFunctionHandler)
        {
            _controllingFunctionHandler = controllingFunctionHandler;
        }

        public void Register(uint userId, IPEndPoint remoteEndPoint)
        {
            var sendBuffer = _protocolSwitch.SendBuffer();
            //packet type (byte)
            sendBuffer[0] = (byte)CombinedPacketType.Registration;
            // User ID (uint32)
            sendBuffer.WriteUint(userId, 1);

            _protocolSwitch.Send(5, remoteEndPoint);
        }

        public void StartGroupCall(uint userId, ushort groupId, IPEndPoint remoteEndPoint)
        {
            var sendBuffer = _protocolSwitch.SendBuffer();
            //packet type (byte)
            sendBuffer[0] = (byte)CombinedPacketType.StartGroupCall;
            // User ID (uint32)
            sendBuffer.WriteUint(userId, 1);
            // Group ID (uint16)
            sendBuffer.WriteUshort(groupId, 5);

            _protocolSwitch.Send(7, remoteEndPoint);
        }

        public void ParseRegistrationResponse(Span<byte> data)
        {
            // User ID (uint32), skip
            // Codec (byte) (defined via an enum, this is the codec/bitrate used by the system, you must support it, this is required so the server doesn’t have to transcode, which is an expensive operation)
            Codec codec = (Codec)data[5];
            // Bitrate (uint16)
            ushort bitrate = data.Slice(6).ParseUshort();
            _controllingFunctionHandler?.HandleRegistrationResponseReceived(codec, bitrate);
        }

        public void ParseCallEnded(Span<byte> data)
        {
            throw new NotImplementedException();
        }

        public void ParseCallStarted(Span<byte> data)
        {
            // User Id (uint32), skip
            // Group ID (uint16)
            uint groupId = data.Slice(5).ParseUshort();
            // Call ID (uint16) unique identifier for the call, to be included in the media stream
            ushort callId = data.Slice(7).ParseUshort();
            // Media Endpoint (4 bytes IP Address, 2 bytes port)
            var mediaEndpoint = data.Slice(9).ParseIPEndPoint();
            // Floor Control Endpoint (4 bytes IP Address, 2 bytes port)
            var floorControlEndpoint = data.Slice(15).ParseIPEndPoint();
            _controllingFunctionHandler?.HandleCallStarted(groupId, callId, mediaEndpoint, floorControlEndpoint);
        }

        public void ParseCallStartFailed(Span<byte> data)
        {
            //User Id (uint) don't parse this is it should only arrive if it's for us, it will just waste cpu cycles

            //reason (byte)
            CallFailedReason reason = (CallFailedReason)data[5];

            _controllingFunctionHandler?.HandleCallStartFailed(reason);
        }
    }
}