using System;
using System.Collections.Specialized;
using Eto.Drawing;
using Eto.Forms;
using Ropu.Gui.Shared.ViewModels;
using Ropu.Shared.Groups;

namespace Ropu.ClientUI.Views
{
    public class BrowseGroupView : Panel
    {
        readonly BrowseGroupViewModel _browseGroupViewModel;

        public BrowseGroupView(BrowseGroupViewModel browseGroupViewModel)
        {
            _browseGroupViewModel = browseGroupViewModel;
        }
    }
}
