using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ropu.Shared.Web
{
    public class Response<T>
    {
        readonly HttpResponseMessage _response;
        public Response(HttpResponseMessage message)
        {
            _response = message;
        }
        public HttpStatusCode StatusCode => _response.StatusCode;
        
        public async ValueTask<T> GetJson()
        {
            var json = await _response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async ValueTask<byte[]> GetBytes()
        {
            var bytes = await _response.Content.ReadAsByteArrayAsync();
            return bytes;
        }

        public async ValueTask<string> GetString()
        {
            return await _response.Content.ReadAsStringAsync();
        }

        public bool IsSuccessfulStatusCode => _response.IsSuccessStatusCode;
    }
}