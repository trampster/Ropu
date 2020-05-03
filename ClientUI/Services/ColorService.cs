using Eto.Drawing;
using Ropu.Gui.Shared.Services;

namespace Ropu.ClientUI.Services
{
    public class ColorService : IColorService<Color>
    {
        public ColorService()
        {
            Blue = FromRgb(0x3193e3);
        }

        public Color FromRgb(int argb)
        {
            return Color.FromRgb(argb);
        }

        public Color Blue
        {
            get;
        }
    }
}