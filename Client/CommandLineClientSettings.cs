using System;
using System.IO;
using Mono.Options;
using JsonSrcGen;
using System.Threading.Tasks;
using Ropu.Shared.Web;

namespace Ropu.Client
{
    [Json]
    public class ClientSettings : IClientSettings
    {
        uint? _userId = null;

        public string? Email
        {
            get;
            set;
        }

        [JsonIgnore]
        public string? Password
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

        public string? WebAddress
        {
            get;
            set;
        }
    }

    public interface ISettingsManager
    {
        Task SaveSettings();

        IClientSettings ClientSettings
        {
            get;
        }
    }

    public class CommandLineClientSettingsReader : ISettingsManager, ICredentialsProvider
    {
        FileSettingService? _fileSettingsService;
        readonly ClientSettings _clientSettings = new ClientSettings();

        public IClientSettings ClientSettings => _clientSettings;

        public string Email
        {
            get => _clientSettings.Email ?? "";
            set => _clientSettings.Email = value;
        }

        public string Password
        {
            get => _clientSettings.Password ?? "";
            set => _clientSettings.Password = value;
        }

        public Task SaveSettings()
        {
            if(_fileSettingsService == null)
            {
                throw new InvalidOperationException("You must call ParseArgs first");
            }
            return _fileSettingsService.Save(_clientSettings);
        }

        string GetDefaultConfigPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "Ropu", "config.json");
        }

        public bool ParseArgs(string[] args)
        {
            bool showHelp = false;

            string? email = null;
            string? password = null;
            bool fakeMedia = false;
            string? fileMediaSource = null;
            string? webAddress = null;
            string? configFile = null;

            var optionSet = new OptionSet () 
            {
                { "u|user=", "the {email} of this client",  v => email = v },
                { "p|password=", "the {password} of this client",  v => password = v },
                { "f|fake-media", "Don't do any media processing", v =>  fakeMedia = v != null },
                { "l|file-media=", "use file as media source", v =>  fileMediaSource = v },
                { "w|web-address=", "Address of the Ropu Web", v =>  webAddress = v },
                { "c|config=", "Json Config File (if set other options are ignored)", v =>  configFile = v },
                { "h|help",  "show this message and exit", v => showHelp = v != null }
            };
            
            optionSet.Parse(args);

            if(configFile != null)
            {
                _fileSettingsService = new FileSettingService(configFile);
                _fileSettingsService.ReadSettings(_clientSettings);
                return true;
            }

            if(showHelp)
            {
                ShowHelp(optionSet);
                return false;
            }

            var defaultConfigFilePath =  GetDefaultConfigPath();
            _fileSettingsService = new FileSettingService(defaultConfigFilePath);
            
            _fileSettingsService.ReadSettings(_clientSettings);

            if(fileMediaSource != null && !File.Exists(fileMediaSource))
            {
                Console.Error.WriteLine($"Could not find file {fileMediaSource}");
                return false;
            }

            if(email != null)
            {
                _clientSettings.Email = email;
            }
            if(password != null)
            {
                _clientSettings.Password = password;
            }
            if(webAddress != null)
            {
                _clientSettings.WebAddress = webAddress;
            }
            if(fileMediaSource != null)
            {
                _clientSettings.FileMediaSource = fileMediaSource;
            }
            _clientSettings.FakeMedia = fakeMedia;
            return true;
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