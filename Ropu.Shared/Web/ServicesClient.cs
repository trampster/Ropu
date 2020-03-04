using System;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared.WebModels;

namespace Ropu.Shared.Web
{
    public class ServicesClient
    {
        readonly RopuWebClient _webClient;
        readonly ServiceType _serviceType;
        uint _userId = 0;

        public ServicesClient(RopuWebClient webClient, ServiceType serviceType)
        {
            _webClient = webClient;
            _serviceType = serviceType;
        }

        public async ValueTask<uint?> GetUserId(CancellationToken cancellationToken)
        {
            //get user Id
            while(!cancellationToken.IsCancellationRequested)
            {
                var response = await _webClient.Get<User>("api/Users/Current");
                if(response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    await Console.Error.WriteLineAsync($"Failed to get our user id service, status code {response.StatusCode}");
                    await Task.Delay(5000);
                    continue;
                }
                _userId = (await response.GetJson()).Id;
                return _userId;
            }
            return null;
        }

        public async Task ServiceRegistration(CancellationToken cancellationToken)
        {
            var serviceInfo = new ServiceInfo()
            {
                UserId = _userId,
                ServiceType = _serviceType,
            };

            while(!cancellationToken.IsCancellationRequested)
            {
                var response = await _webClient.Post<ServiceInfo, byte>("api/Services/Register", serviceInfo);
                if(!response.IsSuccessfulStatusCode)
                {
                    await Console.Error.WriteLineAsync("Failed to register service");
                    await Task.Delay(5000);
                    continue;
                }
                serviceInfo.ServiceId = await response.GetJson();
                await Task.Delay(5 * 60 * 1000);
            }
        }
    }
}