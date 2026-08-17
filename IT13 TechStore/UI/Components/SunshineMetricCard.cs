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
            Size = new Size(240, 110);
            Padding = new Padding(14);

            _lblIcon = new Label
            {
                Text = "📊",
                Font = new Font("Segoe UI", 22F),
                ForeColor = AppTheme.TextDark,
                AutoSize = true,
                Location = new Point(14, 14)
            };

            _lblTitle = new Label
            {
                Text = "Metric Title",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                AutoSize = true,
                Location = new Point(65, 14)
            };

            _lblValue = new Label
            {
                Text = "0",
                Font = AppTheme.StatValueFont,
                ForeColor = AppTheme.TextDark,
                AutoSize = true,
                Location = new Point(65, 34)
            };

            _lblSubtitle = new Label
            {
                Text = "+0% vs last month",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                AutoSize = true,
                Location = new Point(65, 76)
            };

            Controls.Add(_lblIcon);
            Controls.Add(_lblTitle);
            Controls.Add(_lblValue);
            Controls.Add(_lblSubtitle);
        }
    }
}
