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
            this.SuspendLayout();
            Content = null;
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
                    _groupsLayout.Items.Insert(args.NewStartingIndex, 
                        new StackLayoutItem(CreateGroupView(group), HorizontalAlignment.Stretch));
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

            if(Content == null && _groupsLayout.Items.Count > 0)
            {
                Content = _groupsLayout;
            }
            this.ResumeLayout();
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

            var stackLayout = new StackLayout();
            stackLayout.Orientation = Orientation.Vertical;
            stackLayout.Items.Add(layout);
            stackLayout.Items.Add(
                new StackLayoutItem(
                    new Panel(){BackgroundColor=Color.FromRgb(0xC0C0C0), Height=1},
                    HorizontalAlignment.Stretch));
            return stackLayout;
        }

    }
}
