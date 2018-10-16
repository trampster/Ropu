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
    }
}