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

        public ServicesClient(RopuWebClient webClient, ServiceType serviceType)
        {
            _webClient = webClient;
            _serviceType = serviceType;
        }

        public async Task RegisterService(CancellationToken cancellationToken)
        {
            //get user Id
            uint userId = 0;
            while(!cancellationToken.IsCancellationRequested)
            {
                var response = await _webClient.Get<User>("api/Users/Current");
                if(response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    await Console.Error.WriteLineAsync($"Failed to get our user id service, status code {response.StatusCode}");
                    await Task.Delay(5000);
                    continue;
                }
                userId = (await response.GetJson()).Id;
                break;
            }

            var serviceInfo = new ServiceInfo()
            {
                UserId = userId,
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