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
        readonly StackLayout _groupsLayout = new StackLayout();

        public BrowseGroupsView(BrowseGroupsViewModel browseGroupsViewModel)
        {
            _browseGroupsViewModel = browseGroupsViewModel;

            var items = browseGroupsViewModel.Items;
            items.CollectionChanged += (sender, args) => OnGroupsChanged(args);

            Content = _groupsLayout;
        }

        protected override void OnShown(System.EventArgs e)
        {
            if(_browseGroupsViewModel.LoadItemsCommand.CanExecute(null))
            {
                _browseGroupsViewModel.LoadItemsCommand.Execute(null);
            }
        }

        void OnGroupsChanged(NotifyCollectionChangedEventArgs args)
        {
            if(args.Action == NotifyCollectionChangedAction.Reset)
            {
                _groupsLayout.Items.Clear();
            }
            if(args.Action == NotifyCollectionChangedAction.Add)
            {
                foreach(Group? group in args.NewItems)
                {
                    if(group == null)
                    {
                        continue;
                    }
                    _groupsLayout.Items.Insert(args.NewStartingIndex, CreateGroupView(group));
                }
            }
            if(args.Action == NotifyCollectionChangedAction.Remove)
            {
                throw new NotImplementedException();
            }
            if(args.Action == NotifyCollectionChangedAction.Replace)
            {
                throw new NotImplementedException();
            }
            if(args.Action == NotifyCollectionChangedAction.Move)
            {
                throw new NotImplementedException();
            }
        }

        Control CreateGroupView(Group group)
        {
            var image = new ImageView();
            if(group.Image != null)
            {
                image.Image = new Bitmap(group.Image);
            }
            var layout = new DynamicLayout();
            layout.BeginHorizontal();
            layout.Add(image);
            layout.Add(new Label(){Text = group.Name});
            layout.EndHorizontal();
            return layout;
        }

    }
}
