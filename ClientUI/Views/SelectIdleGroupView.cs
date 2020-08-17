using Eto.Drawing;
using Eto.Forms;
using Ropu.Gui.Shared.ViewModels;
using Ropu.Shared.Groups;

namespace Ropu.ClientUI.Views
{
    public class SelectIdleGroupView : Panel
    {
        readonly SelectGroupViewModel _selectGroupViewModel;
        readonly ListView<Group> _listView;

        public SelectIdleGroupView(SelectGroupViewModel selectGroupViewModel)
        {
            _selectGroupViewModel = selectGroupViewModel;
            _listView = new ListView<Group>();
            _listView.Collection = selectGroupViewModel.Items;
            _listView.CreateItem = CreateGroupView;
            _listView.ItemSelectedCommand = selectGroupViewModel.ItemSelectedCommand;
            Content = _listView;
        }

        protected override void OnShown(System.EventArgs e)
        {
            if(_selectGroupViewModel.LoadItemsCommand.CanExecute(null))
            {
                _selectGroupViewModel.LoadItemsCommand.Execute(null);
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
