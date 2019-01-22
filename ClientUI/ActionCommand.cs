using System;
using Eto.Forms;

namespace Ropu.ClientUI
{
    public class ActionCommand : Command
    {
        public ActionCommand(Action action)
        {
            Executed += (sender, args) =>
            {
                action();
            };
        }
    }
}