using System;
using System.Net;

namespace Ropu.ControllingFunction
{
    public class Controller : IControlMessageHandler
    {
        readonly ControlProtocol _controlProtocol;
        readonly Registra _registra;

        public Controller(ControlProtocol controlProtocol, Registra registra)
        {
            _controlProtocol = controlProtocol;
            _controlProtocol.SetMessageHandler(this);
            _registra = registra;
        }

        public void Run()
        {
            _controlProtocol.ProcessPackets();
        }

        public void Registration(uint userId, ushort rtpPort, ushort floorControlPort, IPEndPoint controlEndpoint)
        {
            var registration = new Registration(userId, rtpPort, floorControlPort, controlEndpoint);
            _registra.Register(registration);
            _controlProtocol.SendRegisterResponse(registration);
        }

        public void StartGroupCall(uint userId, uint groupId)
        {
            Console.WriteLine($"Received StartGroupCall request from user {userId} for group {groupId}.");
        }
    }
}