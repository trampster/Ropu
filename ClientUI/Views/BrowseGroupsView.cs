using System;
using System.Collections.Specialized;
using Eto.Drawing;
using Eto.Forms;
using Ropu.Gui.Shared.ViewModels;
using Ropu.Shared.Groups;

namespace Ropu.ClientUI.Views
{
    public class BrowseGroupsView : Panel
    {
        readonly BrowseGroupsViewModel _browseGroupsViewModel;
        readonly ListView<Group> _listView;

        public BrowseGroupsView(BrowseGroupsViewModel browseGroupsViewModel)
        {
            _browseGroupsViewModel = browseGroupsViewModel;
            _listView = new ListView<Group>();
            _listView.Collection = browseGroupsViewModel.Items;
            _listView.CreateItem = CreateGroupView;
            Content = _listView;
        }

        protected override void OnShown(System.EventArgs e)
        {
            if(_browseGroupsViewModel.LoadItemsCommand.CanExecute(null))
            {
                _browseGroupsViewModel.LoadItemsCommand.Execute(null);
            }
        }

        Control CreateGroupView(Group group)
        {
            var image = new ImageView();
            if(group.Image != null)
            {
                image.Image = new Bitmap(group.Image);
            }
            var label = new Label(){Text = group.Name};
            label.VerticalAlignment = VerticalAlignment.Center;

            var layout = new DynamicLayout();
            layout.BeginHorizontal();
            layout.Add(image);
            layout.Add(new Panel(){Width=5});
            layout.Add(label);
            layout.EndHorizontal();
            layout.Padding = 10;

            return layout;
        }

    }
}
