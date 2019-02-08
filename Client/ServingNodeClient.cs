using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class ServingNodeClient : IControlPacketParser
    {
        readonly ProtocolSwitch _protocolSwitch;
        IControllingFunctionPacketHandler _controllingFunctionHandler;

        public ServingNodeClient(ProtocolSwitch protocolSwitch)
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
            sendBuffer[0] = (byte)RopuPacketType.Registration;
            // User ID (uint32)
            sendBuffer.WriteUint(userId, 1);

            _protocolSwitch.Send(5, remoteEndPoint);
        }

        public void SendHeartbeat(uint userId, IPEndPoint remoteEndPoint)
        {
            var sendBuffer = _protocolSwitch.SendBuffer();
            //packet type (byte)
            sendBuffer[0] = (byte)RopuPacketType.Heartbeat;
            // User ID (uint32)
            sendBuffer.WriteUint(userId, 1);

            _protocolSwitch.Send(5, remoteEndPoint);
        }

        public void StartGroupCall(uint userId, ushort groupId, IPEndPoint remoteEndPoint)
        {
            var sendBuffer = _protocolSwitch.SendBuffer();
            //packet type (byte)
            sendBuffer[0] = (byte)RopuPacketType.StartGroupCall;
            // Group ID (uint16)
            sendBuffer.WriteUshort(groupId, 1);
            // User ID (uint32)
            sendBuffer.WriteUint(userId, 3);
            

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

        public void ParseHeartbeatResponse(Span<byte> data)
        {
            _controllingFunctionHandler?.HandleHeartbeatResponseReceived();
        }

        public void ParseNotRegistered(Span<byte> data)
        {
            _controllingFunctionHandler?.HandleNotRegisteredReceived();
        }

        public void ParseCallEnded(Span<byte> data)
        {
            ushort groupId = data.Slice(1).ParseUshort();
            _controllingFunctionHandler?.HandleCallEnded(groupId);
        }

        public void ParseCallStartFailed(Span<byte> data)
        {
            //User Id (uint) don't parse this is it should only arrive if it's for us, it will just waste cpu cycles

            //reason (byte)
            CallFailedReason reason = (CallFailedReason)data[5];

            _controllingFunctionHandler?.HandleCallStartFailed(reason);
        }

        public void Deregister(uint userId, IPEndPoint servingNodeEndpoint)
        {
            var sendBuffer = _protocolSwitch.SendBuffer();
            //packet type (byte)
            sendBuffer[0] = (byte)RopuPacketType.Deregister;
            // User ID (uint32)
            sendBuffer.WriteUint(userId, 1);

            _protocolSwitch.Send(5, servingNodeEndpoint);
        }

        public void SendFloorReleased(ushort callGroup, IPEndPoint servingNodeEndpoint)
        {
            var sendBuffer = _protocolSwitch.SendBuffer();
            //packet type (byte)
            sendBuffer[0] = (byte)RopuPacketType.FloorReleased;
            // User ID (uint32)
            sendBuffer.WriteUshort(callGroup, 1);

            _protocolSwitch.Send(3, servingNodeEndpoint);
        }

        public void ParseDeregisterResponse(Span<byte> data)
        {
            _controllingFunctionHandler?.HandleRegisterResponse();
        }

        public void ParseFloorTaken(Span<byte> data)
        {
            // Group ID (uint16)
            ushort groupId = data.Slice(1).ParseUshort();
            // User Id (uint32), skip
            uint userId = data.Slice(3).ParseUint();
            
            _controllingFunctionHandler?.HandleFloorTaken(groupId, userId);
        }

        public void ParseFloorIdle(Span<byte> data)
        {
            // Group ID (uint16)
            ushort groupId = data.Slice(1).ParseUshort();
            
            _controllingFunctionHandler?.HandleFloorIdle(groupId);
        }
    }
}