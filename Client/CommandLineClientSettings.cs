using System;
using System.IO;
using System.Net;
using Mono.Options;

namespace Ropu.Client
{
    public class CommandLineClientSettings : IClientSettings
    {
        uint _userId = 1234;

        public event EventHandler UserIdChanged;

        public bool ParseArgs(string[] args)
        {
            bool showHelp = false;
            string userIdString = "";
            bool fakeMedia = false;
            string loadBalancerAddress = null;

            var optionSet = new OptionSet () 
            {
                { "n|userid=", "the {User ID} of this client",  v => userIdString = v },
                { "f|fake-media", "Don't do any media processing", v =>  fakeMedia = v != null },
                { "l|file-media=", "use file as media source", v =>  FileMediaSource = v },
                { "b|load-balancer=", "IP Address of load balancer", v => loadBalancerAddress = v},
                { "h|help",  "show this message and exit", v => showHelp = v != null }
            };
            
            optionSet.Parse(args);

            if(showHelp)
            {
                ShowHelp(optionSet);
                return false;
            }

            if(userIdString != "")
            {
                if(!uint.TryParse(userIdString, out _userId))
                {
                    Console.Error.WriteLine("User ID must be a number");
                    return false;
                }
            }

            if(FileMediaSource != null && !File.Exists(FileMediaSource))
            {
                Console.Error.WriteLine($"Could not find file {FileMediaSource}");
                return false;
            }

            if(loadBalancerAddress == null)
            {
                Console.Error.WriteLine($"You must specify the Load Balancer IP Address");
                return false;
            }

            if(!IPAddress.TryParse(loadBalancerAddress, out IPAddress loadBalancerIPAddress))
            {
                Console.Error.WriteLine($"Load Balancer address is invalid");
                return false;
            }
            LoadBalancerIPAddress = loadBalancerIPAddress;

            FakeMedia = fakeMedia;

            return true;
        }

        void ShowHelp (OptionSet optionaSet)
        {
            Console.WriteLine ("Usage: Client [OPTIONS]");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            optionaSet.WriteOptionDescriptions (Console.Out);
        }
        public uint UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                UserIdChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string FileMediaSource
        {
            get;
            set;
        }

        public bool FakeMedia
        {
            get;
            set;
        }

        public IPAddress LoadBalancerIPAddress
        {
            get;
            set;
        }
    }
}