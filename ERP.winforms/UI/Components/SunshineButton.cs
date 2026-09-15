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
        public bool IsDark { get; set; } = false;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color? CustomBgColor { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color? CustomTextColor { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color? CustomBorderColor { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int BorderRadius { get; set; } = 4;

        public SunshineButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = AppTheme.BodyBoldFont;
            Cursor = Cursors.Hand;
            Size = new Size(130, 36);
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

            Color bgColor;
            Color textColor;
            Color borderColor;

            if (CustomBgColor.HasValue)
            {
                bgColor = _isPressed ? Color.FromArgb(Math.Max(0, CustomBgColor.Value.R - 20), Math.Max(0, CustomBgColor.Value.G - 20), Math.Max(0, CustomBgColor.Value.B - 20))
                    : (_isHovered ? Color.FromArgb(Math.Min(255, CustomBgColor.Value.R + 15), Math.Min(255, CustomBgColor.Value.G + 15), Math.Min(255, CustomBgColor.Value.B + 15)) : CustomBgColor.Value);
                textColor = CustomTextColor ?? AppTheme.TextDark;
                borderColor = CustomBorderColor ?? AppTheme.CardBorder;
            }
            else if (IsDark)
            {
                bgColor = _isPressed ? Color.FromArgb(10, 10, 8) : (_isHovered ? Color.FromArgb(35, 36, 30) : AppTheme.HeaderBg);
                textColor = Color.White;
                borderColor = Color.FromArgb(60, 62, 52);
            }
            else if (IsPrimary)
            {
                bgColor = _isPressed ? AppTheme.PrimaryPressed : (_isHovered ? AppTheme.PrimaryHover : AppTheme.Primary);
                textColor = AppTheme.TextDark;
                borderColor = Color.FromArgb(215, 175, 45);
            }
            else
            {
                bgColor = _isPressed ? Color.FromArgb(240, 238, 230) : (_isHovered ? Color.FromArgb(250, 248, 242) : Color.White);
                textColor = AppTheme.TextDark;
                borderColor = CustomBorderColor ?? AppTheme.CardBorder;
            }

            Rectangle drawRect = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width - 1, ClientRectangle.Height - 1);

            if (BorderRadius > 0)
            {
                using (GraphicsPath path = GetRoundedPath(drawRect, BorderRadius))
                {
                    using (SolidBrush brush = new SolidBrush(bgColor))
                    {
                        g.FillPath(brush, path);
                    }

                    using (Pen pen = new Pen(borderColor, 1f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    g.FillRectangle(brush, drawRect);
                }
                using (Pen pen = new Pen(borderColor, 1f))
                {
                    g.DrawRectangle(pen, drawRect);
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
