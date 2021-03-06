using System;
using System.IO;
using System.Net;
using Mono.Options;

namespace Ropu.Client
{
    public class ClientSettings : IClientSettings
    {
        uint? _userId = null;

        public ClientSettings(string email, string password, string webAddress)
        {
            Email = email;
            Password = password;
            WebAddress = webAddress;
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

        public string? FileMediaSource
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

    public class CommandLineClientSettingsReader
    {
        public ClientSettings? ParseArgs(string[] args)
        {
            bool showHelp = false;

            string? email = null;
            string? password = null;
            bool fakeMedia = false;
            string? fileMediaSource = null;
            string? webAddress = null;

            var optionSet = new OptionSet () 
            {
                { "u|user=", "the {email} of this client",  v => email = v },
                { "p|password=", "the {password} of this client",  v => password = v },
                { "f|fake-media", "Don't do any media processing", v =>  fakeMedia = v != null },
                { "l|file-media=", "use file as media source", v =>  fileMediaSource = v },
                { "w|web-address=", "Address of the Ropu Web", v =>  webAddress = v },
                { "h|help",  "show this message and exit", v => showHelp = v != null }
            };
            
            optionSet.Parse(args);

            if(webAddress == null)
            {
                Console.Error.WriteLine("Web address is required");
                showHelp = true;
            }

            if(showHelp || email == null || password == null || webAddress == null)
            {
                ShowHelp(optionSet);
                return null;
            }

            if(fileMediaSource != null && !File.Exists(fileMediaSource))
            {
                Console.Error.WriteLine($"Could not find file {fileMediaSource}");
                return null;
            }

            return new ClientSettings(email, password, webAddress)
            {
                FileMediaSource = fileMediaSource,
                FakeMedia = fakeMedia
            };
        }

        void ShowHelp (OptionSet optionaSet)
        {
            Console.WriteLine ("Usage: Client [OPTIONS]");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            optionaSet.WriteOptionDescriptions (Console.Out);
        }

    }
}