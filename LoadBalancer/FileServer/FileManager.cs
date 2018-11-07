using System.Collections.Generic;

namespace Ropu.LoadBalancer.FileServer
{
    public class FileManager
    {
        const int _mtu = 1500;
        ushort _nextFileId = 0;

        readonly Queue<FilePart> _availableParts = new Queue<FilePart>();
        readonly Queue<File> _availableFiles = new Queue<File>();
        readonly Dictionary<ushort, File> _files = new Dictionary<ushort, File>();

        public FilePart GetAvailablePart()
        {
            if(_availableParts.Count > 0)
            {
                return _availableParts.Dequeue();
            }
            return new FilePart(new byte[_mtu]);
        }

        public File GetAvailableFile()
        {
            if(_availableFiles.Count > 0)
            {
                var file =  _availableFiles.Dequeue();
                file.Reset();
                return file;
            }
            return new File();
        }

        public void MakeAvailable(ushort fileId)
        {
            if(_files.TryGetValue(fileId, out File file))
            {
                foreach(var part in file.Parts)
                {
                    part.Reset();
                    _availableParts.Enqueue(part);
                }
                file.Reset();
                _availableFiles.Enqueue(file);
            }
        }

        public ushort AddFile(File file)
        {
            ushort fileId = _nextFileId;
            _files.Add(_nextFileId, file);
            _nextFileId++;
            return fileId;
        }

        public File GetFile(ushort fileId)
        {
            if(_files.TryGetValue(fileId, out File file))
            {
                return file;
            }
            return null;
        }
    }
}