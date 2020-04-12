using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ropu.Shared.WebModels;

namespace Ropu.Shared.Web
{
    public class RopuWebClient : IDisposable
    {
        readonly HttpClient _httpClient;
        readonly HttpClientHandler _httpClientHandler;
        string? _jwt;
        readonly CredentialsProvider _credentialsProvider;

        public RopuWebClient(string uri, CredentialsProvider credentialsProvider)
        {
            _credentialsProvider = credentialsProvider;
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
            _httpClient = new HttpClient(_httpClientHandler);
            _httpClient.BaseAddress = new Uri(uri);
        }

        public string ServerAddress
        {
            get => _httpClient.BaseAddress.ToString();
            set => _httpClient.BaseAddress = new Uri(value);
        }

        class JwtResponse
        {
            public JwtResponse()
            {
                Token = "";
            }
            
            public string Token
            {
                get;
                set;
            }
        }

        public async Task WaitForLogin()
        {
            await Task.Run(() => _manualResetEvent.WaitOne());
        }

        readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        public async ValueTask<bool> Login()
        {
            var credentials = new Credentials(){Email = _credentialsProvider.Email, Password = _credentialsProvider.Password};
            var json = JsonConvert.SerializeObject(credentials);

            var response = await _httpClient.PostAsync($"api/Login", new StringContent(json, Encoding.UTF8, "application/json"));
            if(response.StatusCode != HttpStatusCode.OK)
            {
                _manualResetEvent.Reset();
                return false;
            }
            _jwt = Newtonsoft.Json.JsonConvert.DeserializeObject<JwtResponse>(await response.Content.ReadAsStringAsync()).Token;
            if(_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _jwt);

            _manualResetEvent.Set();
            return true;
        }

        public async ValueTask<HttpResponseMessage> Post<T>(string uri, T payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            return await Do(() => _httpClient.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json")));
        }

        public async ValueTask<Response<R>> Post<T, R>(string uri, T payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var response = await Do(() => _httpClient.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json")));
            return new Response<R>(response);
        }

        public async ValueTask<Response<T>> Get<T>(string uri)
        {
            var response = await Do(() => _httpClient.GetAsync(uri)).ConfigureAwait(false);
            return new Response<T>(response);
        }

        async ValueTask<HttpResponseMessage> Do(Func<Task<HttpResponseMessage>> func)
        {
            var response = await func().ConfigureAwait(false);
            if(response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await Login().ConfigureAwait(false);
                return await func().ConfigureAwait(false);
            }
            return response;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
                _httpClientHandler.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}