using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RopuForms.Views.TouchTracking;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using Ropu.Shared;

namespace RopuForms.Views
{
    public class PttControl : SKGLView
    {
        readonly PttCircle _circle;
        readonly TransmittingIndicator _transmittingIndicator;
        readonly TransmittingIndicator _receivingIndicator;
        readonly ImageLabel _callGroupDrawable;
        readonly ImageLabel _talkerDrawable;
        readonly IdleGroup _idleGroupDrawable;
        readonly Action _transmittingAnimationAction;
        readonly Action _receivingAnimationAction;
        readonly Task _animationTask;

        public PttControl()
        {
            _circle = new PttCircle();
            _circle.PenWidth = 30;
            
            _transmittingIndicator = new TransmittingIndicator();
            _transmittingIndicator.Hidden = true;
            _transmittingAnimationAction = AnimateTransmitting;

            _receivingIndicator = new TransmittingIndicator();
            _receivingIndicator.Hidden = true;
            _receivingAnimationAction = AnimateReceiving;

            _talkerDrawable = new ImageLabel();
            _talkerDrawable.Text = null;

            _callGroupDrawable = new ImageLabel();
            _callGroupDrawable.Text = "";

            _idleGroupDrawable = new IdleGroup();

            _animationTask = RunAnimations();
        }

        public static readonly BindableProperty CallGroupProperty = Bindings.Create<PttControl, string>("CallGroup", (control, oldValue, newValue) => control.CallGroup = newValue);

        public string? CallGroup
        {
            get => _callGroupDrawable.Text;
            set
            {
                _callGroupDrawable.Hidden = value == null || value == string.Empty;
                _callGroupDrawable.Text = value.EmptyIfNull();
                InvalidateSurface();
            }
        }

        public static readonly BindableProperty CallGroupImageProperty = Bindings.Create<PttControl, byte[]?>("CallGroupImage", (control, oldValue, newValue) => control.CallGroupImage = newValue);

        public byte[]? CallGroupImage
        {
            set
            {
                if (value == null) return;
                _callGroupDrawable.Image = SKImage.FromEncodedData(value);
                InvalidateSurface();
            }
        }

        public static readonly BindableProperty TalkerProperty = Bindings.Create<PttControl, string>("Talker", (control, oldValue, newValue) => control.Talker = newValue);

        public string? Talker
        {
            get => _talkerDrawable.Text;
            set
            {
                if (value == null || value == "")
                {
                    _receivingIndicator.Hidden = true;
                    RemoveAnimation(_receivingAnimationAction);
                }
                else
                {
                    _receivingIndicator.Hidden = false;
                    AddAnimation(_receivingAnimationAction);
                }
                _talkerDrawable.Hidden = value == null;
                _talkerDrawable.Text = value == null ? "" : value;
                InvalidateSurface();
            }
        }

        public static readonly BindableProperty TalkerImageProperty = Bindings.Create<PttControl, byte[]?>("TalkerImage", (control, oldValue, newValue) => control.TalkerImage = newValue);

        public byte[]? TalkerImage
        {
            set
            {
                if (value == null) return;
                _talkerDrawable.Image = SKImage.FromEncodedData(value);
                InvalidateSurface();
            }
        }

        public static readonly BindableProperty IdleGroupProperty = Bindings.Create<PttControl, string>("IdleGroup", (control, oldValue, newValue) => control.IdleGroup = newValue);

        public string IdleGroup
        {
            get => _idleGroupDrawable.GroupName;
            set
            {
                _idleGroupDrawable.GroupName = value;
                InvalidateSurface();
            }
        }

        public static readonly BindableProperty IdleGroupImageProperty = Bindings.Create<PttControl, byte[]?>("TalkerImage", (control, oldValue, newValue) => control.IdleGroupImage = newValue);

        public byte[]? IdleGroupImage
        {
            set
            {
                if (value == null) return;
                _idleGroupDrawable.Image = SKImage.FromEncodedData(value);
                InvalidateSurface();
            }
        }

        protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);
        
            var surface = e.Surface;
            var canvas = surface.Canvas;
            canvas.Clear();

            const int padding = 10;

            _circle.Text = "Avengers";

            _callGroupDrawable.X = padding;
            _callGroupDrawable.Y = padding;
            _callGroupDrawable.Draw(canvas);

            int radius = DrawPttCircle(canvas, 50, padding);

            _transmittingIndicator.X = (int)(CanvasSize.Width / 2);
            _transmittingIndicator.Y = (int)(CanvasSize.Height / 2);
            _transmittingIndicator.MinRadius = radius;
            _transmittingIndicator.MaxRadius = radius + (radius / 2);
            _transmittingIndicator.Draw(canvas);

            _talkerDrawable.X = (int)CanvasSize.Width - _talkerDrawable.Width - padding;
            _talkerDrawable.Y = padding;
            _talkerDrawable.Draw(canvas);

            _idleGroupDrawable.X = (int)CanvasSize.Width - _idleGroupDrawable.Width - padding;
            _idleGroupDrawable.Y = (int)CanvasSize.Height - _idleGroupDrawable.Height - padding;
            _idleGroupDrawable.Draw(canvas);

            _receivingIndicator.X = _talkerDrawable.X + (_talkerDrawable.Width / 2);
            _receivingIndicator.Y = _talkerDrawable.Y + (_talkerDrawable.Height / 2);
            int receivingRadius = (int)(Math.Max(_talkerDrawable.Width, _talkerDrawable.Height) * 0.75);
            _receivingIndicator.MinRadius = receivingRadius;
            _receivingIndicator.MaxRadius = receivingRadius + receivingRadius;
            _receivingIndicator.Draw(canvas);
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

        void AnimateReceiving()
        {
            float fraction = _receivingIndicator.AnimationFraction;
            fraction += 0.01f;
            if (fraction > 1) fraction = 0;
            _receivingIndicator.AnimationFraction = fraction;
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

        public static readonly BindableProperty PttColorProperty = Bindings.Create<PttControl, Color>("PttColor", (control, oldValue, newValue) => control.PttColor = newValue);

        public Color PttColor
        {
            get
            {
                return _circle.Color.ToFormsColor();
            }
            set
            {
                var color = value.ToSKColor();
                _circle.Color = color;
                _transmittingIndicator.CircleColor = color;
                InvalidateSurface();
            }
        }

        public static readonly BindableProperty ReceivingAnimationColorProperty = Bindings.Create<PttControl, Color>("ReceivingAnimationColor", (control, oldValue, newValue) => control.ReceivingAnimationColor = oldValue);

        public Color ReceivingAnimationColor
        {
            set
            {
                _receivingIndicator.CircleColor = value.ToSKColor();
            }
        }

        public static readonly BindableProperty TransmittingAnimationColorProperty = Bindings.Create<PttControl, Color>("TransmittingAnimationColor", (control, oldValue, newValue) => control.TransmittingAnimationColor = newValue);

        public Color TransmittingAnimationColor
        {
            set
            {
                _transmittingIndicator.CircleColor = value.ToSKColor();
            }
        }

        public static readonly BindableProperty TransmittingProperty = Bindings.Create<PttControl, bool>("Transmitting", (control, oldValue, newValue) => control.Transmitting = newValue);

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

            if (startingFingerCount == 0 && _buttonFingers.Count > 0)
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
