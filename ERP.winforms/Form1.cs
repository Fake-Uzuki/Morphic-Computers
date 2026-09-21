using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;
using ERP.winforms.UI.Dialogs;
using ERP.winforms.UI.Views;

namespace ERP.winforms
{
    public partial class Form1 : Form
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly string _currentUser;
        private readonly string _currentRole;

        public bool IsLoggedOut { get; private set; }

        // Top Header & Sub-bars
        private Panel _pnlTopBar = null!;
        private Panel _pnlNavTabs = null!;
        private Panel _pnlBottomBar = null!;
        private Panel _pnlContentArea = null!;

        // Upper-right header controls
        private Panel _pnlTopRight = null!;
        private Button _btnLogout = null!;
        private Button _btnNotifications = null!;
        private Label _lblNotificationBadge = null!;
        private NotificationFlyout _notificationFlyout = null!;

        // Scrollable navigation slidebar controls
        private Button _btnNavScrollLeft = null!;
        private Button _btnNavScrollRight = null!;
        private Panel _pnlTabsViewport = null!;
        private Panel _pnlTabsTrack = null!;
        private Panel _pnlNavSliderTrack = null!;
        private Panel _pnlNavSliderThumb = null!;
        private int _navScrollOffset = 0;

        // Navigation tab buttons
        private Button _btnNavDashboard = null!;
        private Button _btnNavProducts = null!;
        private Button _btnNavPOS = null!;
        private Button _btnNavOrders = null!;
        private Button? _btnNavRepairs;
        private Button? _btnNavSuppliers;
        private Button? _btnNavStaff;
        private Button? _btnNavApprovals;
        private Button? _btnNavCustomers;
        private Button? _btnNavPayroll;
        private Button? _btnNavPolicies;
        private Button? _activeNavButton;
        private Label _lblBottomRight = null!;

        // Views
        private DashboardView _dashboardView = null!;
        private ProductsView _productsView = null!;
        private PosView _posView = null!;
        private OrdersView _ordersView = null!;
        private RepairsView _repairsView = null!;
        private SuppliersView _suppliersView = null!;
        private StaffView _staffView = null!;
        private ApprovalsView _approvalsView = null!;
        private CustomersView _customersView = null!;
        private PayrollView _payrollView = null!;
        private PoliciesView _policiesView = null!;

        private readonly HashSet<string> _dismissedAlertKeys = new();
        private readonly HashSet<string> _readAlertKeys = new();

        public Form1(string userName = "Cirunay", string userRole = "Store Administrator", Company? company = null)
        {
            _currentUser = userName;
            _currentRole = userRole;
            if (company != null)
            {
                _dataService.CurrentCompany = company;
            }

            InitializeComponent();
            FormClosing += (s, e) =>
            {
                SaveNotificationState();
                _dataService.SaveAllToDisk();
            };

            SetupCustomLayout();

            SwitchView(_dashboardView, _btnNavDashboard);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveNotificationState();
            _dataService.SaveAllToDisk();
            base.OnFormClosing(e);
        }

        private void SetupCustomLayout()
        {
            BackColor = AppTheme.AppBackground;
            Size = new Size(1380, 860);
            MinimumSize = new Size(1240, 780);
            StartPosition = FormStartPosition.CenterScreen;
            Text = $"{_dataService.CurrentCompany?.CompanyName ?? "Tenant A"} | {_dataService.CurrentCompany?.PlanName ?? "Micro"} Company Operations - User: {_currentUser}";

            // ========================================================
            // 1. TOP HEADER BANNER (Height: 54px, Dark Charcoal #141511)
            // ========================================================
            _pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = AppTheme.HeaderBg
            };

