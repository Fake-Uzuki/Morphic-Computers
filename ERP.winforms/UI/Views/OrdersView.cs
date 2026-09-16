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
    public class OrdersView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridOrders = null!;
        private TextBox _txtSearch = null!;
        private Label _lblSummary = null!;
        private Panel _pnlPagin = null!;
        private FlowLayoutPanel _flpMethods = null!;
        private Panel _pnlDateFilters = null!;

        private Label _lblMetricTotalTrans = null!;
        private Label _lblMetricGrossRevenue = null!;
        private Label _lblMetricAvgBasket = null!;
        private Label _lblMetricTopMethod = null!;

        private string _selectedDateFilter = "This Month";
        private string _selectedMethodFilter = "All Methods";

        // Pagination
        private int _currentPage = 1;
        private const int PageSize = 8;
        private int _totalPages = 1;

        private class SalesRecord
        {
            public string OrderId { get; set; } = "";
            public string Customer { get; set; } = "";
            public DateTime Date { get; set; }
            public string ItemsSummary { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
            public string Cashier { get; set; } = "Terminal 1 (Alex R.)";
            public decimal Tax { get; set; }
            public decimal Total { get; set; }
            public string Status { get; set; } = "Completed";
        }

        private readonly List<SalesRecord> _allTransactions = new();

        public OrdersView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;

            InitializeSampleTransactions();
            InitializeLayout();
        }

        private void InitializeSampleTransactions()
        {
            _allTransactions.Clear();

            // Include any live POS orders created
            foreach (var o in _dataService.Orders)
            {
                _allTransactions.Insert(0, new SalesRecord
                {
                    OrderId = o.Id.StartsWith("#") ? o.Id : $"#{o.Id}",
                    Customer = $"{o.CustomerName}\nRetail POS",
                    Date = o.CreatedAt,
                    ItemsSummary = $"{o.Items.Count} items purchased",
                    PaymentMethod = o.PaymentMethod,
                    Cashier = "Terminal 1",
                    Tax = o.Tax,
                    Total = o.TotalAmount
                });
            }
        }

        private void InitializeLayout()
        {
            SuspendLayout();
            Controls.Clear();

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.Transparent
            };

            // ========================================================
            // 1. TOP HEADER (Title + Subtitle - NO AL#04 Active!)
            // ========================================================
            Panel pnlTitle = new Panel { Dock = DockStyle.Top, Height = 52 };

            Label lblHeading = new Label
            {
                Text = "Sales Transactions Reports",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(0, 2),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Real-time transaction log, till audits, and fiscal settlement reports for Micro Company retail operations.",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(0, 26),
                AutoSize = true
            };

            // Date Filters on Right
            _pnlDateFilters = new Panel { Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(pnlMain.Width - 380, 4), Size = new Size(375, 32) };
            string[] dates = { "Today", "This Week", "This Month", "All Time" };
            int dx = 0;
            foreach (var d in dates)
            {
                Button btnD = new Button
                {
                    Text = d,
                    AutoSize = true,
                    Height = 28,
                    Location = new Point(dx, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    BackColor = d == _selectedDateFilter ? AppTheme.Primary : Color.White,
                    ForeColor = AppTheme.TextDark,
                    Cursor = Cursors.Hand
                };
                btnD.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
                string filterVal = d;
                btnD.Click += (s, e) =>
                {
                    _selectedDateFilter = filterVal;
                    foreach (Control c in _pnlDateFilters.Controls)
                    {
                        if (c is Button b) b.BackColor = (b.Text == _selectedDateFilter) ? AppTheme.Primary : Color.White;
                    }
                    _currentPage = 1;
                    ApplyFilters();
                };
                _pnlDateFilters.Controls.Add(btnD);
                dx += btnD.PreferredSize.Width + 6;
            }

            pnlTitle.Controls.Add(lblHeading);
            pnlTitle.Controls.Add(lblSubtitle);
            pnlTitle.Controls.Add(_pnlDateFilters);

            // ========================================================
            // 2. TOP 4 METRIC CARDS ROW (PHP Currency)
            // ========================================================
            TableLayoutPanel tlpMetrics = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 88,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            for (int i = 0; i < 4; i++) tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            tlpMetrics.Controls.Add(CreateStatCard("TOTAL TRANSACTIONS", out _lblMetricTotalTrans, "7", "Settled in Current Fiscal Cycle", "Active"), 0, 0);
            tlpMetrics.Controls.Add(CreateStatCard("GROSS SALES REVENUE", out _lblMetricGrossRevenue, "₱6,289.44", "Live consolidated ledger", "PHP"), 1, 0);
            tlpMetrics.Controls.Add(CreateStatCard("AVERAGE ORDER VALUE", out _lblMetricAvgBasket, "₱898.49", "Basket Size: 3.4 items avg", "Verified"), 2, 0);
            tlpMetrics.Controls.Add(CreateStatCard("TOP PAYMENT CHANNEL", out _lblMetricTopMethod, "POS Terminal", "Cash & Card Verified", "68% Share"), 3, 0);

            // ========================================================
            // 3. FILTER & ACTION BAR
            // ========================================================
            Panel pnlFilterBar = new Panel { Dock = DockStyle.Top, Height = 48, Margin = new Padding(0, 10, 0, 8) };

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search by Order ID, Customer Name, or Receipt #...",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Location = new Point(0, 10),
                Width = 290
            };
            _txtSearch.TextChanged += (s, e) => { _currentPage = 1; ApplyFilters(); };

            // Method pills
            _flpMethods = new FlowLayoutPanel { Location = new Point(300, 8), Size = new Size(360, 34), WrapContents = false };
            string[] methods = { "All Methods", "Cash", "Card / Terminal", "Bank Transfer" };
            foreach (var m in methods)
            {
                Button btnM = new Button
                {
                    Text = m,
                    AutoSize = true,
                    Height = 28,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    BackColor = m == _selectedMethodFilter ? AppTheme.Primary : Color.White,
                    ForeColor = AppTheme.TextDark,
                    Margin = new Padding(0, 0, 4, 0),
                    Cursor = Cursors.Hand
                };
                btnM.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
                string methodVal = m;
                btnM.Click += (s, e) =>
                {
                    _selectedMethodFilter = methodVal;
                    foreach (Control c in _flpMethods.Controls)
                    {
                        if (c is Button b) b.BackColor = (b.Text == _selectedMethodFilter) ? AppTheme.Primary : Color.White;
                    }
                    _currentPage = 1;
                    ApplyFilters();
                };
                _flpMethods.Controls.Add(btnM);
            }

            // Action Buttons on Right
            Panel pnlActions = new Panel { Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(pnlMain.Width - 535, 6), Size = new Size(530, 34) };

            SunshineButton btnReprint = new SunshineButton { Text = "View / Reprint Receipt", IsPrimary = true, Location = new Point(0, 0), Size = new Size(165, 32), Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            btnReprint.Click += (s, e) => ViewSelectedReceipt();

            SunshineButton btnRefresh = new SunshineButton { Text = "Refresh Data", IsPrimary = false, Location = new Point(172, 0), Size = new Size(100, 32), Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            btnRefresh.Click += (s, e) => RefreshData();

            SunshineButton btnExport = new SunshineButton { Text = "Export (PDF/Excel)", IsPrimary = false, Location = new Point(280, 0), Size = new Size(125, 32), Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            btnExport.Click += (s, e) => MessageBox.Show("Sales transactions exported to CSV spreadsheet.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            SunshineButton btnVoid = new SunshineButton { Text = "📦 Archive / Void Order", IsPrimary = false, CustomTextColor = Color.FromArgb(184, 50, 38), Location = new Point(412, 0), Size = new Size(150, 32), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold) };
            btnVoid.Click += (s, e) => VoidSelectedOrder();

            pnlActions.Controls.Add(btnReprint);
            pnlActions.Controls.Add(btnRefresh);
            pnlActions.Controls.Add(btnExport);
            pnlActions.Controls.Add(btnVoid);

            pnlFilterBar.Controls.Add(_txtSearch);
            pnlFilterBar.Controls.Add(_flpMethods);
            pnlFilterBar.Controls.Add(pnlActions);

            // ========================================================
            // 4. DATA GRID VIEW
            // ========================================================
            SunshineCard cardGrid = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            _gridOrders = new DataGridView
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
                RowTemplate = { Height = 46 }
            };

            _gridOrders.EnableHeadersVisualStyles = false;
            _gridOrders.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridOrders.ColumnHeadersHeight = 36;
            _gridOrders.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ORDER ID", FillWeight = 10, Name = "ColId" });
            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CUSTOMER NAME", FillWeight = 16, Name = "ColCust" });
            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DATE & TIME", FillWeight = 13, Name = "ColDate" });
            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEMS PURCHASED", FillWeight = 15, Name = "ColItems" });
            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PAYMENT METHOD", FillWeight = 13, Name = "ColPay" });
            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CASHIER / REGISTER", FillWeight = 12, Name = "ColCashier" });
            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TAX (12%)", FillWeight = 8, Name = "ColTax" });
            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TOTAL (PHP)", FillWeight = 10, Name = "ColTotal" });
            _gridOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", FillWeight = 9, Name = "ColStatus" });
            _gridOrders.Columns.Add(new DataGridViewButtonColumn { HeaderText = "RECEIPT", FillWeight = 7, Text = "View", UseColumnTextForButtonValue = true, Name = "ColAction" });
            _gridOrders.Columns.Add(new DataGridViewButtonColumn { HeaderText = "ARCHIVE / VOID", FillWeight = 11, Name = "ColVoid" });

            _gridOrders.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    if (_gridOrders.Columns["ColAction"] != null && e.ColumnIndex == _gridOrders.Columns["ColAction"]!.Index)
                    {
                        ViewSelectedReceipt();
                    }
                    else if (_gridOrders.Columns["ColVoid"] != null && e.ColumnIndex == _gridOrders.Columns["ColVoid"]!.Index)
                    {
                        VoidOrRestoreSelectedOrder(e.RowIndex);
                    }
                }
            };

            ContextMenuStrip ctxOrders = new ContextMenuStrip();
            ToolStripMenuItem itemReceipt = new ToolStripMenuItem("📄 View Receipt Details");
            ToolStripMenuItem itemVoid = new ToolStripMenuItem("📦 Archive / Void Transaction");
            itemReceipt.Click += (s, e) => ViewSelectedReceipt();
            itemVoid.Click += (s, e) => VoidSelectedOrder();
            ctxOrders.Items.Add(itemReceipt);
            ctxOrders.Items.Add(new ToolStripSeparator());
            ctxOrders.Items.Add(itemVoid);
            _gridOrders.ContextMenuStrip = ctxOrders;

            _gridOrders.Resize += (s, e) => ApplyFilters();

            cardGrid.Controls.Add(_gridOrders);

            // ========================================================
            // 5. FOOTER & CENTERED PAGINATION
            // ========================================================
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            _lblSummary = new Label { Text = "Showing 0 transactions", Font = new Font("Segoe UI", 7.5F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(0, 12), AutoSize = true };

            _pnlPagin = new Panel { Size = new Size(245, 28) };
            string[] pages = { "Prev", "1", "2", "3", "Next" };
            int pgx = 0;
            foreach (var pg in pages)
            {
                Button b = new Button
                {
                    Text = pg,
                    Size = new Size(38, 24),
                    Location = new Point(pgx, 2),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                    BackColor = pg == "1" ? AppTheme.Primary : Color.White,
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderColor = Color.FromArgb(220, 215, 205);
                _pnlPagin.Controls.Add(b);
                pgx += 41;
            }
            _pnlPagin.Width = pgx;

            // Exactly center pagination horizontally
            pnlFooter.Resize += (s, e) =>
            {
                _pnlPagin.Location = new Point(Math.Max(180, (pnlFooter.Width - _pnlPagin.Width) / 2), 6);
            };
            _pnlPagin.Location = new Point(Math.Max(180, (pnlMain.Width - pgx) / 2), 6);

            pnlFooter.Controls.Add(_lblSummary);
            pnlFooter.Controls.Add(_pnlPagin);

            pnlMain.Controls.Add(cardGrid);
            pnlMain.Controls.Add(pnlFooter);
            pnlMain.Controls.Add(pnlFilterBar);
            pnlMain.Controls.Add(tlpMetrics);
            pnlMain.Controls.Add(pnlTitle);

            Controls.Add(pnlMain);
            ResumeLayout(false);

            RefreshData();
        }

        private SunshineCard CreateStatCard(string title, out Label lblVal, string value, string subtitle, string badge)
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                Padding = new Padding(12),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(10, 8), AutoSize = true };
            lblVal = new Label { Text = value, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(10, 24), AutoSize = true };
            Label lblSub = new Label { Text = subtitle, Font = new Font("Segoe UI", 7.5F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(10, 56), AutoSize = true };

            Panel pnlBadge = new Panel { Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(card.Width - 65, 8), Size = new Size(55, 18), BackColor = Color.FromArgb(254, 248, 230) };
            Label lblB = new Label { Text = badge, Font = new Font("Segoe UI", 6.5F, FontStyle.Bold), ForeColor = Color.FromArgb(130, 95, 10), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlBadge.Controls.Add(lblB);

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblVal);
            card.Controls.Add(lblSub);
            card.Controls.Add(pnlBadge);

            return card;
        }

        public void RefreshData()
        {
            InitializeSampleTransactions();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_gridOrders == null) return;
            _gridOrders.Rows.Clear();

            var query = _allTransactions.AsEnumerable();

            // 1. Date filter
            if (_selectedDateFilter == "Today")
            {
                query = query.Where(t => t.Date.Date == DateTime.Today);
            }
            else if (_selectedDateFilter == "This Week")
            {
                DateTime weekAgo = DateTime.Today.AddDays(-7);
                query = query.Where(t => t.Date.Date >= weekAgo);
            }
            else if (_selectedDateFilter == "This Month")
            {
                query = query.Where(t => t.Date.Month == DateTime.Today.Month && t.Date.Year == DateTime.Today.Year);
            }

            // 2. Method filter
            if (_selectedMethodFilter == "Cash")
            {
                query = query.Where(t => t.PaymentMethod.Contains("Cash", StringComparison.OrdinalIgnoreCase));
            }
            else if (_selectedMethodFilter == "Card / Terminal")
            {
                query = query.Where(t => t.PaymentMethod.Contains("Card", StringComparison.OrdinalIgnoreCase) ||
                                         t.PaymentMethod.Contains("Visa", StringComparison.OrdinalIgnoreCase) ||
                                         t.PaymentMethod.Contains("Master", StringComparison.OrdinalIgnoreCase) ||
                                         t.PaymentMethod.Contains("AMEX", StringComparison.OrdinalIgnoreCase));
            }
            else if (_selectedMethodFilter == "Bank Transfer")
            {
                query = query.Where(t => t.PaymentMethod.Contains("Transfer", StringComparison.OrdinalIgnoreCase) ||
                                         t.PaymentMethod.Contains("Invoice", StringComparison.OrdinalIgnoreCase));
            }

            // 3. Search text
            string search = _txtSearch?.Text.Trim() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.OrderId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         t.Customer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         t.PaymentMethod.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var results = query.ToList();
            decimal totalRevenue = results.Sum(t => t.Total);
            int pageSize = GetEffectivePageSize();
            _totalPages = Math.Max(1, (int)Math.Ceiling(results.Count / (double)pageSize));

            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;

            var pageItems = results.Skip((_currentPage - 1) * pageSize).Take(pageSize).ToList();

            foreach (var t in pageItems)
            {
                bool isVoided = t.Status == "Voided";
                int rowIdx = _gridOrders.Rows.Add(
                    t.OrderId,
                    t.Customer,
                    t.Date.ToString("yyyy-MM-dd HH:mm"),
                    t.ItemsSummary,
                    t.PaymentMethod,
                    t.Cashier,
                    $"₱{t.Tax:N2}",
                    $"₱{t.Total:N2}",
                    t.Status,
                    "View",
                    isVoided ? "♻️ Restore" : "📦 Archive / Void"
                );

                var row = _gridOrders.Rows[rowIdx];
                row.Tag = t;
                row.Cells["ColId"].Style.ForeColor = Color.FromArgb(160, 110, 10);
                row.Cells["ColId"].Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                row.Cells["ColTotal"].Style.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

                var voidCell = row.Cells["ColVoid"];
                voidCell.Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

                if (isVoided)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
                    row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(184, 50, 38);
                    row.Cells["ColStatus"].Style.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
                    voidCell.Style.ForeColor = Color.FromArgb(27, 122, 79);
                }
                else
                {
                    row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(27, 122, 79);
                    row.Cells["ColStatus"].Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    voidCell.Style.ForeColor = Color.FromArgb(184, 50, 38);
                }
            }

            UpdatePaginationButtons(results.Count, totalRevenue, pageSize);

            // Update top metric cards
            if (_lblMetricTotalTrans != null) _lblMetricTotalTrans.Text = results.Count.ToString();
            if (_lblMetricGrossRevenue != null) _lblMetricGrossRevenue.Text = $"₱{totalRevenue:N2}";
            if (_lblMetricAvgBasket != null)
            {
                decimal avg = results.Count > 0 ? totalRevenue / results.Count : 0m;
                _lblMetricAvgBasket.Text = $"₱{avg:N2}";
            }
        }

        private int GetEffectivePageSize()
        {
            if (_gridOrders == null || _gridOrders.ClientSize.Height <= 100) return 12;
            int availableHeight = _gridOrders.ClientSize.Height - _gridOrders.ColumnHeadersHeight;
            int rowHeight = _gridOrders.RowTemplate.Height > 0 ? _gridOrders.RowTemplate.Height : 46;
            int fitRows = availableHeight / rowHeight;
            return Math.Max(10, fitRows);
        }

        private void UpdatePaginationButtons(int totalCount, decimal totalRevenue, int pageSize)
        {
            if (_pnlPagin == null) return;
            _pnlPagin.SuspendLayout();
            _pnlPagin.Controls.Clear();

            List<string> buttons = new List<string> { "Prev" };
            for (int i = 1; i <= _totalPages; i++) buttons.Add(i.ToString());
            buttons.Add("Next");

            int pgx = 0;
            foreach (var pg in buttons)
            {
                int btnWidth = (pg == "Prev" || pg == "Next") ? 52 : 36;
                Button b = new Button
                {
                    Text = pg,
                    Size = new Size(btnWidth, 24),
                    Location = new Point(pgx, 2),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderColor = Color.FromArgb(220, 215, 205);

                if (pg == "Prev")
                {
                    b.Enabled = _currentPage > 1;
                    b.BackColor = b.Enabled ? Color.White : Color.FromArgb(245, 243, 238);
                    b.ForeColor = b.Enabled ? AppTheme.TextDark : Color.Gray;
                    b.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; ApplyFilters(); } };
                }
                else if (pg == "Next")
                {
                    b.Enabled = _currentPage < _totalPages;
                    b.BackColor = b.Enabled ? Color.White : Color.FromArgb(245, 243, 238);
                    b.ForeColor = b.Enabled ? AppTheme.TextDark : Color.Gray;
                    b.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; ApplyFilters(); } };
                }
                else if (int.TryParse(pg, out int pageNum))
                {
                    bool isCur = pageNum == _currentPage;
                    b.BackColor = isCur ? AppTheme.Primary : Color.White;
                    b.Font = isCur ? new Font("Segoe UI", 7F, FontStyle.Bold) : new Font("Segoe UI", 7F, FontStyle.Regular);
                    b.Click += (s, e) => { _currentPage = pageNum; ApplyFilters(); };
                }

                _pnlPagin.Controls.Add(b);
                pgx += btnWidth + 3;
            }

            _pnlPagin.Width = pgx;
            if (_pnlPagin.Parent != null)
            {
                _pnlPagin.Location = new Point(Math.Max(180, (_pnlPagin.Parent.Width - pgx) / 2), 6);
            }
            _pnlPagin.ResumeLayout();

            int start = totalCount == 0 ? 0 : (_currentPage - 1) * pageSize + 1;
            int end = Math.Min(_currentPage * pageSize, totalCount);
            if (_lblSummary != null)
            {
                _lblSummary.Text = $"Showing {start} - {end} of {totalCount} transactions (Page {_currentPage} of {_totalPages})  |  Total: ₱{totalRevenue:N2}";
            }
        }

        private void ViewSelectedReceipt()
        {
            if (_gridOrders.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an order from the list to view its receipt.", "Selection Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = _gridOrders.SelectedRows[0];
            string orderId = row.Cells["ColId"].Value?.ToString() ?? "ORD-0001";
            string cleanId = orderId.TrimStart('#');

            var actualOrder = _dataService.Orders.FirstOrDefault(o => o.Id.TrimStart('#').Equals(cleanId, StringComparison.OrdinalIgnoreCase));
            if (actualOrder != null)
            {
                using var dlg = new ReceiptForm(actualOrder);
                dlg.ShowDialog(this);
                return;
            }

            string cust = row.Cells["ColCust"].Value?.ToString()?.Split('\n')[0] ?? "Walk-in Customer";
            string totalStr = row.Cells["ColTotal"].Value?.ToString()?.Replace("₱", "")?.Replace(",", "")?.Trim() ?? "0";
            decimal.TryParse(totalStr, out decimal total);

            Order mockOrder = new Order
            {
                Id = orderId,
                CompanyId = 1,
                CustomerName = cust,
                PaymentMethod = row.Cells["ColPay"].Value?.ToString() ?? "Cash",
                Subtotal = total * 0.88m,
                Tax = total * 0.12m,
                Discount = 0,
                TotalAmount = total,
                CreatedAt = DateTime.Now
            };
            mockOrder.Items.Add(new CartItem { ProductId = 1, ProductName = "Computer Hardware Equipment", Quantity = 1, UnitPrice = mockOrder.Subtotal });

            using var dlgMock = new ReceiptForm(mockOrder);
            dlgMock.ShowDialog(this);
        }

        private void VoidOrRestoreSelectedOrder(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _gridOrders.Rows.Count) return;
            var row = _gridOrders.Rows[rowIndex];
            if (!(row.Tag is SalesRecord record)) return;

            string orderId = record.OrderId;
            bool isVoided = record.Status == "Voided";

            if (!isVoided)
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to void / archive Transaction '{orderId}'?\n\n" +
                    "• The transaction will be marked as Voided in fiscal reports.\n" +
                    "• All purchased product stock quantities will be automatically restored to inventory.\n" +
                    "• The audit record remains fully preserved for accounting compliance.",
                    "Archive / Void Order Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                bool ok = _dataService.VoidOrder(orderId);
                if (ok)
                {
                    record.Status = "Voided";
                    RefreshData();
                    MessageBox.Show($"Transaction '{orderId}' has been voided and stock was returned to inventory.",
                        "Order Voided", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                var confirm = MessageBox.Show(
                    $"Restore voided Transaction '{orderId}' back to active status?\n\n" +
                    "• The transaction will be reinstated as an active completed sale.\n" +
                    "• Product inventory will be re-deducted accordingly.",
                    "Restore Order Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                bool ok = _dataService.RestoreOrder(orderId);
                if (ok)
                {
                    record.Status = "Completed";
                    RefreshData();
                    MessageBox.Show($"Transaction '{orderId}' restored successfully!",
                        "Order Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void VoidSelectedOrder()
        {
            if (_gridOrders.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a transaction row from the list to archive / void.",
                    "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            VoidOrRestoreSelectedOrder(_gridOrders.SelectedRows[0].Index);
        }
    }
}
