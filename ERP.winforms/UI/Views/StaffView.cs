using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;
using ERP.winforms.UI.Dialogs;

namespace ERP.winforms.UI.Views
{
    public class StaffView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridStaff = null!;
        private TextBox _txtSearch = null!;
        private FlowLayoutPanel _flpFilterPills = null!;

        // Metric Card Labels
        private Label _lblMetricTotal = null!;
        private Label _lblMetricTechs = null!;
        private Label _lblMetricCashiers = null!;
        private Label _lblMetricPayroll = null!;

        private string _currentRoleFilter = "All";

        public StaffView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;

            InitializeLayout();
            _dataService.StaffMembersChanged += () =>
            {
                if (InvokeRequired) Invoke(new Action(RefreshData));
                else RefreshData();
            };

            RefreshData();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // ========================================================
            // 1. TOP METRICS STRIP (Height: 95px)
            // ========================================================
            Panel pnlMetrics = new Panel
            {
                Dock = DockStyle.Top,
                Height = 95,
                Padding = new Padding(20, 14, 20, 10),
                BackColor = Color.Transparent
            };

            int cardW = 280;
            int cardH = 75;
            int gap = 16;
            int x = 20;

            var cardTotal = CreateMetricCard("ACTIVE EMPLOYEES", "0", "Total registered personnel", x, cardW, cardH, out _lblMetricTotal);
            x += cardW + gap;
            var cardTechs = CreateMetricCard("TECHNICAL BENCH", "0", "Diagnostic & hardware technicians", x, cardW, cardH, out _lblMetricTechs);
            x += cardW + gap;
            var cardCashiers = CreateMetricCard("COUNTER / SALES", "0", "Cashiers & counter operators", x, cardW, cardH, out _lblMetricCashiers);
            x += cardW + gap;
            var cardPay = CreateMetricCard("MONTHLY SALARY BASE", "₱0.00", "Base compensation pool", x, cardW, cardH, out _lblMetricPayroll);

            pnlMetrics.Controls.Add(cardTotal);
            pnlMetrics.Controls.Add(cardTechs);
            pnlMetrics.Controls.Add(cardCashiers);
            pnlMetrics.Controls.Add(cardPay);

