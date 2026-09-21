using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;
using ERP.winforms.UI.Dialogs;

namespace ERP.winforms.UI.Views
{
    public enum DashboardMode
    {
        BusinessIntelligence,
        StoreOperations
    }

    public class DashboardView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private bool IsSmallBusinessOrHigher =>
            _dataService.CurrentCompany != null &&
            !string.Equals(_dataService.CurrentCompany.PlanName, "Micro", StringComparison.OrdinalIgnoreCase);

        public Action? OnNavigateToPOSRequest;
        public Action<bool>? OnNavigateToProductsRequest;
        public Action? OnNavigateToOrdersRequest;

        // Current Active States
        private DashboardMode _currentMode = DashboardMode.BusinessIntelligence;
        private BiTimeRange _currentBiRange = BiTimeRange.Today;

        // Header Mode Switchers & Timeframe Filters
        private Button _btnModeBI = null!;
        private Button _btnModeOps = null!;
        private Button _btnTfToday = null!;
        private Button _btnTfWeek = null!;
        private Button _btnTfMonth = null!;
        private Button _btnTfAll = null!;
        private Panel _pnlTimeframeBar = null!;

        // Containers
        private Panel _pnlTopCommandBar = null!;
        private Panel _pnlMainContent = null!;
        private Panel _pnlBiScrollContainer = null!;
        private TableLayoutPanel _tlpOpsContainer = null!;

        // ========================================================
        // 1. BUSINESS INTELLIGENCE (BI) CONTROLS
        // ========================================================
        // KPI Card Labels
        private Label _lblBiSalesVal = null!;
        private Label _lblBiSalesSub = null!;
        private Label _lblBiSalesOrders = null!;

        private Label _lblBiEarningsVal = null!;
        private Label _lblBiEarningsMargin = null!;
        private Label _lblBiEarningsProjected = null!;

        private Label _lblBiAovVal = null!;
        private Label _lblBiAovUnits = null!;
        private Label _lblBiAovBasket = null!;

        private Label _lblBiStockValuationVal = null!;
        private Label _lblBiStockInStockRate = null!;
        private Label _lblBiStockSkus = null!;

        // Category Breakdown Container
        private Panel _pnlCategoryBars = null!;

        // Best Sellers Container
        private Panel _pnlBestSellers = null!;

        // Stock Trends & Health
        private Panel _pnlStockHealthBar = null!;
        private Label _lblStockHealthyCount = null!;
        private Label _lblStockLowCount = null!;
        private Label _lblStockOutCount = null!;
        private Label _lblStockTotalAssetVal = null!;

        // Inventory Movement
        private Panel _pnlFastMovers = null!;
        private Panel _pnlSlowMovers = null!;

        // ========================================================
        // 2. STORE OPERATIONS (ORIGINAL DASHBOARD) CONTROLS
        // ========================================================
        private Label _lblRevenueVal = null!;
        private Label _lblSalesSub = null!;
        private Label _lblTargetProgressVal = null!;
        private Label _lblTargetSub = null!;
        private Label _lblTransactionsVal = null!;
        private Label _lblTxSub = null!;
        private ProgressBar _pbTarget = null!;

        private Label _lblRefundVal = null!;
        private Label _lblRefundDesc = null!;
        private Label _lblStockCount = null!;
        private Label _lblStockDesc = null!;
        private Label _lblTopProdVal = null!;
        private Label _lblTopProdDesc = null!;
        private Label _lblTopProdAction = null!;
        private Label _lblBenchCount = null!;
        private Label _lblBenchDesc = null!;

        private DataGridView _gridRecentOrders = null!;
        private TextBox _txtSearchOrders = null!;
        private Panel _pnlPagination = null!;
        private Label _lblOrderCount = null!;
        private int _recentOrdersPage = 1;
        private const int RecentPageSize = 8;
        private int _recentTotalPages = 1;
        private Panel _pnlInventoryItems = null!;

        public DashboardView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            SuspendLayout();
            Controls.Clear();

            // ========================================================
            // TOP COMMAND BAR (Mode switch, Timeframe filters, Export)
            // ========================================================
            _pnlTopCommandBar = CreateTopCommandBar();
            _pnlTopCommandBar.Dock = DockStyle.Top;

