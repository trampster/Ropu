using Eto.Drawing;

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
            get;
            set;
        }

        void DrawCircle(Graphics graphics, float animationFraction)
        {
            int radius = (int)((MaxRadius - MinRadius)*animationFraction) + MinRadius;

            int diameter = radius*2;

            var color = CircleColor;
            color.Ab = (int)(0xFF * (1- animationFraction));
            var pen = new Pen(color, 2);
            graphics.DrawEllipse(pen, -radius, -radius, diameter, diameter);
        }
    }
}