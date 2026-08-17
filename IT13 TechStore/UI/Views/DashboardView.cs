using System;
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
                Location = new Point(24, 20),
                AutoSize = true
            };

            Label lblSubHeader = new Label
            {
                Text = "Welcome back! Here is what's happening with your store today.",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, 52),
                AutoSize = true
            };

            Controls.Add(lblHeader);
            Controls.Add(lblSubHeader);

            // 2. Metric Cards Panel
            TableLayoutPanel tlpMetrics = new TableLayoutPanel
            {
                Location = new Point(24, 90),
                Size = new Size(1000, 120),
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
                Subtitle = "▲ 14.5% vs last week",
                Dock = DockStyle.Fill
            };

            _cardOrders = new SunshineMetricCard
            {
                IconText = "🛍️",
                MetricTitle = "TOTAL ORDERS",
                MetricValue = _dataService.GetTotalOrders().ToString(),
                Subtitle = "Today's transactions",
                Dock = DockStyle.Fill
            };

            _cardProducts = new SunshineMetricCard
            {
                IconText = "💻",
                MetricTitle = "TOTAL PRODUCTS",
                MetricValue = _dataService.GetTotalProductsCount().ToString(),
                Subtitle = "Active SKUs in store",
                Dock = DockStyle.Fill
            };

            _cardLowStock = new SunshineMetricCard
            {
                IconText = "⚠️",
                MetricTitle = "LOW STOCK ALERTS",
                MetricValue = _dataService.GetLowStockCount().ToString(),
                Subtitle = "Items need reordering",
                Dock = DockStyle.Fill
            };

            tlpMetrics.Controls.Add(_cardRevenue, 0, 0);
            tlpMetrics.Controls.Add(_cardOrders, 1, 0);
            tlpMetrics.Controls.Add(_cardProducts, 2, 0);
            tlpMetrics.Controls.Add(_cardLowStock, 3, 0);

            Controls.Add(tlpMetrics);

            // 3. Middle Section: Recent Orders & Quick Actions Panel
            SunshineCard cardOrdersContainer = new SunshineCard
            {
                Location = new Point(24, 230),
                Size = new Size(650, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            Label lblOrdersTitle = new Label
            {
                Text = "Recent Store Sales & Orders",
                Font = AppTheme.SubheaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(16, 16),
                AutoSize = true
            };

            _gridRecentOrders = new DataGridView
            {
                Location = new Point(16, 50),
                Size = new Size(618, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = AppTheme.CardBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = AppTheme.BorderColor
            };

            // Custom Styling for DataGridView
            _gridRecentOrders.EnableHeadersVisualStyles = false;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle.Font = AppTheme.BodyBoldFont;
            _gridRecentOrders.ColumnHeadersHeight = 36;

            _gridRecentOrders.DefaultCellStyle.BackColor = AppTheme.CardBackground;
            _gridRecentOrders.DefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridRecentOrders.DefaultCellStyle.Font = AppTheme.BodyFont;
            _gridRecentOrders.DefaultCellStyle.SelectionBackColor = AppTheme.CardHover;
            _gridRecentOrders.DefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridRecentOrders.RowTemplate.Height = 34;

            cardOrdersContainer.Controls.Add(lblOrdersTitle);
            cardOrdersContainer.Controls.Add(_gridRecentOrders);
            Controls.Add(cardOrdersContainer);

            // 4. Right Side Panel: Store Stock Highlights & Quick Shortcuts
            SunshineCard cardSidePanel = new SunshineCard
            {
                Location = new Point(690, 230),
                Size = new Size(334, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            Label lblShortcutsTitle = new Label
            {
                Text = "Quick Actions & Shortcuts",
                Font = AppTheme.SubheaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(16, 16),
                AutoSize = true
            };

            SunshineButton btnNewSale = new SunshineButton
            {
                Text = "🛒 New POS Sale",
                IsPrimary = true,
                Location = new Point(16, 55),
                Size = new Size(300, 44),
                Font = AppTheme.BodyBoldFont
            };

            SunshineButton btnAddProduct = new SunshineButton
            {
                Text = "➕ Add New Product",
                IsPrimary = false,
                Location = new Point(16, 110),
                Size = new Size(300, 44),
                Font = AppTheme.BodyBoldFont
            };

            Label lblLowStockListTitle = new Label
            {
                Text = "Low Stock Watchlist",
                Font = AppTheme.SubheaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(16, 175),
                AutoSize = true
            };

            ListBox lstLowStock = new ListBox
            {
                Location = new Point(16, 205),
                Size = new Size(300, 195),
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

            Controls.Add(cardSidePanel);

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
            var displayData = _dataService.Orders.Select(o => new
            {
                Order_ID = o.Id,
                Customer = o.CustomerName,
                Date = o.CreatedAt.ToString("g"),
                Items_Count = o.Items.Count,
                Payment = o.PaymentMethod,
                Total = $"${o.TotalAmount:N2}"
            }).ToList();

            _gridRecentOrders.DataSource = displayData;
        }
    }
}
