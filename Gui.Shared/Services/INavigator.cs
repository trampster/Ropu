using System;
using System.Threading.Tasks;

namespace Ropu.Gui.Shared.Services
{
    public interface INavigator
    {
        Task ShowModal<T>();

        Task ShowModal<ViewModelT, ParamT>(ParamT? param) where ParamT : class;

        Task Show<T>();

        Task Back();

        Task PopModal();

        Task ShowPttView();

        //new navigate system
        Task Navigate(string viewName, bool addToBackStack = true);
        Task NavigateBack();
    }
}
