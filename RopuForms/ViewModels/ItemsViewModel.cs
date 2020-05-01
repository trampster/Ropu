using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Forms;

using RopuForms.Models;
using RopuForms.Views;
using Ropu.Shared.Groups;

namespace RopuForms.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {
        readonly IGroupsClient _groupsClient;

        public ObservableCollection<Group> Items { get; set; }

        public Command LoadItemsCommand { get; set; }

        public ItemsViewModel(IGroupsClient groupsClient)
        {
            _groupsClient = groupsClient;
            Title = "Browse";
            Items = new ObservableCollection<Group>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            MessagingCenter.Subscribe<NewItemPage, Group>(this, "AddItem", async (obj, item) =>
            {
                var newItem = item as Group;
                Items.Add(newItem);
                //await DataStore.AddItemAsync(newItem);
                await Task.CompletedTask;
            });
        }

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