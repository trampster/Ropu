using System;
using Mono.Options;

namespace Ropu.Client
{
    public class CommandLineClientSettings : IClientSettings
    {
        uint _userId = 1234;

        public bool ParseArgs(string[] args)
        {
            bool showHelp = false;
            string userIdString = "";

            var optionSet = new OptionSet () 
            {
                { "n|userid=", "the {User ID} of this client.",  v => userIdString = v },
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

            return true;
        }

        void ShowHelp (OptionSet optionaSet)
        {
            Console.WriteLine ("Usage: Client [OPTIONS]");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            optionaSet.WriteOptionDescriptions (Console.Out);
        }
        public uint UserId => _userId;
    }
}