            // [M] Gold Brand Badge
            Label lblLogoBadge = new Label
            {
                Text = "M",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                BackColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 13),
                Size = new Size(28, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Dynamic Tenant Brand Title
            Label lblBrandName = new Label
            {
                Text = _dataService.CurrentCompany?.CompanyName ?? "Tenant A",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(56, 12),
                AutoSize = true
            };

            // Subtitle tag cleanly aligned
            Label lblTagline = new Label
            {
                Text = $"|  {_dataService.CurrentCompany?.PlanName ?? "Micro"} Company Operations",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 168, 158),
                Location = new Point(56 + lblBrandName.PreferredSize.Width + 8, 17),
                AutoSize = true
            };

            // ========================================================
            // UPPER-RIGHT HEADER UTILITIES (Notification, User, Logout)
            // ========================================================
            _pnlTopRight = new Panel
            {
                Dock = DockStyle.Right,
                Height = 54,
                Width = 490,
                BackColor = Color.Transparent
            };

            // 1. New Sale Action Button
            SunshineButton btnNewSale = new SunshineButton
            {
                Text = "+ New Sale",
                IsPrimary = true,
                Location = new Point(12, 11),
                Size = new Size(110, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnNewSale.Click += (s, e) => SwitchView(_posView, _btnNavPOS);

            // 2. Notification Center Button with dynamic Unread Badge
            Panel pnlNotifBox = new Panel
            {
                Location = new Point(130, 11),
                Size = new Size(88, 32),
                BackColor = Color.Transparent
            };

            _btnNotifications = new Button
            {
                Text = "🔔 Alerts",
                Location = new Point(0, 0),
                Size = new Size(88, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(32, 34, 28),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnNotifications.FlatAppearance.BorderSize = 0;
            _btnNotifications.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 50, 42);
            _btnNotifications.Click += (s, e) => ToggleNotifications();

            _lblNotificationBadge = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 6.8F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                BackColor = AppTheme.Primary,
                Location = new Point(66, 2),
                Size = new Size(18, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            _lblNotificationBadge.Click += (s, e) => ToggleNotifications();

            pnlNotifBox.Controls.Add(_lblNotificationBadge);
            pnlNotifBox.Controls.Add(_btnNotifications);
            _lblNotificationBadge.BringToFront();

            // 3. User Profile Avatar Pill
            Panel pnlUser = new Panel
            {
                Location = new Point(226, 8),
                Size = new Size(168, 38),
                BackColor = Color.FromArgb(32, 34, 28)
            };
            Label lblUserAvatar = new Label
            {
                Text = _currentUser.Length > 0 ? _currentUser.Substring(0, 1).ToUpperInvariant() : "U",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                BackColor = AppTheme.Primary,
                Location = new Point(6, 6),
                Size = new Size(26, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label lblUserName = new Label
            {
                Text = $"{_currentUser}\n{_currentRole}",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(36, 4),
                Size = new Size(128, 30)
            };
            pnlUser.Controls.Add(lblUserAvatar);
            pnlUser.Controls.Add(lblUserName);

            // 4. Logout Action Button
            _btnLogout = new Button
            {
                Text = "⎋ Logout",
                Location = new Point(402, 11),
                Size = new Size(76, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 235, 220),
                BackColor = Color.FromArgb(42, 36, 30),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnLogout.FlatAppearance.BorderSize = 0;
            _btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(170, 45, 35);
            _btnLogout.Click += BtnLogout_Click;

            _pnlTopRight.Controls.Add(btnNewSale);
            _pnlTopRight.Controls.Add(pnlNotifBox);
            _pnlTopRight.Controls.Add(pnlUser);
            _pnlTopRight.Controls.Add(_btnLogout);

            _pnlTopBar.Controls.Add(lblLogoBadge);
            _pnlTopBar.Controls.Add(lblBrandName);
            _pnlTopBar.Controls.Add(lblTagline);
            _pnlTopBar.Controls.Add(_pnlTopRight);

            // Notification Flyout Setup
            _notificationFlyout = new NotificationFlyout
            {
                Visible = false,
                Location = new Point(Width - 410, 54),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _notificationFlyout.OnRequestClose = () => _notificationFlyout.Visible = false;
            _notificationFlyout.OnNotificationsChanged = () => UpdateNotificationBadge();
            Controls.Add(_notificationFlyout);

            // ========================================================
            // 2. NAVIGATION TABS BAR (Height: 46px, Scrollable Slidebar)
            // ========================================================
            _pnlNavTabs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = AppTheme.HeaderBg
            };

            _btnNavScrollLeft = new Button
            {
                Text = "◀",
                Dock = DockStyle.Left,
                Width = 28,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                BackColor = Color.FromArgb(24, 25, 20),
                Cursor = Cursors.Hand
            };
            _btnNavScrollLeft.FlatAppearance.BorderSize = 0;
            _btnNavScrollLeft.Click += (s, e) => ScrollNavTabs(-200);

            _btnNavScrollRight = new Button
            {
                Text = "▶",
                Dock = DockStyle.Right,
                Width = 28,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                BackColor = Color.FromArgb(24, 25, 20),
                Cursor = Cursors.Hand
            };
            _btnNavScrollRight.FlatAppearance.BorderSize = 0;
            _btnNavScrollRight.Click += (s, e) => ScrollNavTabs(200);

            _pnlTabsViewport = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.HeaderBg,
                AutoScroll = false
            };

            _pnlTabsTrack = new Panel
            {
                Location = new Point(0, 0),
                Height = 43,
                Width = 1600,
                BackColor = Color.Transparent
            };

            // Slidebar track at bottom of navigation
            _pnlNavSliderTrack = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                BackColor = Color.FromArgb(32, 34, 28)
            };
            _pnlNavSliderThumb = new Panel
            {
                Height = 3,
                Width = 100,
                BackColor = AppTheme.Primary,
                Location = new Point(0, 0)
            };
            _pnlNavSliderTrack.Controls.Add(_pnlNavSliderThumb);

            _pnlTabsViewport.Controls.Add(_pnlTabsTrack);
            _pnlNavTabs.Controls.Add(_pnlTabsViewport);
            _pnlNavTabs.Controls.Add(_btnNavScrollLeft);
            _pnlNavTabs.Controls.Add(_btnNavScrollRight);
            _pnlNavTabs.Controls.Add(_pnlNavSliderTrack);

            void HandleWheel(object? s, MouseEventArgs e) => ScrollNavTabs(e.Delta > 0 ? -150 : 150);
            _pnlTabsViewport.MouseWheel += HandleWheel;
            _pnlTabsTrack.MouseWheel += HandleWheel;

            int tabX = 10;
            _btnNavDashboard = CreateEnterpriseTab("Dashboard", tabX, 130);
            tabX += 132;
            _btnNavProducts = CreateEnterpriseTab("Products Stock", tabX, 145);
            tabX += 147;
            _btnNavPOS = CreateEnterpriseTab("Point of Sale", tabX, 135);
            tabX += 137;
            _btnNavOrders = CreateEnterpriseTab("Sales Reports", tabX, 140);
            tabX += 142;

            _btnNavDashboard.Click += (s, e) => SwitchView(_dashboardView, _btnNavDashboard);
            _btnNavProducts.Click += (s, e) => SwitchView(_productsView, _btnNavProducts);
            _btnNavPOS.Click += (s, e) => SwitchView(_posView, _btnNavPOS);
            _btnNavOrders.Click += (s, e) => SwitchView(_ordersView, _btnNavOrders);

            _pnlTabsTrack.Controls.Add(_btnNavDashboard);
            _pnlTabsTrack.Controls.Add(_btnNavProducts);
            _pnlTabsTrack.Controls.Add(_btnNavPOS);
            _pnlTabsTrack.Controls.Add(_btnNavOrders);

            // ========================================================
            // TENANT B (SMALL BUSINESS) MODULES DYNAMIC INTEGRATION
            // ========================================================
            var company = _dataService.ActiveCompany;
            bool isSmallBusinessOrHigher = company != null && (company.IsRepairAllowed || !string.Equals(company.PlanName, "Micro", StringComparison.OrdinalIgnoreCase));

            // 1. Service & Repair Management (Admin, Manager, Staff)
            if (isSmallBusinessOrHigher)
            {
                _btnNavRepairs = CreateEnterpriseTab("Repair Services", tabX, 145);
                _btnNavRepairs.Click += (s, e) => SwitchView(_repairsView, _btnNavRepairs);
                _pnlTabsTrack.Controls.Add(_btnNavRepairs);
                tabX += 147;
            }

            // 2. Supplier Management (Admin per architecture diagram)
            bool isAdmin = _currentRole.Contains("Admin", StringComparison.OrdinalIgnoreCase) || 
                           _currentRole.Contains("Owner", StringComparison.OrdinalIgnoreCase);
            bool isManager = _currentRole.Contains("Manager", StringComparison.OrdinalIgnoreCase);
            bool isStaff = _currentRole.Contains("Staff", StringComparison.OrdinalIgnoreCase) ||
                           _currentRole.Contains("Tech", StringComparison.OrdinalIgnoreCase) ||
                           _currentRole.Contains("Cashier", StringComparison.OrdinalIgnoreCase);

            if (isSmallBusinessOrHigher && isAdmin)
            {
                _btnNavSuppliers = CreateEnterpriseTab("Suppliers", tabX, 130);
                _btnNavSuppliers.Click += (s, e) => SwitchView(_suppliersView, _btnNavSuppliers);
                _pnlTabsTrack.Controls.Add(_btnNavSuppliers);
                tabX += 132;
            }

            // 3. Staff Management (Admin, Manager per architecture diagram)
            if (isSmallBusinessOrHigher && (isAdmin || isManager))
            {
                _btnNavStaff = CreateEnterpriseTab("Staff & Team", tabX, 130);
                _btnNavStaff.Click += (s, e) => SwitchView(_staffView, _btnNavStaff);
                _pnlTabsTrack.Controls.Add(_btnNavStaff);
                tabX += 132;
            }

            // 4. Workflow & Approval (Admin, Manager, Staff per architecture diagram)
            if (isSmallBusinessOrHigher)
            {
                _btnNavApprovals = CreateEnterpriseTab("Approvals", tabX, 120);
                _btnNavApprovals.Click += (s, e) => SwitchView(_approvalsView, _btnNavApprovals);
                _pnlTabsTrack.Controls.Add(_btnNavApprovals);
                tabX += 122;
            }

            // 5. Customer Management (Manager, Staff, Admin per architecture diagram)
            if (isSmallBusinessOrHigher)
            {
                _btnNavCustomers = CreateEnterpriseTab("Customers", tabX, 120);
                _btnNavCustomers.Click += (s, e) => SwitchView(_customersView, _btnNavCustomers);
                _pnlTabsTrack.Controls.Add(_btnNavCustomers);
                tabX += 122;
            }

            // 6. Store Payroll (Manager, Admin per architecture diagram)
            if (isSmallBusinessOrHigher && (isAdmin || isManager))
            {
                _btnNavPayroll = CreateEnterpriseTab("Store Payroll", tabX, 130);
                _btnNavPayroll.Click += (s, e) => SwitchView(_payrollView, _btnNavPayroll);
                _pnlTabsTrack.Controls.Add(_btnNavPayroll);
                tabX += 132;
            }

            // 7. Terms & Policies (Admin per architecture diagram)
            if (isSmallBusinessOrHigher && isAdmin)
            {
                _btnNavPolicies = CreateEnterpriseTab("Policies & Terms", tabX, 140);
                _btnNavPolicies.Click += (s, e) => SwitchView(_policiesView, _btnNavPolicies);
                _pnlTabsTrack.Controls.Add(_btnNavPolicies);
                tabX += 142;
            }

            _pnlTabsTrack.Width = tabX + 20;
            UpdateNavScrollState();
            LoadNotificationState();
            UpdateNotificationCenter();

            // ========================================================
            // 3. BOTTOM SYSTEM STATUS BAR (Height: 28px, Dark Charcoal)
            // ========================================================
            _pnlBottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = AppTheme.BottomBarBg
            };

            Label lblBottomLeft = new Label
            {
                Text = $"Morphic Core ERP  |  Company: {_dataService.CurrentCompany?.CompanyName ?? "Tenant A"}  |  Plan: {_dataService.CurrentCompany?.PlanName ?? "Micro"}  |  Currency: Philippine Peso (PHP ₱)",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = AppTheme.BottomBarText,
                Location = new Point(20, 6),
                AutoSize = true
            };

            _lblBottomRight = new Label
            {
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = AppTheme.BottomBarText,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 580, 6),
                AutoSize = true
            };
            _pnlBottomBar.Controls.Add(lblBottomLeft);
            _pnlBottomBar.Controls.Add(_lblBottomRight);

            // Hook live connectivity & sync status updates
            UpdateConnectionStatus();
            SyncManager.Instance.SyncStatusChanged += (isSynced, remaining, msg) => UpdateConnectionStatus(null, remaining, msg);
            _dataService.ConnectionStatusChanged += (isLive) => UpdateConnectionStatus(isLive);
            System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += (s, e) =>
            {
                UpdateConnectionStatus(e.IsAvailable);
                if (e.IsAvailable)
                {
                    Task.Run(() => _dataService.LoadFromDatabase());
                }
            };

            // ========================================================
            // 4. MAIN CONTENT AREA (Canvas Background #F7F5EE)
            // ========================================================
            _pnlContentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.AppBackground
            };

            // Instantiate Views
            _dashboardView = new DashboardView();
            _productsView = new ProductsView();
            _posView = new PosView();
            _ordersView = new OrdersView();
            _repairsView = new RepairsView();
            _suppliersView = new SuppliersView();
            _staffView = new StaffView();
            _approvalsView = new ApprovalsView(_currentUser, _currentRole);
            _customersView = new CustomersView();
            _payrollView = new PayrollView();
            _policiesView = new PoliciesView();

            // Wire inter-view navigation events
            _dashboardView.OnNavigateToPOSRequest = () => SwitchView(_posView, _btnNavPOS);
            _dashboardView.OnNavigateToProductsRequest = (lowStock) =>
            {
                SwitchView(_productsView, _btnNavProducts);
                if (lowStock) _productsView.FilterLowStockOnly();
            };
            _dashboardView.OnNavigateToOrdersRequest = () => SwitchView(_ordersView, _btnNavOrders);

            _posView.OnOrderCompleted = () =>
            {
                _dashboardView.RefreshMetrics();
                _ordersView.RefreshData();
            };

            _productsView.OnProductsChanged = () =>
            {
                _posView.RefreshCatalog();
                _dashboardView.RefreshMetrics();
            };

            // Add containers in docking sequence
            Controls.Add(_pnlContentArea);
            Controls.Add(_pnlNavTabs);
            Controls.Add(_pnlTopBar);
            Controls.Add(_pnlBottomBar);
        }

        private Button CreateEnterpriseTab(string text, int x, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(width, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = AppTheme.NavTabInactiveText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = AppTheme.NavTabHover;
            btn.MouseWheel += (s, e) => ScrollNavTabs(e.Delta > 0 ? -150 : 150);
            return btn;
        }

        private void SwitchView(UserControl view, Button navButton)
        {
            if (_notificationFlyout != null) _notificationFlyout.Visible = false;

            if (_activeNavButton != null)
            {
                _activeNavButton.BackColor = Color.Transparent;
                _activeNavButton.ForeColor = AppTheme.NavTabInactiveText;
            }

            _activeNavButton = navButton;
            _activeNavButton.BackColor = AppTheme.NavTabActive;
            _activeNavButton.ForeColor = AppTheme.NavTabActiveText;

            ScrollTabIntoView(navButton);

            _pnlContentArea.Controls.Clear();
            view.Dock = DockStyle.Fill;
            _pnlContentArea.Controls.Add(view);

            if (view is DashboardView db) db.RefreshMetrics();
            if (view is ProductsView pv) pv.ApplyFilters();
            if (view is PosView pos) pos.RefreshCatalog();
            if (view is OrdersView ov) ov.RefreshData();
            if (view is RepairsView rv) rv.RefreshData();
            if (view is SuppliersView sv) sv.RefreshData();
            if (view is StaffView stv) stv.RefreshData();
            if (view is ApprovalsView av) av.RefreshData();
            if (view is CustomersView cv) cv.RefreshData();
        }

        private void ToggleNotifications()
        {
            if (_notificationFlyout == null) return;
            _notificationFlyout.Visible = !_notificationFlyout.Visible;
            if (_notificationFlyout.Visible)
            {
                UpdateNotificationCenter();
                _notificationFlyout.Location = new Point(Width - 410, 54);
                _notificationFlyout.BringToFront();
                MarkActiveAlertsAsViewed();
            }
        }

        private void MarkActiveAlertsAsViewed()
        {
            if (_notificationFlyout == null) return;
            bool changed = false;
            foreach (var item in _notificationFlyout.Notifications)
            {
                if (!string.IsNullOrEmpty(item.Key) && !_readAlertKeys.Contains(item.Key))
                {
                    _readAlertKeys.Add(item.Key);
                    item.IsRead = true;
                    changed = true;
                }
            }
            if (changed)
            {
                _notificationFlyout.RefreshUI();
                UpdateNotificationBadge();
                SaveNotificationState();
            }
        }

        private void UpdateNotificationCenter()
        {
            if (_notificationFlyout == null) return;

            var alerts = new System.Collections.Generic.List<NotificationItem>();

            // 1. Critical and Low Stock Alerts
            var lowStockItems = _dataService.Products.Where(p => p.StockQuantity <= 5).Take(6).ToList();
            foreach (var p in lowStockItems)
            {
                string key = $"STOCK_{p.ProductId}_{p.StockQuantity}";
                if (_dismissedAlertKeys.Contains(key)) continue;

                alerts.Add(new NotificationItem
                {
                    Key = key,
                    Category = "STOCK",
                    Title = p.StockQuantity == 0 ? $"Out of Stock: {p.Name}" : $"Low Stock Alert: {p.Name}",
                    Message = p.StockQuantity == 0 ? "Inventory exhausted. Order replenishment immediately." : $"Only {p.StockQuantity} units remaining in stock.",
                    ActionText = "Review Stock →",
                    IsRead = _readAlertKeys.Contains(key),
                    OnClickAction = () =>
                    {
                        SwitchView(_productsView, _btnNavProducts);
                        _productsView.FilterLowStockOnly();
                    },
                    OnDismiss = () =>
                    {
                        _dismissedAlertKeys.Add(key);
                        SaveNotificationState();
                        UpdateNotificationBadge();
                    }
                });
            }

            // 2. Pending Approval Requests
            var pendingApprovals = _dataService.ApprovalRequests.Where(a => a.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)).Take(3).ToList();
            foreach (var app in pendingApprovals)
            {
                string key = $"APPROVAL_{app.RequestId}";
                if (_dismissedAlertKeys.Contains(key)) continue;

                alerts.Add(new NotificationItem
                {
                    Key = key,
                    Category = "APPROVAL",
                    Title = $"Pending Approval: {app.RequestType}",
                    Message = $"Requester: {app.RequestedBy} - {app.ReasonDescription}",
                    ActionText = "Open Approvals →",
                    IsRead = _readAlertKeys.Contains(key),
                    OnClickAction = () =>
                    {
                        if (_btnNavApprovals != null) SwitchView(_approvalsView, _btnNavApprovals);
                    },
                    OnDismiss = () =>
                    {
                        _dismissedAlertKeys.Add(key);
                        SaveNotificationState();
                        UpdateNotificationBadge();
                    }
                });
            }

            // 3. Active Repair Tickets
            var activeRepairs = _dataService.RepairTickets.Where(r => !r.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && !r.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)).Take(3).ToList();
            foreach (var rep in activeRepairs)
            {
                string key = $"REPAIR_{rep.RepairTicketId}_{rep.Status}";
                if (_dismissedAlertKeys.Contains(key)) continue;

                alerts.Add(new NotificationItem
                {
                    Key = key,
                    Category = "REPAIR",
                    Title = $"Repair Job: #{rep.TicketNumber}",
                    Message = $"{rep.CustomerName} - {rep.DeviceBrandModel} ({rep.Status})",
                    ActionText = "View Repairs →",
                    IsRead = _readAlertKeys.Contains(key),
                    OnClickAction = () =>
                    {
                        if (_btnNavRepairs != null) SwitchView(_repairsView, _btnNavRepairs);
                    },
                    OnDismiss = () =>
                    {
                        _dismissedAlertKeys.Add(key);
                        SaveNotificationState();
                        UpdateNotificationBadge();
                    }
                });
            }

            _notificationFlyout.SetNotifications(alerts);
            UpdateNotificationBadge();
        }

