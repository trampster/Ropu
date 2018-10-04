using System;
using System.Net;
using System.Net.Sockets;

namespace Ropu.ControllingFunction
{
    class Program
    {
        static void Main(string[] args)
        {
            var registra = new Registra();
            var controlProtocol = new ControlProtocol(5060);
            var controller = new Controller(controlProtocol, registra);
            controller.Run();
        }   
    }
}
