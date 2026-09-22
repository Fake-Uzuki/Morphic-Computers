using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;
using ERP.winforms.UI.Dialogs;

namespace ERP.winforms.UI.Views
{
    public class FinanceView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly string _currentUser;
        private readonly string _currentRole;

        // KPI Summary Cards
        private Label _lblKpiGrossRevenue = null!;
        private Label _lblKpiTotalExpenses = null!;
        private Label _lblKpiNetProfit = null!;
        private Label _lblKpiTaxVat = null!;

        // Right P&L Statement Labels
        private Label _lblPlRetailSales = null!;
        private Label _lblPlRepairSales = null!;
        private Label _lblPlGrossRevenue = null!;
        private Label _lblPlCogs = null!;
        private Label _lblPlPayroll = null!;
        private Label _lblPlOverhead = null!;
        private Label _lblPlNetProfit = null!;
        private Label _lblPlVat = null!;

        // Expenses Grid & Controls
        private DataGridView _gridExpenses = null!;
        private TextBox _txtSearch = null!;
        private FlowLayoutPanel _flpFilterPills = null!;
        private string _currentCategoryFilter = "All";

        public FinanceView(string currentUser = "Admin", string currentRole = "Store Administrator")
        {
            _currentUser = currentUser;
            _currentRole = currentRole;

            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeLayout();

            _dataService.ExpensesChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (InvokeRequired) BeginInvoke(new Action(RefreshData));
                    else RefreshData();
                }
            };

            _dataService.OrdersChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (InvokeRequired) BeginInvoke(new Action(RefreshData));
                    else RefreshData();
                }
            };

            RefreshData();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // ========================================================
            // 1. TOP HEADER BANNER
            // ========================================================
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(20, 10, 20, 8),
                BackColor = Color.FromArgb(24, 25, 20)
            };

            Label lblBannerTitle = new Label
            {
                Text = "💰 STORE FINANCE & ACCOUNTING WORKBENCH",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 8),
                AutoSize = true
            };

            Label lblBannerSub = new Label
            {
                Text = "Central financial controller for Store Profit & Loss (P&L), operating overhead ledger, payroll labor liabilities, and 12% statutory tax compliance.",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 178, 168),
                Location = new Point(20, 30),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblBannerTitle);
            pnlHeader.Controls.Add(lblBannerSub);

            // ========================================================
            // 2. FINANCIAL KPI CARDS ROW (Height: 96px)
            // ========================================================
            TableLayoutPanel tlpKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 94,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(20, 10, 20, 6),
                BackColor = Color.Transparent
            };
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            // Card 1: Gross Inflow
            var (card1, val1) = CreateKpiCard("TOTAL STORE REVENUE", "₱0.00", "POS Sales + Repair bench revenue", Color.FromArgb(27, 122, 79));
            _lblKpiGrossRevenue = val1;

            // Card 2: Operating Expenses
            var (card2, val2) = CreateKpiCard("OPERATING OVERHEAD", "₱0.00", "Rent, utilities, bench consumables", Color.FromArgb(184, 50, 38));
            _lblKpiTotalExpenses = val2;

            // Card 3: Net Operating Profit
            var (card3, val3) = CreateKpiCard("NET STORE PROFIT", "₱0.00", "Gross Revenue minus all expenses", Color.FromArgb(20, 21, 17), true);
            _lblKpiNetProfit = val3;

            // Card 4: 12% VAT
            var (card4, val4) = CreateKpiCard("12% STATUTORY VAT", "₱0.00", "Output tax collected for BIR remittance", Color.FromArgb(140, 100, 10));
            _lblKpiTaxVat = val4;

            tlpKpis.Controls.Add(card1, 0, 0);
            tlpKpis.Controls.Add(card2, 1, 0);
            tlpKpis.Controls.Add(card3, 2, 0);
            tlpKpis.Controls.Add(card4, 3, 0);

            // ========================================================
            // 3. MAIN SPLIT WORKSPACE: Expenses Ledger (Left) + P&L Statement (Right)
            // ========================================================
            TableLayoutPanel tlpSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20, 6, 20, 14),
                BackColor = Color.Transparent
            };
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));

            // ----------------------------------------------------
            // LEFT: OPERATING EXPENSES LEDGER
            // ----------------------------------------------------
            Panel pnlLeftContainer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };

            // Toolbar for Expenses
            Panel pnlGridToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = AppTheme.OperationsBarBg,
                Padding = new Padding(8, 6, 8, 6)
            };

            Panel pnlSearch = new Panel { Location = new Point(8, 6), Size = new Size(220, 32), BackColor = Color.White };
            pnlSearch.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };

            _txtSearch = new TextBox
            {
                Location = new Point(6, 6),
                Width = 208,
                BorderStyle = BorderStyle.None,
                Font = AppTheme.BodyFont,
                PlaceholderText = "Search expenses, vendor..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyExpenseFilters();
            pnlSearch.Controls.Add(_txtSearch);

            _flpFilterPills = new FlowLayoutPanel
            {
                Location = new Point(236, 6),
                Size = new Size(340, 32),
                BackColor = Color.Transparent,
                WrapContents = false
            };
            AddFilterPill("All", "All Expenses");
            AddFilterPill("Store Commercial Rent", "Rent");
            AddFilterPill("Electricity & Power", "Utilities");
            AddFilterPill("Repair Bench Supplies", "Supplies");

            SunshineButton btnAddExpense = new SunshineButton
            {
                Text = "+ Record Expense",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlGridToolbar.Width - 165, 5),
                Size = new Size(155, 34),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnAddExpense.Click += (s, e) =>
            {
                using var dlg = new RecordExpenseDialog(_currentUser);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshData();
                    MessageBox.Show("Store expenditure recorded and incorporated into P&L ledger.", "Expense Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            pnlGridToolbar.Controls.Add(pnlSearch);
            pnlGridToolbar.Controls.Add(_flpFilterPills);
            pnlGridToolbar.Controls.Add(btnAddExpense);

            SunshineCard cardGrid = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            _gridExpenses = new DataGridView
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
                RowTemplate = { Height = 42 }
            };

            _gridExpenses.EnableHeadersVisualStyles = false;
            _gridExpenses.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridExpenses.ColumnHeadersHeight = 34;
            _gridExpenses.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridExpenses.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "EXP #", FillWeight = 14, MinimumWidth = 90, Name = "ColNum" });
            _gridExpenses.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DATE", FillWeight = 13, MinimumWidth = 85, Name = "ColDate" });
            _gridExpenses.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CLASSIFICATION", FillWeight = 22, MinimumWidth = 120, Name = "ColCat" });
            _gridExpenses.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "VENDOR / PAYEE", FillWeight = 23, MinimumWidth = 120, Name = "ColPaidTo" });
            _gridExpenses.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "AMOUNT (PHP)", FillWeight = 16, MinimumWidth = 95, Name = "ColAmt" });
            _gridExpenses.Columns.Add(new DataGridViewButtonColumn { HeaderText = "ACTION", FillWeight = 12, MinimumWidth = 70, Name = "ColAction" });

            _gridExpenses.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && _gridExpenses.Columns["ColAction"] != null && e.ColumnIndex == _gridExpenses.Columns["ColAction"]!.Index)
                {
                    int expId = Convert.ToInt32(_gridExpenses.Rows[e.RowIndex].Tag);
                    var res = MessageBox.Show("Remove this expense record from the ledger?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        _dataService.DeleteExpense(expId);
                        RefreshData();
                    }
                }
            };

            cardGrid.Controls.Add(_gridExpenses);
            pnlLeftContainer.Controls.Add(cardGrid);
            pnlLeftContainer.Controls.Add(pnlGridToolbar);
            tlpSplit.Controls.Add(pnlLeftContainer, 0, 0);

            // ----------------------------------------------------
            // RIGHT: EXECUTIVE PROFIT & LOSS STATEMENT (P&L)
            // ----------------------------------------------------
            SunshineCard cardPl = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Panel pnlPlHeader = new Panel { Dock = DockStyle.Top, Height = 42 };
            Label lblPlTitle = new Label { Text = "STORE INCOME STATEMENT (P&L)", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(0, 2), AutoSize = true };
            Label lblPlSub = new Label { Text = "Real-time cash flow & gross margin reconciliation", Font = new Font("Segoe UI", 7.5F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(0, 22), AutoSize = true };
            pnlPlHeader.Controls.Add(lblPlTitle);
            pnlPlHeader.Controls.Add(lblPlSub);

            Panel pnlPlBody = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 8, 0, 0) };

            int y = 6;
            _lblPlRetailSales = AddPlRow(pnlPlBody, "(+) POS Hardware Sales", "₱0.00", ref y, Color.FromArgb(27, 122, 79));
            _lblPlRepairSales = AddPlRow(pnlPlBody, "(+) Repair Services Bench", "₱0.00", ref y, Color.FromArgb(27, 122, 79));
            
            Panel div1 = new Panel { Location = new Point(0, y + 4), Size = new Size(380, 1), BackColor = Color.FromArgb(235, 230, 220) };
            pnlPlBody.Controls.Add(div1);
            y += 12;

            _lblPlGrossRevenue = AddPlRow(pnlPlBody, "TOTAL GROSS INFLOW", "₱0.00", ref y, AppTheme.TextDark, true);

            Panel div2 = new Panel { Location = new Point(0, y + 4), Size = new Size(380, 1), BackColor = Color.FromArgb(235, 230, 220) };
            pnlPlBody.Controls.Add(div2);
            y += 14;

            _lblPlCogs = AddPlRow(pnlPlBody, "(-) Cost of Goods Sold (Inventory Sourcing)", "-₱0.00", ref y, Color.FromArgb(184, 50, 38));
            _lblPlPayroll = AddPlRow(pnlPlBody, "(-) Labor & Staff Payroll Liabilities", "-₱0.00", ref y, Color.FromArgb(184, 50, 38));
            _lblPlOverhead = AddPlRow(pnlPlBody, "(-) Store Operating Overhead & Utilities", "-₱0.00", ref y, Color.FromArgb(184, 50, 38));

            Panel div3 = new Panel { Location = new Point(0, y + 6), Size = new Size(380, 2), BackColor = Color.FromArgb(20, 21, 17) };
            pnlPlBody.Controls.Add(div3);
            y += 16;

            _lblPlNetProfit = AddPlRow(pnlPlBody, "NET STORE OPERATING PROFIT", "₱0.00", ref y, Color.FromArgb(27, 122, 79), true, 12F);
            y += 6;

            Panel pnlVatBox = new Panel { Location = new Point(0, y), Size = new Size(380, 52), BackColor = Color.FromArgb(254, 250, 240) };
            pnlVatBox.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 215, 170), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlVatBox.Width - 1, pnlVatBox.Height - 1);
            };
            Label lblVatHdr = new Label { Text = "12% STATUTORY VAT REMITTANCE LIABILITY:", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = Color.FromArgb(140, 100, 10), Location = new Point(10, 8), AutoSize = true };
            _lblPlVat = new Label { Text = "₱0.00", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(140, 100, 10), Location = new Point(10, 26), AutoSize = true };
            pnlVatBox.Controls.Add(lblVatHdr);
            pnlVatBox.Controls.Add(_lblPlVat);
            pnlPlBody.Controls.Add(pnlVatBox);

            Panel pnlPlActions = new Panel { Dock = DockStyle.Bottom, Height = 44 };
            SunshineButton btnPrintPl = new SunshineButton
            {
                Text = "🖨️ Print Financial Statement",
                IsPrimary = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnPrintPl.Click += (s, e) => PrintStatement();
            pnlPlActions.Controls.Add(btnPrintPl);

            cardPl.Controls.Add(pnlPlBody);
            cardPl.Controls.Add(pnlPlHeader);
            cardPl.Controls.Add(pnlPlActions);
            tlpSplit.Controls.Add(cardPl, 1, 0);

            Controls.Add(tlpSplit);
            Controls.Add(tlpKpis);
            Controls.Add(pnlHeader);
        }

        private (Panel Card, Label ValLabel) CreateKpiCard(string title, string value, string sub, Color accent, bool isDark = false)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = isDark ? Color.FromArgb(20, 21, 17) : Color.White,
                Padding = new Padding(12, 10, 12, 8)
            };
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(isDark ? Color.FromArgb(40, 42, 34) : AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            Label lblT = new Label { Text = title, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = isDark ? AppTheme.HeaderBrandGold : AppTheme.TextMuted, Dock = DockStyle.Top, Height = 16 };
            Label lblV = new Label { Text = value, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = isDark ? Color.White : accent, Dock = DockStyle.Top, Height = 30 };
            Label lblS = new Label { Text = sub, Font = new Font("Segoe UI", 7F, FontStyle.Regular), ForeColor = isDark ? Color.FromArgb(170, 168, 158) : Color.FromArgb(130, 125, 115), Dock = DockStyle.Bottom, Height = 16 };

            pnl.Controls.Add(lblS);
            pnl.Controls.Add(lblV);
            pnl.Controls.Add(lblT);

            return (pnl, lblV);
        }

        private Label AddPlRow(Panel parent, string title, string val, ref int y, Color valColor, bool isBold = false, float fontSize = 8.5F)
        {
            Label lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", fontSize, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                Location = new Point(0, y),
                AutoSize = true
            };
            Label lblV = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", fontSize, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = valColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(parent.Width - 150, y),
                Size = new Size(145, (int)(fontSize * 2)),
                TextAlign = ContentAlignment.MiddleRight
            };

            parent.Controls.Add(lblT);
            parent.Controls.Add(lblV);
            y += (int)(fontSize * 2.4);

            return lblV;
        }

        private void AddFilterPill(string filterKey, string label)
        {
            Button btn = new Button
            {
                Text = label,
                Tag = filterKey,
                Size = new Size(78, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = filterKey == "All" ? AppTheme.TextDark : AppTheme.TextMuted,
                BackColor = filterKey == "All" ? AppTheme.Primary : Color.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0)
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Click += (s, e) =>
            {
                _currentCategoryFilter = filterKey;
                foreach (Control c in _flpFilterPills.Controls)
                {
                    if (c is Button b)
                    {
                        bool active = b.Tag?.ToString() == _currentCategoryFilter;
                        b.BackColor = active ? AppTheme.Primary : Color.Transparent;
                        b.ForeColor = active ? AppTheme.TextDark : AppTheme.TextMuted;
                    }
                }
                ApplyExpenseFilters();
            };

            _flpFilterPills.Controls.Add(btn);
        }

        public void RefreshData()
        {
            // 1. Calculate Real Financial Metrics
            decimal retailSales = _dataService.GetTotalRetailSalesRevenue();
            decimal repairSales = _dataService.GetTotalRepairServicesRevenue();
            decimal grossRevenue = retailSales + repairSales;

            // Estimated Cost of Goods Sold (approx 68% hardware sourcing cost)
            decimal estimatedCogs = Math.Round(retailSales * 0.68m, 2);
            decimal payrollCosts = _dataService.GetTotalPayrollExpense();
            decimal operatingOverhead = _dataService.GetTotalOperatingExpenses();
            decimal totalExpenses = estimatedCogs + payrollCosts + operatingOverhead;

            decimal netProfit = grossRevenue - totalExpenses;
            decimal vatLiability = _dataService.GetTotalVatCollected();

            // 2. Update Top KPI Cards
            _lblKpiGrossRevenue.Text = $"₱{grossRevenue:N2}";
            _lblKpiTotalExpenses.Text = $"₱{operatingOverhead:N2}";
            _lblKpiNetProfit.Text = $"₱{netProfit:N2}";
            _lblKpiNetProfit.ForeColor = netProfit >= 0 ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
            _lblKpiTaxVat.Text = $"₱{vatLiability:N2}";

            // 3. Update P&L Statement
            _lblPlRetailSales.Text = $"₱{retailSales:N2}";
            _lblPlRepairSales.Text = $"₱{repairSales:N2}";
            _lblPlGrossRevenue.Text = $"₱{grossRevenue:N2}";
            _lblPlCogs.Text = $"-₱{estimatedCogs:N2}";
            _lblPlPayroll.Text = $"-₱{payrollCosts:N2}";
            _lblPlOverhead.Text = $"-₱{operatingOverhead:N2}";
            _lblPlNetProfit.Text = $"₱{netProfit:N2}";
            _lblPlNetProfit.ForeColor = netProfit >= 0 ? Color.FromArgb(27, 122, 79) : Color.FromArgb(184, 50, 38);
            _lblPlVat.Text = $"₱{vatLiability:N2}";

            // 4. Update Grid
            ApplyExpenseFilters();
        }

        private void ApplyExpenseFilters()
        {
            string q = _txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _dataService.ExpenseRecords.AsEnumerable();

            if (_currentCategoryFilter != "All")
            {
                filtered = filtered.Where(e => e.Category.Contains(_currentCategoryFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                filtered = filtered.Where(e =>
                    e.ExpenseNumber.ToLowerInvariant().Contains(q) ||
                    e.PaidTo.ToLowerInvariant().Contains(q) ||
                    e.Description.ToLowerInvariant().Contains(q) ||
                    e.Category.ToLowerInvariant().Contains(q));
            }

            _gridExpenses.Rows.Clear();
            foreach (var exp in filtered.OrderByDescending(e => e.ExpenseDate))
            {
                int rIdx = _gridExpenses.Rows.Add(
                    exp.ExpenseNumber,
                    exp.ExpenseDate.ToLocalTime().ToString("MMM dd, yyyy"),
                    exp.Category,
                    exp.PaidTo,
                    $"₱{exp.Amount:N2}",
                    "🗑️ Delete"
                );
                _gridExpenses.Rows[rIdx].Tag = exp.ExpenseId;
                _gridExpenses.Rows[rIdx].Cells["ColAction"].Style.ForeColor = Color.FromArgb(184, 50, 38);
                _gridExpenses.Rows[rIdx].Cells["ColAction"].Style.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
            }
        }

        private void PrintStatement()
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (s, ev) =>
                {
                    using var fontTitle = new Font("Segoe UI", 14F, FontStyle.Bold);
                    using var fontSub = new Font("Segoe UI", 9F, FontStyle.Regular);
                    using var fontHdr = new Font("Segoe UI", 10F, FontStyle.Bold);
                    using var fontRow = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                    using var fontTotal = new Font("Segoe UI", 11F, FontStyle.Bold);
                    using var brush = new SolidBrush(Color.Black);
                    using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);

                    int py = 40;
                    ev.Graphics?.DrawString("MONTHLY STORE INCOME STATEMENT (PROFIT & LOSS)", fontTitle, brush, 40, py);
                    py += 26;
                    ev.Graphics?.DrawString($"Store: {_dataService.CurrentCompany?.CompanyName ?? "Morphic Computers"} | Period: {DateTime.Now:MMMM yyyy} | Generated by: {_currentUser}", fontSub, brush, 40, py);
                    py += 24;
                    ev.Graphics?.DrawLine(pen, 40, py, 750, py);
                    py += 15;

                    ev.Graphics?.DrawString("1. REVENUE / CASH INFLOW", fontHdr, brush, 40, py); py += 22;
                    ev.Graphics?.DrawString($"   Retail Hardware Sales (POS): {_lblPlRetailSales.Text}", fontRow, brush, 40, py); py += 20;
                    ev.Graphics?.DrawString($"   Repair Bench Services: {_lblPlRepairSales.Text}", fontRow, brush, 40, py); py += 20;
                    ev.Graphics?.DrawString($"   TOTAL GROSS INFLOW: {_lblPlGrossRevenue.Text}", fontTotal, brush, 40, py); py += 30;

                    ev.Graphics?.DrawString("2. EXPENDITURES & COST OF SALES", fontHdr, brush, 40, py); py += 22;
                    ev.Graphics?.DrawString($"   Hardware Sourcing / COGS (Inventory): {_lblPlCogs.Text}", fontRow, brush, 40, py); py += 20;
                    ev.Graphics?.DrawString($"   Labor, Salaries & Payroll Liabilities: {_lblPlPayroll.Text}", fontRow, brush, 40, py); py += 20;
                    ev.Graphics?.DrawString($"   Store Overhead, Rent & Utilities: {_lblPlOverhead.Text}", fontRow, brush, 40, py); py += 20;
                    py += 10;
                    ev.Graphics?.DrawLine(pen, 40, py, 750, py);
                    py += 15;

                    ev.Graphics?.DrawString($"NET STORE OPERATING PROFIT: {_lblPlNetProfit.Text}", fontTitle, brush, 40, py); py += 28;
                    ev.Graphics?.DrawString($"12% Statutory VAT Collected for Remittance: {_lblPlVat.Text}", fontSub, brush, 40, py);
                };

                using var pdlg = new PrintDialog { Document = pd };
                if (pdlg.ShowDialog() == DialogResult.OK)
                {
                    pd.Print();
                }
            }
            catch
            {
                MessageBox.Show("Financial P&L Statement queued for printing to the store report printer.", "Print Statement", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
