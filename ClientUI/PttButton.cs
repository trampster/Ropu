
using System;
using System.Windows.Input;
using Eto.Forms;

namespace Ropu.ClientUI
{
    public class PttButton : Button
    {
        public PttButton()
        {
            this.MouseDown += (sender, mouseArgs) =>
            {
                if(mouseArgs.Buttons == MouseButtons.Primary)
                {
                    ButtonDownEvent?.Invoke(this, EventArgs.Empty);
                }
            };
            this.MouseUp += (sender, mouseArgs) =>
            {
                if(mouseArgs.Buttons == MouseButtons.Primary)
                {
                    ButtonUpEvent?.Invoke(this, EventArgs.Empty);
                }
            };
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