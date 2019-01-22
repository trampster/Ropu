using System;
using Eto.Forms;
using Eto.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Ropu.ClientUI
{

    public class MyModel : BaseViewModel
    {
        public MyModel()
        {
        }

        string _state = "unregistered";
        public string State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public ICommand PttDownCommand => new ActionCommand(() => State = "PTT Down");
        public ICommand PttUpCommand => new ActionCommand(() => State = "PTT Up");
    }

    public class MyForm : Form
    {
        public MyForm ()
        {
            Title = "Ropu Client";
            ClientSize = new Size(200, 200);

            var stateLabel = new Label();
            stateLabel.TextBinding.BindDataContext<MyModel>(m => m.State);
            var button = new PttButton()
            {
                Text = "Push To Talk"
            };
            button.BindDataContext(c => c.ButtonDownCommand, (MyModel model) => model.PttDownCommand);
            button.BindDataContext(c => c.ButtonUpCommand, (MyModel model) => model.PttUpCommand);

            Content = new TableLayout
            {
                Rows = 
                { 
                    stateLabel, 
                    button,
                }
            };
            DataContext = new MyModel();
        }
        
        [STAThread]
        static void Main()
        {
            new Application().Run(new MyForm());
        }
    }
}
