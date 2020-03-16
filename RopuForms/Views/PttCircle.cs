using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace RopuForms.Views
{
    public class PttCircle : IDrawable
    {
        readonly SKPaint _pen;
        readonly SKPaint _textPaint;

        public PttCircle()
        {
            _pen = new SKPaint()
            {
                Style = SKPaintStyle.Stroke,
                Color = Xamarin.Forms.Color.Red.ToSKColor(),
                StrokeWidth = 25
            };
            _text = "";
            _textPaint = new SKPaint()
            {
                Color = Xamarin.Forms.Color.Red.ToSKColor(),
                TextSize = 12
            };
            _groupTextSize = new SKRect();
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

        int _radius;
        SKRect _groupTextSize;

        public int Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                _textPaint.TextSize = _radius / 4;
                UpdateGroupTextSize();
            }
        }

        void UpdateGroupTextSize()
        {
            var text = string.IsNullOrEmpty(Text) ? "Group" : Text;
            _textPaint.MeasureText(text, ref _groupTextSize);

        }

        public SKColor Color
        {
            get => _pen.Color;
            set
            {
                _pen.Color = value;
                _textPaint.Color = value;
            }
        }

        public float PenWidth
        {
            get => _pen.StrokeWidth;
            set
            {
                _pen.StrokeWidth = value;
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

        public void Draw(SKCanvas graphics)
        {
            var matrix = graphics.TotalMatrix;
            graphics.Translate(X, Y);
            int radius = Radius - (int)(45 / 2);
            int yPosition = -radius;
            int xPosition = -radius;
            int diameter = radius * 2;
            graphics.DrawCircle(0, 0, diameter/2, _pen);
            if (!string.IsNullOrEmpty(Text))
            {
                graphics.DrawText(Text, -(_groupTextSize.Width / 2), (_groupTextSize.Height / 2), _textPaint);
            }
            graphics.SetMatrix(matrix);
        }

       
    }
}
