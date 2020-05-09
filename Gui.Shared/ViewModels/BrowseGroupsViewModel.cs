using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Ropu.Shared.Groups;
using System.Windows.Input;
using Ropu.Gui.Shared.Services;

namespace Ropu.Gui.Shared.ViewModels
{
    public class BrowseGroupsViewModel : BaseViewModel
    {
        readonly IGroupsClient _groupsClient;
        readonly INavigator _navigator;

        public ObservableCollection<Group> Items { get; set; }

        public ICommand LoadItemsCommand { get; set; }

        public BrowseGroupsViewModel(IGroupsClient groupsClient, INavigator navigator)
        {
            _groupsClient = groupsClient;
            _navigator = navigator;
            Title = "Browse";
            Items = new ObservableCollection<Group>();
            LoadItemsCommand = new AsyncCommand(async () => await ExecuteLoadItemsCommand());

            //MessagingCenter.Subscribe<NewItemPage, Group>(this, "AddItem", async (obj, item) =>
            //{
            //    var newItem = item as Group;
            //    Items.Add(newItem);
            //    //await DataStore.AddItemAsync(newItem);
            //    await Task.CompletedTask;
            //});
        }

        public ICommand ItemSelectedCommand => new AsyncCommand<Group>(async group => await _navigator.ShowModal<BrowseGroupViewModel, Group>(group));

        async Task ExecuteLoadItemsCommand()
        {
            await Task.CompletedTask;
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Items.Clear();
                var groupIds = await _groupsClient.GetGroups();
                foreach (var groupId in groupIds)
                {
                    var group = await _groupsClient.Get(groupId);
                    if (group != null)
                    {
                        Items.Add(group);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}