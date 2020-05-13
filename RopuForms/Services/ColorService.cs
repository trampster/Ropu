using Ropu.Gui.Shared.Services;
using Xamarin.Forms;

namespace RopuForms.Services
{
    public class ColorService : IColorService<Color>
    {
        public ColorService()
        {
            Blue = FromRgb(0x3193e3);
        }
        public Color Blue
        {
            get;
        }


        public Color FromRgb(int rgb)
        {
            return Color.FromRgb(rgb >> 16, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }
    }
}
