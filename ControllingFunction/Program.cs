using System;
using System.Net;
using System.Net.Sockets;

namespace Ropu.ContollingFunction
{
    class Program
    {
        static void Main(string[] args)
        {
            var registra = new Registra();
            var controlProtocol = new ControlProtocol(registra);
            controlProtocol.ProcessPackets();
        }   
    }
}
