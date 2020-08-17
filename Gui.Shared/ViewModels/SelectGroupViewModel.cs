using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Ropu.Shared.Groups;
using System.Windows.Input;
using Ropu.Gui.Shared.Services;
using Ropu.Client;

namespace Ropu.Gui.Shared.ViewModels
{
    public class SelectGroupViewModel : BaseViewModel
    {
        readonly IGroupsClient _groupsClient;
        readonly INavigator _navigator;
        readonly RopuClient _ropuClient;

        public ObservableCollection<Group> Items { get; set; }

        public ICommand LoadItemsCommand { get; set; }

        public SelectGroupViewModel(IGroupsClient groupsClient, INavigator navigator, RopuClient ropuClient)
        {
            _groupsClient = groupsClient;
            _navigator = navigator;
            _ropuClient = ropuClient;
            Title = "Browse";
            Items = new ObservableCollection<Group>();
            LoadItemsCommand = new AsyncCommand(async () => await ExecuteLoadItemsCommand());
        }

        public ICommand ItemSelectedCommand => new AsyncCommand<Group>(async group => 
        {
            _ropuClient.IdleGroup = group.Id;
            await _navigator.NavigateBack();
        });

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