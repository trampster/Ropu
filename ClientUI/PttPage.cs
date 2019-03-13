using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Eto.Drawing;
using Eto.Forms;

namespace Ropu.ClientUI
{
    public class PttPage : Drawable
    {
        readonly Task _animationTask;
        FontFamily _fontFamily;

        ImageLabel _callGroupDrawable;
        ImageLabel _talkerDrawable;
        IdleGroup _idleGroupDrawable;
        TransmittingIndicator _transmittingIndicator;
        TransmittingIndicator _receivingIndicator;
        readonly PttCircle _pttCircle;

        bool _buttonDown = false;
        public PttPage()
        {
            Paint += PaintHandler;
            this.MouseDown += (sender, args) =>
            {
                ButtonDownEvent?.Invoke(this, EventArgs.Empty);
                _buttonDown = true;
                _pttCircle.PenWidth = 9;

                Invalidate();

            };
            this.MouseUp += (sender, args) =>
            {
                ButtonUpEvent?.Invoke(this, EventArgs.Empty);
                _buttonDown = false;
                _pttCircle.PenWidth = 6;
                Invalidate();
            };
            _fontFamily = Eto.Drawing.Fonts.AvailableFontFamilies.First();

            _callGroupDrawable = new ImageLabel(_fontFamily);
            _callGroupDrawable.Text = "A Team";
            _callGroupDrawable.Image = new Bitmap("../Icon/knot32.png");

            _talkerDrawable = new ImageLabel(_fontFamily);
            _talkerDrawable.Text = "Franky";
            _talkerDrawable.Image = new Bitmap("../Icon/rope32.png");

            _idleGroupDrawable = new IdleGroup(_fontFamily);
            _idleGroupDrawable.GroupName = "A Team";
            _idleGroupDrawable.Image = new Bitmap("../Icon/knot32.png");

            _transmittingIndicator = new TransmittingIndicator();
            _transmittingAnimationAction = AnimateTransmitting;

            _receivingIndicator = new TransmittingIndicator();
            _receivingAnimationAction = AnimateReceiving;

            _pttCircle = new PttCircle(_fontFamily);

            _animationTask = RunAnimations();
        }

        public Color TransmittingAnimationColor
        {
            set
            {
                _transmittingIndicator.CircleColor = value;
            }
        }

        public Color ReceivingAnimationColor
        {
            set
            {
                _receivingIndicator.CircleColor = value;
            }
        }

        Action _transmittingAnimationAction;
        Action _receivingAnimationAction;

        void AnimateTransmitting()
        {
            float fraction = _transmittingIndicator.AnimationFraction;
            fraction += 0.01f;
            if(fraction > 1) fraction = 0;
            _transmittingIndicator.AnimationFraction = fraction;
        }

        void AnimateReceiving()
        {
            float fraction = _receivingIndicator.AnimationFraction;
            fraction += 0.01f;
            if(fraction > 1) fraction = 0;
            _receivingIndicator.AnimationFraction = fraction;
        }

        public Color PttColor
        {
            get =>  _pttCircle.Color;
            set
            {
                _pttCircle.Color = value;
                Invalidate();
            }
        }

        public BindableBinding<PttPage, Color> PttColorBinding
        { 
            get
            {
                return new BindableBinding<PttPage, Color>(
                    this, 
                    p => p.PttColor, 
                    (p,c) => p.PttColor = c);
            }
        }

        public string Talker
        {
            get =>  _talkerDrawable.Text;
            set
            {
                if(value == null)
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
                _talkerDrawable.Text = value;
                Invalidate();
            }
        }

        public BindableBinding<PttPage, string> TalkerBinding
        { 
            get
            {
                return new BindableBinding<PttPage, string>(
                    this, 
                    p => p.Talker, 
                    (p,c) => p.Talker = c);
            }
        }

        bool _transmitting = false;
        public bool Transmitting
        {
            get =>  _transmitting;
            set
            {
                _transmittingIndicator.Hidden = !value;
                if(value)
                {
                    AddAnimation(_transmittingAnimationAction);
                }
                else
                {
                    RemoveAnimation(_transmittingAnimationAction);
                }
                _transmitting = true;
                Invalidate();
            }
        }

        public BindableBinding<PttPage, bool> TransmittingBinding
        { 
            get
            {
                return new BindableBinding<PttPage, bool>(
                    this, 
                    p => p.Transmitting, 
                    (p,c) => p.Transmitting = c);
            }
        }

        public string CallGroup
        {
            get =>  _callGroupDrawable.Text;
            set
            {
                _callGroupDrawable.Hidden = value == null;
                _callGroupDrawable.Text = value;
                Invalidate();
            }
        }

        public BindableBinding<PttPage, string> CallGroupBinding
        { 
            get
            {
                return new BindableBinding<PttPage, string>(
                    this, 
                    p => p.CallGroup, 
                    (p,c) => p.CallGroup = c);
            }
        }

