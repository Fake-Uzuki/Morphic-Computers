using System;
using System.Drawing;
using System.Windows.Forms;
using IT8_TechStore.Theme;
using IT8_TechStore.UI.Views;

namespace IT8_TechStore
{
    /// <summary>
    /// NAVBAR OPTION 2: Full Top Horizontal Navigation Bar Layout (SELECTED FINAL DESIGN)
    /// Removes the left sidebar completely so the main content area gets 100% full screen width.
    /// Features interactive card navigation & automatic Add Product popup triggers from Dashboard.
    /// </summary>
    public partial class Form1 : Form
    {
        private Panel _pnlHeader = null!;
        private Panel _pnlNavStrip = null!;
        private Panel _pnlContentArea = null!;
        private Label _lblClock = null!;
        private System.Windows.Forms.Timer _clockTimer = null!;

        // Navigation Buttons
        private Button _btnNavDashboard = null!;
        private Button _btnNavProducts = null!;
        private Button _btnNavPOS = null!;
        private Button _btnNavOrders = null!;
        private Button? _activeNavButton;

        // View Instances
        private DashboardView _dashboardView = null!;
        private ProductsView _productsView = null!;
        private PosPlaceholderView _posView = null!;
        private OrdersPlaceholderView _ordersView = null!;

        public Form1()
        {
            InitializeComponent();
            SetupCustomLayout();
            SetupClockTimer();

            // Default View
            SwitchView(_dashboardView, _btnNavDashboard);
        }

        private void SetupCustomLayout()
        {
            BackColor = AppTheme.AppBackground;

            // 1. Top Integrated Header & Navigation Bar (Navbar Option 2)
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 105,
                BackColor = AppTheme.HeaderBg
            };

            // Top Header Line: Logo & Status
            Label lblBrand = new Label
            {
                Text = "☀️ MORPHIC COMPUTERS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Location = new Point(20, 14),
                AutoSize = true
            };

            Label lblTagline = new Label
            {
                Text = "| Store Operations & POS System",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextLightMuted,
                Location = new Point(265, 20),
                AutoSize = true
            };

            _lblClock = new Label
            {
                Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  hh:mm:ss tt"),
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextLight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 360, 20),
                AutoSize = true
            };

            Label lblUser = new Label
            {
                Text = "👤 Admin",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.Primary,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 100, 20),
                AutoSize = true
            };

            _pnlHeader.Controls.Add(lblBrand);
            _pnlHeader.Controls.Add(lblTagline);
            _pnlHeader.Controls.Add(_lblClock);
            _pnlHeader.Controls.Add(lblUser);

            // Bottom Header Line: Horizontal Navigation Strip
            _pnlNavStrip = new Panel
            {
                Location = new Point(20, 56),
                Size = new Size(Width - 40, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(31, 25, 12)
            };

            _btnNavDashboard = CreateHorizontalNavButton("📊  Dashboard Overview", 0);
            _btnNavProducts = CreateHorizontalNavButton("💻  Products & Stock", 200);
            _btnNavPOS = CreateHorizontalNavButton("🛒  Point of Sale (POS)", 400);
            _btnNavOrders = CreateHorizontalNavButton("📋  Sales & Reports", 600);

            _dashboardView = new DashboardView();
            _productsView = new ProductsView();
            _posView = new PosPlaceholderView();
            _ordersView = new OrdersPlaceholderView();

            // Wire Dashboard Interactive Card & Action Navigation Delegates
            _dashboardView.OnNavigateToPOSRequest = () => SwitchView(_posView, _btnNavPOS);
            _dashboardView.OnNavigateToOrdersRequest = () => SwitchView(_ordersView, _btnNavOrders);
            _dashboardView.OnNavigateToProductsRequest = (openAddModal) =>
            {
                SwitchView(_productsView, _btnNavProducts);
                if (openAddModal)
                {
                    _productsView.OpenAddProductDialog();
                }
            };

            _btnNavDashboard.Click += (s, e) => SwitchView(_dashboardView, _btnNavDashboard);
            _btnNavProducts.Click += (s, e) => SwitchView(_productsView, _btnNavProducts);
            _btnNavPOS.Click += (s, e) => SwitchView(_posView, _btnNavPOS);
            _btnNavOrders.Click += (s, e) => SwitchView(_ordersView, _btnNavOrders);

            _pnlNavStrip.Controls.Add(_btnNavDashboard);
            _pnlNavStrip.Controls.Add(_btnNavProducts);
            _pnlNavStrip.Controls.Add(_btnNavPOS);
            _pnlNavStrip.Controls.Add(_btnNavOrders);

            _pnlHeader.Controls.Add(_pnlNavStrip);
            Controls.Add(_pnlHeader);

            // 2. Full-Width Dynamic Content View Container (100% Screen Width!)
            _pnlContentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.AppBackground
            };

            Controls.Add(_pnlContentArea);
            _pnlContentArea.BringToFront();
        }

        private Button CreateHorizontalNavButton(string text, int left)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(left, 2),
                Size = new Size(195, 38),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextLight,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.MouseEnter += (s, e) =>
            {
                if (btn != _activeNavButton)
                    btn.BackColor = AppTheme.SidebarHover;
            };

            btn.MouseLeave += (s, e) =>
            {
                if (btn != _activeNavButton)
                    btn.BackColor = Color.Transparent;
            };

            return btn;
        }

        private void SwitchView(UserControl view, Button navButton)
        {
            if (_activeNavButton != null)
            {
                _activeNavButton.BackColor = Color.Transparent;
                _activeNavButton.ForeColor = AppTheme.TextLight;
            }

            _activeNavButton = navButton;
            _activeNavButton.BackColor = AppTheme.SidebarSelected;
            _activeNavButton.ForeColor = AppTheme.SidebarSelectedText;

            _pnlContentArea.Controls.Clear();
            view.Dock = DockStyle.Fill;
            _pnlContentArea.Controls.Add(view);

            if (view is DashboardView dbView)
            {
                dbView.RefreshDashboard();
            }
        }

        private void SetupClockTimer()
        {
            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) =>
            {
                _lblClock.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  hh:mm:ss tt");
            };
            _clockTimer.Start();
        }
    }
}
