using System;
using System.IO;
using System.Security.Cryptography;
using StackExchange.Redis;

namespace Ropu.Web.Services
{
    public class ImageService : IImageService
    {
        readonly ConnectionMultiplexer _connectionMultiplexer;

        public ImageService(ConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
            AddDefaultImages();
        }

        void AddDefaultImages()
        {
            var hash = Add(File.ReadAllBytes("../Icon/knot32.png"));
            DefaultUserImageHash = hash;
        }

        public string DefaultUserImageHash
        {
            get
            {
                IDatabase db = _connectionMultiplexer.GetDatabase();
                var defaultUserImageHash = db.StringGet($"ImageHash:defaultUser");
                return defaultUserImageHash;
            }
            private set
            {
                IDatabase db = _connectionMultiplexer.GetDatabase();
                db.StringSet($"ImageHash:defaultUser", value);
            }
        }

        public string Add(byte[] image)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();

            var hash = GenerateHash(image);
            string key = $"Image:{hash}";
            if(db.KeyExists(key))
            {
                return hash;
            }

            db.StringSet(key, image);

            return hash;
        }

        string GenerateHash(byte[] image)
        {
            char[] padding = { '=' };
            using (SHA256 hasher = SHA256.Create())
            {
                byte[] hashBytes = hasher.ComputeHash(image);
                string hash = Convert.ToBase64String(hashBytes)
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
                return hash;
            }
        }

        public byte[] Get(string hash)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var result = db.StringGet($"Image:{hash}");
            if(result.IsNull)
            {
                return null;
            }
            byte[] image = result;
            return image;
        }
    }
}