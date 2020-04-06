using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using RopuForms.Inject;
using Ropu.Client;
using RopuForms.Droid.AAudio;

namespace RopuForms.Droid
{
    [Activity(Label = "RopuForms", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            Injection.RegisterTypes(RegisterTypes);

            LoadApplication(new App());
        }

        async void RegisterTypes(Injection injection)
        {
            injection.RegisterSingleton<IAudioSource>(i => new AAudioSource());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}