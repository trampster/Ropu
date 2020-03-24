﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RopuForms.Views.TouchTracking;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace RopuForms.Views
{
    public class PttControl : SKGLView
    {
        readonly PttCircle _circle;
        readonly TransmittingIndicator _transmittingIndicator;
        readonly Action _transmittingAnimationAction;
        readonly Task _animationTask;

        public PttControl()
        {
            _circle = new PttCircle();
            _circle.PenWidth = 30;
            _transmittingIndicator = new TransmittingIndicator();
            _transmittingIndicator.Hidden = true;

            _transmittingAnimationAction = AnimateTransmitting;
            _animationTask = RunAnimations();
        }   

        protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);
        
            var surface = e.Surface;
            var canvas = surface.Canvas;
            canvas.Clear();

            const int padding = 5;

            _circle.Text = "Avengers";

            int radius = DrawPttCircle(canvas, 50, padding);

            _transmittingIndicator.X = (int)(CanvasSize.Width / 2);
            _transmittingIndicator.Y = (int)(CanvasSize.Height / 2);
            _transmittingIndicator.MinRadius = radius;
            _transmittingIndicator.MaxRadius = radius + (radius / 2);
            _transmittingIndicator.Draw(canvas);
        }

        int DrawPttCircle(SKCanvas graphics, int topSpace, int padding)
        {
            var size = CanvasSize;

            float heightAvailable = size.Height - topSpace * 2;
            float diameter = Math.Min(size.Width - (padding * 2), heightAvailable);

            _circle.X = (int)(size.Width / 2);
            _circle.Y = (int)(size.Height / 2);
            _circle.Radius = (int)(diameter / 2);
            _circle.Draw(graphics);

            return _circle.Radius;
        }

        List<Action> _animationUpdates = new List<Action>();
        ManualResetEvent _animationNeeded = new ManualResetEvent(false);

        void AddAnimation(Action action)
        {
            _animationUpdates.Add(action);
            _animationNeeded.Set();
        }

        void RemoveAnimation(Action action)
        {
            _animationUpdates.Remove(action);
            if (_animationUpdates.Count == 0)
            {
                _animationNeeded.Reset();
            }
        }

        void AnimateTransmitting()
        {
            float fraction = _transmittingIndicator.AnimationFraction;
            fraction += 0.01f;
            if (fraction > 1) fraction = 0;
            _transmittingIndicator.AnimationFraction = fraction;
        }

        async Task RunAnimations()
        {
            while (true)
            {
                await Task.Delay(16);
                if (_animationUpdates.Count == 0)
                {
                    await Task.Run(() => _animationNeeded.WaitOne());
                }
                foreach (var update in _animationUpdates)
                {
                    update();
                }
                if (_animationUpdates.Count != 0)
                {
                    InvalidateSurface();
                }
            }
        }

        readonly List<long> _buttonFingers = new List<long>();

        bool IsInCircle(SKPoint point)
        {
            //find the circle center
            var circleCenter = new SKPoint((float)CanvasSize.Width / 2, (float)CanvasSize.Height / 2);

            var xDiff = point.X - circleCenter.X;
            var yDiff = point.Y - circleCenter.Y;
            var distanceSquared = (xDiff * xDiff) + (yDiff * yDiff);
            var radiusSquared = _circle.Radius * _circle.Radius;
            return distanceSquared <= radiusSquared;
        }

        public Color PttColor
        {
            get
            {
                return _circle.Color.ToFormsColor();
            }
            set
            {
                _circle.Color = value.ToSKColor();
                InvalidateSurface();
            }
        }

        public static readonly BindableProperty PttColorProperty = BindableProperty.Create(
                                                 propertyName: "PttColor",
                                                 returnType: typeof(Color),
                                                 declaringType: typeof(PttControl),
                                                 defaultValue: Color.Black,
                                                 defaultBindingMode: BindingMode.TwoWay,
                                                 propertyChanged: PttColorPropertyChanged);

        static void PttColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (PttControl)bindable;
            control.PttColor = (Color)newValue;
        }

        public Color TransmittingAnimationColor
        {
            set
            {
                _transmittingIndicator.CircleColor = value.ToSKColor();
            }
        }

        public static readonly BindableProperty TransmittingAnimationColorProperty = BindableProperty.Create(
                                                 propertyName: "TransmittingAnimationColor",
                                                 returnType: typeof(Color),
                                                 declaringType: typeof(PttControl),
                                                 defaultValue: Color.Black,
                                                 defaultBindingMode: BindingMode.TwoWay,
                                                 propertyChanged: TransmittingAnimationColorPropertyChanged);

        static void TransmittingAnimationColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (PttControl)bindable;
            control.TransmittingAnimationColor = (Color)newValue;
        }

        bool _transmitting = false;
        public bool Transmitting
        {
            get => _transmitting;
            set
            {
                _transmittingIndicator.Hidden = !value;
                if (value)
                {
                    AddAnimation(_transmittingAnimationAction);
                }
                else
                {
                    RemoveAnimation(_transmittingAnimationAction);
                }
                _transmitting = value;
                InvalidateSurface();
            }
        }

        public static readonly BindableProperty TransmittingProperty = BindableProperty.Create(
                                                 propertyName: "Transmitting",
                                                 returnType: typeof(bool),
                                                 declaringType: typeof(PttControl),
                                                 defaultValue: false,
                                                 defaultBindingMode: BindingMode.TwoWay,
                                                 propertyChanged: TransmittingrPropertyChanged);

        static void TransmittingrPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (PttControl)bindable;
            control.Transmitting = (bool)newValue;
        }

        public void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            var location = args.Location;
            var point = new SKPoint(
                (float)(CanvasSize.Width * location.X / Width),
                (float)(CanvasSize.Height * location.Y / Height));

            int startingFingerCount = _buttonFingers.Count;

            if (args.Type == TouchActionType.Pressed || args.Type == TouchActionType.Moved)
            {
                if (IsInCircle(point))
                {
                    //touch was inside the button
                    if (!_buttonFingers.Contains(args.Id))
                    {
                        _buttonFingers.Add(args.Id);
                    }
                }
                else
                {
                    if (_buttonFingers.Contains(args.Id))
                    {
                        _buttonFingers.Remove(args.Id);
                    }
                }
            }
            if (args.Type == TouchActionType.Released)
            {
                if (_buttonFingers.Contains(args.Id))
                {
                    _buttonFingers.Remove(args.Id);
                }
            }

            if (_buttonFingers.Count > 0)
            {
                _circle.PenWidth = 45;
                InvalidateSurface();
                Execute(PttDownCommand);
            }
            if (startingFingerCount != 0 && _buttonFingers.Count == 0)
            {
                _circle.PenWidth = 30;
                InvalidateSurface();
                Execute(PttUpCommand);
                return;
            }
        }

        public static readonly BindableProperty PttDownCommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(PttControl), null);

        public ICommand PttDownCommand
        {
            get { return (ICommand)GetValue(PttDownCommandProperty); }
            set { SetValue(PttDownCommandProperty, value); }
        }


        public static readonly BindableProperty PttUpCommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(PttControl), null);

        public ICommand PttUpCommand
        {
            get { return (ICommand)GetValue(PttUpCommandProperty); }
            set { SetValue(PttUpCommandProperty, value); }
        }

        public static void Execute(ICommand command)
        {
            if (command == null) return;
            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }
}