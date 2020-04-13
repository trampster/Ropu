using System;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace RopuForms.Views
{
    class IdleGroup : IDrawable
    {
        readonly SKPaint _textPaint;

        const int _padding = 10;

        public int Width => (int)(ImageWidth + _padding + GroupNameSize.Width + _padding + TriangleSize);

        public int Height => Math.Max(ImageHeight, (int)GroupNameSize.Height);

        public int X
        {
            set;
            private get;
        }

        public int Y
        {
            set;
            private get;
        }

        public IdleGroup()
        {
            _textPaint = new SKPaint()
            {
                Color = Xamarin.Forms.Color.FromRgba(50, 50, 50, 0xFF).ToSKColor(),
                TextSize = 60
            };
            GroupName = "Team A";
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

        public SKImage? Image
        {
            get;
            set;
        }

        public string GroupName
        {
            get;
            set;
        }

        SKRect GroupNameSize
        {
            get
            {
                SKRect size = new SKRect();
                _textPaint.MeasureText(GroupName == null ? "Test" : GroupName, ref size);
                return size;
            }
        }

        int TriangleSize
        {
            get
            {
                return (int)GroupNameSize.Height;
            }
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
            var matrix = graphics.TotalMatrix;
            graphics.Translate(X, Y);

            int width = Width;

            int textY = (int)((ImageHeight + GroupNameSize.Height) / 2);

            if (Image != null)
            {
                var rect = new SKRect(0, 0, ImageWidth, ImageHeight);
                graphics.DrawImage(Image, rect);
            }

            graphics.DrawText(GroupName, ImageWidth + _padding, textY, _textPaint);

            DrawTriangle(graphics);

            graphics.SetMatrix(matrix);
        }

        void DrawTriangle(SKCanvas graphics)
        {
            int triangleX = (int)(ImageWidth + _padding + GroupNameSize.Width + _padding);

            int triangleWidth = (int)GroupNameSize.Height;
            int triangleHeigth = GetTriangleHeight(triangleWidth);

            int triangleY = (Height - triangleHeigth) / 2;

            var trangleColor = Color.FromRgba(70, 70, 70, 0xFF).ToSKColor();

            _trianglePath.Reset();
            _trianglePath.MoveTo(triangleX, triangleY);
            _trianglePath.LineTo(triangleX + triangleWidth, triangleY);
            _trianglePath.LineTo(triangleX + (triangleWidth / 2), triangleY + triangleHeigth);
            _trianglePath.LineTo(triangleX, triangleY);
            _trianglePath.Close();

            SKPaint fillPaint = new SKPaint
            {
                Style = SKPaintStyle.StrokeAndFill,
                Color = trangleColor
            };

            graphics.DrawPath(_trianglePath, fillPaint);
        }

        readonly SKPath _trianglePath = new SKPath { FillType = SKPathFillType.EvenOdd };

        int GetTriangleHeight(int width)
        {
            return (int)GroupNameSize.Height;
        }
    }
}
