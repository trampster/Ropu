using System;
using Mono.Options;

namespace Ropu.ServingNode
{
    public class Settings
    {
        public Settings(string email, string password)
        {
            Email = email;
            Password = password;
        }

        public string Email
        {
            get;
        }

        public string Password
        {
            get;
        }
    }

    public class CommandLineSettingsReader
    {
        public Settings? ParseArgs(string[] args)
        {
            bool showHelp = false;
            string? email = null;
            string? password = null;

            var optionSet = new OptionSet () 
            {
                { "u|user=", "the {email} of this client",  v => email = v },
                { "p|password=", "the {password} of this client",  v => password = v },
                { "h|help",  "show this message and exit", v => showHelp = v != null }
            };
            
            optionSet.Parse(args);

            if(showHelp || email == null || password == null)
            {
                ShowHelp(optionSet);
                return null;
            }

            return new Settings(email, password);
        }

        void ShowHelp (OptionSet optionaSet)
        {
            Console.WriteLine ("Usage: ServingNode [OPTIONS]");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            optionaSet.WriteOptionDescriptions (Console.Out);
        }
    }
}