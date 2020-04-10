using System;
using System.IO;
using System.Reflection;
using SkiaSharp;
using Xamarin.Forms;

namespace RopuForms.Services
{
    public class ImageService
    {
        readonly string _imageFolder;

        public ImageService()
        {
            var codeBase = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Console.WriteLine($"Codebase: {codeBase}");
            var imageFolder = Path.GetDirectoryName(codeBase);
            if (imageFolder == null) throw new IOException($"Could not get directory name for image folder path {codeBase}");
            _imageFolder = imageFolder;
            Console.WriteLine($"ImageFolder: {_imageFolder}");

        }

        public string ImageFolder => _imageFolder;

        public ImageSource Ropu
        {
            get
            {
                return ImageSource.FromResource("Ropu256.png");
            }
        }
        public ImageSource Knot => ImageSource.FromFile(Path.Combine(_imageFolder, "knot32.png"));
        public ImageSource Rope => ImageSource.FromFile(Path.Combine(_imageFolder, "rope32.png"));

        public SKImage RopeSKImage()
        {
            string resourceID = "RopuForms.rope.svg";
            Assembly assembly = GetType().GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceID))
            {
                return SKImage.FromEncodedData(stream);
            }
        }
    }
}
