using System.Drawing;
using System.Windows.Forms;
using IT8_TechStore.Theme;

namespace IT8_TechStore.UI.Views
{
    public class ProductsPlaceholderView : UserControl
    {
        public ProductsPlaceholderView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;

            Label lbl = new Label
            {
                Text = "📦 Product Inventory View\n(To be implemented in Part 2)",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            Controls.Add(lbl);
        }
    }
}
