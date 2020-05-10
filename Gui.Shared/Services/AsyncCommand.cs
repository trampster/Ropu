using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ropu.Gui.Shared.Services
{
    public class AsyncCommand : ICommand
    {
        readonly Func<Task> _func;
        public AsyncCommand(Func<Task> func)
        {
            _func = func;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            await _func();
        }
    }

    public class AsyncCommand<T> : ICommand
    {
        readonly Func<T, Task> _func;
        public AsyncCommand(Func<T, Task> func)
        {
            _func = func;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            await _func((T)parameter);
        }
    }
}
