using Eto.Drawing;
using Ropu.Gui.Shared.Services;

namespace Ropu.ClientUI.Services
{
    public class ColorService : IColorService<Color>
    {
        public Color FromRgb(int argb)
        {
            return Color.FromRgb(argb);
        }
    }
}