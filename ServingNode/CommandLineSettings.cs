using System;
using Mono.Options;

namespace Ropu.ServingNode
{
    public class CommandLineSettings
    {
        public bool ParseArgs(string[] args)
        {
            bool showHelp = false;

            var optionSet = new OptionSet () 
            {
                { "u|user=", "the {email} of this client",  v => Email = v },
                { "p|password=", "the {password} of this client",  v => Password = v },
                { "h|help",  "show this message and exit", v => showHelp = v != null }
            };
            
            optionSet.Parse(args);

            if(showHelp)
            {
                ShowHelp(optionSet);
                return false;
            }

            return true;
        }

        void ShowHelp (OptionSet optionaSet)
        {
            Console.WriteLine ("Usage: ServingNode [OPTIONS]");
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
    }
}