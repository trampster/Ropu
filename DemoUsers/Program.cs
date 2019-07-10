using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DemoUsers
{

    public class NewUser
    {
        public string Email
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {

            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var client = new HttpClient(httpClientHandler))
                {                    
                    await AddUser(client, "Batman", "batman@dc.com", "password1");
                    await AddUser(client, "Superman", "souperman@dc.com", "password2");
                    await AddUser(client, "Green Lantin", "green.lantin@dc.com", "password3");
                    await AddUser(client, "Flash", "password4@dc.com", "password4");
                    await AddUser(client, "Wonder Woman", "wonder.woman@dc.com", "password5");
                }
            }
        }

        static async Task AddUser(HttpClient client, string name, string email, string password)
        {
            var user = new NewUser()
            {
                Name = name,
                Email = email,
                Password = password
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            var response = await client.PostAsync("http://localhost:5000/api/Users/Create", new StringContent(json, Encoding.UTF8, "application/json"));

            if(response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Failed to add user");
            }
        }
    }
}
