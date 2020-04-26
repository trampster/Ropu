using System.Threading.Tasks;

namespace Ropu.Gui.Shared.Services
{
    public interface INavigator
    {
        Task ShowModal<T>();

        Task Show<T>();

        Task Back();

        Task PopModal();
    }
}
