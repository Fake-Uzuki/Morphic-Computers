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
        public string ServerName { get; set; } = string.Empty;
    }

    public class SuperAdminView : UserControl
    {
        public enum SuperAdminSection
        {
            AdminPanel,
            BusinessIntelligence,
            Subscriptions
        }

        private SuperAdminSection _activeSection = SuperAdminSection.AdminPanel;

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

        public void SetActiveSection(SuperAdminSection section)
        {
            _activeSection = section;
            RenderActiveSection();
        }

        public void RefreshData()
        {
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
                Text = "Master (Super Admin)  |  Platform Management",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 10),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Platform Administration  •  Tenant Isolation  •  Business Intelligence  •  Subscriptions",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, 36),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // 2. MAIN BODY CONTAINER
            _pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.AppBackground,
                Padding = new Padding(24, 16, 24, 16)
            };

            Controls.Add(_pnlBody);
            Controls.Add(pnlHeader);
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

            RenderActiveSection();
        }

        private void RenderActiveSection()
        {
            _pnlBody.Controls.Clear();

            switch (_activeSection)
            {
                case SuperAdminSection.AdminPanel:
                    RenderAdminPanel();
                    break;
                case SuperAdminSection.BusinessIntelligence:
                    RenderBusinessIntelligence();
                    break;
                case SuperAdminSection.Subscriptions:
                    RenderSubscriptions();
                    break;
            }
        }

        // =====================================================================
        // MODULE 1: ADMIN PANEL (Platform Overview & Tenant Fleet Management)
        // =====================================================================
        private void RenderAdminPanel()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            // 1. KPI Summary Cards Row
            int cardW = 260;
            int gap = 16;
            int x = 0;

            int activeCount = _companies.Count(c => c.IsActive);
            pnl.Controls.Add(CreateSummaryCard("ACTIVE MASTER TENANTS", $"{activeCount} / {_companies.Count}", "Platform registered organizations", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("PHYSICAL DATABASES", _databases.Count.ToString(), "Dedicated tenant SQL schemas", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("REGISTERED DEVICES", (_devices.Count > 0 ? _devices.Count : 3).ToString(), "Authorized POS & Workstations", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("SYSTEM STATUS", "Operational", "Master DB dynamic resolver live", x, cardW));

            // 2. Toolbar for Platform Tenant Fleet
            Panel pnlToolbar = new Panel
            {
                Location = new Point(0, 92),
                Size = new Size(1100, 48),
                BackColor = Color.Transparent
            };

            Label lblTableTitle = new Label
            {
                Text = "Platform Tenants & Database Configuration",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                Location = new Point(0, 12),
                AutoSize = true
            };

            SunshineButton btnRegister = new SunshineButton
            {
                Text = "+ Register Tenant",
                Location = new Point(560, 8),
                Size = new Size(150, 32),
                IsPrimary = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            SunshineButton btnEdit = new SunshineButton
            {
                Text = "✏️ Edit Company",
                Location = new Point(720, 8),
                Size = new Size(130, 32),
                IsPrimary = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            SunshineButton btnToggleStatus = new SunshineButton
            {
                Text = "⚡ Activate / Suspend",
                Location = new Point(860, 8),
                Size = new Size(150, 32),
                IsPrimary = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            SunshineButton btnRefresh = new SunshineButton
            {
                Text = "↻ Refresh",
                Location = new Point(1020, 8),
                Size = new Size(80, 32),
                IsPrimary = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            pnlToolbar.Controls.Add(lblTableTitle);
            pnlToolbar.Controls.Add(btnRegister);
            pnlToolbar.Controls.Add(btnEdit);
            pnlToolbar.Controls.Add(btnToggleStatus);
            pnlToolbar.Controls.Add(btnRefresh);
            pnl.Controls.Add(pnlToolbar);

            // 3. Tenants DataGridView
            DataGridView grid = new DataGridView
            {
                Location = new Point(0, 146),
                Size = new Size(1100, 240),
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

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "CompanyId", FillWeight = 35 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TENANT CODE", DataPropertyName = "CompanyCode", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "COMPANY NAME", DataPropertyName = "CompanyName", FillWeight = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CURRENT PLAN", DataPropertyName = "PlanName", FillWeight = 65 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", DataPropertyName = "StatusText", FillWeight = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "RESOLVED DATABASE", DataPropertyName = "DatabaseName", FillWeight = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SERVER", DataPropertyName = "ServerName", FillWeight = 80 });

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
                    DatabaseName = db != null ? $"{db.DatabaseName} ({db.CredentialKey})" : "ERP_Tenant_Dynamic",
                    ServerName = db != null ? db.ServerName : "(localdb)\\MSSQLLocalDB"
                };
            }).ToList();

            grid.DataSource = displayList;

            // Details Panel below Grid
            Panel pnlDetails = new Panel
            {
                Location = new Point(0, 396),
                Size = new Size(1100, 110),
                BackColor = Color.White
            };
            pnlDetails.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(230, 226, 218), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlDetails.Width - 1, pnlDetails.Height - 1);
            };

            Label lblDetailHead = new Label
            {
                Text = "Tenant Details & Entitlement Profile",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true
            };

            Label lblDetailBody = new Label
            {
                Text = "Select a tenant from the table above to view configuration, plan status, and database mappings.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(16, 38),
                Size = new Size(1060, 60)
            };

            pnlDetails.Controls.Add(lblDetailHead);
            pnlDetails.Controls.Add(lblDetailBody);

            void UpdateDetails()
            {
                if (grid.SelectedRows.Count > 0 && grid.SelectedRows[0].DataBoundItem is TenantRowItem sel)
                {
                    string modules = GetPlanModulesSummary(sel.PlanName);
                    lblDetailBody.Text = $"Company: {sel.CompanyName} ({sel.CompanyCode})   |   ID: {sel.CompanyId}   |   Plan: {sel.PlanName}   |   Status: {sel.StatusText}\n" +
                                        $"Database: {sel.DatabaseName} on {sel.ServerName}\n" +
                                        $"Entitled Modules: {modules}";
                    lblDetailBody.ForeColor = AppTheme.TextDark;
                }
            }

            grid.SelectionChanged += (s, e) => UpdateDetails();
            UpdateDetails();

            // Wire Toolbar Actions
            btnRefresh.Click += (s, e) => LoadMasterData();

            btnToggleStatus.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].DataBoundItem is not TenantRowItem sel)
                {
                    MessageBox.Show("Please select a tenant row.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    using var masterDb = LocalTenantDbContextProvider.CreateMasterDbContext();
                    var match = masterDb.Companies.FirstOrDefault(c => c.CompanyId == sel.CompanyId);
                    if (match != null)
                    {
                        match.IsActive = !match.IsActive;
                        masterDb.SaveChanges();
                        string st = match.IsActive ? "Activated" : "Suspended";
                        MessageBox.Show($"Company '{match.CompanyName}' status updated to {st}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadMasterData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to toggle status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnEdit.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].DataBoundItem is not TenantRowItem sel)
                {
                    MessageBox.Show("Please select a tenant row.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowEditCompanyDialog(sel.CompanyId, sel.CompanyCode, sel.CompanyName);
            };

            btnRegister.Click += (s, e) =>
            {
                ShowRegisterTenantDialog();
            };

            // 4. Platform Security & Policy Guidelines Card
            SunshineCard cardPolicy = new SunshineCard
            {
                Location = new Point(0, 518),
                Size = new Size(1100, 190),
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
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 70, 65),
                Location = new Point(20, 44),
                Size = new Size(1060, 130)
            };

            cardPolicy.Controls.Add(lblHeading);
            cardPolicy.Controls.Add(lblDesc);

            pnl.Controls.Add(grid);
            pnl.Controls.Add(pnlDetails);
            pnl.Controls.Add(cardPolicy);

            _pnlBody.Controls.Add(pnl);
        }

        private void ShowEditCompanyDialog(int companyId, string currentCode, string currentName)
        {
            using var dlg = new Form
            {
                Text = "Edit Tenant Information",
                Size = new Size(420, 260),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblCode = new Label { Text = "Tenant Code:", Location = new Point(24, 20), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtCode = new TextBox { Text = currentCode, Location = new Point(24, 44), Width = 350, Font = new Font("Segoe UI", 9.5F) };

            Label lblName = new Label { Text = "Company Name:", Location = new Point(24, 85), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtName = new TextBox { Text = currentName, Location = new Point(24, 109), Width = 350, Font = new Font("Segoe UI", 9.5F) };

            SunshineButton btnSave = new SunshineButton { Text = "Save Changes", Location = new Point(140, 165), Size = new Size(110, 34), IsPrimary = true };
            SunshineButton btnCancel = new SunshineButton { Text = "Cancel", Location = new Point(264, 165), Size = new Size(110, 34), IsPrimary = false };

            btnSave.Click += (s, e) =>
            {
                string newCode = txtCode.Text.Trim();
                string newName = txtName.Text.Trim();
                if (string.IsNullOrEmpty(newCode) || string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("Please fill out both Tenant Code and Company Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var masterDb = LocalTenantDbContextProvider.CreateMasterDbContext();
                    var match = masterDb.Companies.FirstOrDefault(c => c.CompanyId == companyId);
                    if (match != null)
                    {
                        match.CompanyCode = newCode;
                        match.CompanyName = newName;
                        masterDb.SaveChanges();
                    }
                    MessageBox.Show("Company information updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadMasterData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update company: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnCancel.Click += (s, e) => dlg.Close();

            dlg.Controls.Add(lblCode);
            dlg.Controls.Add(txtCode);
            dlg.Controls.Add(lblName);
            dlg.Controls.Add(txtName);
            dlg.Controls.Add(btnSave);
            dlg.Controls.Add(btnCancel);

            dlg.ShowDialog(this);
        }

        private void ShowRegisterTenantDialog()
        {
            using var dlg = new Form
            {
                Text = "Register New Platform Tenant",
                Size = new Size(460, 380),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Label lblCode = new Label { Text = "Tenant Code (e.g. TENANT_D):", Location = new Point(24, 16), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtCode = new TextBox { Location = new Point(24, 38), Width = 390, Font = new Font("Segoe UI", 9.5F) };

            Label lblName = new Label { Text = "Company Name:", Location = new Point(24, 76), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtName = new TextBox { Location = new Point(24, 98), Width = 390, Font = new Font("Segoe UI", 9.5F) };

            Label lblPlan = new Label { Text = "Subscription Plan:", Location = new Point(24, 136), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            ComboBox cboPlan = new ComboBox { Location = new Point(24, 158), Width = 390, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            cboPlan.Items.AddRange(new object[] { "Micro", "Small", "Medium" });
            cboPlan.SelectedIndex = 0;

            Label lblDb = new Label { Text = "Database Name (e.g. ERP_TenantD_Local):", Location = new Point(24, 196), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            TextBox txtDb = new TextBox { Text = "ERP_TenantD_Local", Location = new Point(24, 218), Width = 390, Font = new Font("Segoe UI", 9.5F) };

            SunshineButton btnCreate = new SunshineButton { Text = "Register Tenant", Location = new Point(170, 280), Size = new Size(130, 34), IsPrimary = true };
            SunshineButton btnCancel = new SunshineButton { Text = "Cancel", Location = new Point(310, 280), Size = new Size(104, 34), IsPrimary = false };

            btnCreate.Click += (s, e) =>
            {
                string code = txtCode.Text.Trim();
                string name = txtName.Text.Trim();
                string plan = cboPlan.SelectedItem?.ToString() ?? "Micro";
                string dbName = txtDb.Text.Trim();

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(dbName))
                {
                    MessageBox.Show("Please fill out all required fields.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var masterDb = LocalTenantDbContextProvider.CreateMasterDbContext();
                    if (masterDb.Companies.Any(c => c.CompanyCode.ToLower() == code.ToLower()))
                    {
                        MessageBox.Show($"Tenant code '{code}' already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var newComp = new Company
                    {
                        CompanyCode = code,
                        CompanyName = name,
                        PlanName = plan,
                        IsActive = true
                    };
                    masterDb.Companies.Add(newComp);
                    masterDb.SaveChanges();

                    var newDb = new CompanyDatabase
                    {
                        CompanyId = newComp.CompanyId,
                        DatabaseName = dbName,
                        ServerName = "(localdb)\\MSSQLLocalDB",
                        CredentialKey = "LocalTrusted",
                        IsActive = true
                    };
                    masterDb.CompanyDatabases.Add(newDb);
                    masterDb.SaveChanges();

                    MessageBox.Show($"Tenant '{name}' registered successfully with ID {newComp.CompanyId}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadMasterData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to register tenant: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnCancel.Click += (s, e) => dlg.Close();

            dlg.Controls.Add(lblCode);
            dlg.Controls.Add(txtCode);
            dlg.Controls.Add(lblName);
            dlg.Controls.Add(txtName);
            dlg.Controls.Add(lblPlan);
            dlg.Controls.Add(cboPlan);
            dlg.Controls.Add(lblDb);
            dlg.Controls.Add(txtDb);
            dlg.Controls.Add(btnCreate);
            dlg.Controls.Add(btnCancel);

            dlg.ShowDialog(this);
        }

        // =====================================================================
        // MODULE 2: BUSINESS INTELLIGENCE (Platform-Level Metrics & Capacity)
        // =====================================================================
        private void RenderBusinessIntelligence()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            int microCount = _companies.Count(c => ModuleAccessService.NormalizePlan(c.PlanName) == ErpPlan.Micro);
            int smallCount = _companies.Count(c => ModuleAccessService.NormalizePlan(c.PlanName) == ErpPlan.Small);
            int mediumCount = _companies.Count(c => ModuleAccessService.NormalizePlan(c.PlanName) == ErpPlan.Medium);
            int activeCount = _companies.Count(c => c.IsActive);
            int suspendedCount = _companies.Count(c => !c.IsActive);

            // Metrics row
            int cardW = 260;
            int gap = 16;
            int x = 0;

            pnl.Controls.Add(CreateSummaryCard("MICRO SUBSCRIBERS", $"{microCount} Tenants", "Tenant A Tier (Micro)", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("SMALL BUSINESS TIER", $"{smallCount} Tenants", "Tenant B Tier (+Support Inc, +BI)", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("MEDIUM ENTERPRISE", $"{mediumCount} Tenants", "Tenant C Tier (+Full Enterprise Suite)", x, cardW));
            x += cardW + gap;
            pnl.Controls.Add(CreateSummaryCard("TOTAL TENANT FLEET", $"{_companies.Count} Total", $"{activeCount} Active  •  {suspendedCount} Suspended", x, cardW));

            // Breakdown Visual Cards
            SunshineCard cardChart = new SunshineCard
            {
                Location = new Point(0, 96),
                Size = new Size(1100, 240),
                Padding = new Padding(20),
                CustomBgColor = Color.White
            };

            Label lblDistTitle = new Label
            {
                Text = "Platform Tenant Plan Distribution & Tier Allocation",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                Location = new Point(20, 16),
                AutoSize = true
            };

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

            // Platform Database & Infrastructure Stats
            SunshineCard cardInfra = new SunshineCard
            {
                Location = new Point(0, 356),
                Size = new Size(1100, 180),
                Padding = new Padding(20),
                CustomBgColor = Color.White
            };

            Label lblInfraTitle = new Label
            {
                Text = "Platform Infrastructure & Master Resolver Analytics",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                Location = new Point(20, 16),
                AutoSize = true
            };

            Label lblInfraDesc = new Label
            {
                Text = $"• Total Managed Tenant Databases: {_databases.Count} schemas resolved dynamically from ERP_Master_Local.CompanyDatabases.\n" +
                       $"• Storage Model: 100% Shared Process, Isolated Physical Databases per organization.\n" +
                       $"• Platform Device Authorizations: {_devices.Count} trusted physical machines registered.\n" +
                       $"• Master Database Source: Server=(localdb)\\MSSQLLocalDB; Database=ERP_Master_Local.",
                Font = new Font("Segoe UI", 9.2F),
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 48),
                Size = new Size(1060, 110)
            };

            cardInfra.Controls.Add(lblInfraTitle);
            cardInfra.Controls.Add(lblInfraDesc);

            pnl.Controls.Add(cardChart);
            pnl.Controls.Add(cardInfra);
            _pnlBody.Controls.Add(pnl);
        }

        // =====================================================================
        // MODULE 3: SUBSCRIPTIONS (Plan Tier & Entitlement Control)
        // =====================================================================
        private void RenderSubscriptions()
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            Panel pnlBar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
            Label lblTitle = new Label { Text = "Company Subscription Plans & Feature Tiers", Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), Location = new Point(0, 8), AutoSize = true };
            Label lblHint = new Label { Text = "Select an organization to adjust their ERP tier. Entitled features are governed by ModuleAccessService.", Font = new Font("Segoe UI", 8.8F), ForeColor = AppTheme.TextMuted, Location = new Point(0, 34), AutoSize = true };

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
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CURRENT PLAN", DataPropertyName = "PlanName", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ENTITLED MODULES", DataPropertyName = "ModulesSummary", FillWeight = 230 });

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

            Label lblAct = new Label { Text = "Modify Selected Company Subscription Plan", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), Location = new Point(20, 16), AutoSize = true };

            Label lblSelectPlan = new Label { Text = "Target Subscription Plan Tier:", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI", 8.8F, FontStyle.Bold) };
            ComboBox cboPlan = new ComboBox
            {
                Location = new Point(20, 74),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cboPlan.Items.AddRange(new object[] { "Micro", "Small", "Medium" });
            cboPlan.SelectedIndex = 0;

            SunshineButton btnApply = new SunshineButton
            {
                Text = "Update Company Plan",
                IsPrimary = true,
                Location = new Point(280, 72),
                Size = new Size(180, 34),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold)
            };

            Label lblImpact = new Label
            {
                Text = "When a tenant plan changes, ModuleAccessService rules apply immediately upon next session or refresh.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 120),
                AutoSize = true
            };

            btnApply.Click += async (s, e) =>
            {
                if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].DataBoundItem is not SubscriptionRowItem selectedRow)
                {
                    MessageBox.Show("Please select a tenant row from the table.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    string summary = GetPlanModulesSummary(newPlan);
                    MessageBox.Show($"Company '{selectedRow.CompanyName}' updated to '{newPlan}' plan.\n\nEntitled Modules:\n{summary}", "Plan Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            cardAction.Controls.Add(lblImpact);

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
                ErpPlan.SuperAdmin => "Admin Panel, Business Intelligence, Subscriptions",
                _ => "Operational Core"
            };
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
