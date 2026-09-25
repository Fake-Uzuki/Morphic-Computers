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
    public class CustomersView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridCustomers = null!;
        private TextBox _txtSearch = null!;
        private FlowLayoutPanel _flpStatus = null!;
        private string _selectedStatus = "Active";

        public CustomersView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeLayout();
            _dataService.CustomersChanged += () =>
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
            // TOOLBAR (Search, Status Filter, Actions)
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
                PlaceholderText = "Search customer name, phone, address..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlSearch.Controls.Add(_txtSearch);

            // Status filter pills
            _flpStatus = new FlowLayoutPanel
            {
                Location = new Point(290, 11),
                Size = new Size(300, 32),
                WrapContents = false,
                BackColor = Color.Transparent
            };
            string[] statuses = { "Active", "Archived", "All" };
            foreach (var st in statuses)
            {
                Button btnS = new Button
                {
                    Text = st,
                    Size = new Size(78, 28),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    BackColor = st == _selectedStatus ? AppTheme.Primary : Color.White,
                    ForeColor = AppTheme.TextDark,
                    Margin = new Padding(0, 0, 4, 0),
                    Cursor = Cursors.Hand
                };
                btnS.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
                string filterVal = st;
                btnS.Click += (s, e) =>
                {
                    _selectedStatus = filterVal;
                    foreach (Control c in _flpStatus.Controls)
                    {
                        if (c is Button b) b.BackColor = (b.Text == _selectedStatus) ? AppTheme.Primary : Color.White;
                    }
                    ApplyFilters();
                };
                _flpStatus.Controls.Add(btnS);
            }

            SunshineButton btnAddCust = new SunshineButton
            {
                Text = "+ Register Customer",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 180, 8),
                Size = new Size(160, 34),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAddCust.Click += (s, e) =>
            {
                using var dialog = new CustomerEditDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RefreshData();
                }
            };

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(_flpStatus);
            pnlToolbar.Controls.Add(btnAddCust);

            // ========================================================
            // MAIN TABLE CONTAINER
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

            _gridCustomers = new DataGridView
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

            _gridCustomers.EnableHeadersVisualStyles = false;
            _gridCustomers.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridCustomers.ColumnHeadersHeight = 36;
            _gridCustomers.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CODE", FillWeight = 11, MinimumWidth = 75, Name = "ColCode" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CUSTOMER / COMPANY NAME", FillWeight = 22, MinimumWidth = 130, Name = "ColName" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PHONE NUMBER", FillWeight = 14, MinimumWidth = 95, Name = "ColPhone" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "EMAIL ADDRESS", FillWeight = 16, MinimumWidth = 100, Name = "ColEmail" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ADDRESS", FillWeight = 19, MinimumWidth = 120, Name = "ColAddress" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LIFETIME ORDERS", FillWeight = 10, MinimumWidth = 85, Name = "ColOrders" });
            _gridCustomers.Columns.Add(new DataGridViewButtonColumn { HeaderText = "EDIT", FillWeight = 7, MinimumWidth = 60, Text = "Edit", UseColumnTextForButtonValue = true, Name = "ColEdit" });
            _gridCustomers.Columns.Add(new DataGridViewButtonColumn { HeaderText = "ARCHIVE / STATUS", FillWeight = 11, MinimumWidth = 85, Name = "ColArchive" });

            _gridCustomers.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int customerId = Convert.ToInt32(_gridCustomers.Rows[e.RowIndex].Tag);
                    var cust = _dataService.Customers.FirstOrDefault(c => c.CustomerId == customerId);
                    if (cust == null) return;

                    if (_gridCustomers.Columns["ColEdit"] != null && e.ColumnIndex == _gridCustomers.Columns["ColEdit"]!.Index)
                    {
                        using var editDlg = new CustomerEditDialog(cust);
                        if (editDlg.ShowDialog() == DialogResult.OK)
                        {
                            RefreshData();
                        }
                    }
                    else if (_gridCustomers.Columns["ColArchive"] != null && e.ColumnIndex == _gridCustomers.Columns["ColArchive"]!.Index)
                    {
                        string action = cust.IsActive ? "archive / disable" : "restore";
                        var res = MessageBox.Show(
                            $"Are you sure you want to {action} customer profile for '{cust.CustomerName}'?\n\n" +
                            (cust.IsActive ? "• The customer will be marked as archived/inactive." : "• The customer will be reinstated as active."),
                            $"Confirm {(cust.IsActive ? "Archive" : "Restore")}",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (res == DialogResult.Yes)
                        {
                            bool ok = _dataService.ToggleCustomerArchive(customerId);
                            if (!ok)
                            {
                                MessageBox.Show("Failed to change customer status. Please check network/database connectivity.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            RefreshData();
                        }
                    }
                }
            };

            cardGrid.Controls.Add(_gridCustomers);
            pnlMainContainer.Controls.Add(cardGrid);
            cardGrid.BringToFront();

            Controls.Add(pnlMainContainer);
            Controls.Add(pnlToolbar);
        }

        public void RefreshData()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_gridCustomers == null) return;
            string query = _txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _dataService.Customers.AsEnumerable();

            if (_selectedStatus == "Active")
            {
                filtered = filtered.Where(c => c.IsActive);
            }
            else if (_selectedStatus == "Archived")
            {
                filtered = filtered.Where(c => !c.IsActive);
            }

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(c =>
                    c.CustomerName.ToLowerInvariant().Contains(query) ||
                    c.CustomerCode.ToLowerInvariant().Contains(query) ||
                    (c.ContactNumber != null && c.ContactNumber.ToLowerInvariant().Contains(query)) ||
                    (c.Address != null && c.Address.ToLowerInvariant().Contains(query)) ||
                    (c.EmailAddress != null && c.EmailAddress.ToLowerInvariant().Contains(query)));
            }

            _gridCustomers.Rows.Clear();
            foreach (var c in filtered)
            {
                string displayName = c.IsActive ? c.CustomerName : $"[ARCHIVED] {c.CustomerName}";
                int rowIdx = _gridCustomers.Rows.Add(
                    c.CustomerCode,
                    displayName,
                    c.ContactNumber ?? "N/A",
                    c.EmailAddress ?? "N/A",
                    c.Address ?? "N/A",
                    $"{c.TotalOrders} orders (₱{c.TotalSpent:N0})",
                    "Edit",
                    c.IsActive ? "📦 Archive" : "♻️ Restore"
                );
                var row = _gridCustomers.Rows[rowIdx];
                row.Tag = c.CustomerId;

                if (!c.IsActive)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(145, 140, 130);
                    row.Cells["ColName"].Style.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
                    if (_gridCustomers.Columns["ColArchive"] != null)
                    {
                        row.Cells["ColArchive"].Style.ForeColor = Color.FromArgb(27, 122, 79);
                    }
                }
            }
        }
    }
}
