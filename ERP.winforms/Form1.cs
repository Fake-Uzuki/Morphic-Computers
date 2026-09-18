using System;
using System.Drawing;
using System.IO;
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

        // Top Header & Sub-bars
        private Panel _pnlTopBar = null!;
        private Panel _pnlNavTabs = null!;
        private Panel _pnlBottomBar = null!;
        private Panel _pnlContentArea = null!;

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

        public Form1(string userName = "Cirunay", string userRole = "Store Administrator", Company? company = null)
        {
            _currentUser = userName;
            _currentRole = userRole;
            if (company != null)
            {
                _dataService.CurrentCompany = company;
            }

            InitializeComponent();
            KeyPreview = true;
            KeyDown += Form1_KeyDown;
            FormClosing += (s, e) => _dataService.SaveAllToDisk();

            SetupCustomLayout();

            SwitchView(_dashboardView, _btnNavDashboard);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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

            // Right Header Utilities
            Label lblCloudSync = new Label
            {
                Text = "Central Cloud Sync: Active (100%)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 178, 168),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 560, 18),
                AutoSize = true
            };

            SunshineButton btnNewSale = new SunshineButton
            {
                Text = "+ New Sale (F2)",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 340, 11),
                Size = new Size(130, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnNewSale.Click += (s, e) => SwitchView(_posView, _btnNavPOS);

            // User Avatar Pill
            Panel pnlUser = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 195, 8),
                Size = new Size(175, 38),
                BackColor = Color.FromArgb(32, 34, 28)
            };
            Label lblUserAvatar = new Label
            {
                Text = _currentUser.Substring(0, 1).ToUpperInvariant(),
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
                Size = new Size(135, 30)
            };
            pnlUser.Controls.Add(lblUserAvatar);
            pnlUser.Controls.Add(lblUserName);

            _pnlTopBar.Controls.Add(lblLogoBadge);
            _pnlTopBar.Controls.Add(lblBrandName);
            _pnlTopBar.Controls.Add(lblTagline);
            _pnlTopBar.Controls.Add(lblCloudSync);
            _pnlTopBar.Controls.Add(btnNewSale);
            _pnlTopBar.Controls.Add(pnlUser);

            // ========================================================
            // 2. NAVIGATION TABS BAR (Height: 42px, Flush Dark Background)
            // ========================================================
            _pnlNavTabs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = AppTheme.HeaderBg
            };

            int tabX = 20;
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

            _pnlNavTabs.Controls.Add(_btnNavDashboard);
            _pnlNavTabs.Controls.Add(_btnNavProducts);
            _pnlNavTabs.Controls.Add(_btnNavPOS);
            _pnlNavTabs.Controls.Add(_btnNavOrders);

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
                _pnlNavTabs.Controls.Add(_btnNavRepairs);
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
                _pnlNavTabs.Controls.Add(_btnNavSuppliers);
                tabX += 132;
            }

            // 3. Staff Management (Admin, Manager per architecture diagram)
            if (isSmallBusinessOrHigher && (isAdmin || isManager))
            {
                _btnNavStaff = CreateEnterpriseTab("Staff & Team", tabX, 130);
                _btnNavStaff.Click += (s, e) => SwitchView(_staffView, _btnNavStaff);
                _pnlNavTabs.Controls.Add(_btnNavStaff);
                tabX += 132;
            }

            // 4. Workflow & Approval (Admin, Manager, Staff per architecture diagram)
            if (isSmallBusinessOrHigher)
            {
                _btnNavApprovals = CreateEnterpriseTab("Approvals", tabX, 120);
                _btnNavApprovals.Click += (s, e) => SwitchView(_approvalsView, _btnNavApprovals);
                _pnlNavTabs.Controls.Add(_btnNavApprovals);
                tabX += 122;
            }

            // 5. Customer Management (Manager, Staff, Admin per architecture diagram)
            if (isSmallBusinessOrHigher)
            {
                _btnNavCustomers = CreateEnterpriseTab("Customers", tabX, 120);
                _btnNavCustomers.Click += (s, e) => SwitchView(_customersView, _btnNavCustomers);
                _pnlNavTabs.Controls.Add(_btnNavCustomers);
                tabX += 122;
            }

            // 6. Store Payroll (Manager, Admin per architecture diagram)
            if (isSmallBusinessOrHigher && (isAdmin || isManager))
            {
                _btnNavPayroll = CreateEnterpriseTab("Store Payroll", tabX, 130);
                _btnNavPayroll.Click += (s, e) => SwitchView(_payrollView, _btnNavPayroll);
                _pnlNavTabs.Controls.Add(_btnNavPayroll);
                tabX += 132;
            }

            // 7. Terms & Policies (Admin per architecture diagram)
            if (isSmallBusinessOrHigher && isAdmin)
            {
                _btnNavPolicies = CreateEnterpriseTab("Policies & Terms", tabX, 140);
                _btnNavPolicies.Click += (s, e) => SwitchView(_policiesView, _btnNavPolicies);
                _pnlNavTabs.Controls.Add(_btnNavPolicies);
                tabX += 142;
            }

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
            return btn;
        }

        private void SwitchView(UserControl view, Button navButton)
        {
            if (_activeNavButton != null)
            {
                _activeNavButton.BackColor = Color.Transparent;
                _activeNavButton.ForeColor = AppTheme.NavTabInactiveText;
            }

            _activeNavButton = navButton;
            _activeNavButton.BackColor = AppTheme.NavTabActive;
            _activeNavButton.ForeColor = AppTheme.NavTabActiveText;

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

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                SwitchView(_posView, _btnNavPOS);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                SwitchView(_productsView, _btnNavProducts);
                e.Handled = true;
            }
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
                _lblBottomRight.Text = $"{customMsg}  |  [F2 New Sale]  [F3 Catalog]";
            }
            else if (online)
            {
                _lblBottomRight.ForeColor = AppTheme.BottomBarText;
                _lblBottomRight.Text = $"Latency: 14ms  |  ☁️ MonsterASP Cloud ({companyDb}) Connected  |  [F2 New Sale]  [F3 Catalog]";
            }
            else
            {
                _lblBottomRight.ForeColor = Color.FromArgb(243, 156, 18);
                string syncText = pending > 0 ? $"({pending} queued for sync)" : "(Local Cache Active)";
                _lblBottomRight.Text = $"📶 Offline Mode {syncText}  |  [F2 New Sale]  [F3 Catalog]";
            }
        }
    }
}
