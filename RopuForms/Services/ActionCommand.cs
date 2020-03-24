using System;
using Xamarin.Forms;

namespace RopuForms.Services
{
    public class ActionCommand : Command
    {
        public ActionCommand(Action action) : base(action)
        {
        }
    }
}