            // Main Content Display Container
            _pnlMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.AppBackground
            };

            // 1. Build BI Container
            _pnlBiScrollContainer = CreateBusinessIntelligenceView();
            _pnlBiScrollContainer.Dock = DockStyle.Fill;

            // 2. Build Operations Container
            _tlpOpsContainer = CreateOperationsView();
            _tlpOpsContainer.Dock = DockStyle.Fill;

            // Add default view based on subscription plan
            if (IsSmallBusinessOrHigher)
            {
                _currentMode = DashboardMode.BusinessIntelligence;
                _pnlMainContent.Controls.Add(_pnlBiScrollContainer);
            }
            else
            {
                _currentMode = DashboardMode.StoreOperations;
                _pnlMainContent.Controls.Add(_tlpOpsContainer);
            }

            Controls.Add(_pnlMainContent);
            Controls.Add(_pnlTopCommandBar);

            UpdatePlanAccessUI();

            ResumeLayout(false);

            RefreshMetrics();
        }

        private Panel CreateTopCommandBar()
        {
            Panel pnl = new Panel
            {
                Height = 52,
                BackColor = Color.FromArgb(244, 241, 232),
                Padding = new Padding(16, 8, 16, 8)
            };

            pnl.Paint += (s, e) =>
            {
                using Pen borderPen = new Pen(Color.FromArgb(226, 221, 208), 1);
                e.Graphics.DrawLine(borderPen, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
            };

            // Mode Selector (Left)
            Panel pnlModeSwitcher = new Panel
            {
                Dock = DockStyle.Left,
                Width = 430,
                BackColor = Color.Transparent
            };

            _btnModeBI = new Button
            {
                Text = "📊 Executive BI & Analytics",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(0, 3),
                Size = new Size(220, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Primary,
                ForeColor = AppTheme.TextDark,
                Cursor = Cursors.Hand
            };
            _btnModeBI.FlatAppearance.BorderSize = 0;
            _btnModeBI.Click += (s, e) => SetDashboardMode(DashboardMode.BusinessIntelligence);

            _btnModeOps = new Button
            {
                Text = "⚡ Store Operations",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(226, 3),
                Size = new Size(185, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 230, 220),
                ForeColor = AppTheme.TextDark,
                Cursor = Cursors.Hand
            };
            _btnModeOps.FlatAppearance.BorderSize = 0;
            _btnModeOps.Click += (s, e) => SetDashboardMode(DashboardMode.StoreOperations);

            pnlModeSwitcher.Controls.Add(_btnModeBI);
            pnlModeSwitcher.Controls.Add(_btnModeOps);

            // Timeframe Selector & Export Report (Right)
            _pnlTimeframeBar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 470,
                BackColor = Color.Transparent
            };

            Label lblTf = new Label
            {
                Text = "PERIOD:",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(10, 10),
                AutoSize = true
            };

            int bx = 65;
            _btnTfToday = CreateTimeframeButton("Today", BiTimeRange.Today, bx, 55);
            bx += 60;
            _btnTfWeek = CreateTimeframeButton("This Week", BiTimeRange.ThisWeek, bx, 75);
            bx += 80;
            _btnTfMonth = CreateTimeframeButton("This Month", BiTimeRange.ThisMonth, bx, 80);
            bx += 85;
            _btnTfAll = CreateTimeframeButton("All-Time", BiTimeRange.AllTime, bx, 70);
            bx += 76;

            Button btnExportCsv = new Button
            {
                Text = "Export BI ▾",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(bx, 3),
                Size = new Size(88, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 34, 28),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnExportCsv.FlatAppearance.BorderSize = 0;
            btnExportCsv.Click += (s, e) => ExportBiCsvReport();

            _pnlTimeframeBar.Controls.Add(lblTf);
            _pnlTimeframeBar.Controls.Add(_btnTfToday);
            _pnlTimeframeBar.Controls.Add(_btnTfWeek);
            _pnlTimeframeBar.Controls.Add(_btnTfMonth);
            _pnlTimeframeBar.Controls.Add(_btnTfAll);
            _pnlTimeframeBar.Controls.Add(btnExportCsv);

            pnl.Controls.Add(pnlModeSwitcher);
            pnl.Controls.Add(_pnlTimeframeBar);

            return pnl;
        }

        private Button CreateTimeframeButton(string text, BiTimeRange range, int x, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(x, 4),
                Size = new Size(width, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => SetBiTimeRange(range);
            return btn;
        }

        private void SetDashboardMode(DashboardMode mode)
        {
            if (mode == DashboardMode.BusinessIntelligence && !IsSmallBusinessOrHigher)
            {
                using var upgradeDlg = new UpgradeSubscriptionDialog("Executive Business Intelligence & Profit Analytics");
                var result = upgradeDlg.ShowDialog(FindForm());
                if (result == DialogResult.OK && upgradeDlg.Upgraded)
                {
                    UpdatePlanAccessUI();
                    SetDashboardMode(DashboardMode.BusinessIntelligence);
                }
                return;
            }

            _currentMode = mode;
            UpdatePlanAccessUI();

            _pnlMainContent.Controls.Clear();
            if (mode == DashboardMode.BusinessIntelligence)
            {
                _pnlMainContent.Controls.Add(_pnlBiScrollContainer);
            }
            else
            {
                _pnlMainContent.Controls.Add(_tlpOpsContainer);
            }

            RefreshMetrics();
        }

        private void UpdatePlanAccessUI()
        {
            bool isSmallBiz = IsSmallBusinessOrHigher;
            if (!isSmallBiz)
            {
                if (_pnlTopCommandBar != null) _pnlTopCommandBar.Visible = false;
                if (_pnlTimeframeBar != null) _pnlTimeframeBar.Visible = false;

                if (_currentMode != DashboardMode.StoreOperations)
                {
                    _currentMode = DashboardMode.StoreOperations;
                    _pnlMainContent.Controls.Clear();
                    _pnlMainContent.Controls.Add(_tlpOpsContainer);
                }
            }
            else
            {
                if (_pnlTopCommandBar != null) _pnlTopCommandBar.Visible = true;
                _btnModeBI.Text = "📊 Executive BI & Analytics";
                _btnModeOps.Text = "⚡ Store Operations";

                _btnModeBI.BackColor = _currentMode == DashboardMode.BusinessIntelligence ? AppTheme.Primary : Color.FromArgb(235, 230, 220);
                _btnModeBI.ForeColor = AppTheme.TextDark;

                _btnModeOps.BackColor = _currentMode == DashboardMode.StoreOperations ? AppTheme.Primary : Color.FromArgb(235, 230, 220);
                _btnModeOps.ForeColor = AppTheme.TextDark;

                _pnlTimeframeBar.Visible = _currentMode == DashboardMode.BusinessIntelligence;
            }
        }

        private void SetBiTimeRange(BiTimeRange range)
        {
            _currentBiRange = range;
            UpdateTimeframeButtonStyles();
            RefreshBiData();
        }

        private void UpdateTimeframeButtonStyles()
        {
            void ApplyStyle(Button btn, bool isActive)
            {
                btn.BackColor = isActive ? AppTheme.Primary : Color.White;
                btn.ForeColor = isActive ? AppTheme.TextDark : AppTheme.TextMuted;
                btn.Font = new Font("Segoe UI", 7.5F, isActive ? FontStyle.Bold : FontStyle.Regular);
            }

            ApplyStyle(_btnTfToday, _currentBiRange == BiTimeRange.Today);
            ApplyStyle(_btnTfWeek, _currentBiRange == BiTimeRange.ThisWeek);
            ApplyStyle(_btnTfMonth, _currentBiRange == BiTimeRange.ThisMonth);
            ApplyStyle(_btnTfAll, _currentBiRange == BiTimeRange.AllTime);
        }

        // ========================================================
        // 1. BUSINESS INTELLIGENCE (BI) VIEW IMPLEMENTATION
        // ========================================================
        private Panel CreateBusinessIntelligenceView()
        {
            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.Transparent
            };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 115f)); // Row 0: 4 Hero KPI Cards
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 330f)); // Row 1: Category Shares (55%) + Best Sellers (45%)
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 270f)); // Row 2: Stock Trends & Health (50%) + Velocity (50%)

            // ROW 0: 4 KPI Cards
            TableLayoutPanel tlpKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++) tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            // KPI 1: Sales Revenue
            tlpKpis.Controls.Add(CreateBiKpiCard(
                "SALES REVENUE",
                out _lblBiSalesVal,
                out _lblBiSalesSub,
                out _lblBiSalesOrders,
                "₱0.00",
                "+0.0% vs prev period",
                "0 orders completed",
                AppTheme.Primary), 0, 0);

            // KPI 2: Monthly Earnings (Gross Profit)
            tlpKpis.Controls.Add(CreateBiKpiCard(
                "MONTHLY EARNINGS (EST. PROFIT)",
                out _lblBiEarningsVal,
                out _lblBiEarningsMargin,
                out _lblBiEarningsProjected,
                "₱0.00",
                "28.5% Gross Margin",
                "Projected: ₱0.00",
                Color.FromArgb(27, 122, 79)), 1, 0);

            // KPI 3: Average Order Value (AOV)
            tlpKpis.Controls.Add(CreateBiKpiCard(
                "AVERAGE ORDER VALUE (AOV)",
                out _lblBiAovVal,
                out _lblBiAovUnits,
                out _lblBiAovBasket,
                "₱0.00",
                "0 units sold",
                "Store retail basket",
                Color.FromArgb(30, 95, 180)), 2, 0);

            // KPI 4: Inventory Valuation
            tlpKpis.Controls.Add(CreateBiKpiCard(
                "INVENTORY ASSET VALUATION",
                out _lblBiStockValuationVal,
                out _lblBiStockInStockRate,
                out _lblBiStockSkus,
                "₱0.00",
                "100% In-Stock Rate",
                "0 catalog products",
                Color.FromArgb(168, 110, 5)), 3, 0);

            tlp.Controls.Add(tlpKpis, 0, 0);

            // ROW 1: Category Shares (Left 55%) + Best Sellers (Right 45%)
            TableLayoutPanel tlpMiddle = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };
            tlpMiddle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tlpMiddle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            Panel pnlCategories = CreateCategoryDistributionCard();
            Panel pnlBestSellersCard = CreateBestSellersCard();

            tlpMiddle.Controls.Add(pnlCategories, 0, 0);
            tlpMiddle.Controls.Add(pnlBestSellersCard, 1, 0);

            tlp.Controls.Add(tlpMiddle, 0, 1);

            // ROW 2: Stock Health & Trends (Left 50%) + Inventory Velocity (Right 50%)
            TableLayoutPanel tlpBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            Panel pnlStockTrendsCard = CreateStockTrendsCard();
            Panel pnlInventoryMovementCard = CreateInventoryMovementCard();

            tlpBottom.Controls.Add(pnlStockTrendsCard, 0, 0);
            tlpBottom.Controls.Add(pnlInventoryMovementCard, 1, 0);

            tlp.Controls.Add(tlpBottom, 0, 2);

            scrollPanel.Controls.Add(tlp);
            return scrollPanel;
        }

        private SunshineCard CreateBiKpiCard(
            string title,
            out Label lblVal,
            out Label lblSub,
            out Label lblDetail,
            string defVal,
            string defSub,
            string defDetail,
            Color accentColor)
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                Padding = new Padding(14, 10, 14, 10),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            card.Paint += (s, e) =>
            {
                using Brush brush = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(brush, 0, 0, 4, card.Height);
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 16,
                UseMnemonic = false
            };

            lblVal = new Label
            {
                Text = defVal,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Top,
                Height = 32,
                AutoEllipsis = true
            };

            lblSub = new Label
            {
                Text = defSub,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Top,
                Height = 18,
                AutoEllipsis = true
            };

            lblDetail = new Label
            {
                Text = defDetail,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Bottom,
                Height = 16,
                AutoEllipsis = true
            };

            card.Controls.Add(lblDetail);
            card.Controls.Add(lblSub);
            card.Controls.Add(lblVal);
            card.Controls.Add(lblTitle);

            return card;
        }

        private Panel CreateCategoryDistributionCard()
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(14),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 32 };
            Label lblTitle = new Label
            {
                Text = "Sales by Product & Category",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Left,
                AutoSize = true
            };
            Label lblSubtitle = new Label
            {
                Text = "Revenue contribution and volume breakdown",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Right,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            _pnlCategoryBars = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 6, 0, 0)
            };

            card.Controls.Add(_pnlCategoryBars);
            card.Controls.Add(pnlHeader);

            return card;
        }

        private Panel CreateBestSellersCard()
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0),
                Padding = new Padding(14),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 32 };
            Label lblTitle = new Label
            {
                Text = "Best-Selling Products",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Left,
                AutoSize = true
            };
            Label lblRank = new Label
            {
                Text = "Top 5 Volume Leaders",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Right,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblRank);

            _pnlBestSellers = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 6, 0, 0)
            };

            card.Controls.Add(_pnlBestSellers);
            card.Controls.Add(pnlHeader);

            return card;
        }

        private Panel CreateStockTrendsCard()
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(14),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 32 };
            Label lblTitle = new Label
            {
                Text = "Stock Trends & Catalog Health",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Left,
                AutoSize = true
            };
            _lblStockTotalAssetVal = new Label
            {
                Text = "Asset: ₱0.00",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Right,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(_lblStockTotalAssetVal);

            _pnlStockHealthBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 18,
                BackColor = Color.FromArgb(235, 232, 222),
                Margin = new Padding(0, 6, 0, 10)
            };

            Panel pnlCounters = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };

            _lblStockHealthyCount = new Label
            {
                Text = "● Healthy Stock (>5): 0 SKUs",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 122, 79),
                Dock = DockStyle.Top,
                Height = 26
            };
            _lblStockLowCount = new Label
            {
                Text = "● Low Stock Warning (1-5): 0 SKUs",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(168, 110, 5),
                Dock = DockStyle.Top,
                Height = 26
            };
            _lblStockOutCount = new Label
            {
                Text = "● Out of Stock (0): 0 SKUs",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(184, 50, 38),
                Dock = DockStyle.Top,
                Height = 26
            };

            pnlCounters.Controls.Add(_lblStockOutCount);
            pnlCounters.Controls.Add(_lblStockLowCount);
            pnlCounters.Controls.Add(_lblStockHealthyCount);

            card.Controls.Add(pnlCounters);
            card.Controls.Add(_pnlStockHealthBar);
            card.Controls.Add(pnlHeader);

            return card;
        }

        private Panel CreateInventoryMovementCard()
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0),
                Padding = new Padding(14),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 32 };
            Label lblTitle = new Label
            {
                Text = "Inventory Movement Velocity",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Left,
                AutoSize = true
            };
            Label lblSub = new Label
            {
                Text = "Fast vs. Slow-Moving Stock",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Right,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            TableLayoutPanel tlpSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // Fast Movers Left
            Panel pnlFast = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 4, 0) };
            Label lblFastTitle = new Label { Text = "⚡ Fast-Moving High Demand", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(27, 122, 79), Dock = DockStyle.Top, Height = 20 };
            _pnlFastMovers = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            pnlFast.Controls.Add(_pnlFastMovers);
            pnlFast.Controls.Add(lblFastTitle);

            // Slow Movers Right
            Panel pnlSlow = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 0, 0, 0) };
            Label lblSlowTitle = new Label { Text = "⏳ Stagnant / Slow-Moving", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(168, 110, 5), Dock = DockStyle.Top, Height = 20 };
            _pnlSlowMovers = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            pnlSlow.Controls.Add(_pnlSlowMovers);
            pnlSlow.Controls.Add(lblSlowTitle);

            tlpSplit.Controls.Add(pnlFast, 0, 0);
            tlpSplit.Controls.Add(pnlSlow, 1, 0);

            card.Controls.Add(tlpSplit);
            card.Controls.Add(pnlHeader);

            return card;
        }

        private void RefreshBiData()
        {
            // 1. Sales & Performance Metrics for current selected range
            var salesMetrics = _dataService.GetBiSalesMetrics(_currentBiRange);
            if (_lblBiSalesVal != null) _lblBiSalesVal.Text = $"₱{salesMetrics.TotalRevenue:N2}";
            if (_lblBiSalesSub != null)
            {
                string sign = salesMetrics.GrowthRate >= 0 ? "+" : "";
                _lblBiSalesSub.Text = $"{sign}{salesMetrics.GrowthRate:N1}% vs prior period";
                _lblBiSalesSub.ForeColor = salesMetrics.GrowthRate >= 0 ? Color.FromArgb(27, 122, 79) : Color.FromArgb(184, 50, 38);
            }
            if (_lblBiSalesOrders != null)
            {
                _lblBiSalesOrders.Text = $"{salesMetrics.OrderCount} orders  •  {salesMetrics.TotalItemsSold} items sold";
            }

            // 2. Monthly Earnings (Profit Margin)
            var earnings = _dataService.GetBiEarningsSummary();
            if (_lblBiEarningsVal != null) _lblBiEarningsVal.Text = $"₱{earnings.GrossProfit:N2}";
            if (_lblBiEarningsMargin != null) _lblBiEarningsMargin.Text = $"{earnings.MarginPercent:N1}% Gross Margin (₱{earnings.GrossRevenue:N0} rev)";
            if (_lblBiEarningsProjected != null) _lblBiEarningsProjected.Text = $"Projected Month-End: ₱{earnings.ProjectedMonthEnd:N2}";

            // 3. Average Order Value
            if (_lblBiAovVal != null) _lblBiAovVal.Text = $"₱{salesMetrics.AverageOrderValue:N2}";
            if (_lblBiAovUnits != null) _lblBiAovUnits.Text = $"{salesMetrics.TotalItemsSold} units shipped across all tickets";
            if (_lblBiAovBasket != null) _lblBiAovBasket.Text = $"{salesMetrics.OrderCount} total transactions in period";

            // 4. Inventory Valuation & Health
            var stockTrends = _dataService.GetBiStockTrends();
            if (_lblBiStockValuationVal != null) _lblBiStockValuationVal.Text = $"₱{stockTrends.TotalAssetValuation:N2}";
            if (_lblBiStockInStockRate != null) _lblBiStockInStockRate.Text = $"{stockTrends.InStockRate:N1}% In-Stock Health Rate";
            if (_lblBiStockSkus != null) _lblBiStockSkus.Text = $"{stockTrends.TotalCatalogItems} active SKUs ({stockTrends.TotalStockUnits} units)";

            // 5. Category Breakdown Bars
            PopulateCategoryBars();

            // 6. Best-Selling Products Leaderboard
            PopulateBestSellers();

            // 7. Stock Health Trends
            PopulateStockHealth(stockTrends);

            // 8. Inventory Movement
            PopulateInventoryMovement(stockTrends);
        }

        private void PopulateCategoryBars()
        {
            if (_pnlCategoryBars == null) return;
            _pnlCategoryBars.SuspendLayout();
            _pnlCategoryBars.Controls.Clear();

            var categories = _dataService.GetBiCategoryDistribution(_currentBiRange);
            if (categories.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No sales recorded for this timeframe.",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    ForeColor = AppTheme.TextMuted,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _pnlCategoryBars.Controls.Add(lblEmpty);
            }
            else
            {
                Color[] barColors = new[]
                {
                    AppTheme.Primary,
                    Color.FromArgb(52, 152, 219),
                    Color.FromArgb(46, 204, 113),
                    Color.FromArgb(155, 89, 182),
                    Color.FromArgb(243, 156, 18)
                };

                int colorIdx = 0;
                foreach (var cat in categories)
                {
                    Color color = barColors[colorIdx % barColors.Length];
                    colorIdx++;

                    Panel row = new Panel
                    {
                        Dock = DockStyle.Top,
                        Height = 46,
                        Margin = new Padding(0, 0, 0, 6),
                        BackColor = Color.Transparent
                    };

                    // Label Top: Name and Amount
                    Label lblName = new Label
                    {
                        Text = $"{cat.CategoryName} ({cat.Percentage:N1}%)",
                        Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                        ForeColor = AppTheme.TextDark,
                        Location = new Point(0, 2),
                        AutoSize = true
                    };

                    Label lblAmount = new Label
                    {
                        Text = $"₱{cat.Revenue:N2} ({cat.UnitsSold} sold)",
                        Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                        ForeColor = AppTheme.TextMuted,
                        Anchor = AnchorStyles.Top | AnchorStyles.Right,
                        Location = new Point(row.Width - 180, 2),
                        Size = new Size(175, 16),
                        TextAlign = ContentAlignment.MiddleRight
                    };

                    // Proportional Progress Bar Panel
                    Panel pnlBarBg = new Panel
                    {
                        Location = new Point(0, 22),
                        Size = new Size(row.Width - 10, 12),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        BackColor = Color.FromArgb(240, 237, 228)
                    };

                    int barWidth = Math.Max(6, (int)((cat.Percentage / 100m) * pnlBarBg.Width));
                    Panel pnlBarFill = new Panel
                    {
                        Location = new Point(0, 0),
                        Size = new Size(barWidth, 12),
                        BackColor = color
                    };
                    pnlBarBg.Controls.Add(pnlBarFill);

                    pnlBarBg.Resize += (s, e) =>
                    {
                        int newW = Math.Max(6, (int)((cat.Percentage / 100m) * pnlBarBg.Width));
                        pnlBarFill.Width = newW;
                    };

                    row.Controls.Add(lblName);
                    row.Controls.Add(lblAmount);
                    row.Controls.Add(pnlBarBg);

                    _pnlCategoryBars.Controls.Add(row);
                }
            }

            _pnlCategoryBars.ResumeLayout();
        }

        private void PopulateBestSellers()
        {
            if (_pnlBestSellers == null) return;
            _pnlBestSellers.SuspendLayout();
            _pnlBestSellers.Controls.Clear();

            var bestSellers = _dataService.GetBiBestSellers(_currentBiRange, 5);
            if (bestSellers.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No product sales recorded in selected timeframe.",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    ForeColor = AppTheme.TextMuted,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _pnlBestSellers.Controls.Add(lblEmpty);
            }
            else
            {
                string[] ranks = new[] { "🥇 #1", "🥈 #2", "🥉 #3", "   #4", "   #5" };
                for (int i = 0; i < bestSellers.Count; i++)
                {
                    var prod = bestSellers[i];
                    string rankStr = i < ranks.Length ? ranks[i] : $"   #{i + 1}";

                    Panel row = new Panel
                    {
                        Dock = DockStyle.Top,
                        Height = 44,
                        Margin = new Padding(0, 0, 0, 4),
                        Padding = new Padding(6, 4, 6, 4),
                        BackColor = Color.FromArgb(253, 251, 246)
                    };

                    row.Paint += (s, e) =>
                    {
                        using Pen pen = new Pen(Color.FromArgb(235, 230, 220), 1);
                        e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
                    };

                    Label lblRank = new Label
                    {
                        Text = rankStr,
                        Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                        ForeColor = AppTheme.TextDark,
                        Location = new Point(4, 10),
                        Size = new Size(50, 20),
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    Label lblName = new Label
                    {
                        Text = prod.ProductName,
                        Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                        ForeColor = AppTheme.TextDark,
                        Location = new Point(58, 4),
                        Size = new Size(row.Width - 230, 16),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        AutoEllipsis = true
                    };

                    Label lblDetails = new Label
                    {
                        Text = $"{prod.UnitsSold} sold  •  ₱{prod.Revenue:N2}",
                        Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                        ForeColor = Color.FromArgb(140, 100, 10),
                        Location = new Point(58, 22),
                        Size = new Size(row.Width - 230, 16),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        AutoEllipsis = true
                    };

                    Label lblStock = new Label
                    {
                        Text = $"{prod.CurrentStock} in stock",
                        Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                        ForeColor = prod.CurrentStock <= 5 ? AppTheme.RedPillText : AppTheme.GreenPillText,
                        BackColor = prod.CurrentStock <= 5 ? AppTheme.RedPillBg : AppTheme.GreenPillBg,
                        Anchor = AnchorStyles.Top | AnchorStyles.Right,
                        Location = new Point(row.Width - 100, 10),
                        Size = new Size(90, 20),
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    row.Controls.Add(lblRank);
                    row.Controls.Add(lblName);
                    row.Controls.Add(lblDetails);
                    row.Controls.Add(lblStock);

                    _pnlBestSellers.Controls.Add(row);
                }
            }

            _pnlBestSellers.ResumeLayout();
        }

        private void PopulateStockHealth(BiStockTrends trends)
        {
            if (_lblStockTotalAssetVal != null)
            {
                _lblStockTotalAssetVal.Text = $"Total Inventory: ₱{trends.TotalAssetValuation:N2}";
            }
            if (_lblStockHealthyCount != null)
            {
                _lblStockHealthyCount.Text = $"● Healthy Stock (>5): {trends.HealthyStockCount} SKUs";
            }
            if (_lblStockLowCount != null)
            {
                _lblStockLowCount.Text = $"● Low Stock Safety Alert (1-5): {trends.LowStockCount} SKUs";
            }
            if (_lblStockOutCount != null)
            {
                _lblStockOutCount.Text = $"● Out of Stock (Critical): {trends.OutOfStockCount} SKUs";
            }

            if (_pnlStockHealthBar != null)
            {
                _pnlStockHealthBar.SuspendLayout();
                _pnlStockHealthBar.Controls.Clear();

                int total = Math.Max(1, trends.TotalCatalogItems);
                float healthyRatio = (float)trends.HealthyStockCount / total;
                float lowRatio = (float)trends.LowStockCount / total;
                float outRatio = (float)trends.OutOfStockCount / total;

                int w = _pnlStockHealthBar.Width;
                int hW = (int)(healthyRatio * w);
                int lW = (int)(lowRatio * w);
                int oW = w - hW - lW;

                Panel pnlH = new Panel { Dock = DockStyle.Left, Width = hW, BackColor = Color.FromArgb(46, 204, 113) };
                Panel pnlL = new Panel { Dock = DockStyle.Left, Width = lW, BackColor = Color.FromArgb(241, 196, 15) };
                Panel pnlO = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(231, 76, 60) };

                _pnlStockHealthBar.Controls.Add(pnlO);
                _pnlStockHealthBar.Controls.Add(pnlL);
                _pnlStockHealthBar.Controls.Add(pnlH);

                _pnlStockHealthBar.ResumeLayout();
            }
        }

        private void PopulateInventoryMovement(BiStockTrends trends)
        {
            if (_pnlFastMovers != null)
            {
                _pnlFastMovers.SuspendLayout();
                _pnlFastMovers.Controls.Clear();
                foreach (var item in trends.FastMovingProducts.Take(3))
                {
                    Label lbl = new Label
                    {
                        Text = $"⚡ {item.ProductName}\n   {item.UnitsSold} sold  |  ₱{item.Revenue:N0}",
                        Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                        ForeColor = AppTheme.TextDark,
                        Dock = DockStyle.Top,
                        Height = 34,
                        AutoEllipsis = true
                    };
                    _pnlFastMovers.Controls.Add(lbl);
                }
                _pnlFastMovers.ResumeLayout();
            }

            if (_pnlSlowMovers != null)
            {
                _pnlSlowMovers.SuspendLayout();
                _pnlSlowMovers.Controls.Clear();
                foreach (var item in trends.SlowMovingProducts.Take(3))
                {
                    Label lbl = new Label
                    {
                        Text = $"⏳ {item.Name}\n   {item.StockQuantity} shelf stock  |  ₱{item.Price:N0}",
                        Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                        ForeColor = AppTheme.TextMuted,
                        Dock = DockStyle.Top,
                        Height = 34,
                        AutoEllipsis = true
                    };
                    _pnlSlowMovers.Controls.Add(lbl);
                }
                _pnlSlowMovers.ResumeLayout();
            }
        }

        private void ExportBiCsvReport()
        {
            try
            {
                var metrics = _dataService.GetBiSalesMetrics(_currentBiRange);
                var earnings = _dataService.GetBiEarningsSummary();
                var stock = _dataService.GetBiStockTrends();
                var cats = _dataService.GetBiCategoryDistribution(_currentBiRange);
                var best = _dataService.GetBiBestSellers(_currentBiRange, 10);

                using var sfd = new SaveFileDialog
                {
                    Title = "Export Business Intelligence CSV Report",
                    Filter = "CSV File (*.csv)|*.csv",
                    FileName = $"BI_Report_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    using var sw = new StreamWriter(sfd.FileName);
                    sw.WriteLine($"Morphic Core ERP - Business Intelligence Report ({_currentBiRange})");
                    sw.WriteLine($"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine();
                    sw.WriteLine("--- EXECUTIVE METRICS ---");
                    sw.WriteLine($"Total Revenue,PHP {metrics.TotalRevenue:N2}");
                    sw.WriteLine($"Completed Orders,{metrics.OrderCount}");
                    sw.WriteLine($"Units Sold,{metrics.TotalItemsSold}");
                    sw.WriteLine($"Average Order Value,PHP {metrics.AverageOrderValue:N2}");
                    sw.WriteLine($"Estimated Gross Profit,PHP {earnings.GrossProfit:N2}");
                    sw.WriteLine($"Gross Margin Percentage,{earnings.MarginPercent:N1}%");
                    sw.WriteLine($"Total Inventory Valuation,PHP {stock.TotalAssetValuation:N2}");
                    sw.WriteLine();
                    sw.WriteLine("--- SALES BY CATEGORY ---");
                    sw.WriteLine("Category,Revenue,Units Sold,Share %");
                    foreach (var c in cats) sw.WriteLine($"\"{c.CategoryName}\",{c.Revenue:N2},{c.UnitsSold},{c.Percentage:N1}%");
                    sw.WriteLine();
                    sw.WriteLine("--- BEST SELLING PRODUCTS ---");
                    sw.WriteLine("Product,Category,Revenue,Units Sold,Remaining Stock");
                    foreach (var b in best) sw.WriteLine($"\"{b.ProductName}\",\"{b.CategoryName}\",{b.Revenue:N2},{b.UnitsSold},{b.CurrentStock}");

                    MessageBox.Show($"Business Intelligence report exported successfully!\n\nFile: {sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export report: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========================================================
        // 2. STORE OPERATIONS (ORIGINAL DASHBOARD) IMPLEMENTATION
        // ========================================================
        private TableLayoutPanel CreateOperationsView()
        {
            TableLayoutPanel tlpContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12),
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            tlpContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63f));
            tlpContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37f));
            tlpContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 230f));
            tlpContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel pnlOverview = CreateOverviewHero();
            tlpContainer.Controls.Add(pnlOverview, 0, 0);

            TableLayoutPanel tlpMiniCards = CreateMiniCardsGrid();
            tlpContainer.Controls.Add(tlpMiniCards, 1, 0);

            Panel pnlRecentOrders = CreateRecentOrdersCard();
            tlpContainer.Controls.Add(pnlRecentOrders, 0, 1);

            Panel pnlInventoryWatch = CreateCriticalInventoryCard();
            tlpContainer.Controls.Add(pnlInventoryWatch, 1, 1);

            return tlpContainer;
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
            tlpLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            tlpLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

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
                UseMnemonic = false
            };

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
            _gridRecentOrders.Resize += (s, e) => PopulateRecentOrders();

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

            _gridRecentOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ORDER ID", FillWeight = 22, MinimumWidth = 100, Name = "ColId" });
            _gridRecentOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CUSTOMER", FillWeight = 42, MinimumWidth = 140, Name = "ColCust" });
            _gridRecentOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TIME", FillWeight = 18, MinimumWidth = 80, Name = "ColTime" });
            _gridRecentOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TOTAL", FillWeight = 18, MinimumWidth = 90, Name = "ColTotal" });

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

            int effectivePageSize = GetEffectiveRecentOrdersPageSize();
            int totalCount = ordersList.Count;
            _recentTotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)effectivePageSize));

            if (_recentOrdersPage > _recentTotalPages) _recentOrdersPage = _recentTotalPages;
            if (_recentOrdersPage < 1) _recentOrdersPage = 1;

            var pageItems = ordersList.Skip((_recentOrdersPage - 1) * effectivePageSize).Take(effectivePageSize).ToList();

            foreach (var item in pageItems)
            {
                int rowIdx = _gridRecentOrders.Rows.Add(item.Id, item.Cust, item.Time, item.Total);
                var row = _gridRecentOrders.Rows[rowIdx];
                row.Cells["ColId"].Style.ForeColor = Color.FromArgb(160, 110, 10);
                row.Cells["ColId"].Style.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                row.Cells["ColTotal"].Style.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            }

            int start = totalCount == 0 ? 0 : (_recentOrdersPage - 1) * effectivePageSize + 1;
            int end = Math.Min(_recentOrdersPage * effectivePageSize, totalCount);
            if (_lblOrderCount != null)
            {
                _lblOrderCount.Text = totalCount == 0
                    ? "No transactions recorded yet"
                    : $"Displaying rows {start}-{end} of {totalCount} transactions today";
            }

            UpdateDashboardPagination();
        }

        private int GetEffectiveRecentOrdersPageSize()
        {
            if (_gridRecentOrders == null || _gridRecentOrders.ClientSize.Height <= 80) return 8;
            int availHeight = _gridRecentOrders.ClientSize.Height - _gridRecentOrders.ColumnHeadersHeight;
            int rowHeight = _gridRecentOrders.RowTemplate.Height > 0 ? _gridRecentOrders.RowTemplate.Height : 38;
            int fitRows = availHeight / rowHeight;
            return Math.Clamp(fitRows, 6, 25);
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
            UpdatePlanAccessUI();

            // 1. Refresh BI Data if authorized
            if (IsSmallBusinessOrHigher)
            {
                RefreshBiData();
            }

            // 2. Refresh Operations Data
            var todayOrders = _dataService.Orders
                .Where(o => o.CreatedAt.Date == DateTime.Today)
                .ToList();

            decimal todaySales = todayOrders.Sum(o => o.TotalAmount);

            if (_lblRevenueVal != null) _lblRevenueVal.Text = $"₱{todaySales:N2}";
            if (_lblSalesSub != null)
            {
                _lblSalesSub.Text = todaySales > 0 ? "Live gross today" : "No sales recorded today";
            }

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

            int todayTx = todayOrders.Count;
            if (_lblTransactionsVal != null) _lblTransactionsVal.Text = todayTx.ToString("N0");
            if (_lblTxSub != null)
            {
                _lblTxSub.Text = todayTx == 1 ? "1 completed today" : $"{todayTx} completed today";
            }

            var refunds = todayOrders
                .Where(o => o.TotalAmount < 0 || (o.PaymentMethod != null && o.PaymentMethod.Equals("Refund", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            decimal refundTotal = Math.Abs(refunds.Sum(r => r.TotalAmount));
            int refundCount = refunds.Count;

            if (_lblRefundVal != null) _lblRefundVal.Text = $"₱{refundTotal:N2}";
            if (_lblRefundDesc != null) _lblRefundDesc.Text = $"{refundCount} refund{(refundCount == 1 ? "" : "s")} today";

            int lowStockCount = _dataService.Products.Count(p => p.StockQuantity <= 5);
            if (_lblStockCount != null) _lblStockCount.Text = $"{lowStockCount} SKUs";
            if (_lblStockDesc != null) _lblStockDesc.Text = lowStockCount > 0 ? "under safety mark" : "all items well stocked";

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
            if (_lblBenchCount != null) _lblBenchCount.Text = topCat;
            if (_lblBenchDesc != null) _lblBenchDesc.Text = catDesc;

            PopulateRecentOrders();
            PopulateCriticalInventory();
        }
    }
}
