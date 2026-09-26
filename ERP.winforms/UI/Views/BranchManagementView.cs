using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Views
{
    public class BranchManagementView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridBranches = null!;
        private TextBox _txtSearch = null!;
        private Label _lblTotalBranches = null!;
        private Label _lblActiveBranches = null!;
        private Label _lblTotalStaff = null!;
        private Label _lblMainHub = null!;

        public BranchManagementView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeLayout();

            _dataService.BranchesChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (InvokeRequired) BeginInvoke(new Action(RefreshGrid));
                    else RefreshGrid();
                }
            };

            RefreshGrid();
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
                Text = "Medium Enterprise Multi-Store Operations  |  Centralized Network Monitoring",
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
            Panel pnlCard1 = CreateKpiCard("TOTAL BRANCHES", "0", "Store locations", startX, cardW, out _lblTotalBranches);
            startX += cardW + cardGap;

            // KPI 2: Active Locations
            Panel pnlCard2 = CreateKpiCard("OPERATIONAL STATUS", "0 Online", "Network status", startX, cardW, out _lblActiveBranches);
            startX += cardW + cardGap;

            // KPI 3: Assigned Staff
            Panel pnlCard3 = CreateKpiCard("BRANCH STAFF", "0 Staff Members", "Across all locations", startX, cardW, out _lblTotalStaff);
            startX += cardW + cardGap;

            // KPI 4: Primary Hub
            Panel pnlCard4 = CreateKpiCard("PRIMARY HUB", "N/A", "Central Inventory Node", startX, cardW, out _lblMainHub);

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
                Width = 260,
                PlaceholderText = "Search by code, name, city, manager..."
            };
            _txtSearch.TextChanged += (s, e) => RefreshGrid();
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

            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CODE", Name = "ColCode", FillWeight = 50 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BRANCH NAME", Name = "ColName", FillWeight = 120 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CITY / REGION", Name = "ColCity", FillWeight = 65 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PHYSICAL ADDRESS", Name = "ColAddress", FillWeight = 120 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BRANCH MANAGER", Name = "ColManager", FillWeight = 80 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CONTACT PHONE", Name = "ColContact", FillWeight = 70 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STAFF", Name = "ColStaff", FillWeight = 40 });
            _gridBranches.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", Name = "ColStatus", FillWeight = 45 });
            _gridBranches.Columns.Add(new DataGridViewButtonColumn { HeaderText = "EDIT", Name = "ColEdit", FillWeight = 40 });
            _gridBranches.Columns.Add(new DataGridViewButtonColumn { HeaderText = "ACTION", Name = "ColAction", FillWeight = 50 });

            _gridBranches.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int branchId = Convert.ToInt32(_gridBranches.Rows[e.RowIndex].Tag);
                    var branch = _dataService.Branches.FirstOrDefault(b => b.BranchId == branchId);
                    if (branch == null) return;

                    if (_gridBranches.Columns["ColEdit"] != null && e.ColumnIndex == _gridBranches.Columns["ColEdit"]!.Index)
                    {
                        ShowBranchEditDialog(branch);
                    }
                    else if (_gridBranches.Columns["ColAction"] != null && e.ColumnIndex == _gridBranches.Columns["ColAction"]!.Index)
                    {
                        string action = branch.IsActive ? "archive / deactivate" : "restore / activate";
                        var res = MessageBox.Show(
                            $"Are you sure you want to {action} branch '{branch.BranchName}' ({branch.BranchCode})?",
                            $"Confirm {(branch.IsActive ? "Archive" : "Restore")}",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (res == DialogResult.Yes)
                        {
                            bool ok = _dataService.ToggleBranchArchive(branchId);
                            if (!ok)
                            {
                                MessageBox.Show("Failed to update branch status. Please check network/database connectivity.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            RefreshGrid();
                        }
                    }
                }
            };

            _gridBranches.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int branchId = Convert.ToInt32(_gridBranches.Rows[e.RowIndex].Tag);
                    var branch = _dataService.Branches.FirstOrDefault(b => b.BranchId == branchId);
                    if (branch != null)
                    {
                        ShowBranchEditDialog(branch);
                    }
                }
            };

            pnlGridContainer.Controls.Add(_gridBranches);

            Controls.Add(pnlGridContainer);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlKpis);
            Controls.Add(pnlHeader);
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

        public void RefreshGrid()
        {
            if (_gridBranches == null) return;

            string query = _txtSearch?.Text.Trim().ToLowerInvariant() ?? string.Empty;
            var list = _dataService.Branches.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                list = list.Where(b =>
                    b.BranchCode.ToLowerInvariant().Contains(query) ||
                    b.BranchName.ToLowerInvariant().Contains(query) ||
                    b.City.ToLowerInvariant().Contains(query) ||
                    b.Address.ToLowerInvariant().Contains(query) ||
                    b.ManagerName.ToLowerInvariant().Contains(query) ||
                    b.ContactNumber.ToLowerInvariant().Contains(query));
            }

            _gridBranches.Rows.Clear();
            foreach (var b in list.OrderBy(b => b.BranchCode))
            {
                int rIdx = _gridBranches.Rows.Add(
                    b.BranchCode,
                    b.BranchName,
                    b.City,
                    b.Address,
                    string.IsNullOrWhiteSpace(b.ManagerName) ? "Unassigned" : b.ManagerName,
                    string.IsNullOrWhiteSpace(b.ContactNumber) ? "N/A" : b.ContactNumber,
                    b.AssignedStaffCount.ToString(),
                    b.IsActive ? "Active" : "Archived",
                    "✏️ Edit",
                    b.IsActive ? "📁 Archive" : "♻️ Restore"
                );

                var row = _gridBranches.Rows[rIdx];
                row.Tag = b.BranchId;

                if (!b.IsActive)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(145, 140, 130);
                    row.Cells["ColAction"].Style.ForeColor = Color.FromArgb(27, 122, 79);
                }
                else
                {
                    row.Cells["ColAction"].Style.ForeColor = Color.FromArgb(184, 50, 38);
                }
            }

            // Update KPI cards from real SQL data
            int total = _dataService.Branches.Count;
            int active = _dataService.Branches.Count(b => b.IsActive);
            int staff = _dataService.Branches.Sum(b => b.AssignedStaffCount);
            var hub = _dataService.Branches.FirstOrDefault(b => b.IsActive);

            if (_lblTotalBranches != null) _lblTotalBranches.Text = total.ToString();
            if (_lblActiveBranches != null) _lblActiveBranches.Text = $"{active} Online";
            if (_lblTotalStaff != null) _lblTotalStaff.Text = $"{staff} Staff Members";
            if (_lblMainHub != null) _lblMainHub.Text = hub != null ? hub.BranchName : "Central Hub";
        }

        private void BtnAddBranch_Click(object? sender, EventArgs e)
        {
            using var dlg = new Form
            {
                Text = "Add Store Branch Location",
                Size = new Size(460, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            int y = 14;
            Label lblCode = new Label { Text = "Branch Code (leave empty to auto-generate)", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtCode = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "e.g. BR-001" };
            y += 34;

            Label lblName = new Label { Text = "Branch Name *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtName = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "e.g. TechStore Downtown Branch" };
            y += 34;

            Label lblCity = new Label { Text = "City / Region *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtCity = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "e.g. Davao City" };
            y += 34;

            Label lblAddress = new Label { Text = "Address *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtAddress = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "e.g. Unit 101 Bajada Commercial Hub" };
            y += 34;

            Label lblManager = new Label { Text = "Branch Manager", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtManager = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "e.g. Marcus Vance" };
            y += 34;

            Label lblContact = new Label { Text = "Contact Phone", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtContact = new TextBox { Location = new Point(24, y), Width = 230, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "+63 82 299 1234" };

            Label lblStaff = new Label { Text = "Assigned Staff", Location = new Point(270, y - 20), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            NumericUpDown numStaff = new NumericUpDown { Location = new Point(270, y), Width = 144, Font = new Font("Segoe UI", 9.5F), Minimum = 0, Maximum = 500, Value = 4 };
            y += 44;

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

                var branch = new Branch
                {
                    BranchCode = txtCode.Text.Trim(),
                    BranchName = txtName.Text.Trim(),
                    City = txtCity.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    ManagerName = txtManager.Text.Trim(),
                    ContactNumber = txtContact.Text.Trim(),
                    AssignedStaffCount = (int)numStaff.Value,
                    IsActive = true
                };

                bool ok = _dataService.AddBranch(branch);
                if (ok)
                {
                    RefreshGrid();
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                else
                {
                    MessageBox.Show("Failed to save branch. Please check for duplicate branch code or connectivity issues.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(200, y),
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, ev) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            dlg.Controls.Add(lblCode);
            dlg.Controls.Add(txtCode);
            dlg.Controls.Add(lblName);
            dlg.Controls.Add(txtName);
            dlg.Controls.Add(lblCity);
            dlg.Controls.Add(txtCity);
            dlg.Controls.Add(lblAddress);
            dlg.Controls.Add(txtAddress);
            dlg.Controls.Add(lblManager);
            dlg.Controls.Add(txtManager);
            dlg.Controls.Add(lblContact);
            dlg.Controls.Add(txtContact);
            dlg.Controls.Add(lblStaff);
            dlg.Controls.Add(numStaff);
            dlg.Controls.Add(btnSave);
            dlg.Controls.Add(btnCancel);

            dlg.ShowDialog(this);
        }

        private void ShowBranchEditDialog(Branch branch)
        {
            using var dlg = new Form
            {
                Text = $"Edit Branch - {branch.BranchCode}",
                Size = new Size(460, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            int y = 14;
            Label lblCode = new Label { Text = "Branch Code *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtCode = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), Text = branch.BranchCode };
            y += 34;

            Label lblName = new Label { Text = "Branch Name *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtName = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), Text = branch.BranchName };
            y += 34;

            Label lblCity = new Label { Text = "City / Region *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtCity = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), Text = branch.City };
            y += 34;

            Label lblAddress = new Label { Text = "Address *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtAddress = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), Text = branch.Address };
            y += 34;

            Label lblManager = new Label { Text = "Branch Manager", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtManager = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F), Text = branch.ManagerName };
            y += 34;

            Label lblContact = new Label { Text = "Contact Phone", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtContact = new TextBox { Location = new Point(24, y), Width = 230, Font = new Font("Segoe UI", 9.5F), Text = branch.ContactNumber };

            Label lblStaff = new Label { Text = "Assigned Staff", Location = new Point(270, y - 20), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            NumericUpDown numStaff = new NumericUpDown { Location = new Point(270, y), Width = 144, Font = new Font("Segoe UI", 9.5F), Minimum = 0, Maximum = 500, Value = branch.AssignedStaffCount };
            y += 44;

            SunshineButton btnSave = new SunshineButton
            {
                Text = "Update Branch",
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

                branch.BranchCode = txtCode.Text.Trim();
                branch.BranchName = txtName.Text.Trim();
                branch.City = txtCity.Text.Trim();
                branch.Address = txtAddress.Text.Trim();
                branch.ManagerName = txtManager.Text.Trim();
                branch.ContactNumber = txtContact.Text.Trim();
                branch.AssignedStaffCount = (int)numStaff.Value;

                bool ok = _dataService.UpdateBranch(branch);
                if (ok)
                {
                    RefreshGrid();
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                else
                {
                    MessageBox.Show("Failed to update branch. Please check for duplicate branch code or connectivity issues.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(200, y),
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, ev) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            dlg.Controls.Add(lblCode);
            dlg.Controls.Add(txtCode);
            dlg.Controls.Add(lblName);
            dlg.Controls.Add(txtName);
            dlg.Controls.Add(lblCity);
            dlg.Controls.Add(txtCity);
            dlg.Controls.Add(lblAddress);
            dlg.Controls.Add(txtAddress);
            dlg.Controls.Add(lblManager);
            dlg.Controls.Add(txtManager);
            dlg.Controls.Add(lblContact);
            dlg.Controls.Add(txtContact);
            dlg.Controls.Add(lblStaff);
            dlg.Controls.Add(numStaff);
            dlg.Controls.Add(btnSave);
            dlg.Controls.Add(btnCancel);

            dlg.ShowDialog(this);
        }
    }
}
