using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using IT8_TechStore.Theme;

namespace IT8_TechStore.UI.Components
{
    public class SunshineButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsPrimary { get; set; } = true;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int BorderRadius { get; set; } = 8;

        public SunshineButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = AppTheme.BodyBoldFont;
            Cursor = Cursors.Hand;
            Size = new Size(130, 38);
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = IsPrimary
                ? (_isPressed ? AppTheme.PrimaryPressed : (_isHovered ? AppTheme.PrimaryHover : AppTheme.Primary))
                : (_isPressed ? AppTheme.CardHover : (_isHovered ? AppTheme.CardBackground : AppTheme.AppBackground));

            Color textColor = IsPrimary ? AppTheme.TextDark : AppTheme.TextDark;
            Color borderColor = AppTheme.BorderColor;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using GraphicsPath path = GetRoundedRectanglePath(rect, BorderRadius);

            // Fill Background
            using (SolidBrush bgBrush = new SolidBrush(bg))
            {
                g.FillPath(bgBrush, path);
            }

            // Draw Border
            using (Pen borderPen = new Pen(borderColor, IsPrimary ? 1.5f : 1.0f))
            {
                g.DrawPath(borderPen, path);
            }

            // Draw Text & Image
            TextRenderer.DrawText(g, Text, Font, rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
