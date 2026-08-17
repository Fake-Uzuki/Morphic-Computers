using System.Drawing;
using System.Windows.Forms;
using IT8_TechStore.Theme;

namespace IT8_TechStore.UI.Views
{
    public class OrdersPlaceholderView : UserControl
    {
        public OrdersPlaceholderView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;

            Label lbl = new Label
            {
                Text = "📋 Order History & Reports View\n(To be implemented in Part 4)",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            Controls.Add(lbl);
        }
    }
}
