using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ERP.winforms.Theme;

namespace ERP.winforms.UI.Components
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
            Size = new Size(140, 40);
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

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bgColor = IsPrimary
                ? (_isPressed ? AppTheme.PrimaryPressed : (_isHovered ? AppTheme.PrimaryHover : AppTheme.Primary))
                : (_isPressed ? AppTheme.CardHover : (_isHovered ? AppTheme.CardBackground : AppTheme.AppBackground));

            Color textColor = IsPrimary ? AppTheme.TextDark : AppTheme.TextDark;
            Color borderColor = AppTheme.BorderColor;

            using (GraphicsPath path = GetRoundedPath(ClientRectangle, BorderRadius))
            {
                Region = new Region(path);

                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(borderColor, 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                ClientRectangle,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            );
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            path.AddArc(arc, 180, 90);

            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = rect.X;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
