using Eto.Drawing;

namespace Ropu.ClientUI
{
    public class PttCircle : IDrawable
    {
        readonly FontFamily _fontFamily;
        readonly SolidBrush _brush;
        readonly Pen _pen;

        public PttCircle(FontFamily fontFamily)
        {
            _fontFamily = fontFamily;
            _brush = new SolidBrush(new Color());
            _pen = new Pen(_brush, 6);
            _font = new Font(_fontFamily,12);
            _text = "";
        }

        public int Width
        {
            get;
            set;
        }

        public int Height
        {
            get;
            set;
        }

        public int X 
        {
            get;
            set;
        }
        public int Y 
        { 
            get;
            set;
        }
        public bool Hidden 
        { 
            get;
            set;
        }

        Font _font;
        int _radius;
        SizeF _groupTextSize;

        public int Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                _font = new Font(_fontFamily, _radius/4);
                UpdateGroupTextSize();
            }            
        }

        void UpdateGroupTextSize()
        {
            var text = string.IsNullOrEmpty(Text) ? "Group" : Text;
            _groupTextSize = _font.MeasureString(text);
        }

        public Color Color
        {
            get => _brush.Color;
            set => _brush.Color = value;
        }

        public float PenWidth
        {
            get => _pen.Thickness;
            set 
            {
                _pen.Thickness = value;
            }
        }

        string _text;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                UpdateGroupTextSize();
            }
        }

        public void Draw(Graphics graphics)
        {
            graphics.SaveTransform();
            graphics.TranslateTransform(X,Y);
            int radius = Radius - (int)(9/2);
            int yPosition = -radius;
            int xPosition = -radius;
            _brush.Color = Color;
            int diameter = radius * 2;
            graphics.DrawEllipse(_pen, xPosition, yPosition, diameter, diameter);
            if(!string.IsNullOrEmpty(Text))
            {
                graphics.DrawText(_font, _brush, - (_groupTextSize.Width/2), - (_groupTextSize.Height/2), Text);
            }
            graphics.RestoreTransform();
        }
    }
}