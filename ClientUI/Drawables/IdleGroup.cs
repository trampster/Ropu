using System;
using System.Windows.Input;
using Eto.Drawing;
using Eto.Forms;

namespace Ropu.ClientUI.Drawables
{
    public class IdleGroup : IDrawable
    {
        readonly FontFamily _fontFamily;
        readonly Font _font;
        readonly SolidBrush _fontBrush;

        const int _padding = 3;

        public int Width => (int)(ImageWidth + _padding + GroupNameSize.Width + _padding +  TriangleSize + (_margin*2));

        public int Height =>  Math.Max(ImageHeight, (int)GroupNameSize.Height) + (_margin *2);

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

        readonly Action _invalidate;

        public IdleGroup(FontFamily fontFamily, Action invalidate)
        {
            _invalidate = invalidate;
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

        public Image? Image
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

        const int _margin = 5;

        public void Draw(Graphics graphics)
        {
            if(Hidden)
            {
                return;
            }
            graphics.SaveTransform();
            graphics.TranslateTransform(X,Y);

            int width = Width;

            int textY = (int)( (ImageHeight - GroupNameSize.Height) /2 ) + _margin;

            if(_isSelected)
            {
                graphics.FillRectangle(Color.FromRgb(0xC0C0C0), 0, 0, width, Height);
            }

            if(Image != null)
            {
                graphics.DrawImage(Image, _margin, _margin);
            }

            graphics.DrawText(_font, _fontBrush, ImageWidth + _padding + _margin, textY, GroupName);

            int triangleX = (int)(ImageWidth + _padding + GroupNameSize.Width + _padding) + _margin;

            int triangleWidth = (int)GroupNameSize.Height;
            int triangleHeigth = GetTriangleHeight(triangleWidth);

            int triangleY = (Height - triangleHeigth)/2;

            _trianglePoints[0].X = triangleX;
            _trianglePoints[0].Y = triangleY;
            _trianglePoints[1].X = triangleX + triangleWidth;  
            _trianglePoints[1].Y = triangleY;
            _trianglePoints[2].X = triangleX + (triangleWidth/2);  
            _trianglePoints[2].Y = triangleY + triangleHeigth;

            graphics.FillPolygon(_trangleColor, _trianglePoints);

            graphics.RestoreTransform();
        }

        readonly Color _trangleColor = Color.FromArgb(70,70,70,0xFF);

        bool _isSelected = false;

        readonly PointF[] _trianglePoints = new PointF[3];

        int GetTriangleHeight(int width)
        {
            return (int)(0.866*width);
        }

        public ICommand? ClickedCommand
        {
            get;
			set;
        }

        public void MouseUp(MouseEventArgs args)
        {
            if(args.Location.X < X || args.Location.Y > Y + Width ||
                args.Location.Y < Y || args.Location.Y > Y + Height)
            {
                return;
            }
            _isSelected = false;
            ClickedCommand?.Execute(this);
            _invalidate();
        }

        public void MouseDown(MouseEventArgs args)
        {
            if(args.Location.X < X || args.Location.Y > Y + Width ||
                args.Location.Y < Y || args.Location.Y > Y + Height)
            {
                return;
            }
            _isSelected = true;
            _invalidate();
        }
    }
}