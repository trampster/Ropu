using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RopuForms.ViewModels;
using RopuForms.Views.TouchTracking;
using SkiaSharp;
using SkiaSharp.Views;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RopuForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PttPage : ContentPage
    {
        readonly PttViewModel _pttViewModel;
        public PttPage()
        {
            InitializeComponent();

            BindingContext = _pttViewModel = Inject.Injection.Resolve<PttViewModel>();
        }

        void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            canvisView.OnTouchEffectAction(sender, args);
        }
    }
}