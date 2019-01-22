using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ropu.ClientUI
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        
        protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string memberName = null)
        {
            if(!property.Equals(value))
            {
                property = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}