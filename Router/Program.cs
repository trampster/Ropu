﻿using System.ComponentModel.Design.Serialization;
using System.Net;
using Ropu.Router;
using Serilog;

Console.WriteLine("Ropu Router");

var arguments = Environment.GetCommandLineArgs();
if (arguments.Length != 2)
{
    Console.Error.WriteLine($"Usage: router {{port}}");
    return;
}

string portString = arguments[1];
if (!ushort.TryParse(portString, out ushort port))
{
    Console.Error.WriteLine($"First argument must be the port but was {portString}");
    return;
}

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var ballancerClient = new BalancerClient(
    logger,
    new IPEndPoint(IPAddress.Parse("192.168.1.115"), port),
    new IPEndPoint(IPAddress.Parse("192.168.1.115"), 2000),
    100);

await ballancerClient.RunAsync();

Console.WriteLine("Ropu Router Stopped");