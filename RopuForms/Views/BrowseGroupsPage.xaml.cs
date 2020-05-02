using System;
using System.ComponentModel;
using Ropu.Gui.Shared.ViewModels;
using RopuForms.Models;
using RopuForms.ViewModels;
using Xamarin.Forms;

namespace RopuForms.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class BrowseGroupsPage : ContentPage
    {
        readonly BrowseGroupsViewModel _viewModel;

        public BrowseGroupsPage(BrowseGroupsViewModel itemsViewModel)
        {
            InitializeComponent();

            _viewModel = itemsViewModel;

            BindingContext = _viewModel;
        }

        async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var item = args.SelectedItem as Item;
            if (item == null)
                return;

            await Navigation.PushAsync(new ItemDetailPage(new ItemDetailViewModel(item)));

            // Manually deselect item.
            ItemsListView.SelectedItem = null;
        }

        async void AddItem_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new NavigationPage(new NewItemPage()));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel.Items.Count == 0)
                _viewModel.LoadItemsCommand.Execute(null);
        }
    }
}