using System.Threading.Tasks;

namespace Ropu.Gui.Shared.Services
{
    public interface IPermissionService
    {
        Task<bool> RequestAudioRecordPermission();
    }
}
