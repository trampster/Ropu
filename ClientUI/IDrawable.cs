using System;
using Eto.Drawing;
using Eto.Forms;

namespace Ropu.ClientUI
{
    public interface IDrawable
    {
        int Width {get;}
        int Height {get;}

        int X {set;}
        int Y {set;}

        bool Hidden
        {
            get;
            set;
        }

        void Draw(Graphics graphics);

        void MouseUp(MouseEventArgs args);

        void MouseDown(MouseEventArgs args);
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
            Text = "";
        }

        public Image? Image
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

        public void MouseUp(MouseEventArgs args)
        {
        }

        public void MouseDown(MouseEventArgs args)
        {
        }
    }    
}