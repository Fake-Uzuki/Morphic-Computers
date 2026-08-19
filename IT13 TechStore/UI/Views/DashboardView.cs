using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IT8_TechStore.Services;
using IT8_TechStore.Theme;
using IT8_TechStore.UI.Components;

namespace IT8_TechStore.UI.Views
{
    public class DashboardView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private SunshineMetricCard _cardRevenue = null!;
        private SunshineMetricCard _cardOrders = null!;
        private SunshineMetricCard _cardProducts = null!;
        private SunshineMetricCard _cardLowStock = null!;
        private DataGridView _gridRecentOrders = null!;
        private TableLayoutPanel _tlpMetrics = null!;
        private TableLayoutPanel _tlpContent = null!;

        public DashboardView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;
            InitializeView();
        }

        private void InitializeView()
        {
            SuspendLayout();
            Controls.Clear();

            // 1. Top Header Banner
            Label lblHeader = new Label
            {
                Text = "Dashboard Overview",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 16),
                AutoSize = true
            };

            Label lblSubHeader = new Label
            {
                Text = "Welcome back! Here is what's happening with your store today.",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 48),
                AutoSize = true
            };

            Controls.Add(lblHeader);
            Controls.Add(lblSubHeader);

            // 2. Metric Cards Panel (Responsive 4-column TableLayoutPanel)
            _tlpMetrics = new TableLayoutPanel
            {
                Location = new Point(20, 80),
                Size = new Size(ClientSize.Width - 40, 115),
                ColumnCount = 4,
                RowCount = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _cardRevenue = new SunshineMetricCard
            {
                IconText = "💰",
                MetricTitle = "TOTAL REVENUE",
                MetricValue = $"${_dataService.GetTotalRevenue():N2}",
                Subtitle = "▲ 14.5% vs last week",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };

            _cardOrders = new SunshineMetricCard
            {
                IconText = "🛍️",
                MetricTitle = "TOTAL ORDERS",
                MetricValue = _dataService.GetTotalOrders().ToString(),
                Subtitle = "Today's transactions",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };

            _cardProducts = new SunshineMetricCard
            {
                IconText = "💻",
                MetricTitle = "TOTAL PRODUCTS",
                MetricValue = _dataService.GetTotalProductsCount().ToString(),
                Subtitle = "Active items in store",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };

            _cardLowStock = new SunshineMetricCard
            {
                IconText = "⚠️",
                MetricTitle = "LOW STOCK ALERTS",
                MetricValue = _dataService.GetLowStockCount().ToString(),
                Subtitle = "Items need reordering",
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0)
            };

            _tlpMetrics.Controls.Add(_cardRevenue, 0, 0);
            _tlpMetrics.Controls.Add(_cardOrders, 1, 0);
            _tlpMetrics.Controls.Add(_cardProducts, 2, 0);
            _tlpMetrics.Controls.Add(_cardLowStock, 3, 0);

            Controls.Add(_tlpMetrics);

            // 3. Responsive Lower Content Panel (65% Table / 35% Side Panel)
            _tlpContent = new TableLayoutPanel
            {
                Location = new Point(20, 210),
                Size = new Size(ClientSize.Width - 40, 460),
                ColumnCount = 2,
                RowCount = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _tlpContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66f));
            _tlpContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));

            // 3A. Left Side: Recent Sales DataGrid Container
            SunshineCard cardOrdersContainer = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12)
            };

            Label lblOrdersTitle = new Label
            {
                Text = "Recent Store Sales & Orders",
                Font = AppTheme.SubheaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(14, 12),
                AutoSize = true
            };

            _gridRecentOrders = new DataGridView
            {
                Location = new Point(14, 44),
                Size = new Size(cardOrdersContainer.Width - 28, cardOrdersContainer.Height - 58),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = AppTheme.CardBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,   // Disable column resizing
                AllowUserToResizeRows = false,      // Disable row resizing
                AllowUserToOrderColumns = false,    // Disable column reordering
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = AppTheme.BorderColor,
                MultiSelect = false
            };

            // Custom Styling for DataGridView to prevent blue highlights and ensure clean Sunshine theme
            _gridRecentOrders.EnableHeadersVisualStyles = false;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle.Font = AppTheme.BodyBoldFont;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.Primary;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridRecentOrders.ColumnHeadersHeight = 36;
            _gridRecentOrders.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            _gridRecentOrders.DefaultCellStyle.BackColor = AppTheme.CardBackground;
            _gridRecentOrders.DefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridRecentOrders.DefaultCellStyle.Font = AppTheme.BodyFont;
            _gridRecentOrders.DefaultCellStyle.SelectionBackColor = AppTheme.CardHover;
            _gridRecentOrders.DefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridRecentOrders.RowTemplate.Height = 34;

            cardOrdersContainer.Controls.Add(lblOrdersTitle);
            cardOrdersContainer.Controls.Add(_gridRecentOrders);
            _tlpContent.Controls.Add(cardOrdersContainer, 0, 0);

            // 3B. Right Side Panel: Quick Shortcuts & Low Stock Watchlist
            SunshineCard cardSidePanel = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0),
                Padding = new Padding(14)
            };

            Label lblShortcutsTitle = new Label
            {
                Text = "Quick Actions & Shortcuts",
                Font = AppTheme.SubheaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(14, 12),
                AutoSize = true
            };

            SunshineButton btnNewSale = new SunshineButton
            {
                Text = "🛒 New POS Sale",
                IsPrimary = true,
                Location = new Point(14, 46),
                Width = cardSidePanel.Width - 28,
                Height = 42,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = AppTheme.BodyBoldFont
            };

            SunshineButton btnAddProduct = new SunshineButton
            {
                Text = "➕ Add New Product",
                IsPrimary = false,
                Location = new Point(14, 98),
                Width = cardSidePanel.Width - 28,
                Height = 42,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = AppTheme.BodyBoldFont
            };

            Label lblLowStockListTitle = new Label
            {
                Text = "Low Stock Watchlist",
                Font = AppTheme.SubheaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(14, 156),
                AutoSize = true
            };

            ListBox lstLowStock = new ListBox
            {
                Location = new Point(14, 186),
                Size = new Size(cardSidePanel.Width - 28, 230),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = AppTheme.AppBackground,
                ForeColor = AppTheme.TextDark,
                Font = AppTheme.BodyFont,
                BorderStyle = BorderStyle.FixedSingle
            };

            foreach (var item in _dataService.Products.Where(p => p.IsLowStock))
            {
                lstLowStock.Items.Add($"⚠️ {item.Name} - Qty: {item.StockQuantity}");
            }

            cardSidePanel.Controls.Add(lblShortcutsTitle);
            cardSidePanel.Controls.Add(btnNewSale);
            cardSidePanel.Controls.Add(btnAddProduct);
            cardSidePanel.Controls.Add(lblLowStockListTitle);
            cardSidePanel.Controls.Add(lstLowStock);

            _tlpContent.Controls.Add(cardSidePanel, 1, 0);
            Controls.Add(_tlpContent);

            LoadOrderGridData();

            ResumeLayout(false);
        }

        public void RefreshDashboard()
        {
            _cardRevenue.MetricValue = $"${_dataService.GetTotalRevenue():N2}";
            _cardOrders.MetricValue = _dataService.GetTotalOrders().ToString();
            _cardProducts.MetricValue = _dataService.GetTotalProductsCount().ToString();
            _cardLowStock.MetricValue = _dataService.GetLowStockCount().ToString();
            LoadOrderGridData();
        }

        private void LoadOrderGridData()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Order ID", typeof(string));
            dt.Columns.Add("Customer", typeof(string));
            dt.Columns.Add("Date & Time", typeof(string));
            dt.Columns.Add("Items Count", typeof(int));
            dt.Columns.Add("Payment", typeof(string));
            dt.Columns.Add("Total Amount", typeof(string));

            foreach (var o in _dataService.Orders)
            {
                dt.Rows.Add(o.Id, o.CustomerName, o.CreatedAt.ToString("g"), o.Items.Count, o.PaymentMethod, $"${o.TotalAmount:N2}");
            }

            _gridRecentOrders.DataSource = dt;
        }
    }
}
