using Eto.Forms;
using Ropu.ClientUI.Services;
using Ropu.Gui.Shared.Services;

namespace Ropu.ClientUI.Views
{
    public abstract class ModalPage : Panel
    {
        readonly ImageService _imageService;
        readonly Panel _modalContent;

        public ModalPage(ImageService imageService, INavigator navigator, string backText)
        {
            _imageService = imageService;

            var backImageView = new ImageView(){Image = _imageService.Back};
            backImageView.MouseDown += (sender, args) => 
            {
                args.Handled = true;
            };
            backImageView.MouseUp += (sender, args) => navigator.Back();
            var cancelLabel = new Label() {Text = backText};
            cancelLabel.MouseDown += (sender, args) => args.Handled = true;
            cancelLabel.MouseUp += (sender, args) => navigator.Back();


            var cancelLayout = new StackLayout();
            cancelLayout.Padding = 10;
            cancelLayout.Orientation = Orientation.Horizontal;
            cancelLayout.Spacing = 5;
            cancelLayout.Items.Add(backImageView);
            cancelLayout.Items.Add(cancelLabel);

            _modalContent = new Panel();
            

            var pageLayout = new DynamicLayout();
            pageLayout.BeginVertical();
            pageLayout.Add(cancelLayout);
            pageLayout.Add(_modalContent, false, true);
            pageLayout.BeginVertical();
                
            base.Content = pageLayout;
        }

        public Control ModalContent
        {
            set => _modalContent.Content = value;
        }
    }
}
