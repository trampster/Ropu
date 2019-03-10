using System;
using Eto.Drawing;

namespace Ropu.ClientUI
{
    public interface IDrawable
    {
        int Width{get;}
        int Height {get;}

        int X {set;}
        int Y {set;}

        bool Hidden
        {
            get;
            set;
        }

        void Draw(Graphics graphics);
    }

    public class ImageLabel : IDrawable
    {
        readonly FontFamily _fontFamily;
        readonly Font _font;

        readonly SolidBrush _textBrush;

        const int _padding = 3;

        int TextHeight => (int)_font.MeasureString("Test").Height;

        public int Width 
        {
            get
            {
                return Math.Max(ImageWidth, (int)_font.MeasureString(Text == null ? "Test" : Text).Width);
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
                if(Image == null)
                {
                    return 32;
                }
                return Image.Size.Height;
            }
        }
        int ImageWidth
        {
            get
            {
                if(Image == null)
                {
                    return 32;
                }
                return Image.Size.Width;
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

        public ImageLabel(FontFamily fontFamily)
        {
            _fontFamily = fontFamily;
            _font = new Font(fontFamily, 12);
            _textBrush = new SolidBrush(Color.FromArgb(50,50,50,0xFF));
        }

        public Image Image
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

        public void Draw(Graphics graphics)
        {
            if(Hidden)
            {
                return;
            }

            int width = Width;
            int imageX = width > ImageWidth ? (width - ImageWidth)/2 + X: X;

            if(Image != null)
            {
                graphics.DrawImage(Image, imageX, Y);
            }

            graphics.DrawText(_font, _textBrush, X, ImageHeight + _padding + Y, Text);
        }
    }

    public class IdleGroup : IDrawable
    {
        readonly FontFamily _fontFamily;
        readonly Font _font;
        readonly SolidBrush _fontBrush;

        const int _padding = 3;

        public int Width => (int)(ImageWidth + _padding + GroupNameSize.Width + _padding +  TriangleSize);

        public int Height =>  Math.Max(ImageHeight, (int)GroupNameSize.Height);

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

        public IdleGroup(FontFamily fontFamily)
        {
            _fontFamily = fontFamily;
            _font = new Font(fontFamily, 12);
            GroupName = "Team A";

            var textColor = Color.FromArgb(50,50,50,0xFF);

            _fontBrush = new SolidBrush(textColor);
        }


        int ImageHeight
        {
            get
            {
                if(Image == null)
                {
                    return 32;
                }
                return Image.Size.Height;
            }
        }

        int ImageWidth
        {
            get
            {
                if(Image == null)
                {
                    return 32;
                }
                return Image.Size.Width;
            }
        }

        public Image Image
        {
            get;
            set;
        }

        public string GroupName
        {
            get;
            set;
        }

        SizeF GroupNameSize => _font.MeasureString(GroupName == null ? "Test" : GroupName);

        int TriangleSize => (int)GroupNameSize.Height;

        public bool Hidden
        {
            get;
            set;
        }

        public void Draw(Graphics graphics)
        {
            if(Hidden)
            {
                return;
            }
            graphics.SaveTransform();
            graphics.TranslateTransform(X,Y);

            int width = Width;

            int textY = (int)( (ImageHeight - GroupNameSize.Height) /2 );

            if(Image != null)
            {
                graphics.DrawImage(Image, 0, 0);
            }

            graphics.DrawText(_font, _fontBrush, ImageWidth + _padding, textY, GroupName);

            int triangleX = (int)(ImageWidth + _padding + GroupNameSize.Width + _padding);

            int triangleWidth = (int)GroupNameSize.Height;
            int triangleHeigth = GetTriangleHeight(triangleWidth);

            int triangleY = (Height - triangleHeigth)/2;

            var trangleColor = Color.FromArgb(70,70,70,0xFF);

            _trianglePoints[0].X = triangleX;
            _trianglePoints[0].Y = triangleY;
            _trianglePoints[1].X = triangleX + triangleWidth;  
            _trianglePoints[1].Y = triangleY;
            _trianglePoints[2].X = triangleX + (triangleWidth/2);  
            _trianglePoints[2].Y = triangleY + triangleHeigth;

            graphics.FillPolygon(trangleColor, _trianglePoints);

            graphics.RestoreTransform();
        }

        readonly PointF[] _trianglePoints = new PointF[3];

        int GetTriangleHeight(int width)
        {
            return (int)(0.866*width);
        }
    }
    
}