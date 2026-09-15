using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Views
{
    public class DashboardView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        public Action? OnNavigateToPOSRequest;
        public Action<bool>? OnNavigateToProductsRequest;
        public Action? OnNavigateToOrdersRequest;

        // Command Center Labels (3 Main Stats Only)
        private Label _lblRevenueVal = null!;
        private Label _lblSalesSub = null!;
        private Label _lblTargetProgressVal = null!;
        private Label _lblTargetSub = null!;
        private Label _lblTransactionsVal = null!;
        private Label _lblTxSub = null!;
        private ProgressBar _pbTarget = null!;

        // 2x2 Metric Labels
        private Label _lblRefundVal = null!;
        private Label _lblRefundDesc = null!;
        private Label _lblStockCount = null!;
        private Label _lblStockDesc = null!;
        private Label _lblTopProdVal = null!;
        private Label _lblTopProdDesc = null!;
        private Label _lblTopProdAction = null!;
        private Label _lblBenchCount = null!;
        private Label _lblBenchDesc = null!;

        // Tables & Lists
        private DataGridView _gridRecentOrders = null!;
        private TextBox _txtSearchOrders = null!;
        private Panel _pnlPagination = null!;
        private Label _lblOrderCount = null!;
        private int _recentOrdersPage = 1;
        private const int RecentPageSize = 8;
        private int _recentTotalPages = 1;

        // Critical Inventory Container
        private Panel _pnlInventoryItems = null!;

        public DashboardView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            SuspendLayout();
            Controls.Clear();

            // Main 2-Row Layout Container
            TableLayoutPanel tlpContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12),
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            tlpContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63f)); // Left: 63%
            tlpContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37f)); // Right: 37%
            tlpContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 230f));      // Top: 230px (ample space to prevent any text clipping)
            tlpContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));       // Bottom: Fills remaining height

            // TOP-LEFT: TODAY'S OVERVIEW (Minimal Hero Card)
            Panel pnlOverview = CreateOverviewHero();
            tlpContainer.Controls.Add(pnlOverview, 0, 0);

            // TOP-RIGHT: 2x2 MINI CARDS
            TableLayoutPanel tlpMiniCards = CreateMiniCardsGrid();
            tlpContainer.Controls.Add(tlpMiniCards, 1, 0);

            // BOTTOM-LEFT: RECENT TRANSACTIONS (Fills width & height, no dead space)
            Panel pnlRecentOrders = CreateRecentOrdersCard();
            tlpContainer.Controls.Add(pnlRecentOrders, 0, 1);

            // BOTTOM-RIGHT: CRITICAL INVENTORY WATCH (Minimal & Dynamic)
            Panel pnlInventoryWatch = CreateCriticalInventoryCard();
            tlpContainer.Controls.Add(pnlInventoryWatch, 1, 1);

            Controls.Add(tlpContainer);
            ResumeLayout(false);

            RefreshMetrics();
        }

        private Panel CreateOverviewHero()
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 8),
                Padding = new Padding(18, 16, 18, 14),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            TableLayoutPanel tlpLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // Row 0: Clean Header
            tlpLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 1: 3 Big Hero Stats

            // ========================================================
            // ROW 0: HEADER (Title "Today's Overview", no subtitle)
            // ========================================================
            Panel pnlHeader = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };

            Label lblTitle = new Label
            {
                Text = "Today's Overview",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(0, 4),
                AutoSize = true
            };

            SunshineButton btnExport = new SunshineButton
            {
                Text = "Export Audit",
                IsPrimary = false,
                Dock = DockStyle.Right,
                Width = 95,
                Height = 26,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Margin = new Padding(4, 2, 0, 2)
            };
            btnExport.Click += (s, e) => MessageBox.Show("Daily store ledger audit exported to CSV.", "Audit Export", MessageBoxButtons.OK, MessageBoxIcon.Information);

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnExport);

            // ========================================================
            // ROW 1: 3 MAIN STATS (Sales, Shift Target, Transactions)
            // ========================================================
            TableLayoutPanel tlpStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));

            // Stat 1: Sales
            Panel pnlStat1 = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
            Label lblStat1Title = new Label { Text = "SALES", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Dock = DockStyle.Top, Height = 18 };
            _lblRevenueVal = new Label { Text = "₱0.00", Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Dock = DockStyle.Top, Height = 44, TextAlign = ContentAlignment.MiddleLeft };
            _lblSalesSub = new Label { Text = "No sales recorded today", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 122, 79), Dock = DockStyle.Top, Height = 20, TextAlign = ContentAlignment.MiddleLeft };
            pnlStat1.Controls.Add(_lblSalesSub);
            pnlStat1.Controls.Add(_lblRevenueVal);
            pnlStat1.Controls.Add(lblStat1Title);

            // Stat 2: Shift Target
            Panel pnlStat2 = new Panel { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 10, 0) };
            Label lblStat2Title = new Label { Text = "SHIFT TARGET", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Dock = DockStyle.Top, Height = 18 };
            _lblTargetProgressVal = new Label { Text = "0.0%", Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Dock = DockStyle.Top, Height = 44, TextAlign = ContentAlignment.MiddleLeft };
            Panel pnlPb = new Panel { Dock = DockStyle.Top, Height = 18, Padding = new Padding(0, 3, 16, 2) };
            _pbTarget = new ProgressBar { Dock = DockStyle.Fill, Value = 0, Maximum = 100 };
            pnlPb.Controls.Add(_pbTarget);
            _lblTargetSub = new Label { Text = "Target: ₱20,000.00", Font = new Font("Segoe UI", 7.5F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Dock = DockStyle.Top, Height = 18, TextAlign = ContentAlignment.MiddleLeft };
            pnlStat2.Controls.Add(_lblTargetSub);
            pnlStat2.Controls.Add(pnlPb);
            pnlStat2.Controls.Add(_lblTargetProgressVal);
            pnlStat2.Controls.Add(lblStat2Title);

            // Stat 3: Transactions
            Panel pnlStat3 = new Panel { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0) };
            Label lblStat3Title = new Label { Text = "TRANSACTIONS", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Dock = DockStyle.Top, Height = 18 };
            _lblTransactionsVal = new Label { Text = "0", Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Dock = DockStyle.Top, Height = 44, TextAlign = ContentAlignment.MiddleLeft };
            _lblTxSub = new Label { Text = "0 completed today", Font = new Font("Segoe UI", 8F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Dock = DockStyle.Top, Height = 20, TextAlign = ContentAlignment.MiddleLeft };
            pnlStat3.Controls.Add(_lblTxSub);
            pnlStat3.Controls.Add(_lblTransactionsVal);
            pnlStat3.Controls.Add(lblStat3Title);

            tlpStats.Controls.Add(pnlStat1, 0, 0);
            tlpStats.Controls.Add(pnlStat2, 1, 0);
            tlpStats.Controls.Add(pnlStat3, 2, 0);

            tlpLayout.Controls.Add(pnlHeader, 0, 0);
            tlpLayout.Controls.Add(tlpStats, 0, 1);

            card.Controls.Add(tlpLayout);

            return card;
        }

        private TableLayoutPanel CreateMiniCardsGrid()
        {
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(8, 0, 0, 8)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            // Card 1 (Row 0, Col 0): Returns & Refunds
            tlp.Controls.Add(CreateMiniMetricCard(
                "RETURNS & REFUNDS",
                out _lblRefundVal,
                out _lblRefundDesc,
                out _,
                "₱0.00",
                "0 refunds today",
                "View Returns →",
                () => OnNavigateToOrdersRequest?.Invoke()), 0, 0);

            // Card 2 (Row 0, Col 1): Low Stock Alert
            tlp.Controls.Add(CreateMiniMetricCard(
                "LOW STOCK ALERT",
                out _lblStockCount,
                out _lblStockDesc,
                out _,
                "0 SKUs",
                "all items well stocked",
                "Review Stock →",
                () => OnNavigateToProductsRequest?.Invoke(true)), 1, 0);

            // Card 3 (Row 1, Col 0): Top Product
            tlp.Controls.Add(CreateMiniMetricCard(
                "TOP PRODUCT",
                out _lblTopProdVal,
                out _lblTopProdDesc,
                out _lblTopProdAction,
                "No sales yet",
                "0 products sold today",
                "View Products →",
                () => OnNavigateToProductsRequest?.Invoke(false)), 0, 1);

            // Card 4 (Row 1, Col 1): Top Category
            tlp.Controls.Add(CreateMiniMetricCard(
                "TOP CATEGORY",
                out _lblBenchCount,
                out _lblBenchDesc,
                out _,
                "None",
                "0 products in stock",
                "View Stock →",
                () => OnNavigateToProductsRequest?.Invoke(false)), 1, 1);

            return tlp;
        }

        private SunshineCard CreateMiniMetricCard(
            string title,
            out Label lblVal,
            out Label lblDesc,
            out Label lblAction,
            string defValue,
            string desc,
            string actionText,
            Action onAction)
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                Padding = new Padding(12, 10, 12, 8),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder,
                Cursor = Cursors.Hand
            };
            card.Click += (s, e) => onAction();

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Padding = new Padding(0, 0, 4, 0)
            };

            lblAction = new Label
            {
                Text = actionText,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(145, 105, 15),
                Dock = DockStyle.Right,
                AutoSize = true,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleRight
            };
            lblAction.Click += (s, e) => onAction();
            pnlFooter.Controls.Add(lblAction);

            lblDesc = new Label
            {
                Text = desc,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 18,
                AutoEllipsis = true
            };

            lblVal = new Label
            {
                Text = defValue,
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Top,
                Height = 26,
                AutoEllipsis = true
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 16,
                UseMnemonic = false // Ensures '&' displays cleanly without clipping
            };

            // Stacking order: Top-to-bottom so no overlapping occurs
            card.Controls.Add(pnlFooter);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblVal);
            card.Controls.Add(lblTitle);

            return card;
        }

        private Panel CreateRecentOrdersCard()
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 8, 0),
                Padding = new Padding(14),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            // Top Bar of Card
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 38 };

            Label lblTitle = new Label
            {
                Text = "Recent Transactions",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(0, 6),
                AutoSize = true
            };

            SunshineButton btnRefresh = new SunshineButton
            {
                Text = "Refresh",
                IsPrimary = false,
                Dock = DockStyle.Right,
                Width = 75,
                Height = 26,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Margin = new Padding(6, 4, 0, 6)
            };
            btnRefresh.Click += (s, e) => PopulateRecentOrders();

            _txtSearchOrders = new TextBox
            {
                PlaceholderText = "Search Order # or Customer...",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Dock = DockStyle.Right,
                Width = 210
            };
            _txtSearchOrders.TextChanged += (s, e) =>
            {
                _recentOrdersPage = 1;
                PopulateRecentOrders();
            };

            Panel pnlSearchContainer = new Panel { Dock = DockStyle.Right, Width = 300, Height = 30, Padding = new Padding(0, 3, 0, 3) };
            pnlSearchContainer.Controls.Add(_txtSearchOrders);
            pnlSearchContainer.Controls.Add(btnRefresh);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(pnlSearchContainer);

            // Table DataGridView - 4 Columns Only: Order ID, Customer, Time, Total
            _gridRecentOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 238, 230),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 38 }
            };

            _gridRecentOrders.EnableHeadersVisualStyles = false;
            _gridRecentOrders.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            _gridRecentOrders.ColumnHeadersHeight = 34;
            _gridRecentOrders.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(10, 0, 0, 0)
            };

            _gridRecentOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ORDER ID", FillWeight = 22, Name = "ColId" });
            _gridRecentOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CUSTOMER", FillWeight = 42, Name = "ColCust" });
            _gridRecentOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TIME", FillWeight = 18, Name = "ColTime" });
            _gridRecentOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TOTAL", FillWeight = 18, Name = "ColTotal" });

            // Bottom Footer
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            _lblOrderCount = new Label { Text = "No transactions recorded yet", Font = new Font("Segoe UI", 7.5F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(0, 8), AutoSize = true };

            _pnlPagination = new Panel { Size = new Size(220, 26) };
            pnlFooter.Resize += (s, e) => _pnlPagination.Location = new Point((pnlFooter.Width - _pnlPagination.Width) / 2, 2);

            pnlFooter.Controls.Add(_lblOrderCount);
            pnlFooter.Controls.Add(_pnlPagination);

            card.Controls.Add(_gridRecentOrders);
            card.Controls.Add(pnlFooter);
            card.Controls.Add(pnlTop);

            PopulateRecentOrders();

            return card;
        }

        private Panel CreateCriticalInventoryCard()
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 8, 0, 0),
                Padding = new Padding(14),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 34 };
            Label lblTitle = new Label { Text = "Critical Inventory Watch", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(0, 4), AutoSize = true };
            Label lblOpenStock = new Label { Text = "Open Stock Manager →", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(130, 95, 10), Dock = DockStyle.Right, AutoSize = true, Cursor = Cursors.Hand };
            lblOpenStock.Click += (s, e) => OnNavigateToProductsRequest?.Invoke(false);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblOpenStock);

            // Dynamic Items Container
            _pnlInventoryItems = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 4, 0, 4)
            };

            SunshineButton btnStockMgr = new SunshineButton
            {
                Text = "Open Stock Manager",
                IsPrimary = false,
                Dock = DockStyle.Bottom,
                Height = 32,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnStockMgr.Click += (s, e) => OnNavigateToProductsRequest?.Invoke(false);

            card.Controls.Add(_pnlInventoryItems);
            card.Controls.Add(btnStockMgr);
            card.Controls.Add(pnlTop);

            PopulateCriticalInventory();

            return card;
        }

        private Panel CreateWatchItem(string name, int reorderLevel, string stockBadge, Color badgeBg, Color badgeText, string price)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(10, 6, 10, 6),
                BackColor = Color.FromArgb(254, 252, 248)
            };
            pnl.Paint += (s, e) =>
            {
                using Pen pen = new Pen(Color.FromArgb(235, 230, 218), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            // Left: Product Name & Reorder Level
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 8, 0) };
            Label lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Top,
                Height = 20,
                AutoEllipsis = true
            };
            Label lblReorder = new Label
            {
                Text = $"Reorder Level: {reorderLevel}",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 16
            };
            pnlLeft.Controls.Add(lblReorder);
            pnlLeft.Controls.Add(lblName);

            // Right: Stock badge & Price
            Panel pnlRight = new Panel { Dock = DockStyle.Right, Width = 110, Padding = new Padding(0, 2, 0, 0) };
            Panel pnlBadge = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = badgeBg, Margin = new Padding(0) };
            Label lblBadge = new Label { Text = stockBadge, Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = badgeText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlBadge.Controls.Add(lblBadge);

            Label lblPrice = new Label { Text = price, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Dock = DockStyle.Bottom, Height = 18, TextAlign = ContentAlignment.MiddleRight };
            pnlRight.Controls.Add(lblPrice);
            pnlRight.Controls.Add(pnlBadge);

            pnl.Controls.Add(pnlLeft);
            pnl.Controls.Add(pnlRight);

            return pnl;
        }

        private void PopulateCriticalInventory()
        {
            if (_pnlInventoryItems == null) return;
            _pnlInventoryItems.SuspendLayout();
            _pnlInventoryItems.Controls.Clear();

            var products = _dataService.Products
                .OrderBy(p => p.StockQuantity)
                .Take(6)
                .ToList();

            if (products.Count > 0)
            {
                foreach (var p in products)
                {
                    int reorderLevel = 5;
                    Color badgeBg = p.StockQuantity == 0 ? AppTheme.RedPillBg : (p.StockQuantity <= reorderLevel ? AppTheme.AmberPillBg : AppTheme.GreenPillBg);
                    Color badgeText = p.StockQuantity == 0 ? AppTheme.RedPillText : (p.StockQuantity <= reorderLevel ? AppTheme.AmberPillText : AppTheme.GreenPillText);
                    string stockText = p.StockQuantity == 0 ? "Out of stock" : $"{p.StockQuantity} in stock";

                    _pnlInventoryItems.Controls.Add(CreateWatchItem(p.Name, reorderLevel, stockText, badgeBg, badgeText, $"₱{p.Price:N2}"));
                }
            }
            else
            {
                Label lblEmpty = new Label
                {
                    Text = "No products registered yet.\nAdd products in Products Stock tab.",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    ForeColor = AppTheme.TextMuted,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _pnlInventoryItems.Controls.Add(lblEmpty);
            }

            _pnlInventoryItems.ResumeLayout();
        }

        private void PopulateRecentOrders()
        {
            if (_gridRecentOrders == null) return;
            _gridRecentOrders.Rows.Clear();

            string filter = _txtSearchOrders?.Text?.Trim() ?? string.Empty;

            var ordersList = _dataService.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => (
                    Id: o.Id.StartsWith("#") ? o.Id : $"#{o.Id}",
                    Cust: string.IsNullOrWhiteSpace(o.CustomerName) ? "Walk-in Customer" : o.CustomerName,
                    Time: o.CreatedAt.ToString("HH:mm:ss"),
                    Total: $"₱{o.TotalAmount:N2}"
                ))
                .ToList();

            if (!string.IsNullOrEmpty(filter))
            {
                ordersList = ordersList
                    .Where(o => o.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                o.Cust.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int totalCount = ordersList.Count;
            _recentTotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)RecentPageSize));

            if (_recentOrdersPage > _recentTotalPages) _recentOrdersPage = _recentTotalPages;
            if (_recentOrdersPage < 1) _recentOrdersPage = 1;

            var pageItems = ordersList.Skip((_recentOrdersPage - 1) * RecentPageSize).Take(RecentPageSize).ToList();

            foreach (var item in pageItems)
            {
                int rowIdx = _gridRecentOrders.Rows.Add(item.Id, item.Cust, item.Time, item.Total);
                var row = _gridRecentOrders.Rows[rowIdx];
                row.Cells["ColId"].Style.ForeColor = Color.FromArgb(160, 110, 10);
                row.Cells["ColId"].Style.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                row.Cells["ColTotal"].Style.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            }

            int start = totalCount == 0 ? 0 : (_recentOrdersPage - 1) * RecentPageSize + 1;
            int end = Math.Min(_recentOrdersPage * RecentPageSize, totalCount);
            if (_lblOrderCount != null)
            {
                _lblOrderCount.Text = totalCount == 0
                    ? "No transactions recorded yet"
                    : $"Displaying rows {start}-{end} of {totalCount} transactions today";
            }

            UpdateDashboardPagination();
        }

        private void UpdateDashboardPagination()
        {
            if (_pnlPagination == null) return;
            _pnlPagination.SuspendLayout();
            _pnlPagination.Controls.Clear();

            List<string> buttons = new List<string> { "Prev" };
            for (int i = 1; i <= _recentTotalPages; i++) buttons.Add(i.ToString());
            buttons.Add("Next");

            int px = 0;
            foreach (var p in buttons)
            {
                Button btnP = new Button
                {
                    Text = p,
                    Size = new Size(38, 24),
                    Location = new Point(px, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                    Cursor = Cursors.Hand
                };
                btnP.FlatAppearance.BorderColor = Color.FromArgb(220, 215, 205);

                if (p == "Prev")
                {
                    btnP.Enabled = _recentOrdersPage > 1;
                    btnP.BackColor = btnP.Enabled ? Color.White : Color.FromArgb(245, 243, 238);
                    btnP.ForeColor = btnP.Enabled ? AppTheme.TextDark : Color.Gray;
                    btnP.Click += (s, e) => { if (_recentOrdersPage > 1) { _recentOrdersPage--; PopulateRecentOrders(); } };
                }
                else if (p == "Next")
                {
                    btnP.Enabled = _recentOrdersPage < _recentTotalPages;
                    btnP.BackColor = btnP.Enabled ? Color.White : Color.FromArgb(245, 243, 238);
                    btnP.ForeColor = btnP.Enabled ? AppTheme.TextDark : Color.Gray;
                    btnP.Click += (s, e) => { if (_recentOrdersPage < _recentTotalPages) { _recentOrdersPage++; PopulateRecentOrders(); } };
                }
                else if (int.TryParse(p, out int pageNum))
                {
                    bool isCur = pageNum == _recentOrdersPage;
                    btnP.BackColor = isCur ? AppTheme.Primary : Color.White;
                    btnP.Font = isCur ? new Font("Segoe UI", 7F, FontStyle.Bold) : new Font("Segoe UI", 7F, FontStyle.Regular);
                    btnP.Click += (s, e) => { _recentOrdersPage = pageNum; PopulateRecentOrders(); };
                }

                _pnlPagination.Controls.Add(btnP);
                px += 41;
            }

            _pnlPagination.Width = px;
            if (_pnlPagination.Parent != null)
            {
                _pnlPagination.Location = new Point((_pnlPagination.Parent.Width - px) / 2, 2);
            }
            _pnlPagination.ResumeLayout();
        }

        public void RefreshMetrics()
        {
            // Calculate real today's sales directly from live orders
            var todayOrders = _dataService.Orders
                .Where(o => o.CreatedAt.Date == DateTime.Today)
                .ToList();

            decimal todaySales = todayOrders.Sum(o => o.TotalAmount);

            if (_lblRevenueVal != null) _lblRevenueVal.Text = $"₱{todaySales:N2}";
            if (_lblSalesSub != null)
            {
                _lblSalesSub.Text = todaySales > 0 ? "Live gross today" : "No sales recorded today";
            }

            // Dynamic Shift Target Progress based on ₱20,000.00 daily target
            decimal shiftTarget = 20000.00m;
            decimal progressPct = shiftTarget > 0 ? (todaySales / shiftTarget) * 100m : 0m;
            int pbVal = Math.Min(100, Math.Max(0, (int)Math.Round(progressPct)));

            if (_pbTarget != null) _pbTarget.Value = pbVal;
            if (_lblTargetProgressVal != null) _lblTargetProgressVal.Text = $"{Math.Min(100.0m, progressPct):N1}%";
            if (_lblTargetSub != null)
            {
                if (progressPct >= 100.0m)
                {
                    _lblTargetSub.Text = $"Goal Reached! (Target: ₱{shiftTarget:N2})";
                    _lblTargetSub.ForeColor = Color.FromArgb(27, 122, 79);
                }
                else
                {
                    _lblTargetSub.Text = $"Target: ₱{shiftTarget:N2}";
                    _lblTargetSub.ForeColor = AppTheme.TextMuted;
                }
            }

            // Dynamic Completed Transactions
            int todayTx = todayOrders.Count;
            if (_lblTransactionsVal != null) _lblTransactionsVal.Text = todayTx.ToString("N0");
            if (_lblTxSub != null)
            {
                _lblTxSub.Text = todayTx == 1 ? "1 completed today" : $"{todayTx} completed today";
            }

            // 1. Dynamic Returns & Refunds
            var refunds = todayOrders
                .Where(o => o.TotalAmount < 0 || (o.PaymentMethod != null && o.PaymentMethod.Equals("Refund", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            decimal refundTotal = Math.Abs(refunds.Sum(r => r.TotalAmount));
            int refundCount = refunds.Count;

            if (_lblRefundVal != null) _lblRefundVal.Text = $"₱{refundTotal:N2}";
            if (_lblRefundDesc != null) _lblRefundDesc.Text = $"{refundCount} refund{(refundCount == 1 ? "" : "s")} today";

            // 2. Dynamic Low Stock Alert from live Products
            int lowStockCount = _dataService.Products.Count(p => p.StockQuantity <= 5);
            if (_lblStockCount != null) _lblStockCount.Text = $"{lowStockCount} SKUs";
            if (_lblStockDesc != null) _lblStockDesc.Text = lowStockCount > 0 ? "under safety mark" : "all items well stocked";

            // 3. Dynamic Top Product
            var topProductItem = todayOrders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Qty)
                .FirstOrDefault();

            if (topProductItem != null && topProductItem.Qty > 0)
            {
                string pName = topProductItem.Name;
                if (pName.Length > 18) pName = pName.Substring(0, 16) + "...";

                if (_lblTopProdVal != null) _lblTopProdVal.Text = pName;
                if (_lblTopProdDesc != null) _lblTopProdDesc.Text = $"{topProductItem.Qty} sold today";
                if (_lblTopProdAction != null) _lblTopProdAction.Text = "View Product →";
            }
            else
            {
                if (_lblTopProdVal != null) _lblTopProdVal.Text = "No sales yet";
                if (_lblTopProdDesc != null) _lblTopProdDesc.Text = "0 products sold today";
                if (_lblTopProdAction != null) _lblTopProdAction.Text = "View Products →";
            }

            // 4. Dynamic Top Category
            string topCat = "None";
            string catDesc = "0 products in stock";
            if (_dataService.Products.Count > 0)
            {
                var topCatGroup = _dataService.Products
                    .Where(p => !string.IsNullOrWhiteSpace(p.CategoryName))
                    .GroupBy(p => p.CategoryName)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (topCatGroup != null)
                {
                    topCat = topCatGroup.Key.Split('(')[0].Trim();
                    catDesc = $"{topCatGroup.Count()} products in stock";
                }
            }
            else if (_dataService.Categories.Count > 0)
            {
                topCat = _dataService.Categories[0].Name.Split('(')[0].Trim();
            }
            if (_lblBenchCount != null) _lblBenchCount.Text = topCat;
            if (_lblBenchDesc != null) _lblBenchDesc.Text = catDesc;

            PopulateRecentOrders();
            PopulateCriticalInventory();
        }
    }
}
