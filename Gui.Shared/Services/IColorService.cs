namespace Ropu.Gui.Shared.Services
{
    public interface IColorService<ColorT>
    {
        ColorT FromRgb(int argb);
    }
}