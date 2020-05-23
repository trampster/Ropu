using System;
using System.Windows.Input;

namespace Ropu.Gui.Shared.Services
{
    public class ActionCommand : ICommand
    {
        readonly Action _action;
        public ActionCommand(Action action)
        {
            _action = action;
        }

#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action();
        }
    }
}
