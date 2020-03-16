using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace RopuForms.Views
{
    public class TransmittingIndicator : IDrawable
    {
        public int Width
        {
            get => MaxRadius;
        }

        public int Height
        {
            get => MaxRadius;
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

        public float AnimationFraction
        {
            get;
            set;
        }

        public int MinRadius
        {
            get;
            set;
        }

        public int MaxRadius
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

            DrawCircle(graphics, AnimationFraction);

            float secondryFraction = (AnimationFraction + 0.5f);
            if (secondryFraction > 1)
            {
                secondryFraction -= 1;
            }
            DrawCircle(graphics, secondryFraction);

            graphics.SetMatrix(matrix);
        }

        public SKColor CircleColor
        {
            get => _pen.Color;
            set => _pen.Color = value;
        }

        public TransmittingIndicator()
        {
            _pen = new SKPaint()
            {
                Style = SKPaintStyle.Stroke,
                Color = Xamarin.Forms.Color.Black.ToSKColor(),
                StrokeWidth = 10
            };
        }

        SKPaint _pen;

        void DrawCircle(SKCanvas graphics, float animationFraction)
        {
            int radius = (int)((MaxRadius - MinRadius) * animationFraction) + MinRadius;

            int diameter = radius * 2;

            var color = CircleColor;
            color = color.WithAlpha((byte)(0xFF * (1 - animationFraction)));
            _pen.Color = color;
            graphics.DrawCircle(0, 0, radius, _pen);
        }
    }
}
