using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using IT8_TechStore.Theme;

namespace IT8_TechStore.UI.Components
{
    public class SunshineCard : Panel
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int BorderRadius { get; set; } = 12;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color CustomBgColor { get; set; } = AppTheme.CardBackground;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color CustomBorderColor { get; set; } = AppTheme.BorderColor;

        public SunshineCard()
        {
            DoubleBuffered = true;
            Padding = new Padding(12);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using GraphicsPath path = GetRoundedRectanglePath(rect, BorderRadius);

            // Fill card body
            using (SolidBrush fillBrush = new SolidBrush(CustomBgColor))
            {
                g.FillPath(fillBrush, path);
            }

            // Draw card border
            using (Pen borderPen = new Pen(CustomBorderColor, 1.2f))
            {
                g.DrawPath(borderPen, path);
            }
        }

        private static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
