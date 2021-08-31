using Eto.Drawing;
using Ropu.Gui.Shared.Services;

namespace Ropu.ClientUI.Services
{
    public class ColorService : IColorService<Color>
    {
        public ColorService()
        {
            Blue = FromRgb(0x3193e3);
            Grey = FromRgb(0x505050);
            Transparent = Color.FromArgb(0x00000000);
        }

        public Color FromRgb(int argb)
        {
            return Color.FromRgb(argb);
        }

        public Color Blue
        {
            get;
        }

        public Color Grey
        {
            get;
        }

        public Color Transparent
        {
            get;
        }
    }
}