using System.Threading.Tasks;

namespace Ropu.Gui.Shared.Services
{
    public interface INavigator
    {
        Task ShowModal<T>();
        Task Back();
    }
}
