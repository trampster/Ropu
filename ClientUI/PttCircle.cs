using System;
using System.Windows.Input;
using Eto.Drawing;
using Eto.Forms;

namespace Ropu.ClientUI
{
    public class PttCircle : Drawable
    {
        readonly Color _blue = Color.FromRgb(0x3193e3);

        bool _buttonDown = false;
        public PttCircle()
        {
            Paint += PaintHandler;
            this.MouseDown += (sender, args) =>
            {
                ButtonDownEvent?.Invoke(this, EventArgs.Empty);
                _buttonDown = true;
                Invalidate();

            };
            this.MouseUp += (sender, args) =>
            {
                ButtonUpEvent?.Invoke(this, EventArgs.Empty);
                _buttonDown = false;
                Invalidate();
            };
        }

        Color _pttColor;
        public Color PttColor
        {
            get =>  _pttColor;
            set
            {
                _pttColor = value;
                Invalidate();
            }
        }

        public BindableBinding<PttCircle, Color> PttColorBinding
        { 
            get
            {
                return new BindableBinding<PttCircle, Color>(
                    this, 
                    p => p.PttColor, 
                    (p,c) => p.PttColor = c);
            }
        }

        void PaintHandler(object caller, PaintEventArgs paintEventArgs)
        {
            var graphics = paintEventArgs.Graphics;

            int penWidth = _buttonDown ? 9 : 6;
            int diameter = Math.Min(Width, Height) -penWidth - (_buttonDown ? 0 : 3);

            int yPosition = (Height/2) - (diameter/2);
            int xPosition = (Width/2) - (diameter/2);


            Pen pen = new Pen(PttColor, penWidth);
            graphics.DrawEllipse(pen, xPosition, yPosition, diameter, diameter);
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
			set { Properties.SetCommand(ButtonUpCommandKey, value, e => Enabled = e, r => this.ButtonUpEvent += r, r => ButtonUpEvent -= r, () => ButtonUPCommandParameter); }
        }

        public object ButtonUPCommandParameter
		{
			get { return Properties.Get<object>(ButtonUpCommandKey); }
			set { Properties.Set(ButtonUpCommandKey, value, () => Properties.UpdateCommandCanExecute(ButtonUpCommandKey)); }
		}
    }
}