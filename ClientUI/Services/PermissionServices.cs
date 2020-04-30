using System.Threading.Tasks;
using Ropu.Gui.Shared.Services;

namespace Ropu.ClientUI.Services
{
    public class PermissionServices : IPermissionService
    {
        public async Task<bool> RequestAudioRecordPermission()
        {
            await Task.CompletedTask;
            return true;
        }
    }
}