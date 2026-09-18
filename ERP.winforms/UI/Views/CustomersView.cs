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

        // Metric Card Labels
        private Label _lblMetricTotal = null!;
        private Label _lblMetricRepairs = null!;
        private Label _lblMetricSpend = null!;

        public CustomersView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;

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
            // 1. TOP METRICS STRIP (Height: 95px)
            // ========================================================
            Panel pnlMetrics = new Panel
            {
                Dock = DockStyle.Top,
                Height = 95,
                Padding = new Padding(20, 14, 20, 10),
                BackColor = Color.Transparent
            };

            int cardW = 320;
            int cardH = 75;
            int gap = 16;
            int x = 20;

            var cardTotal = CreateMetricCard("REGISTERED CLIENTS", "0", "Retail buyers & corporate accounts", x, cardW, cardH, out _lblMetricTotal);
            x += cardW + gap;
            var cardRepairs = CreateMetricCard("ACTIVE SERVICE ACCOUNTS", "0", "Customers with open repair tickets", x, cardW, cardH, out _lblMetricRepairs);
            x += cardW + gap;
            var cardSpend = CreateMetricCard("LIFETIME CLIENT PURCHASES", "₱0.00", "Combined retail & service spending", x, cardW, cardH, out _lblMetricSpend);

            pnlMetrics.Controls.Add(cardTotal);
            pnlMetrics.Controls.Add(cardRepairs);
            pnlMetrics.Controls.Add(cardSpend);

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
                Size = new Size(320, 34),
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
                Width = 304,
                BorderStyle = BorderStyle.None,
                Font = AppTheme.BodyFont,
                PlaceholderText = "Search customer name, phone, address..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlSearch.Controls.Add(_txtSearch);

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
            pnlToolbar.Controls.Add(btnAddCust);

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

            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CODE", FillWeight = 11, Name = "ColCode" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CUSTOMER / COMPANY NAME", FillWeight = 22, Name = "ColName" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PHONE NUMBER", FillWeight = 14, Name = "ColPhone" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "EMAIL ADDRESS", FillWeight = 16, Name = "ColEmail" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ADDRESS", FillWeight = 19, Name = "ColAddress" });
            _gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LIFETIME ORDERS", FillWeight = 10, Name = "ColOrders" });
            _gridCustomers.Columns.Add(new DataGridViewButtonColumn { HeaderText = "EDIT", FillWeight = 7, Text = "Edit", UseColumnTextForButtonValue = true, Name = "ColEdit" });
            _gridCustomers.Columns.Add(new DataGridViewButtonColumn { HeaderText = "DELETE", FillWeight = 8, Text = "Delete", UseColumnTextForButtonValue = true, Name = "ColDelete" });

            _gridCustomers.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int customerId = Convert.ToInt32(_gridCustomers.Rows[e.RowIndex].Tag);
                    var cust = _dataService.Customers.FirstOrDefault(c => c.CustomerId == customerId);
                    if (cust == null) return;

                    if (e.ColumnIndex == _gridCustomers.Columns["ColEdit"]!.Index)
                    {
                        using var editDlg = new CustomerEditDialog(cust);
                        if (editDlg.ShowDialog() == DialogResult.OK)
                        {
                            RefreshData();
                        }
                    }
                    else if (e.ColumnIndex == _gridCustomers.Columns["ColDelete"]!.Index)
                    {
                        var res = MessageBox.Show(
                            $"Are you sure you want to deactivate customer profile for '{cust.CustomerName}'?",
                            "Confirm Deactivation",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (res == DialogResult.Yes)
                        {
                            _dataService.DeleteCustomer(customerId);
                            RefreshData();
                        }
                    }
                }
            };

            cardGrid.Controls.Add(_gridCustomers);
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

        public void RefreshData()
        {
            var all = _dataService.Customers;
            _lblMetricTotal.Text = all.Count.ToString();

            // Count customers currently in repair bench
            var activeRepairCusts = _dataService.RepairTickets
                .Where(t => t.Status is "Diagnosing" or "InRepair" or "AwaitingParts" or "ReadyForPickup")
                .Select(t => t.CustomerName.ToLowerInvariant())
                .Distinct();

            int repairCount = all.Count(c => activeRepairCusts.Contains(c.CustomerName.ToLowerInvariant()));
            _lblMetricRepairs.Text = repairCount.ToString();

            decimal totalSpent = all.Sum(c => c.TotalSpent);
            _lblMetricSpend.Text = $"₱{totalSpent:N0}";

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string query = _txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _dataService.Customers.AsEnumerable();

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
                int rowIdx = _gridCustomers.Rows.Add(
                    c.CustomerCode,
                    c.CustomerName,
                    c.ContactNumber ?? "N/A",
                    c.EmailAddress ?? "N/A",
                    c.Address ?? "N/A",
                    $"{c.TotalOrders} orders (₱{c.TotalSpent:N0})"
                );
                _gridCustomers.Rows[rowIdx].Tag = c.CustomerId;
            }
        }
    }
}
