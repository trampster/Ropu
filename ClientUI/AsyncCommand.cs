using System;
using System.Threading.Tasks;
using Eto.Forms;

namespace Ropu.ClientUI
{
    public class AsyncCommand : Command
    {
        public AsyncCommand(Func<Task> func)
        {
            Executed += async (sender, args) =>
            {
                await func();
            };
        }
    }
}