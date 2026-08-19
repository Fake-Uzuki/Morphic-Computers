using System;
using System.Drawing;
using System.Windows.Forms;
using IT8_TechStore.Theme;

namespace IT8_TechStore.UI.Components
{
    public class SunshineMetricCard : SunshineCard
    {
        private readonly Label _lblTitle;
        private readonly Label _lblValue;
        private readonly Label _lblSubtitle;
        private readonly Label _lblIcon;

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
        public string IconText
        {
            get => _lblIcon.Text;
            set => _lblIcon.Text = value;
        }

        public SunshineMetricCard()
        {
            Height = 110;
            Padding = new Padding(10);
            CustomBgColor = AppTheme.CardBackground;
            CustomBorderColor = AppTheme.BorderColor;
            Cursor = Cursors.Hand;

            _lblIcon = new Label
            {
                Text = "📊",
                Font = new Font("Segoe UI", 20F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                Size = new Size(38, 38),
                Location = new Point(10, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            _lblTitle = new Label
            {
                Text = "METRIC TITLE",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(52, 14),
                Size = new Size(160, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            _lblValue = new Label
            {
                Text = "0",
                Font = AppTheme.StatValueFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(52, 34),
                Size = new Size(170, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            _lblSubtitle = new Label
            {
                Text = "+0% vs last month",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(52, 74),
                Size = new Size(170, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            // Forward click events from child labels to parent card container
            _lblIcon.Click += (s, e) => OnClick(e);
            _lblTitle.Click += (s, e) => OnClick(e);
            _lblValue.Click += (s, e) => OnClick(e);
            _lblSubtitle.Click += (s, e) => OnClick(e);

            Controls.Add(_lblIcon);
            Controls.Add(_lblTitle);
            Controls.Add(_lblValue);
            Controls.Add(_lblSubtitle);
        }
    }
}
