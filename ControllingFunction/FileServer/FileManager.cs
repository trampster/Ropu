using System.Collections.Generic;

namespace Ropu.ControllingFunction.FileServer
{
    public class FileManager
    {
        const int _mtu = 1500;
        ushort _nextFileId = 0;

        readonly Queue<FilePart> _available = new Queue<FilePart>();
        readonly Dictionary<ushort, File> _files = new Dictionary<ushort, File>();

        public FilePart GetAvailablePart()
        {
            if(_available.Count > 0)
            {
                return _available.Dequeue();
            }
            return new FilePart(new byte[_mtu]);
        }

        public ushort AddFile(File file)
        {
            ushort fileId = _nextFileId;
            _files.Add(_nextFileId, file);
            _nextFileId++;
            return fileId;
        }
    }
}