using System;
using Mono.Options;

namespace Ropu.LoadBalancer
{
    public class CommandLineSettings
    {
        public CommandLineSettings(string email, string password, string publicIPEndpoint)
        {
            Email = email;
            Password = password;
            PublicIPEndpoint = publicIPEndpoint;
        }

        public string Email
        {
            get;
        }

        public string Password
        {
            get;
        }

        public string PublicIPEndpoint
        {
            get;
        }
    }

    public class CommandLineSettingsReader
    {
        public CommandLineSettings? ParseArgs(string[] args)
        {
            bool showHelp = false;
            string? email = null;
            string? password = null;
            string? publicIPEndpoint = null;
            var optionSet = new OptionSet () 
            {
                { "u|user=", "the {email} of this client",  v => email = v },
                { "p|password=", "the {password} of this client",  v => password = v },
                { "i|ipendpoint=", "the public endpoint", v => publicIPEndpoint = v},
                { "h|help",  "show this message and exit", v => showHelp = v != null }
            };
            
            optionSet.Parse(args);

            if(email == null) 
            {
                Console.Error.WriteLine("email command line arg is required");
            }
            if(password == null) 
            {
                Console.Error.WriteLine("password command line arg is required");
            }
            if(publicIPEndpoint == null) 
            {
                Console.Error.WriteLine("ipaddress command line arg is required");
            }

            if(showHelp || email == null || password == null || publicIPEndpoint == null)
            {
                ShowHelp(optionSet);
                return null;
            }

            return new CommandLineSettings(email, password, publicIPEndpoint);
        }

        void ShowHelp (OptionSet optionaSet)
        {
            Console.WriteLine ("Usage: LoadBalancer [OPTIONS]");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            optionaSet.WriteOptionDescriptions (Console.Out);
        }
    }
}