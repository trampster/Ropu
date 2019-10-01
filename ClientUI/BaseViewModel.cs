using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Ropu.ClientUI
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string memberName = "")
        {
            if(property == null || !property.Equals(value))
            {
                property = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
            }
        }

        protected void RaisePropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }

        public virtual Task Initialize()
        {
            return Task.CompletedTask;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}