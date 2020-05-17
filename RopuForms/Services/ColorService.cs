using Ropu.Gui.Shared.Services;
using Xamarin.Forms;

namespace RopuForms.Services
{
    public class ColorService : IColorService<Color>
    {
        public ColorService()
        {
        }
        public Color Blue => RopuBlue;

        public Color FromRgb(int rgb) => RopuFromRgb(rgb);

        static Color RopuFromRgb(int rgb)
        {
            return Color.FromRgb(rgb >> 16, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }

        public static readonly Color RopuBlue = RopuFromRgb(0x3193e3);
        public static readonly Color RopuGreen = RopuFromRgb(0x31e393);
        public static readonly Color RopuGray = RopuFromRgb(0x999999);
        public static readonly Color RopuRed = RopuFromRgb(0xFF6961);
    }
}
