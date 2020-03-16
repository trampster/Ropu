using System.Windows.Input;
using Xamarin.Forms;

namespace RopuForms.Views
{
    public class ReleasedGestureRecognizer : Element, IGestureRecognizer
    {
        public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(ReleasedGestureRecognizer), (object)null, BindingMode.OneWay, (BindableProperty.ValidateValueDelegate)null, (BindableProperty.BindingPropertyChangedDelegate)null, (BindableProperty.BindingPropertyChangingDelegate)null, (BindableProperty.CoerceValueDelegate)null, (BindableProperty.CreateDefaultValueDelegate)null);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(object), typeof(ReleasedGestureRecognizer), (object)null, BindingMode.TwoWay, (BindableProperty.ValidateValueDelegate)null, (BindableProperty.BindingPropertyChangedDelegate)null, (BindableProperty.BindingPropertyChangingDelegate)null, (BindableProperty.CoerceValueDelegate)null, (BindableProperty.CreateDefaultValueDelegate)null);

        public ICommand Command
        {
            get { return (ICommand)this.GetValue(CommandProperty); }
            set { this.SetValue(CommandProperty, (object)value); }
        }

        public object CommandParameter
        {
            get { return this.GetValue(CommandParameterProperty); }
            set { this.SetValue(CommandParameterProperty, value); }
        }

    }
}
