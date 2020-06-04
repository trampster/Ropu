using Eto.Drawing;
using Eto.Forms;

namespace Ropu.ClientUI
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

        public void Draw(Graphics graphics)
        {
            if(Hidden)
            {
                return;
            }
            graphics.SaveTransform();
            graphics.TranslateTransform(X,Y);

            DrawCircle(graphics, AnimationFraction);

            float secondryFraction = (AnimationFraction + 0.5f);
            if(secondryFraction >1) 
            {
                secondryFraction -= 1;
            }
            DrawCircle(graphics, secondryFraction);

            graphics.RestoreTransform();
        }

        public Color CircleColor
        {
            get => _brush.Color;
            set => _brush.Color = value;
        }

        public TransmittingIndicator()
        {
            _brush = new SolidBrush(new Color());
            _pen = new Pen(_brush, 2);
        }

        SolidBrush _brush;
        Pen _pen;

        void DrawCircle(Graphics graphics, float animationFraction)
        {
            int radius = (int)((MaxRadius - MinRadius)*animationFraction) + MinRadius;

            int diameter = radius*2;

            var color = CircleColor;
            color.Ab = (int)(0xFF * (1- animationFraction));
            _brush.Color = color;
            graphics.DrawEllipse(_pen, -radius, -radius, diameter, diameter);
        }

        public void MouseUp(MouseEventArgs args)
        {
        }

        public void MouseDown(MouseEventArgs args)
        {
        }
    }
}