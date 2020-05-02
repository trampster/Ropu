using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using RopuForms.Models;
using RopuForms.ViewModels;
using RopuForms.Inject;

namespace RopuForms.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        Dictionary<int, NavigationPage> MenuPages = new Dictionary<int, NavigationPage>();
        readonly MainViewModel _mainViewModel;
        readonly Func<BrowseGroupsPage> _itemsPageFactory;

        public MainPage()
        {
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;
            MenuPages.Add((int)MenuItemType.Ptt, (NavigationPage)Detail);
        }

        public MainPage(MainViewModel mainViewModel, Func<BrowseGroupsPage> itemsPageFactory)
        {
            InitializeComponent();

            _mainViewModel = mainViewModel;
            _itemsPageFactory = itemsPageFactory;

            MasterBehavior = MasterBehavior.Popover;
            MenuPages.Add((int)MenuItemType.Ptt, (NavigationPage)Detail);
        }

        public async Task NavigateFromMenu(int id)
        {
            if (!MenuPages.ContainsKey(id))
            {
                switch (id)
                {
                    case (int)MenuItemType.Browse:
                        MenuPages.Add(id, new NavigationPage(_itemsPageFactory()));
                        break;
                    case (int)MenuItemType.About:
                        MenuPages.Add(id, new NavigationPage(new AboutPage()));
                        break;
                    case (int)MenuItemType.Ptt:
                        MenuPages.Add(id, new NavigationPage(Injection.Resolve<PttPage>()));
                        break;
                }
            }

            var newPage = MenuPages[id];

            if (newPage != null && Detail != newPage)
            {
                Detail = newPage;

                if (Device.RuntimePlatform == Device.Android)
                    await Task.Delay(100);

                IsPresented = false;
            }
        }
    }
}