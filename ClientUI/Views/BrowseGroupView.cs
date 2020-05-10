using System;
using System.Collections.Specialized;
using Eto.Drawing;
using Eto.Forms;
using Ropu.ClientUI.Services;
using Ropu.Gui.Shared.Services;
using Ropu.Gui.Shared.ViewModels;

namespace Ropu.ClientUI.Views
{
    public class BrowseGroupView : ModalPage
    {
        readonly BrowseGroupViewModel _browseGroupViewModel;

        public BrowseGroupView(BrowseGroupViewModel browseGroupViewModel, ImageService imageService, INavigator navigator, ColorService colorService)
            : base(imageService, navigator, "Back")
        {
            _browseGroupViewModel = browseGroupViewModel;

            DataContext = _browseGroupViewModel;
            var label = new Label(){Text = "Browse Group View"};

            var image = new ImageView(){Image = new Bitmap(_browseGroupViewModel.GroupImage)};
            if(image.Size.Width > 64)
            {
                image.Size = new Size(64,64);
            }

            var joinButton = new Button(){Text = "Join"};
            joinButton.BindDataContext(c => c.Visible, (BrowseGroupViewModel m) => m.CanJoin);
            joinButton.Command = _browseGroupViewModel.Join;

            var leaveButton = new Button(){Text = "Leave"};
            leaveButton.BindDataContext(c => c.Visible, (BrowseGroupViewModel m) => m.CanLeave);
            leaveButton.Command = _browseGroupViewModel.Leave;

            var layout = new DynamicLayout();
            layout.BeginVertical();
            layout.Add(image);
            layout.Add(new Label(){Text = _browseGroupViewModel.Name, TextAlignment = TextAlignment.Center, Font = new Font(FontFamilies.Sans, 24)});
            layout.AddCentered(joinButton);
            layout.AddCentered(leaveButton);
            layout.AddSpace();

            ModalContent = layout;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await _browseGroupViewModel.Initialize();
        }
    }
}
