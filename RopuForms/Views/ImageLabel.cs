using System;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace RopuForms.Views
{
    public class ImageLabel : IDrawable
    {
        readonly SKPaint _textPaint;
        const int _padding = 3;

        int TextHeight
        {
            get
            {
                SKRect size = new SKRect();
                _textPaint.MeasureText("Test", ref size);
                return (int)size.Height;
            }
        }

        public ImageLabel()
        {
            _textPaint = new SKPaint()
            {
                Color = Xamarin.Forms.Color.FromRgba(50, 50, 50, 0xFF).ToSKColor(),
                TextSize = 60
            };
            Text = "";
        }

        public int Width
        {
            get
            {
                SKRect size = new SKRect();
                _textPaint.MeasureText(Text == null ? "Test" : Text, ref size);
                return Math.Max(ImageWidth, (int)(int)size.Width);
            }
        }

        public int Height
        {
            get
            {
                return ImageHeight + TextHeight + _padding;
            }
        }

        int ImageHeight
        {
            get
            {
                return 128;
            }
        }
        int ImageWidth
        {
            get
            {
                return 128;
            }
        }

        public int X
        {
            set;
            get;
        }

        public int Y
        {
            set;
            get;
        }



        public SKImage? Image
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }

        public bool Hidden
        {
            get;
            set;
        }

        public void Draw(SKCanvas graphics)
        {
            if (Hidden)
            {
                return;
            }

            int width = Width;
            int imageX = width > ImageWidth ? (width - ImageWidth) / 2 + X : X;

            if (Image != null)
            {
                var rect = new SKRect(imageX, Y, imageX + ImageWidth, Y + ImageHeight);
                graphics.DrawImage(Image, rect);
            }

            graphics.DrawText(Text == null ? "" : Text, X, ImageHeight + _padding + Y + TextHeight, _textPaint);
        }
    }
}
