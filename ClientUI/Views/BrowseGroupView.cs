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

        public BrowseGroupView(BrowseGroupViewModel browseGroupViewModel, ImageService imageService, INavigator navigator)
            : base(imageService, navigator, "Back")
        {
            _browseGroupViewModel = browseGroupViewModel;

            ModalContent = new Label(){Text = "Browse Group View"};
        }
    }
}
