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

            graphics.DrawText(Text, X, ImageHeight + _padding + Y + TextHeight, _textPaint);
        }
    }

    //public class IdleGroup : IDrawable
    //{
    //    readonly FontFamily _fontFamily;
    //    readonly Font _font;
    //    readonly SolidBrush _fontBrush;

    //    const int _padding = 3;

    //    public int Width => (int)(ImageWidth + _padding + GroupNameSize.Width + _padding + TriangleSize);

    //    public int Height => Math.Max(ImageHeight, (int)GroupNameSize.Height);

    //    public int X
    //    {
    //        set;
    //        private get;
    //    }

    //    public int Y
    //    {
    //        set;
    //        private get;
    //    }

    //    public IdleGroup(FontFamily fontFamily)
    //    {
    //        _fontFamily = fontFamily;
    //        _font = new Font(fontFamily, 12);
    //        GroupName = "Team A";

    //        var textColor = Color.FromArgb(50, 50, 50, 0xFF);

    //        _fontBrush = new SolidBrush(textColor);
    //    }


    //    int ImageHeight
    //    {
    //        get
    //        {
    //            if (Image == null)
    //            {
    //                return 32;
    //            }
    //            return Image.Size.Height;
    //        }
    //    }

    //    int ImageWidth
    //    {
    //        get
    //        {
    //            if (Image == null)
    //            {
    //                return 32;
    //            }
    //            return Image.Size.Width;
    //        }
    //    }

    //    public Image? Image
    //    {
    //        get;
    //        set;
    //    }

    //    public string GroupName
    //    {
    //        get;
    //        set;
    //    }

    //    SizeF GroupNameSize => _font.MeasureString(GroupName == null ? "Test" : GroupName);

    //    int TriangleSize => (int)GroupNameSize.Height;

    //    public bool Hidden
    //    {
    //        get;
    //        set;
    //    }

    //    public void Draw(Graphics graphics)
    //    {
    //        if (Hidden)
    //        {
    //            return;
    //        }
    //        graphics.SaveTransform();
    //        graphics.TranslateTransform(X, Y);

    //        int width = Width;

    //        int textY = (int)((ImageHeight - GroupNameSize.Height) / 2);

    //        if (Image != null)
    //        {
    //            graphics.DrawImage(Image, 0, 0);
    //        }

    //        graphics.DrawText(_font, _fontBrush, ImageWidth + _padding, textY, GroupName);

    //        int triangleX = (int)(ImageWidth + _padding + GroupNameSize.Width + _padding);

    //        int triangleWidth = (int)GroupNameSize.Height;
    //        int triangleHeigth = GetTriangleHeight(triangleWidth);

    //        int triangleY = (Height - triangleHeigth) / 2;

    //        var trangleColor = Color.FromArgb(70, 70, 70, 0xFF);

    //        _trianglePoints[0].X = triangleX;
    //        _trianglePoints[0].Y = triangleY;
    //        _trianglePoints[1].X = triangleX + triangleWidth;
    //        _trianglePoints[1].Y = triangleY;
    //        _trianglePoints[2].X = triangleX + (triangleWidth / 2);
    //        _trianglePoints[2].Y = triangleY + triangleHeigth;

    //        graphics.FillPolygon(trangleColor, _trianglePoints);

    //        graphics.RestoreTransform();
    //    }

    //    readonly PointF[] _trianglePoints = new PointF[3];

    //    int GetTriangleHeight(int width)
    //    {
    //        return (int)(0.866 * width);
    //    }
    //}
}
