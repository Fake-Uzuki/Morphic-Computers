using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.winforms.Theme;

namespace ERP.winforms.UI.Components
{
    public class SunshineMetricCard : SunshineCard
    {
        private readonly Label _lblTitle;
        private readonly Label _lblValue;
        private readonly Label _lblSubtitle;
        private readonly Label _lblBadge;
        private bool _isMainCard;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string MetricTitle
        {
            get => _lblTitle.Text;
            set => _lblTitle.Text = value;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string MetricValue
        {
            get => _lblValue.Text;
            set => _lblValue.Text = value;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Subtitle
        {
            get => _lblSubtitle.Text;
            set => _lblSubtitle.Text = value;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string BadgeText
        {
            get => _lblBadge.Text;
            set
            {
                _lblBadge.Text = value;
                _lblBadge.Visible = !string.IsNullOrWhiteSpace(value);
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string IconText
        {
            get => BadgeText;
            set => BadgeText = value; // compatibility
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsMainCard
        {
            get => _isMainCard;
            set
            {
                _isMainCard = value;
                ApplyMainCardStyle();
            }
        }

        public SunshineMetricCard()
        {
            Height = 120;
            Padding = new Padding(16);
            CustomBgColor = AppTheme.CardBackground;
            CustomBorderColor = AppTheme.BorderColor;
            Cursor = Cursors.Hand;

            _lblBadge = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                BackColor = Color.FromArgb(45, 36, 20),
                Size = new Size(70, 20),
                Location = new Point(16, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                Cursor = Cursors.Hand
            };

            _lblTitle = new Label
            {
                Text = "METRIC TITLE",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(16, 16),
                Size = new Size(240, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            _lblValue = new Label
            {
                Text = "0",
                Font = AppTheme.StatValueFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(16, 40),
                Size = new Size(280, 42),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            _lblSubtitle = new Label
            {
                Text = "",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(16, 84),
                Size = new Size(280, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            _lblBadge.Click += (s, e) => OnClick(e);
            _lblTitle.Click += (s, e) => OnClick(e);
            _lblValue.Click += (s, e) => OnClick(e);
            _lblSubtitle.Click += (s, e) => OnClick(e);

            Controls.Add(_lblBadge);
            Controls.Add(_lblTitle);
            Controls.Add(_lblValue);
            Controls.Add(_lblSubtitle);
        }

        private void ApplyMainCardStyle()
        {
            if (_isMainCard)
            {
                CustomBgColor = Color.FromArgb(38, 30, 15);
                CustomBorderColor = AppTheme.Primary;
                _lblTitle.ForeColor = AppTheme.Primary;
                _lblTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                _lblValue.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
                _lblValue.ForeColor = Color.White;
                _lblSubtitle.ForeColor = Color.FromArgb(240, 190, 80);
            }
            else
            {
                CustomBgColor = AppTheme.CardBackground;
                CustomBorderColor = AppTheme.BorderColor;
                _lblTitle.ForeColor = AppTheme.TextMuted;
                _lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                _lblValue.Font = AppTheme.StatValueFont;
                _lblValue.ForeColor = AppTheme.TextDark;
                _lblSubtitle.ForeColor = AppTheme.TextMuted;
            }
            Invalidate();
        }
    }
}
