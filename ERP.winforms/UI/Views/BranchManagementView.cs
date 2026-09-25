using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Views
{
    public class BranchItem
    {
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string CityLocation { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public int AssignedStaffCount { get; set; } = 4;
        public string Status { get; set; } = "Active";
        public DateTime EstablishedDate { get; set; } = DateTime.UtcNow.AddMonths(-12);
    }

    public class BranchManagementView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridBranches = null!;
        private TextBox _txtSearch = null!;
        private Label _lblTotalBranches = null!;
        private Label _lblActiveBranches = null!;
        private Label _lblTotalStaff = null!;
        private Label _lblMainHub = null!;

        private readonly List<BranchItem> _branches = new();

        public BranchManagementView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeDefaultBranches();
            InitializeLayout();
        }

        private void InitializeDefaultBranches()
        {
            _branches.Clear();
            _branches.Add(new BranchItem
            {
                BranchCode = "BR-001",
                BranchName = "TechStore Main Hub (Flagship)",
                CityLocation = "Cebu City",
                Address = "Unit 402 IT Park Hub, Lahug, Cebu City",
                ManagerName = "Marcus V. (Manager)",
                ContactNumber = "+63 32 238 9012",
                AssignedStaffCount = 8,
                Status = "Active",
                EstablishedDate = DateTime.UtcNow.AddMonths(-24)
            });
            _branches.Add(new BranchItem
            {
                BranchCode = "BR-002",
                BranchName = "TechStore Metro Hub",
                CityLocation = "Mandaue City",
                Address = "GF City Galleria, Subangdaku, Mandaue City",
                ManagerName = "Sarah L. (Asst. Manager)",
                ContactNumber = "+63 32 344 8821",
                AssignedStaffCount = 5,
                Status = "Active",
                EstablishedDate = DateTime.UtcNow.AddMonths(-14)
            });
            _branches.Add(new BranchItem
            {
                BranchCode = "BR-003",
                BranchName = "TechStore Express Depot",
                CityLocation = "Lapu-Lapu City",
                Address = "Stall 14 Island Mall, Basak, Lapu-Lapu City",
                ManagerName = "Alex R. (Hardware Lead)",
                ContactNumber = "+63 32 495 1102",
                AssignedStaffCount = 3,
                Status = "Active",
                EstablishedDate = DateTime.UtcNow.AddMonths(-6)
            });
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
                Text = "Branch & Location Management",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 10),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Medium Enterprise Multi-Store Operations  |  Centralized Network Monitoring (Scaffolded / Local Preview)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, 36),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // 2. KPI METRICS CARDS PANEL
            Panel pnlKpis = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(24, 10, 24, 10),
                BackColor = Color.White
            };

            int cardW = 240;
            int cardGap = 16;
            int startX = 24;

            // KPI 1: Total Locations
            Panel pnlCard1 = CreateKpiCard("TOTAL BRANCHES", _branches.Count.ToString(), "Active store locations", startX, cardW, out _lblTotalBranches);
            startX += cardW + cardGap;

            // KPI 2: Active Locations
            Panel pnlCard2 = CreateKpiCard("OPERATIONAL STATUS", $"{_branches.Count(b => b.Status == "Active")} Online", "100% Network uptime", startX, cardW, out _lblActiveBranches);
            startX += cardW + cardGap;

            // KPI 3: Assigned Staff
            Panel pnlCard3 = CreateKpiCard("BRANCH STAFF", $"{_branches.Sum(b => b.AssignedStaffCount)} Staff Members", "Across all active locations", startX, cardW, out _lblTotalStaff);
            startX += cardW + cardGap;

            // KPI 4: Primary Hub
            Panel pnlCard4 = CreateKpiCard("PRIMARY HUB", "Cebu City Flagship", "Central Inventory Node", startX, cardW, out _lblMainHub);

            pnlKpis.Controls.Add(pnlCard1);
            pnlKpis.Controls.Add(pnlCard2);
            pnlKpis.Controls.Add(pnlCard3);
            pnlKpis.Controls.Add(pnlCard4);

            // 3. TOOLBAR (Search & Actions)
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(24, 8, 24, 8),
                BackColor = AppTheme.OperationsBarBg
            };

            Panel pnlSearch = new Panel
            {
                Location = new Point(24, 9),
                Size = new Size(280, 34),
                BackColor = Color.White
            };
            pnlSearch.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(220, 215, 205), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };

            _txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                Location = new Point(10, 8),
                Width = 260
            };
            _txtSearch.TextChanged += (s, e) => FilterBranches();
            pnlSearch.Controls.Add(_txtSearch);

            SunshineButton btnAddBranch = new SunshineButton
            {
                Text = "+ Add Branch Location",
                IsPrimary = true,
                Location = new Point(320, 9),
                Size = new Size(180, 34),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold)
            };
            btnAddBranch.Click += BtnAddBranch_Click;

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(btnAddBranch);

            // 4. MAIN DATA GRID
            Panel pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 12, 24, 16),
                BackColor = AppTheme.AppBackground
            };

            _gridBranches = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 238, 232),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 40 },
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _gridBranches.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 244, 239);
            _gridBranches.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridBranches.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _gridBranches.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            _gridBranches.DefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridBranches.DefaultCellStyle.SelectionBackColor = Color.FromArgb(244, 234, 185);
            _gridBranches.DefaultCellStyle.SelectionForeColor = AppTheme.TextDark;

            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CODE", DataPropertyName = "BranchCode", FillWeight = 50 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BRANCH NAME", DataPropertyName = "BranchName", FillWeight = 120 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CITY / REGION", DataPropertyName = "CityLocation", FillWeight = 60 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PHYSICAL ADDRESS", DataPropertyName = "Address", FillWeight = 130 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BRANCH MANAGER", DataPropertyName = "ManagerName", FillWeight = 80 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CONTACT PHONE", DataPropertyName = "ContactNumber", FillWeight = 70 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STAFF", DataPropertyName = "AssignedStaffCount", FillWeight = 40 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", DataPropertyName = "Status", FillWeight = 45 });

            pnlGridContainer.Controls.Add(_gridBranches);

            Controls.Add(pnlGridContainer);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlKpis);
            Controls.Add(pnlHeader);

            RefreshGrid();
        }

        private Panel CreateKpiCard(string title, string value, string subtitle, int x, int width, out Label lblVal)
        {
            Panel card = new Panel
            {
                Location = new Point(x, 8),
                Size = new Size(width, 68),
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
                Location = new Point(12, 8),
                AutoSize = true
            };

            lblVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(12, 24),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 7.2F, FontStyle.Regular),
                ForeColor = Color.FromArgb(140, 135, 125),
                Location = new Point(12, 48),
                AutoSize = true
            };

            card.Controls.Add(lblT);
            card.Controls.Add(lblVal);
            card.Controls.Add(lblSub);

            return card;
        }

        private void FilterBranches()
        {
            string query = _txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                _gridBranches.DataSource = _branches.ToList();
            }
            else
            {
                _gridBranches.DataSource = _branches
                    .Where(b => b.BranchCode.ToLower().Contains(query) ||
                                b.BranchName.ToLower().Contains(query) ||
                                b.CityLocation.ToLower().Contains(query) ||
                                b.ManagerName.ToLower().Contains(query))
                    .ToList();
            }
        }

        private void RefreshGrid()
        {
            _gridBranches.DataSource = null;
            _gridBranches.DataSource = _branches.ToList();

            if (_lblTotalBranches != null) _lblTotalBranches.Text = _branches.Count.ToString();
            if (_lblActiveBranches != null) _lblActiveBranches.Text = $"{_branches.Count(b => b.Status == "Active")} Online";
            if (_lblTotalStaff != null) _lblTotalStaff.Text = $"{_branches.Sum(b => b.AssignedStaffCount)} Staff Members";
        }

        private void BtnAddBranch_Click(object? sender, EventArgs e)
        {
            using var dlg = new Form
            {
                Text = "Add Store Branch Location",
                Size = new Size(420, 380),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            int y = 16;
            Label lblName = new Label { Text = "Branch Name *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtName = new TextBox { Location = new Point(24, y), Width = 350, Font = new Font("Segoe UI", 9.5F) };
            y += 34;

            Label lblCity = new Label { Text = "City / Region *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtCity = new TextBox { Location = new Point(24, y), Width = 350, Font = new Font("Segoe UI", 9.5F) };
            y += 34;

            Label lblAddress = new Label { Text = "Address *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtAddress = new TextBox { Location = new Point(24, y), Width = 350, Font = new Font("Segoe UI", 9.5F) };
            y += 34;

            Label lblManager = new Label { Text = "Branch Manager *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtManager = new TextBox { Location = new Point(24, y), Width = 350, Font = new Font("Segoe UI", 9.5F) };
            y += 40;

            SunshineButton btnSave = new SunshineButton
            {
                Text = "Save Branch",
                IsPrimary = true,
                Location = new Point(24, y),
                Size = new Size(160, 36)
            };
            btnSave.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCity.Text))
                {
                    MessageBox.Show("Please provide branch name and city.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _branches.Add(new BranchItem
                {
                    BranchCode = $"BR-{_branches.Count + 1:D3}",
                    BranchName = txtName.Text.Trim(),
                    CityLocation = txtCity.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    ManagerName = txtManager.Text.Trim(),
                    ContactNumber = "+63 32 800 0000",
                    AssignedStaffCount = 4,
                    Status = "Active",
                    EstablishedDate = DateTime.UtcNow
                });
                RefreshGrid();
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.Add(lblName);
            dlg.Controls.Add(txtName);
            dlg.Controls.Add(lblCity);
            dlg.Controls.Add(txtCity);
            dlg.Controls.Add(lblAddress);
            dlg.Controls.Add(txtAddress);
            dlg.Controls.Add(lblManager);
            dlg.Controls.Add(txtManager);
            dlg.Controls.Add(btnSave);

            dlg.ShowDialog(this);
        }
    }
}
