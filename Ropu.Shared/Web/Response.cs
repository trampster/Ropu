using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ropu.Shared.Web
{
    public class Response
    {
        protected readonly HttpResponseMessage? _response;
        readonly HttpRequestException? _exception;

        public Response(HttpResponseMessage message)
        {
            _response = message;
            _exception = null;
        }

        public Response(HttpRequestException exception)
        {
            _response = null;
            _exception = exception;
        }

        public HttpStatusCode? StatusCode => _response?.StatusCode;

        public async ValueTask<byte[]> GetBytes()
        {
            if(_response == null) throw new InvalidOperationException("No response was received");
            var bytes = await _response.Content.ReadAsByteArrayAsync();
            return bytes;
        }

        public async ValueTask<string> GetString()
        {
            if(_response == null) throw new InvalidOperationException("No response was received");
            return await _response.Content.ReadAsStringAsync();
        }

        public bool IsSuccessful 
        {
            get
            {
                if(_exception != null) return false;
                if(_response == null) return false;
                return _response.IsSuccessStatusCode;
            }
        }

        public string FailureReason
        {
            get
            {
                if(Exception != null)
                {
                    return Exception.ToString();
                }
                return _response == null ? "" : _response.ReasonPhrase;
            }
        }

        public HttpRequestException? Exception => _exception;
    }

    public class Response<T> : Response
    {
        public Response(HttpResponseMessage message) : base(message)
        {
        }

        public Response(HttpRequestException exception): base(exception)
        {
        }

        public async ValueTask<T> GetJson()
        {
            if(_response == null) throw new InvalidOperationException("No response was received");
            var json = await _response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}