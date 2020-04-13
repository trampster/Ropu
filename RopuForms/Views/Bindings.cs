using System;
using Xamarin.Forms;

namespace RopuForms.Views
{
    public static class Bindings
    {
        public delegate void PropertyChanged<TControl, TProperty>(TControl control, TProperty oldValue, TProperty newValue);

        public static BindableProperty Create<TControl, TProperty>(string propertyName, PropertyChanged<TControl, TProperty> propertyChanged)
        {
            return BindableProperty.Create(
                propertyName: propertyName,
                returnType: typeof(TProperty),
                declaringType: typeof(TControl),
                defaultValue: null,
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: (bindable, oldValue, newValue) => propertyChanged((TControl)(object)bindable, (TProperty)oldValue, (TProperty)newValue));
        }
    }
}
