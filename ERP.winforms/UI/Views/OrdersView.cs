using System;
using System.Data;
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
    public class OrdersView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridOrders = null!;
        private TextBox _txtSearch = null!;
        private Label _lblSummary = null!;

        public OrdersView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            InitializeView();
        }

        private void InitializeView()
        {
            SuspendLayout();
            Controls.Clear();

            Label lblTitle = new Label
            {
                Text = "Sales Transactions & Reports",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 16),
                AutoSize = true
            };

            _lblSummary = new Label
            {
                Text = $"Total Transactions: {_dataService.GetTotalOrders()}  |  Total Sales Revenue: ${_dataService.GetTotalRevenue():N2}",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 48),
                AutoSize = true
            };

            _txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Search by Order ID or Customer Name...",
                Font = AppTheme.BodyFont,
                Location = new Point(20, 80),
                Width = 360
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();

            SunshineButton btnReprint = new SunshineButton
            {
                Text = "🧾 View & Reprint Receipt",
                IsPrimary = true,
                Location = new Point(390, 78),
                Width = 210,
                Height = 36,
                Font = AppTheme.BodyBoldFont
            };
            btnReprint.Click += BtnReprint_Click;

            SunshineButton btnRefresh = new SunshineButton
            {
                Text = "🔄 Refresh Data",
                IsPrimary = false,
                Location = new Point(610, 78),
                Width = 140,
                Height = 36,
                Font = AppTheme.BodyBoldFont
            };
            btnRefresh.Click += (s, e) => RefreshData();

            Controls.Add(lblTitle);
            Controls.Add(_lblSummary);
            Controls.Add(_txtSearch);
            Controls.Add(btnReprint);
            Controls.Add(btnRefresh);

            SunshineCard cardGrid = new SunshineCard
            {
                Location = new Point(20, 126),
                Size = new Size(ClientSize.Width - 40, ClientSize.Height - 146),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12)
            };

            _gridOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.CardBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = AppTheme.BorderColor,
                MultiSelect = false
            };

            _gridOrders.EnableHeadersVisualStyles = false;
            _gridOrders.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
            _gridOrders.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridOrders.ColumnHeadersDefaultCellStyle.Font = AppTheme.BodyBoldFont;
            _gridOrders.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.Primary;
            _gridOrders.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridOrders.ColumnHeadersHeight = 38;

            _gridOrders.DefaultCellStyle.BackColor = AppTheme.CardBackground;
            _gridOrders.DefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridOrders.DefaultCellStyle.Font = AppTheme.BodyFont;
            _gridOrders.DefaultCellStyle.SelectionBackColor = AppTheme.CardHover;
            _gridOrders.DefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridOrders.RowTemplate.Height = 36;

            cardGrid.Controls.Add(_gridOrders);
            Controls.Add(cardGrid);

            RefreshData();

            ResumeLayout(false);
        }

        public void RefreshData()
        {
            _lblSummary.Text = $"Total Transactions: {_dataService.GetTotalOrders()}  |  Total Sales Revenue: ${_dataService.GetTotalRevenue():N2}";
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Order ID", typeof(string));
            dt.Columns.Add("Customer Name", typeof(string));
            dt.Columns.Add("Date & Time", typeof(string));
            dt.Columns.Add("Items Purchased", typeof(int));
            dt.Columns.Add("Payment Method", typeof(string));
            dt.Columns.Add("Total Amount", typeof(string));

            string query = _txtSearch?.Text ?? "";
            var orders = _dataService.Orders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                orders = orders.Where(o => o.Id.Contains(query, StringComparison.OrdinalIgnoreCase) || o.CustomerName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var o in orders)
            {
                dt.Rows.Add(o.Id, o.CustomerName, o.CreatedAt.ToString("g"), o.Items.Count, o.PaymentMethod, $"${o.TotalAmount:N2}");
            }

            _gridOrders.DataSource = dt;
        }

        private void BtnReprint_Click(object? sender, EventArgs e)
        {
            if (_gridOrders.CurrentRow == null) return;

            string orderId = _gridOrders.CurrentRow.Cells[0].Value?.ToString() ?? "";
            var order = _dataService.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                using var dlg = new ReceiptForm(order);
                dlg.ShowDialog();
            }
        }
    }
}
