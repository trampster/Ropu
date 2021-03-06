using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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
            _navigator.RegisterNavigatorHome("HomeRightPanel", async view => 
            {
                _rightPanel.Content = view;
                await Task.CompletedTask;
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

        readonly List<Panel> _menuItems = new List<Panel>();

        Panel CreateMenuItem(string text, ICommand command)
        {
            var label = new Label(){Text = text};
            var panel = new Panel(){Content = label};

            bool labelSelected = false;
            bool panelSelected = false;

            Action changeColor = () =>
            {
                if(panel.BackgroundColor == _colorService.Blue)
                {
                    return;
                }
                if(labelSelected || panelSelected)
                {
                    panel.BackgroundColor = Color.FromRgb(0x233236);
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
            Action clicked = () => 
            {
                foreach(var menuItem in _menuItems)
                {
                    menuItem.BackgroundColor = _menuLayout.BackgroundColor;
                }
                panel.BackgroundColor = _colorService.Blue;
                if(command.CanExecute(null))
                {
                    command.Execute(null);
                }
            };
            panel.MouseUp += (sender, args) => clicked();
            label.MouseUp += (sender, args) => clicked();
            int topPadding = _menuLayout.Children.Any() ? 5 : 10;
            panel.Padding = new Padding(10,topPadding,10,5);
            if(_menuItems.Count == 0)
            {
                panel.BackgroundColor = _colorService.Blue;
            }
            _menuItems.Add(panel);
            return panel;
        }

        readonly DynamicLayout _menuLayout = new DynamicLayout();

        Control CreateMenu()
        {
            _menuLayout.BeginVertical();
            _menuLayout.Add(CreateMenuItem("Push To Talk", _homeViewModel.ShowPttView));
            _menuLayout.Add(CreateMenuItem("Browse Groups", _homeViewModel.ShowBrowseGroupsView));
            _menuLayout.Add(CreateMenuItem("About", _homeViewModel.ShowAboutView));
            _menuLayout.AddSpace();
            _menuLayout.EndVertical();
            _menuLayout.BackgroundColor = Color.FromRgb(0x1C282B);
            return _menuLayout;
        }
    }
}