using System.Windows.Media;

namespace costbenefi
{
    public class ButtonAnimation
    {
        public Brush MouseOverBackground { get; set; }

        public ButtonAnimation()
        {
            MouseOverBackground = Brushes.Transparent;
        }

        public ButtonAnimation(Brush mouseOverBackground)
        {
            MouseOverBackground = mouseOverBackground;
        }
    }
}