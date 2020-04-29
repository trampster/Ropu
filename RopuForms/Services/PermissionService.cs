using System.Threading.Tasks;
using Ropu.Gui.Shared.Services;
using Xamarin.Essentials;

namespace RopuForms.Services
{
    public class PermissionService : IPermissionService
    {
        public async Task<bool> RequestAudioRecordPermission()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Speech>();
            if (status != PermissionStatus.Granted)
            {
                var statusResult = await Permissions.RequestAsync<Permissions.Speech>();
                return statusResult == PermissionStatus.Granted;
            }
            return true;
        }
    }
}
