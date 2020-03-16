using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RopuForms.Services
{
    class AsyncCommand : Command
    {
        public AsyncCommand(Func<Task> func) : 
            base(async () =>
            {
                await func();
            })
        {
        }
    }
}
