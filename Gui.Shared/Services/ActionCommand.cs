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

        public event EventHandler CanExecuteChanged;

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