        public string CircleText
        {
            get =>  _pttCircle.Text;
            set
            {
                _pttCircle.Text = value;
                Invalidate();
            }
        }

        public BindableBinding<PttPage, string> CircleTextBinding
        { 
            get
            {
                return new BindableBinding<PttPage, string>(
                    this, 
                    p => p.CircleText, 
                    (p,c) => p.CircleText = c);
            }
        }

        public string IdleGroup
        {
            get =>  _idleGroupDrawable.GroupName;
            set
            {
                _idleGroupDrawable.GroupName = value;
                Invalidate();
            }
        }

        public BindableBinding<PttPage, string> IdleGroupBinding
        { 
            get
            {
                return new BindableBinding<PttPage, string>(
                    this, 
                    p => p.IdleGroup, 
                    (p,c) => p.IdleGroup = c);
            }
        }

        void PaintHandler(object caller, PaintEventArgs paintEventArgs)
        {
            var graphics = paintEventArgs.Graphics;

            const int padding = 5;

            _callGroupDrawable.X = padding;
            _callGroupDrawable.Y = padding;
            _callGroupDrawable.Draw(graphics);
            
            _talkerDrawable.X = Width - _talkerDrawable.Width - padding;
            _talkerDrawable.Y = padding;
            _talkerDrawable.Draw(graphics);

            _idleGroupDrawable.X = Width - _idleGroupDrawable.Width - padding;
            _idleGroupDrawable.Y = Height - _idleGroupDrawable.Height - padding;
            _idleGroupDrawable.Draw(graphics);

            int radius = DrawPttCircle(graphics, _callGroupDrawable.Height + padding, padding);

            _transmittingIndicator.X = Width/2;
            _transmittingIndicator.Y = Height/2;
            _transmittingIndicator.MinRadius = radius;
            _transmittingIndicator.MaxRadius = radius + (radius/2);
            _transmittingIndicator.Draw(graphics);

            _receivingIndicator.X = _talkerDrawable.X + (_talkerDrawable.Width/2);
            _receivingIndicator.Y = _talkerDrawable.Y + (_talkerDrawable.Height/2);
            int receivingRadius = (int)( Math.Max(_talkerDrawable.Width, _talkerDrawable.Height)*0.75);
            _receivingIndicator.MinRadius = receivingRadius;
            _receivingIndicator.MaxRadius = receivingRadius + receivingRadius;
            _receivingIndicator.Draw(graphics);
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
            if(_animationUpdates.Count == 0)
            {
                _animationNeeded.Reset();
            }
        }

        async Task RunAnimations()
        {
            while(true)
            {
                await Task.Delay(16);
                if(_animationUpdates.Count == 0)
                {
                    await Task.Run(() => _animationNeeded.WaitOne());
                }
                foreach(var update in _animationUpdates)
                {
                    update();
                }
                if(_animationUpdates.Count != 0)
                {
                    Invalidate();
                }
            }
        }

        int DrawPttCircle(Graphics graphics, int topSpace, int padding)
        {
            int heightAvailable = Height - topSpace*2;
            int diameter = Math.Min(Width - (padding*2), heightAvailable);
            
            _pttCircle.X = Width/2;
            _pttCircle.Y = Height/2;
            _pttCircle.Radius = diameter/2;
            _pttCircle.Draw(graphics);

            return _pttCircle.Radius;
        }

        event EventHandler<EventArgs> ButtonDownEvent;

        static readonly object ButtonDownCommandKey = new object();

        public ICommand ButtonDownCommand
        {
            get { return Properties.GetCommand(ButtonDownCommandKey); }
			set { Properties.SetCommand(ButtonDownCommandKey, value, e => Enabled = e, r => this.ButtonDownEvent += r, r => ButtonDownEvent -= r, () => ButtonDownCommandParameter); }
        }

        public object ButtonDownCommandParameter
		{
			get { return Properties.Get<object>(ButtonDownCommandKey); }
			set { Properties.Set(ButtonDownCommandKey, value, () => Properties.UpdateCommandCanExecute(ButtonDownCommandKey)); }
		}

        event EventHandler<EventArgs> ButtonUpEvent;

        static readonly object ButtonUpCommandKey = new object();

        public ICommand ButtonUpCommand
        {
            get { return Properties.GetCommand(ButtonUpCommandKey); }
			set { Properties.SetCommand(ButtonUpCommandKey, value, e => Enabled = e, r => this.ButtonUpEvent += r, r => ButtonUpEvent -= r, () => ButtonUpCommandParameter); }
        }

        public object ButtonUpCommandParameter
		{
			get { return Properties.Get<object>(ButtonUpCommandKey); }
			set { Properties.Set(ButtonUpCommandKey, value, () => Properties.UpdateCommandCanExecute(ButtonUpCommandKey)); }
		}
    }
}