        private void UpdateNotificationBadge()
        {
            if (_lblNotificationBadge == null || _notificationFlyout == null) return;
            int count = _notificationFlyout.UnreadCount;
            _lblNotificationBadge.Text = count > 9 ? "9+" : count.ToString();
            _lblNotificationBadge.Visible = count > 0;
        }

        private string GetNotificationStateFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"tenant_{_dataService.ActiveCompanyId}_notifications.json");
        }

        private void LoadNotificationState()
        {
            try
            {
                string filePath = GetNotificationStateFilePath();
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<NotificationStateData>(json);
                    if (data != null)
                    {
                        _dismissedAlertKeys.Clear();
                        if (data.DismissedKeys != null)
                        {
                            foreach (var k in data.DismissedKeys) _dismissedAlertKeys.Add(k);
                        }
                        _readAlertKeys.Clear();
                        if (data.ReadKeys != null)
                        {
                            foreach (var k in data.ReadKeys) _readAlertKeys.Add(k);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notifications] Error loading state: {ex.Message}");
            }
        }

        private void SaveNotificationState()
        {
            try
            {
                string filePath = GetNotificationStateFilePath();
                var data = new NotificationStateData
                {
                    DismissedKeys = _dismissedAlertKeys.ToList(),
                    ReadKeys = _readAlertKeys.ToList()
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notifications] Error saving state: {ex.Message}");
            }
        }

        private class NotificationStateData
        {
            public List<string> DismissedKeys { get; set; } = new();
            public List<string> ReadKeys { get; set; } = new();
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to log out from {_dataService.CurrentCompany?.CompanyName ?? "the system"}?\n\nAll session data and pending operations will be safely preserved.",
                "Confirm Sign Out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                IsLoggedOut = true;
                SaveNotificationState();
                _dataService.SaveAllToDisk();
                Close();
            }
        }

        private void ScrollNavTabs(int delta)
        {
            if (_pnlTabsTrack == null || _pnlTabsViewport == null) return;
            int maxScroll = Math.Max(0, _pnlTabsTrack.Width - _pnlTabsViewport.Width);
            _navScrollOffset = Math.Clamp(_navScrollOffset + delta, 0, maxScroll);
            _pnlTabsTrack.Location = new Point(-_navScrollOffset, 0);
            UpdateNavScrollState();
        }

        private void ScrollTabIntoView(Button? tabButton)
        {
            if (tabButton == null || _pnlTabsViewport == null || _pnlTabsTrack == null) return;
            int maxScroll = Math.Max(0, _pnlTabsTrack.Width - _pnlTabsViewport.Width);
            if (maxScroll <= 0) return;

            int tabLeft = tabButton.Left;
            int tabRight = tabButton.Right;
            int viewLeft = _navScrollOffset;
            int viewRight = _navScrollOffset + _pnlTabsViewport.Width;

            if (tabLeft < viewLeft)
            {
                ScrollNavTabs(tabLeft - viewLeft - 30);
            }
            else if (tabRight > viewRight)
            {
                ScrollNavTabs(tabRight - viewRight + 30);
            }
        }

        private void UpdateNavScrollState()
        {
            if (_pnlTabsTrack == null || _pnlTabsViewport == null || _pnlNavSliderTrack == null || _pnlNavSliderThumb == null) return;
            int maxScroll = Math.Max(0, _pnlTabsTrack.Width - _pnlTabsViewport.Width);
            bool canScroll = maxScroll > 0;

            _btnNavScrollLeft.Visible = canScroll;
            _btnNavScrollRight.Visible = canScroll;
            _pnlNavSliderTrack.Visible = canScroll;

            if (canScroll)
            {
                _btnNavScrollLeft.Enabled = _navScrollOffset > 0;
                _btnNavScrollLeft.ForeColor = _btnNavScrollLeft.Enabled ? AppTheme.Primary : Color.FromArgb(90, 88, 80);

                _btnNavScrollRight.Enabled = _navScrollOffset < maxScroll;
                _btnNavScrollRight.ForeColor = _btnNavScrollRight.Enabled ? AppTheme.Primary : Color.FromArgb(90, 88, 80);

                float ratio = (float)_navScrollOffset / maxScroll;
                int trackWidth = Math.Max(1, _pnlNavSliderTrack.Width);
                int thumbWidth = Math.Max(40, (int)((float)_pnlTabsViewport.Width / _pnlTabsTrack.Width * trackWidth));
                int thumbX = (int)(ratio * (trackWidth - thumbWidth));
                _pnlNavSliderThumb.Width = thumbWidth;
                _pnlNavSliderThumb.Location = new Point(thumbX, 0);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_notificationFlyout != null)
            {
                _notificationFlyout.Location = new Point(Width - 410, 54);
            }
            UpdateNavScrollState();
        }

        private void BtnNavBackup_Click(object? sender, EventArgs e)
        {
            using SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Backup Isolated Tenant Store Data",
                Filter = "JSON Backup File (*.json)|*.json",
                FileName = $"TenantA_Backup_{DateTime.Now:yyyyMMdd_HHmm}.json"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _dataService.ExportTenantBackup(sfd.FileName);
                    MessageBox.Show($"Tenant A database backup created successfully!\n\nFile saved to:\n{sfd.FileName}", "Backup Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export tenant backup: {ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateConnectionStatus(bool? isOnline = null, int? pendingSync = null, string? customMsg = null)
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateConnectionStatus(isOnline, pendingSync, customMsg)));
                return;
            }

            bool online = isOnline ?? (_dataService.IsUsingLiveCloudDatabase && System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable());
            int pending = pendingSync ?? SyncManager.Instance.PendingCount;
            string companyDb = _dataService.CurrentCompany?.CompanyId == 1 ? "db67673" : "Isolated Tenant";

            if (!string.IsNullOrEmpty(customMsg))
            {
                _lblBottomRight.Text = customMsg;
            }
            else if (online)
            {
                _lblBottomRight.ForeColor = AppTheme.BottomBarText;
                _lblBottomRight.Text = $"Latency: 14ms  |  ☁️ Cloud Database ({companyDb}) Connected  |  Store Operations Online";
            }
            else
            {
                _lblBottomRight.ForeColor = Color.FromArgb(243, 156, 18);
                string syncText = pending > 0 ? $"({pending} queued for sync)" : "(Local Cache Active)";
                _lblBottomRight.Text = $"📶 Offline Mode {syncText}  |  Local Storage Active";
            }
        }
    }
}
