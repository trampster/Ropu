using System;
using System.Net;
using System.Threading.Tasks;

namespace Ropu.Shared.Web
{
    public class ImageClient
    {
        readonly RopuWebClient _webClient;
        public ImageClient(RopuWebClient webClient)
        {
            _webClient = webClient;
        }

        public async Task<byte[]> GetImage(string hash)
        {
            var response = await _webClient.Get<byte[]>($"api/Image/{hash}");
            if(response.StatusCode == HttpStatusCode.OK)
            {
                return await response.GetBytes();
            }
            Console.Error.WriteLine($"Failed to get image with hash {hash } Http Status Code {response.StatusCode}");
            return null;
        }
    }
}