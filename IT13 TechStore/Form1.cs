using System;
using System.Drawing;
using System.Windows.Forms;
using IT8_TechStore.Theme;
using IT8_TechStore.UI.Views;

namespace IT8_TechStore
{
    public partial class Form1 : Form
    {
        private Panel _pnlHeader = null!;
        private Panel _pnlSidebar = null!;
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
        private ProductsPlaceholderView _productsView = null!;
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

            // 1. Top Header Bar
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = AppTheme.HeaderBg
            };

            Label lblBrand = new Label
            {
                Text = "☀️ MORPHIC COMPUTERS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Location = new Point(20, 16),
                AutoSize = true
            };

            Label lblTagline = new Label
            {
                Text = "| Inventory & POS Operations Center",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextLightMuted,
                Location = new Point(265, 22),
                AutoSize = true
            };

            _lblClock = new Label
            {
                Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  hh:mm:ss tt"),
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextLight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 360, 22),
                AutoSize = true
            };

            Label lblUser = new Label
            {
                Text = "👤 Admin",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.Primary,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 100, 22),
                AutoSize = true
            };

            _pnlHeader.Controls.Add(lblBrand);
            _pnlHeader.Controls.Add(lblTagline);
            _pnlHeader.Controls.Add(_lblClock);
            _pnlHeader.Controls.Add(lblUser);
            Controls.Add(_pnlHeader);

            // 2. Left Sidebar Navigation
            _pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 230,
                BackColor = AppTheme.SidebarBg
            };

            Label lblNavSection = new Label
            {
                Text = "MAIN NAVIGATION",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextLightMuted,
                Location = new Point(20, 20),
                AutoSize = true
            };

            _btnNavDashboard = CreateNavButton("📊  Dashboard Overview", 50);
            _btnNavProducts = CreateNavButton("💻  Products & Stock", 102);
            _btnNavPOS = CreateNavButton("🛒  Point of Sale (POS)", 154);
            _btnNavOrders = CreateNavButton("📋  Sales & Reports", 206);

            _dashboardView = new DashboardView();
            _productsView = new ProductsPlaceholderView();
            _posView = new PosPlaceholderView();
            _ordersView = new OrdersPlaceholderView();

            _btnNavDashboard.Click += (s, e) => SwitchView(_dashboardView, _btnNavDashboard);
            _btnNavProducts.Click += (s, e) => SwitchView(_productsView, _btnNavProducts);
            _btnNavPOS.Click += (s, e) => SwitchView(_posView, _btnNavPOS);
            _btnNavOrders.Click += (s, e) => SwitchView(_ordersView, _btnNavOrders);

            _pnlSidebar.Controls.Add(lblNavSection);
            _pnlSidebar.Controls.Add(_btnNavDashboard);
            _pnlSidebar.Controls.Add(_btnNavProducts);
            _pnlSidebar.Controls.Add(_btnNavPOS);
            _pnlSidebar.Controls.Add(_btnNavOrders);

            // System Footer inside sidebar
            Panel pnlSidebarFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(42, 34, 4)
            };

            Label lblVersion = new Label
            {
                Text = "Morphic Computers v1.0.0\nTheme: Sunshine Yellow (#F4D772)",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextLightMuted,
                Location = new Point(16, 12),
                AutoSize = true
            };
            pnlSidebarFooter.Controls.Add(lblVersion);
            _pnlSidebar.Controls.Add(pnlSidebarFooter);

            Controls.Add(_pnlSidebar);

            // 3. Dynamic Content View Container
            _pnlContentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.AppBackground
            };

            Controls.Add(_pnlContentArea);
            _pnlContentArea.BringToFront();
        }

        private Button CreateNavButton(string text, int top)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(12, top),
                Size = new Size(206, 44),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
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
