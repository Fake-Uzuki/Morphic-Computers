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
    /// <summary>
    /// CONCEPT 2 LAYOUT with Full Interactive Navigation
    /// Cards and Quick Action buttons redirect smoothly to their respective views.
    /// </summary>
    public class DashboardView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        public Action? OnNavigateToPOSRequest;
        public Action<bool>? OnNavigateToProductsRequest;
        public Action? OnNavigateToOrdersRequest;

        private SunshineMetricCard _cardRevenue = null!;
        private SunshineMetricCard _cardOrders = null!;
        private SunshineMetricCard _cardProducts = null!;
        private SunshineMetricCard _cardLowStock = null!;
        private DataGridView _gridRecentOrders = null!;
        private TableLayoutPanel _tlpCategoryMeters = null!;

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

            // 1. Top Subheader Banner
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

            // 2. Metric Cards Panel (Interactive Floating Stat Cards)
            TableLayoutPanel tlpMetrics = new TableLayoutPanel
            {
                Location = new Point(20, 80),
                Size = new Size(ClientSize.Width - 40, 115),
                ColumnCount = 4,
                RowCount = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _cardRevenue = new SunshineMetricCard
            {
                IconText = "💰",
                MetricTitle = "TOTAL REVENUE",
                MetricValue = $"${_dataService.GetTotalRevenue():N2}",
                Subtitle = "▲ Click to view reports",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };
            _cardRevenue.Click += (s, e) => OnNavigateToOrdersRequest?.Invoke();

            _cardOrders = new SunshineMetricCard
            {
                IconText = "🛍️",
                MetricTitle = "TOTAL ORDERS",
                MetricValue = _dataService.GetTotalOrders().ToString(),
                Subtitle = "Click to view sales",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };
            _cardOrders.Click += (s, e) => OnNavigateToOrdersRequest?.Invoke();

            _cardProducts = new SunshineMetricCard
            {
                IconText = "💻",
                MetricTitle = "TOTAL PRODUCTS",
                MetricValue = _dataService.GetTotalProductsCount().ToString(),
                Subtitle = "Click to manage stock",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };
            _cardProducts.Click += (s, e) => OnNavigateToProductsRequest?.Invoke(false);

            _cardLowStock = new SunshineMetricCard
            {
                IconText = "⚠️",
                MetricTitle = "LOW STOCK ALERTS",
                MetricValue = _dataService.GetLowStockCount().ToString(),
                Subtitle = "Click to reorder stock",
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0)
            };
            _cardLowStock.Click += (s, e) => OnNavigateToProductsRequest?.Invoke(false);

            tlpMetrics.Controls.Add(_cardRevenue, 0, 0);
            tlpMetrics.Controls.Add(_cardOrders, 1, 0);
            tlpMetrics.Controls.Add(_cardProducts, 2, 0);
            tlpMetrics.Controls.Add(_cardLowStock, 3, 0);

            Controls.Add(tlpMetrics);

            // 3. Middle Section: Recent Sales & Department Breakdown Split (60% / 40%)
            TableLayoutPanel tlpMiddle = new TableLayoutPanel
            {
                Location = new Point(20, 210),
                Size = new Size(ClientSize.Width - 40, 360),
                ColumnCount = 2,
                RowCount = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            tlpMiddle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tlpMiddle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            // 3A. Left Side: Sales Orders DataGrid
            SunshineCard cardOrders = new SunshineCard
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
                Size = new Size(cardOrders.Width - 28, cardOrders.Height - 56),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = AppTheme.CardBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = AppTheme.BorderColor,
                MultiSelect = false
            };

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

            cardOrders.Controls.Add(lblOrdersTitle);
            cardOrders.Controls.Add(_gridRecentOrders);
            tlpMiddle.Controls.Add(cardOrders, 0, 0);

            // 3B. Right Side: Department Stock Breakdown
            SunshineCard cardBreakdown = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0),
                Padding = new Padding(14)
            };

            Label lblBreakdownTitle = new Label
            {
                Text = "Department Stock Distribution",
                Font = AppTheme.SubheaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(14, 12),
                AutoSize = true
            };

            _tlpCategoryMeters = new TableLayoutPanel
            {
                Location = new Point(14, 46),
                Size = new Size(cardBreakdown.Width - 28, cardBreakdown.Height - 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 1,
                RowCount = 5
            };

            PopulateCategoryMeters();

            cardBreakdown.Controls.Add(lblBreakdownTitle);
            cardBreakdown.Controls.Add(_tlpCategoryMeters);
            tlpMiddle.Controls.Add(cardBreakdown, 1, 0);

            Controls.Add(tlpMiddle);

            // 4. Bottom Horizontal Command Hub Bar
            SunshineCard cardActionHub = new SunshineCard
            {
                Location = new Point(20, 580),
                Size = new Size(ClientSize.Width - 40, 75),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12)
            };

            TableLayoutPanel tlpActions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            tlpActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            SunshineButton btnNewSale = new SunshineButton { Text = "🛒 New POS Sale", IsPrimary = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
            btnNewSale.Click += (s, e) => OnNavigateToPOSRequest?.Invoke();

            SunshineButton btnAddProd = new SunshineButton { Text = "➕ Add Product", IsPrimary = false, Dock = DockStyle.Fill, Margin = new Padding(3, 0, 3, 0) };
            btnAddProd.Click += (s, e) => OnNavigateToProductsRequest?.Invoke(true);

            SunshineButton btnInventory = new SunshineButton { Text = "💻 Inventory List", IsPrimary = false, Dock = DockStyle.Fill, Margin = new Padding(3, 0, 3, 0) };
            btnInventory.Click += (s, e) => OnNavigateToProductsRequest?.Invoke(false);

            SunshineButton btnReports = new SunshineButton { Text = "📋 Sales Reports", IsPrimary = false, Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0) };
            btnReports.Click += (s, e) => OnNavigateToOrdersRequest?.Invoke();

            tlpActions.Controls.Add(btnNewSale, 0, 0);
            tlpActions.Controls.Add(btnAddProd, 1, 0);
            tlpActions.Controls.Add(btnInventory, 2, 0);
            tlpActions.Controls.Add(btnReports, 3, 0);

            cardActionHub.Controls.Add(tlpActions);
            Controls.Add(cardActionHub);

            LoadOrderGridData();

            ResumeLayout(false);
        }

        private void PopulateCategoryMeters()
        {
            _tlpCategoryMeters.Controls.Clear();
            int total = Math.Max(1, _dataService.GetTotalProductsCount());
            int y = 0;

            foreach (var cat in _dataService.Categories.Take(5))
            {
                int count = _dataService.Products.Count(p => p.CategoryName == cat.Name);
                int pct = (int)((double)count / total * 100);

                Panel pnl = new Panel { Dock = DockStyle.Fill, Height = 48, Margin = new Padding(0, 2, 0, 6) };
                Label lbl = new Label { Text = $"{cat.Icon} {cat.Name} ({count} items)", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(0, 0), AutoSize = true };

                ProgressBar pb = new ProgressBar
                {
                    Location = new Point(0, 24),
                    Size = new Size(_tlpCategoryMeters.Width - 10, 14),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Maximum = 100,
                    Value = Math.Min(pct, 100)
                };

                pnl.Controls.Add(lbl);
                pnl.Controls.Add(pb);
                _tlpCategoryMeters.Controls.Add(pnl, 0, y++);
            }
        }

        public void RefreshDashboard()
        {
            _cardRevenue.MetricValue = $"${_dataService.GetTotalRevenue():N2}";
            _cardOrders.MetricValue = _dataService.GetTotalOrders().ToString();
            _cardProducts.MetricValue = _dataService.GetTotalProductsCount().ToString();
            _cardLowStock.MetricValue = _dataService.GetLowStockCount().ToString();
            LoadOrderGridData();
            PopulateCategoryMeters();
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