            // ========================================================
            // 2. TOOLBAR (Search & Actions)
            // ========================================================
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(20, 4, 20, 6),
                BackColor = AppTheme.OperationsBarBg
            };

            Panel pnlSearch = new Panel
            {
                Location = new Point(20, 9),
                Size = new Size(260, 34),
                BackColor = Color.White
            };
            pnlSearch.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };

            _txtSearch = new TextBox
            {
                Location = new Point(8, 7),
                Width = 244,
                BorderStyle = BorderStyle.None,
                Font = AppTheme.BodyFont,
                PlaceholderText = "Search staff, role, email..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlSearch.Controls.Add(_txtSearch);

            // Role Pills
            _flpFilterPills = new FlowLayoutPanel
            {
                Location = new Point(295, 8),
                Size = new Size(580, 36),
                BackColor = Color.Transparent,
                WrapContents = false
            };

            AddFilterPill("All", "All Staff");
            AddFilterPill("Technician", "Technicians");
            AddFilterPill("Manager", "Managers");
            AddFilterPill("Cashier", "Cashiers");
            AddFilterPill("Administrator", "Admins");

            SunshineButton btnAddStaff = new SunshineButton
            {
                Text = "+ Register Staff Member",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 210, 8),
                Size = new Size(190, 34),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAddStaff.Click += (s, e) =>
            {
                using var dialog = new StaffEditDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RefreshData();
                }
            };

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(_flpFilterPills);
            pnlToolbar.Controls.Add(btnAddStaff);

            // ========================================================
            // 3. MAIN TABLE CONTAINER
            // ========================================================
            Panel pnlMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 14),
                BackColor = Color.Transparent
            };

            SunshineCard cardGrid = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            _gridStaff = new DataGridView
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
                RowTemplate = { Height = 44 }
            };

            _gridStaff.EnableHeadersVisualStyles = false;
            _gridStaff.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridStaff.ColumnHeadersHeight = 36;
            _gridStaff.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STAFF ID", FillWeight = 11, Name = "ColCode" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "FULL NAME", FillWeight = 18, Name = "ColName" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "USERNAME", FillWeight = 12, Name = "ColUser" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ROLE", FillWeight = 16, Name = "ColRole" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "POSITION TITLE", FillWeight = 18, Name = "ColPos" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CONTACT NUMBER", FillWeight = 14, Name = "ColPhone" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SALARY / RATE", FillWeight = 13, Name = "ColRate" });
            _gridStaff.Columns.Add(new DataGridViewButtonColumn { HeaderText = "EDIT", FillWeight = 7, Text = "Edit", UseColumnTextForButtonValue = true, Name = "ColEdit" });
            _gridStaff.Columns.Add(new DataGridViewButtonColumn { HeaderText = "REMOVE", FillWeight = 9, Text = "Remove", UseColumnTextForButtonValue = true, Name = "ColDelete" });

            _gridStaff.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int staffId = Convert.ToInt32(_gridStaff.Rows[e.RowIndex].Tag);
                    var staff = _dataService.StaffMembers.FirstOrDefault(s => s.StaffId == staffId);
                    if (staff == null) return;

                    if (e.ColumnIndex == _gridStaff.Columns["ColEdit"]!.Index)
                    {
                        using var editDlg = new StaffEditDialog(staff);
                        if (editDlg.ShowDialog() == DialogResult.OK)
                        {
                            RefreshData();
                        }
                    }
                    else if (e.ColumnIndex == _gridStaff.Columns["ColDelete"]!.Index)
                    {
                        var res = MessageBox.Show(
                            $"Are you sure you want to deactivate staff profile for '{staff.FullName}'?",
                            "Confirm Deactivation",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (res == DialogResult.Yes)
                        {
                            _dataService.DeactivateStaffMember(staffId);
                            RefreshData();
                        }
                    }
                }
            };

            cardGrid.Controls.Add(_gridStaff);
            pnlMainContainer.Controls.Add(cardGrid);

            Controls.Add(pnlMainContainer);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlMetrics);
        }

        private Panel CreateMetricCard(string title, string val, string sub, int x, int w, int h, out Label valLabel)
        {
            Panel card = new Panel
            {
                Location = new Point(x, 10),
                Size = new Size(w, h),
                BackColor = Color.White
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(14, 10),
                AutoSize = true
            };

            valLabel = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(12, 26),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = sub,
                Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                ForeColor = AppTheme.TextSubtle,
                Location = new Point(14, 54),
                AutoSize = true
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(valLabel);
            card.Controls.Add(lblSub);
            return card;
        }

        private void AddFilterPill(string filterKey, string label)
        {
            Button btn = new Button
            {
                Text = label,
                Tag = filterKey,
                Size = new Size(95, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = filterKey == "All" ? AppTheme.TextDark : AppTheme.TextMuted,
                BackColor = filterKey == "All" ? AppTheme.Primary : Color.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Click += (s, e) =>
            {
                _currentRoleFilter = filterKey;
                foreach (Control c in _flpFilterPills.Controls)
                {
                    if (c is Button b)
                    {
                        bool active = b.Tag?.ToString() == _currentRoleFilter;
                        b.BackColor = active ? AppTheme.Primary : Color.Transparent;
                        b.ForeColor = active ? AppTheme.TextDark : AppTheme.TextMuted;
                    }
                }
                ApplyFilters();
            };

            _flpFilterPills.Controls.Add(btn);
        }

        public void RefreshData()
        {
            var all = _dataService.StaffMembers;
            _lblMetricTotal.Text = all.Count.ToString();
            _lblMetricTechs.Text = all.Count(s => s.Role.Contains("Tech")).ToString();
            _lblMetricCashiers.Text = all.Count(s => s.Role.Contains("Cashier")).ToString();
            decimal totalSalary = all.Sum(s => s.MonthlySalary);
            _lblMetricPayroll.Text = $"₱{totalSalary:N0}";

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string query = _txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _dataService.StaffMembers.AsEnumerable();

            if (_currentRoleFilter != "All")
            {
                filtered = filtered.Where(s => s.Role.ToLowerInvariant().Contains(_currentRoleFilter.ToLowerInvariant()));
            }

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(s =>
                    s.FullName.ToLowerInvariant().Contains(query) ||
                    s.Username.ToLowerInvariant().Contains(query) ||
                    s.StaffCode.ToLowerInvariant().Contains(query) ||
                    s.Role.ToLowerInvariant().Contains(query) ||
                    s.PositionTitle.ToLowerInvariant().Contains(query));
            }

            _gridStaff.Rows.Clear();
            foreach (var s in filtered)
            {
                int rowIdx = _gridStaff.Rows.Add(
                    s.StaffCode,
                    s.FullName,
                    s.Username,
                    s.Role,
                    s.PositionTitle,
                    s.PhoneNumber ?? "N/A",
                    $"₱{s.MonthlySalary:N0}/mo (₱{s.HourlyRate:N0}/hr)"
                );
                _gridStaff.Rows[rowIdx].Tag = s.StaffId;
            }
        }
    }
}
