using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace RopuForms.Views
{
    public interface IDrawable
    {
        int Width { get; }
        int Height { get; }

        int X { set; }
        int Y { set; }

        bool Hidden
        {
            get;
            set;
        }

        void Draw(SKCanvas cavas);
    }
}
