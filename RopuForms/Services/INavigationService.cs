using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RopuForms.Services
{
    public interface INavigationService
    {
        void Register<T>(Func<T> baseViewModelFactory) where T : Page;
        void AddRootPage(Page page);
        Task ShowModal<T>();
        Task Back();
    }
}
