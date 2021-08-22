using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Eto.Drawing;
using Eto.Forms;

namespace Ropu.ClientUI.Views
{
    public class ListView<T> : Panel where T : class
    {
        readonly StackLayout _itemsLayout = new StackLayout();
        ObservableCollection<T>? _collection;

        public ListView()
        {
        }

        public ObservableCollection<T> Collection
        {
            set
            {
                _collection = value;
                _collection.CollectionChanged += (sender, args) => OnItemsChanged(args);
            }
        }

        void OnItemsChanged(NotifyCollectionChangedEventArgs args)
        {
            this.SuspendLayout();
            Content = null;
            if(args.Action == NotifyCollectionChangedAction.Reset)
            {
                _itemsLayout.Items.Clear();
            }
            if(args.Action == NotifyCollectionChangedAction.Add)
            {
                foreach(T? group in args.NewItems!)
                {
                    if(group == null)
                    {
                        continue;
                    }
                    _itemsLayout.Items.Insert(args.NewStartingIndex, 
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

            if(Content == null && _itemsLayout.Items.Count > 0)
            {
                Content = _itemsLayout;
            }
            this.ResumeLayout();
        }

        public Func<T, Control>? CreateItem;

        public ICommand? ItemSelectedCommand
        {
            set;
            private get;
        }

        Control CreateGroupView(T group)
        {
            if(CreateItem == null)
            {
                throw new InvalidOperationException("CreateItem must be set before adding items");
            }
            var layout = CreateItem(group);

            var stackLayout = new StackLayout();
            stackLayout.Orientation = Orientation.Vertical;
            stackLayout.Items.Add(layout);
            stackLayout.Items.Add(
                new StackLayoutItem(
                    new Panel(){BackgroundColor=Color.FromRgb(0xC0C0C0), Height=1},
                    HorizontalAlignment.Stretch));
            stackLayout.MouseEnter += (sender, args) => stackLayout.BackgroundColor = Color.FromRgb(0xE0E0E0);
            stackLayout.MouseLeave += (sender, args) => stackLayout.BackgroundColor = this.BackgroundColor;
            stackLayout.MouseUp += (sender, args) => 
            {
                args.Handled = true;
                if(ItemSelectedCommand != null)
                {
                    ItemSelectedCommand.CanExecute(group);
                    ItemSelectedCommand.Execute(group);
                }
            };
            return stackLayout;
        }
    }
}
