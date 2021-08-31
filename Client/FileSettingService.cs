using System.IO;
using Ropu.Client;
using JsonSrcGen;
using System;
using System.Threading.Tasks;

namespace Ropu.Client
{
    public class FileSettingService
    {
        readonly JsonConverter _jsonConverter = new JsonConverter();
        readonly string _file;

        public FileSettingService(string file)
        {
            _file = file;
        }
        
        public void ReadSettings(ClientSettings clientSetting)    
        {
            if(!File.Exists(_file))
            {
                Save(clientSetting);
            }
            _jsonConverter.FromJson(clientSetting, File.ReadAllBytes(_file));
        }

        public Task Save(ClientSettings clientSetting)
        {
            var directory = Path.GetDirectoryName(_file);
            if(!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var jsonBytes = _jsonConverter.ToJsonUtf8(clientSetting);
            return File.WriteAllBytesAsync(_file, jsonBytes.ToArray());
        }
    }
}