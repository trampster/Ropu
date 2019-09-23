using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Ropu.Shared.Web
{
    public class ImageClient
    {
        readonly RopuWebClient _webClient;
        readonly Dictionary<string, byte[]> _imageCache = new Dictionary<string, byte[]>();
        public ImageClient(RopuWebClient webClient)
        {
            _webClient = webClient;
        }

        public async Task<byte[]> GetImage(string hash)
        {
            if(_imageCache.TryGetValue(hash, out byte[] imageBytes))
            {
                return imageBytes;
            }
            Console.WriteLine("GetImage");
            var response = await _webClient.Get<byte[]>($"api/Image/{hash}");
            if(response.StatusCode == HttpStatusCode.OK)
            {
                imageBytes = await response.GetBytes();
                _imageCache[hash] = imageBytes;
                return imageBytes;
            }
            Console.Error.WriteLine($"Failed to get image with hash {hash } Http Status Code {response.StatusCode}");
            return null;
        }
    }
}