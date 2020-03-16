using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using RopuForms.Views;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(SKGLView), typeof(RopuForms.Droid.PageRender))]
namespace RopuForms.Droid
{
    public class PageRender : SkiaSharp.Views.Forms.SKGLViewRenderer
    {
        public PageRender(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<SKGLView> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement == null)
            {
                if (!e.NewElement.GestureRecognizers.Any())
                    return;

                if (e.NewElement.GestureRecognizers.Any(x => x.GetType() == typeof(PressedGestureRecognizer)
                                                            || x.GetType() == typeof(ReleasedGestureRecognizer)))
                    Control.Touch += Control_Touch;

            }
        }

        private void Control_Touch(object sender, TouchEventArgs e)
        {
            switch (e.Event.Action)
            {
                case MotionEventActions.Down:
                    foreach (var recognizer in this.Element.GestureRecognizers.Where(x => x.GetType() == typeof(PressedGestureRecognizer)))
                    {
                        var gesture = recognizer as PressedGestureRecognizer;
                        if (gesture != null)
                            if (gesture.Command != null)
                                gesture.Command.Execute(gesture.CommandParameter);
                    }
                    break;

                case MotionEventActions.Up:
                    foreach (var recognizer in this.Element.GestureRecognizers.Where(x => x.GetType() == typeof(ReleasedGestureRecognizer)))
                    {
                        var gesture = recognizer as ReleasedGestureRecognizer;
                        if (gesture != null)
                            if (gesture.Command != null)
                                gesture.Command.Execute(gesture.CommandParameter);
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
