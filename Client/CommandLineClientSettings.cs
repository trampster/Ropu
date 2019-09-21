using System;
using System.IO;
using System.Net;
using Mono.Options;

namespace Ropu.Client
{
    public class CommandLineClientSettings : IClientSettings
    {
        uint? _userId = null;

        public bool ParseArgs(string[] args)
        {
            bool showHelp = false;
            bool fakeMedia = false;

            var optionSet = new OptionSet () 
            {
                { "u|user=", "the {email} of this client",  v => Email = v },
                { "p|password=", "the {password} of this client",  v => Password = v },
                { "f|fake-media", "Don't do any media processing", v =>  fakeMedia = v != null },
                { "l|file-media=", "use file as media source", v =>  FileMediaSource = v },
                { "w|web-address=", "Address of the Ropu Web", v =>  WebAddress = v },
                { "h|help",  "show this message and exit", v => showHelp = v != null }
            };
            
            optionSet.Parse(args);

            if(WebAddress == null)
            {
                Console.Error.WriteLine("Web address is required");
                showHelp = true;
            }

            if(showHelp)
            {
                ShowHelp(optionSet);
                return false;
            }

            if(FileMediaSource != null && !File.Exists(FileMediaSource))
            {
                Console.Error.WriteLine($"Could not find file {FileMediaSource}");
                return false;
            }

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

        public string Email
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        public uint? UserId
        {
            get => _userId;
            set
            {
                _userId = value;
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

        public string WebAddress
        {
            get;
            set;
        }
    }
}