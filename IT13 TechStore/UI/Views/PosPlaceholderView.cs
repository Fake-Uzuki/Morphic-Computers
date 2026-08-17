using System.Drawing;
using System.Windows.Forms;
using IT8_TechStore.Theme;

namespace IT8_TechStore.UI.Views
{
    public class PosPlaceholderView : UserControl
    {
        public PosPlaceholderView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;

            Label lbl = new Label
            {
                Text = "🛒 Point of Sale (POS) Terminal View\n(To be implemented in Part 3)",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            Controls.Add(lbl);
        }
    }
}
