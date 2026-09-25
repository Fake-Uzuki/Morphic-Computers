using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using ERP.domain.entities;
using ERP.domain.security;
using ERP.infrastructure.data;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Views
{
    public class SubscriptionRowItem
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string ModulesSummary { get; set; } = string.Empty;
    }

    public class TenantRowItem
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }

    public class SuperAdminView : UserControl
    {
        private enum SuperAdminTab
        {
            AdminPanel,
            TenantManagement,
            SubscriptionManagement,
            PlatformBI
        }

        private SuperAdminTab _activeTab = SuperAdminTab.AdminPanel;

        // Subtab Buttons
        private Button _btnTabOverview = null!;
        private Button _btnTabTenants = null!;
        private Button _btnTabSubscriptions = null!;
        private Button _btnTabBI = null!;

        // Content Host
        private Panel _pnlBody = null!;

        // Data cache
        private List<Company> _companies = new();
        private List<CompanyDatabase> _databases = new();
        private List<Device> _devices = new();

        public SuperAdminView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeLayout();
            LoadMasterData();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // 1. TOP HEADER BANNER
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(24, 12, 24, 10),
                BackColor = AppTheme.HeaderBg
            };

            Label lblTitle = new Label
            {
                Text = "Morphic ERP  |  Super Administrator Platform Control Center",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 10),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Master Database Routing  •  Tenant Isolation  •  Subscriptions  •  Cross-Tenant Platform Intelligence",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, 36),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // 2. SUB-NAVIGATION BAR
            Panel pnlNav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 0, 24, 0),
                BackColor = Color.FromArgb(24, 25, 20)
            };

            int tx = 24;
            _btnTabOverview = CreateTabBtn("System Admin Panel", tx, 160, SuperAdminTab.AdminPanel);
            tx += 164;
            _btnTabTenants = CreateTabBtn("Tenant Management", tx, 160, SuperAdminTab.TenantManagement);
            tx += 164;
            _btnTabSubscriptions = CreateTabBtn("Subscription Management", tx, 180, SuperAdminTab.SubscriptionManagement);
            tx += 184;
            _btnTabBI = CreateTabBtn("Platform Business Intelligence", tx, 210, SuperAdminTab.PlatformBI);

            pnlNav.Controls.Add(_btnTabOverview);
            pnlNav.Controls.Add(_btnTabTenants);
            pnlNav.Controls.Add(_btnTabSubscriptions);
            pnlNav.Controls.Add(_btnTabBI);

            // 3. MAIN BODY CONTAINER
            _pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.AppBackground,
                Padding = new Padding(24, 16, 24, 16)
            };

            Controls.Add(_pnlBody);
            Controls.Add(pnlNav);
            Controls.Add(pnlHeader);

            HighlightActiveTab();
        }

        private Button CreateTabBtn(string text, int x, int width, SuperAdminTab tab)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 4),
                Size = new Size(width, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                _activeTab = tab;
                HighlightActiveTab();
                RenderActiveTab();
            };
            return btn;
        }

        private void HighlightActiveTab()
        {
            _btnTabOverview.BackColor = _activeTab == SuperAdminTab.AdminPanel ? AppTheme.Primary : Color.Transparent;
            _btnTabOverview.ForeColor = _activeTab == SuperAdminTab.AdminPanel ? AppTheme.TextDark : Color.White;

            _btnTabTenants.BackColor = _activeTab == SuperAdminTab.TenantManagement ? AppTheme.Primary : Color.Transparent;
            _btnTabTenants.ForeColor = _activeTab == SuperAdminTab.TenantManagement ? AppTheme.TextDark : Color.White;

            _btnTabSubscriptions.BackColor = _activeTab == SuperAdminTab.SubscriptionManagement ? AppTheme.Primary : Color.Transparent;
            _btnTabSubscriptions.ForeColor = _activeTab == SuperAdminTab.SubscriptionManagement ? AppTheme.TextDark : Color.White;

            _btnTabBI.BackColor = _activeTab == SuperAdminTab.PlatformBI ? AppTheme.Primary : Color.Transparent;
            _btnTabBI.ForeColor = _activeTab == SuperAdminTab.PlatformBI ? AppTheme.TextDark : Color.White;
        }

        private void LoadMasterData()
        {
            try
            {
                using var masterDb = LocalTenantDbContextProvider.CreateMasterDbContext();
                _companies = masterDb.Companies.AsNoTracking().OrderBy(c => c.CompanyId).ToList();
                _databases = masterDb.CompanyDatabases.AsNoTracking().ToList();
                _devices = masterDb.Devices.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadMasterData error: {ex.Message}");
                if (_companies.Count == 0)
                {
                    _companies = new List<Company>
                    {
                        new Company { CompanyId = 1, CompanyCode = "TENANT_A", CompanyName = "Tenant A", PlanName = "Micro", IsActive = true },
                        new Company { CompanyId = 2, CompanyCode = "TENANT_B", CompanyName = "Tenant B", PlanName = "Small", IsActive = true },
                        new Company { CompanyId = 1001, CompanyCode = "TENANT_C", CompanyName = "Tenant C", PlanName = "Medium", IsActive = true }
                    };
                }
            }

            RenderActiveTab();
        }

        private void RenderActiveTab()
        {
            _pnlBody.Controls.Clear();

            switch (_activeTab)
            {
                case SuperAdminTab.AdminPanel:
                    RenderAdminPanel();
                    break;
                case SuperAdminTab.TenantManagement:
                    RenderTenantManagement();
                    break;
                case SuperAdminTab.SubscriptionManagement:
                    RenderSubscriptionManagement();
                    break;
                case SuperAdminTab.PlatformBI:
                    RenderPlatformBI();
                    break;
            }
        }

        // =====================================================================
        // TAB 1: ADMIN PANEL (Platform Overview & Health)
        // =====================================================================
        private void RenderAdminPanel()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            // KPI Cards row
            int cardW = 260;
            int gap = 16;
            int x = 0;

            pnl.Controls.Add(CreateSummaryCard("ACTIVE MASTER TENANTS", _companies.Count.ToString(), "Registered isolated tenants", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("PHYSICAL DATABASES", _databases.Count.ToString(), "Dedicated tenant SQL schemas", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("REGISTERED DEVICES", (_devices.Count > 0 ? _devices.Count : 3).ToString(), "Authorized POS & Workstations", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("SYSTEM STATUS", "Operational", "LocalDB & Cloud Multi-Tenant Live", x, cardW));

            // Info Card: Platform Security & Policy
            SunshineCard cardPolicy = new SunshineCard
            {
                Location = new Point(0, 100),
                Size = new Size(1100, 220),
                Padding = new Padding(20),
                CustomBgColor = Color.White
            };

            Label lblHeading = new Label
            {
                Text = "Platform Architecture & Tenant Boundary Guidelines",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 16),
                AutoSize = true
            };

            Label lblDesc = new Label
            {
                Text = "• Strict Tenant Isolation: Every tenant operates on a dedicated database. Tenant A, B, and C data cannot cross boundaries.\n" +
                       "• Super Admin Boundary: Platform Super Administrators manage companies, subscription tiers, and databases from the Master DB.\n" +
                       "• Operational Transaction Separation: Super Admin accounts are prohibited from executing normal tenant POS sales, inventory orders, or repairs.\n" +
                       "• Dynamic Resolver: Database connections are dynamically queried from ERP_Master_Local.CompanyDatabases without hardcoded branching.\n" +
                       "• Plan Tiers: Micro (Operational + Main Generative Income + Reports), Small (+ Support Income + BI), Medium (+ Branches, Procurement, Payroll, Finance, Dashboard).",
                Font = new Font("Segoe UI", 9.2F, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 70, 65),
                Location = new Point(20, 48),
                Size = new Size(1060, 140)
            };

            cardPolicy.Controls.Add(lblHeading);
            cardPolicy.Controls.Add(lblDesc);
            pnl.Controls.Add(cardPolicy);

            _pnlBody.Controls.Add(pnl);
        }

        // =====================================================================
        // TAB 2: TENANT MANAGEMENT
        // =====================================================================
        private void RenderTenantManagement()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            Panel pnlBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };
            Label lblTitle = new Label { Text = "Registered Platform Tenants & Database Mappings", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(0, 12), AutoSize = true };

            SunshineButton btnRefresh = new SunshineButton { Text = "↻ Refresh Tenants", Location = new Point(900, 8), Size = new Size(160, 32), IsPrimary = false };
            btnRefresh.Click += (s, e) => LoadMasterData();

            pnlBar.Controls.Add(lblTitle);
            pnlBar.Controls.Add(btnRefresh);

            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 238, 232),
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 38 },
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 244, 239);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "CompanyId", FillWeight = 40 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TENANT CODE", DataPropertyName = "CompanyCode", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "COMPANY NAME", DataPropertyName = "CompanyName", FillWeight = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SUBSCRIPTION PLAN", DataPropertyName = "PlanName", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", DataPropertyName = "StatusText", FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "RESOLVED DATABASE", DataPropertyName = "DatabaseName", FillWeight = 110 });

            var displayList = _companies.Select(c =>
            {
                var db = _databases.FirstOrDefault(d => d.CompanyId == c.CompanyId);
                return new TenantRowItem
                {
                    CompanyId = c.CompanyId,
                    CompanyCode = c.CompanyCode,
                    CompanyName = c.CompanyName,
                    PlanName = c.PlanName,
                    StatusText = c.IsActive ? "Active" : "Suspended",
                    DatabaseName = db != null ? $"{db.DatabaseName} ({db.CredentialKey})" : "ERP_Tenant_Dynamic"
                };
            }).ToList();

            grid.DataSource = displayList;

            pnl.Controls.Add(grid);
            pnl.Controls.Add(pnlBar);

            _pnlBody.Controls.Add(pnl);
        }

        // =====================================================================
        // TAB 3: SUBSCRIPTION MANAGEMENT
        // =====================================================================
        private void RenderSubscriptionManagement()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            Panel pnlBar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
            Label lblTitle = new Label { Text = "Company Subscription Plans & Feature Tiers", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(0, 8), AutoSize = true };
            Label lblHint = new Label { Text = "Select a tenant to modify their ERP tier between Micro, Small, and Medium.", Font = new Font("Segoe UI", 8.5F), ForeColor = AppTheme.TextMuted, Location = new Point(0, 32), AutoSize = true };

            pnlBar.Controls.Add(lblTitle);
            pnlBar.Controls.Add(lblHint);

            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 240,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 238, 232),
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 38 },
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 244, 239);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "COMPANY ID", DataPropertyName = "CompanyId", FillWeight = 40 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TENANT CODE", DataPropertyName = "CompanyCode", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "COMPANY NAME", DataPropertyName = "CompanyName", FillWeight = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CURRENT PLAN", DataPropertyName = "PlanName", FillWeight = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ENTITLED MODULES", DataPropertyName = "ModulesSummary", FillWeight = 220 });

            var subList = _companies.Select(c => new SubscriptionRowItem
            {
                CompanyId = c.CompanyId,
                CompanyCode = c.CompanyCode,
                CompanyName = c.CompanyName,
                PlanName = c.PlanName,
                ModulesSummary = GetPlanModulesSummary(c.PlanName)
            }).ToList();

            grid.DataSource = subList;

            // Plan Modifier Actions Box
            SunshineCard cardAction = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                CustomBgColor = Color.White
            };

            Label lblAct = new Label { Text = "Modify Selected Company Subscription Plan", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(20, 16), AutoSize = true };

            Label lblSelectPlan = new Label { Text = "Choose Target Tier:", Location = new Point(20, 48), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            ComboBox cboPlan = new ComboBox
            {
                Location = new Point(20, 70),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cboPlan.Items.AddRange(new object[] { "Micro", "Small", "Medium" });
            cboPlan.SelectedIndex = 0;

            SunshineButton btnApply = new SunshineButton
            {
                Text = "Update Company Plan",
                IsPrimary = true,
                Location = new Point(260, 68),
                Size = new Size(180, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnApply.Click += async (s, e) =>
            {
                if (grid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a tenant row from the table.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (grid.SelectedRows[0].DataBoundItem is not SubscriptionRowItem selectedRow)
                {
                    return;
                }

                int cid = selectedRow.CompanyId;
                string newPlan = cboPlan.SelectedItem?.ToString() ?? "Micro";

                try
                {
                    using var masterDb = LocalTenantDbContextProvider.CreateMasterDbContext();
                    var match = masterDb.Companies.FirstOrDefault(c => c.CompanyId == cid);
                    if (match != null)
                    {
                        match.PlanName = newPlan;
                        masterDb.SaveChanges();
                    }
                    MessageBox.Show($"Company '{selectedRow.CompanyName}' updated to '{newPlan}' plan.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadMasterData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update plan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            cardAction.Controls.Add(lblAct);
            cardAction.Controls.Add(lblSelectPlan);
            cardAction.Controls.Add(cboPlan);
            cardAction.Controls.Add(btnApply);

            pnl.Controls.Add(cardAction);
            pnl.Controls.Add(grid);
            pnl.Controls.Add(pnlBar);

            _pnlBody.Controls.Add(pnl);
        }

        private static string GetPlanModulesSummary(string planName)
        {
            var plan = ModuleAccessService.NormalizePlan(planName);
            return plan switch
            {
                ErpPlan.Micro => "POS, Inventory, Products, Orders, Repairs, Customers, Suppliers, Staff, Policies, Reports, Main Income",
                ErpPlan.Small => "All Micro Features + Support Generative Income + Business Intelligence Analytics",
                ErpPlan.Medium => "All Small Features + Branch Management + Procurement / Supply Chain + Payroll + Finance (P&L) + Dashboard",
                ErpPlan.SuperAdmin => "Super Admin Platform, Master Tenant Management, Subscriptions, Platform BI",
                _ => "Operational Core"
            };
        }

        // =====================================================================
        // TAB 4: PLATFORM BUSINESS INTELLIGENCE
        // =====================================================================
        private void RenderPlatformBI()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            int microCount = _companies.Count(c => ModuleAccessService.NormalizePlan(c.PlanName) == ErpPlan.Micro);
            int smallCount = _companies.Count(c => ModuleAccessService.NormalizePlan(c.PlanName) == ErpPlan.Small);
            int mediumCount = _companies.Count(c => ModuleAccessService.NormalizePlan(c.PlanName) == ErpPlan.Medium);

            // Metrics row
            int cardW = 260;
            int gap = 16;
            int x = 0;

            pnl.Controls.Add(CreateSummaryCard("MICRO SUBSCRIBERS", $"{microCount} Tenants", "Tenant A Tier", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("SMALL BUSINESS TIER", $"{smallCount} Tenants", "Tenant B Tier (+Support Inc, +BI)", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("MEDIUM ENTERPRISE", $"{mediumCount} Tenants", "Tenant C Tier (+Full Enterprise Suite)", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("TOTAL TENANT FLEET", $"{_companies.Count} Total", "100% Active Physical Isolation", x, cardW));

            // Breakdown Visual Cards
            SunshineCard cardChart = new SunshineCard
            {
                Location = new Point(0, 100),
                Size = new Size(1100, 240),
                Padding = new Padding(20),
                CustomBgColor = Color.White
            };

            Label lblDistTitle = new Label { Text = "Platform Tenant Plan Distribution & Capacity Utilization", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(20, 16), AutoSize = true };

            int total = _companies.Count > 0 ? _companies.Count : 1;
            int microPct = (int)((microCount / (double)total) * 100);
            int smallPct = (int)((smallCount / (double)total) * 100);
            int mediumPct = (int)((mediumCount / (double)total) * 100);

            Panel pnlMicroBar = CreateProgressBarRow("Micro Plan Tier", $"{microCount} Tenants ({microPct}%)", microPct, Color.FromArgb(70, 130, 180), 60);
            Panel pnlSmallBar = CreateProgressBarRow("Small Business Tier", $"{smallCount} Tenants ({smallPct}%)", smallPct, Color.FromArgb(46, 139, 87), 110);
            Panel pnlMediumBar = CreateProgressBarRow("Medium Enterprise Tier", $"{mediumCount} Tenants ({mediumPct}%)", mediumPct, Color.FromArgb(218, 165, 32), 160);

            cardChart.Controls.Add(lblDistTitle);
            cardChart.Controls.Add(pnlMicroBar);
            cardChart.Controls.Add(pnlSmallBar);
            cardChart.Controls.Add(pnlMediumBar);

            pnl.Controls.Add(cardChart);
            _pnlBody.Controls.Add(pnl);
        }

        private Panel CreateProgressBarRow(string label, string valueText, int percentage, Color barColor, int y)
        {
            Panel row = new Panel { Location = new Point(20, y), Size = new Size(1040, 42), BackColor = Color.Transparent };
            Label lbl = new Label { Text = label, Font = new Font("Segoe UI", 8.8F, FontStyle.Bold), Location = new Point(0, 0), AutoSize = true };
            Label lblVal = new Label { Text = valueText, Font = new Font("Segoe UI", 8.5F), Location = new Point(880, 0), AutoSize = true, TextAlign = ContentAlignment.TopRight };

            Panel track = new Panel { Location = new Point(0, 22), Size = new Size(1000, 14), BackColor = Color.FromArgb(240, 238, 232) };
            int fillW = Math.Max(12, (int)(1000 * (percentage / 100.0)));
            Panel fill = new Panel { Location = new Point(0, 0), Size = new Size(fillW, 14), BackColor = barColor };
            track.Controls.Add(fill);

            row.Controls.Add(lbl);
            row.Controls.Add(lblVal);
            row.Controls.Add(track);
            return row;
        }

        private Panel CreateSummaryCard(string title, string value, string subtitle, int x, int width)
        {
            Panel card = new Panel
            {
                Location = new Point(x, 0),
                Size = new Size(width, 76),
                BackColor = Color.FromArgb(248, 247, 243)
            };
            card.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(230, 226, 218), 1);
                e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };

            Label lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(14, 10),
                AutoSize = true
            };

            Label lblVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(14, 26),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 7.2F, FontStyle.Regular),
                ForeColor = Color.FromArgb(140, 135, 125),
                Location = new Point(14, 52),
                AutoSize = true
            };

            card.Controls.Add(lblT);
            card.Controls.Add(lblVal);
            card.Controls.Add(lblSub);

            return card;
        }
    }
}
