using System;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Ropu.ClientUI.Services;
using Ropu.Gui.Shared.ViewModels;

namespace Ropu.ClientUI.Views
{
    public class HomeView : Panel
    {
        readonly HomeViewModel _homeViewModel;
        readonly Navigator _navigator;
        readonly Panel _rightPanel = new Panel();
        readonly ColorService _colorService;

        public HomeView(HomeViewModel homeViewModel, Navigator navigator, ColorService colorService)
        {
            _homeViewModel = homeViewModel;
            _navigator = navigator;
            _colorService = colorService;
            _navigator.SetSubViewChangeHandler(view => 
            {
                _rightPanel.Content = view;
            });

            this.Content = CreateLayout();
        }

        Control CreateLayout()
        {
            var layout = new DynamicLayout();
            layout.BeginHorizontal();
            layout.Add(CreateMenu());
            layout.Add(_rightPanel);
            layout.EndHorizontal();
            return layout;
        }

        Panel CreateMenuItem(string text)
        {
            var label = new Label(){Text = text};
            var panel = new Panel(){Content = label};

            bool labelSelected = false;
            bool panelSelected = false;

            Action changeColor = () =>
            {
                if(labelSelected || panelSelected)
                {
                    panel.BackgroundColor = _colorService.Blue;
                    return;
                }
                panel.BackgroundColor = this.BackgroundColor;
            };

            panel.MouseEnter += (sender, args) => 
            {
                panelSelected = true;
                changeColor();
            };
            panel.MouseLeave += (sender, args) =>
            {
                panelSelected = false;
                changeColor();
            };
            label.MouseEnter += (sender, args) => 
            {
                labelSelected = true;
                changeColor();
            };
            label.MouseLeave += (sender, args) =>
            {
                labelSelected = false;
                changeColor();
            };
            int topPadding = _menuLayout.Children.Any() ? 5 : 10;
            panel.Padding = new Padding(10,topPadding,10,5);

            return panel;
        }

        readonly DynamicLayout _menuLayout = new DynamicLayout();

        Control CreateMenu()
        {
            _menuLayout.BeginVertical();
            _menuLayout.Add(CreateMenuItem("Push To Talk"));
            _menuLayout.Add(CreateMenuItem("Browse Groups"));
            _menuLayout.Add(CreateMenuItem("About"));
            _menuLayout.AddSpace();
            _menuLayout.EndVertical();
            _menuLayout.BackgroundColor = Color.FromRgb(0x1C282B);
            return _menuLayout;
        }
    }
}