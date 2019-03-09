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
        Font _font;

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
            var font = new Font(_fontFamily, 14);

            graphics.DrawText(_font, new SolidBrush(Color.FromArgb(50,50,50,0xFF)), X, ImageHeight + _padding + Y, Text);
        }
    }

    public class IdleGroup : IDrawable
    {
        readonly FontFamily _fontFamily;
        Font _font;

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

            var textColor = Color.FromArgb(50,50,50,0xFF);

            var fontBrush = new SolidBrush(textColor);

            if(Image != null)
            {
                graphics.DrawImage(Image, 0, 0);
            }

            graphics.DrawText(_font, fontBrush, ImageWidth + _padding, textY, GroupName);

            int triangleX = (int)(ImageWidth + _padding + GroupNameSize.Width + _padding);

            int triangleWidth = (int)GroupNameSize.Height;
            int triangleHeigth = GetTriangleHeight(triangleWidth);

            int triangleY = (Height - triangleHeigth)/2;

            var trangleColor = Color.FromArgb(70,70,70,0xFF);

            graphics.FillPolygon(trangleColor, new PointF[]
            {
                new PointF(triangleX, triangleY),
                new PointF(triangleX + triangleWidth, triangleY),
                new PointF(triangleX + (triangleWidth/2), triangleY + triangleHeigth)
            });

            graphics.RestoreTransform();
        }

        int GetTriangleHeight(int width)
        {
            return (int)(0.866*width);
        }
    }
    
}