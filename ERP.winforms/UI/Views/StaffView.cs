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

        private string _currentRoleFilter = "All";

        public StaffView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

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
            AddFilterPill("Archived", "📦 Archived");

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

            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STAFF ID", FillWeight = 11, MinimumWidth = 75, Name = "ColCode" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "FULL NAME", FillWeight = 18, MinimumWidth = 120, Name = "ColName" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "USERNAME", FillWeight = 12, MinimumWidth = 85, Name = "ColUser" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ROLE", FillWeight = 16, MinimumWidth = 110, Name = "ColRole" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "POSITION TITLE", FillWeight = 18, MinimumWidth = 120, Name = "ColPos" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CONTACT NUMBER", FillWeight = 14, MinimumWidth = 95, Name = "ColPhone" });
            _gridStaff.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SALARY / RATE", FillWeight = 13, MinimumWidth = 110, Name = "ColRate" });
            _gridStaff.Columns.Add(new DataGridViewButtonColumn { HeaderText = "EDIT", FillWeight = 7, MinimumWidth = 60, Text = "Edit", UseColumnTextForButtonValue = true, Name = "ColEdit" });
            _gridStaff.Columns.Add(new DataGridViewButtonColumn { HeaderText = "ARCHIVE / STATUS", FillWeight = 11, MinimumWidth = 85, Name = "ColArchive" });

            _gridStaff.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int staffId = Convert.ToInt32(_gridStaff.Rows[e.RowIndex].Tag);
                    var staff = _dataService.StaffMembers.FirstOrDefault(st => st.StaffId == staffId);
                    if (staff == null) return;

                    if (_gridStaff.Columns["ColEdit"] != null && e.ColumnIndex == _gridStaff.Columns["ColEdit"]!.Index)
                    {
                        using var editDlg = new StaffEditDialog(staff);
                        if (editDlg.ShowDialog() == DialogResult.OK)
                        {
                            RefreshData();
                        }
                    }
                    else if (_gridStaff.Columns["ColArchive"] != null && e.ColumnIndex == _gridStaff.Columns["ColArchive"]!.Index)
                    {
                        string action = staff.IsActive ? "archive / disable" : "restore";
                        var res = MessageBox.Show(
                            $"Are you sure you want to {action} staff profile for '{staff.FullName}'?\n\n" +
                            (staff.IsActive ? "• The staff account will be marked as archived/inactive." : "• The staff account will be reinstated as active."),
                            $"Confirm {(staff.IsActive ? "Archive" : "Restore")}",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (res == DialogResult.Yes)
                        {
                            _dataService.ToggleStaffArchive(staffId);
                            RefreshData();
                        }
                    }
                }
            };

            cardGrid.Controls.Add(_gridStaff);
            pnlMainContainer.Controls.Add(cardGrid);
            cardGrid.BringToFront();

            Controls.Add(pnlMainContainer);
            Controls.Add(pnlToolbar);
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
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string query = _txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _dataService.StaffMembers.AsEnumerable();

            if (_currentRoleFilter == "Archived")
            {
                filtered = filtered.Where(s => !s.IsActive);
            }
            else if (_currentRoleFilter != "All")
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
                string displayName = s.IsActive ? s.FullName : $"[ARCHIVED] {s.FullName}";
                int rowIdx = _gridStaff.Rows.Add(
                    s.StaffCode,
                    displayName,
                    s.Username,
                    s.Role,
                    s.PositionTitle,
                    s.PhoneNumber ?? "N/A",
                    $"₱{s.MonthlySalary:N0}/mo (₱{s.HourlyRate:N0}/hr)",
                    "Edit",
                    s.IsActive ? "📦 Archive" : "♻️ Restore"
                );
                var row = _gridStaff.Rows[rowIdx];
                row.Tag = s.StaffId;

                if (!s.IsActive)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(145, 140, 130);
                    row.Cells["ColName"].Style.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
                    if (_gridStaff.Columns["ColArchive"] != null)
                    {
                        row.Cells["ColArchive"].Style.ForeColor = Color.FromArgb(27, 122, 79);
                    }
                }
            }
        }
    }
}
