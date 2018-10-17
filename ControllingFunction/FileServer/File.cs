using System;
using System.Collections.Generic;

namespace Ropu.ControllingFunction.FileServer
{
    public class File
    {
        readonly List<FilePart> _parts = new List<FilePart>();

        public void AddPart(FilePart part)
        {
            _parts.Add(part);
        }

        public void Reset()
        {
            _parts.Clear();
        }

        public IEnumerable<FilePart> Parts => _parts;

        public int NumberOfParts => _parts.Count;
    }
}