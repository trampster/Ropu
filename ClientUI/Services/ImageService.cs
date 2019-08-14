using System;
using System.IO;
using Eto.Drawing;

namespace Ropu.ClientUI.Services
{
    public class ImageService
    {
        readonly string _imageFolder;

        public ImageService()
        {
            var codeBase = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Console.WriteLine($"Codebase: {codeBase}");
            _imageFolder = Path.GetDirectoryName(codeBase);
            Console.WriteLine($"ImageFolder: {_imageFolder}");

        }

        public string ImageFolder => _imageFolder;

        public Icon Ropu => new Icon(Path.Combine(_imageFolder, "Ropu.ico"));
        public Bitmap Knot => new Bitmap(Path.Combine(_imageFolder, "knot32.png"));
        public Bitmap Rope => new Bitmap(Path.Combine(_imageFolder, "rope32.png"));
    }
}