using Eto.Forms;
using Eto.Drawing;
using Ropu.ClientUI.ViewModels;

namespace Ropu.ClientUI.Views
{
    public class MainView : Form
    {
        readonly Navigator _navigator;
        readonly MainViewModel _mainViewModel;

        public MainView(Navigator navigator, MainViewModel mainViewModel)
        {
            _navigator = navigator;
            _mainViewModel = mainViewModel;
            _navigator.SetViewChangeHandler(view => 
            {
                Content = view;
            });
            Title = "Ropu Client";
            ClientSize = new Size(300, 500);

            Shown += async (sender, args) => await mainViewModel.Initialize();
        }
    }
}
