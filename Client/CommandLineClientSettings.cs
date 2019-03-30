using System;
using System.IO;
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

            var optionSet = new OptionSet () 
            {
                { "n|userid=", "the {User ID} of this client",  v => userIdString = v },
                { "f|fakemedia", "Don't do any media processing", v =>  fakeMedia = v != null },
                { "l|filemedia=", "use file as media source", v =>  FileMediaSource = v },
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
    }
}