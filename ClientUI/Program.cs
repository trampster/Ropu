using System;
using Eto.Forms;
using Eto.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ClientUI
{

    public class MyModel : INotifyPropertyChanged
    {
        public MyModel()
        {
           var pttCommand = new Command();
           pttCommand.Executed += (sender, args)  =>
           {
               State = "Clicked";
           };
           PttClickCommand = pttCommand;
        }
        string _state = "unregistered";
        public string State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }

        void OnPropertyChanged([CallerMemberName] string memberName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand PttClickCommand { get; }
    }

    public class MyForm : Form
    {
        public MyForm ()
        {
            Title = "Ropu Client";
            ClientSize = new Size(200, 200);

            var stateLabel = new Label();
            stateLabel.TextBinding.BindDataContext<MyModel>(m => m.State);
            var button = new Button()
            {
                Text = "Push To Talk"
            };
            button.BindDataContext(c => c.Command, (MyModel model) => model.PttClickCommand);

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
