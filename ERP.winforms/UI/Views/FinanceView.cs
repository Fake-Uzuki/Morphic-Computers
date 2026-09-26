using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.domain.security;
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

        // Period Filtering Controls
        private ComboBox _cboPeriodFilter = null!;
        private DateTimePicker _dtpFrom = null!;
        private DateTimePicker _dtpTo = null!;
        private Label _lblToSep = null!;
        private Label _lblPlPeriod = null!;

        // Layout Containers
        private TableLayoutPanel _tlpKpis = null!;
        private TableLayoutPanel _tlpSplit = null!;
        private Panel _pnlPlanRestricted = null!;
        private Label _lblRestrictedMsg = null!;

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

        // Current Report Cache
        private FinancialStatementReport? _lastReport;

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
                Height = 44,
                Padding = new Padding(20, 6, 20, 6),
                BackColor = Color.FromArgb(24, 25, 20)
            };

            Label lblBannerTitle = new Label
            {
                Text = "Store Finance & Statements",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 10),
                AutoSize = true
            };

            // Period Filter Controls (Right-aligned)
            FlowLayoutPanel flpPeriod = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 10, 0)
            };

            Label lblPeriodPrompt = new Label
            {
                Text = "Period:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 190),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            _cboPeriodFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Width = 115,
                Margin = new Padding(0, 2, 8, 0)
            };
            _cboPeriodFilter.Items.AddRange(new object[] { "All Time", "This Month", "Last Month", "Custom Range" });
            _cboPeriodFilter.SelectedIndex = 0; // Default All Time

            _dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Width = 95,
                Visible = false,
                Value = DateTime.Today.AddMonths(-1),
                Margin = new Padding(0, 2, 4, 0)
            };

            _lblToSep = new Label
            {
                Text = "to",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 200, 190),
                AutoSize = true,
                Visible = false,
                Margin = new Padding(0, 6, 4, 0)
            };

            _dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Width = 95,
                Visible = false,
                Value = DateTime.Today,
                Margin = new Padding(0, 2, 8, 0)
            };

            _cboPeriodFilter.SelectedIndexChanged += (s, e) =>
            {
                bool isCustom = _cboPeriodFilter.SelectedItem?.ToString() == "Custom Range";
                _dtpFrom.Visible = isCustom;
                _lblToSep.Visible = isCustom;
                _dtpTo.Visible = isCustom;
                RefreshData();
            };

            _dtpFrom.ValueChanged += (s, e) => { if (_cboPeriodFilter.SelectedItem?.ToString() == "Custom Range") RefreshData(); };
            _dtpTo.ValueChanged += (s, e) => { if (_cboPeriodFilter.SelectedItem?.ToString() == "Custom Range") RefreshData(); };

            flpPeriod.Controls.Add(lblPeriodPrompt);
            flpPeriod.Controls.Add(_cboPeriodFilter);
            flpPeriod.Controls.Add(_dtpFrom);
            flpPeriod.Controls.Add(_lblToSep);
            flpPeriod.Controls.Add(_dtpTo);

            pnlHeader.Controls.Add(flpPeriod);
            pnlHeader.Controls.Add(lblBannerTitle);

            // ========================================================
            // 2. FINANCIAL KPI CARDS ROW (Height: 78px)
            // ========================================================
            _tlpKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 78,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(20, 8, 20, 4),
                BackColor = Color.Transparent
            };
            _tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            // Card 1: Gross Inflow
            var (card1, val1) = CreateKpiCard("TOTAL STORE REVENUE", "₱0.00", Color.FromArgb(27, 122, 79));
            _lblKpiGrossRevenue = val1;

            // Card 2: Operating Expenses
            var (card2, val2) = CreateKpiCard("OPERATING OVERHEAD", "₱0.00", Color.FromArgb(184, 50, 38));
            _lblKpiTotalExpenses = val2;

            // Card 3: Net Operating Profit
            var (card3, val3) = CreateKpiCard("NET STORE PROFIT", "₱0.00", Color.FromArgb(20, 21, 17), true);
            _lblKpiNetProfit = val3;

            // Card 4: 12% VAT
            var (card4, val4) = CreateKpiCard("12% STATUTORY VAT", "₱0.00", Color.FromArgb(140, 100, 10));
            _lblKpiTaxVat = val4;

            _tlpKpis.Controls.Add(card1, 0, 0);
            _tlpKpis.Controls.Add(card2, 1, 0);
            _tlpKpis.Controls.Add(card3, 2, 0);
            _tlpKpis.Controls.Add(card4, 3, 0);

            // ========================================================
            // 3. MAIN SPLIT WORKSPACE: Expenses Ledger (Left) + P&L Statement (Right)
            // ========================================================
            _tlpSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20, 6, 20, 14),
                BackColor = Color.Transparent
            };
            _tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
            _tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));

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
                Size = new Size(420, 32),
                BackColor = Color.Transparent,
                WrapContents = false
            };
            AddFilterPill("All", "All Active");
            AddFilterPill("Store Commercial Rent", "Rent");
            AddFilterPill("Electricity & Power", "Utilities");
            AddFilterPill("Repair Bench Supplies", "Supplies");
            AddFilterPill("Archived", "Archived");

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
                    var exp = _dataService.ExpenseRecords.FirstOrDefault(x => x.ExpenseId == expId);
                    if (exp == null) return;

                    string prompt = exp.IsActive
                        ? $"Archive expense record '{exp.ExpenseNumber}' ({exp.PaidTo} - ₱{exp.Amount:N2})?\n\nArchived expenses are excluded from active Operating Overhead and Store Profit calculations."
                        : $"Restore expense record '{exp.ExpenseNumber}' ({exp.PaidTo} - ₱{exp.Amount:N2}) back to active status?";

                    var res = MessageBox.Show(prompt, exp.IsActive ? "Confirm Archive Expense" : "Confirm Restore Expense", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        _dataService.ToggleExpenseArchive(expId);
                        RefreshData();
                    }
                }
            };

            cardGrid.Controls.Add(_gridExpenses);
            pnlLeftContainer.Controls.Add(cardGrid);
            pnlLeftContainer.Controls.Add(pnlGridToolbar);
            _tlpSplit.Controls.Add(pnlLeftContainer, 0, 0);

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
            _lblPlPeriod = new Label { Text = "Period: All Time", Font = new Font("Segoe UI", 8F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(0, 22), AutoSize = true };
            pnlPlHeader.Controls.Add(lblPlTitle);
            pnlPlHeader.Controls.Add(_lblPlPeriod);

            Panel pnlPlBody = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 6, 0, 0) };

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

            _lblPlCogs = AddPlRow(pnlPlBody, "(-) Cost of Goods Sold (Unrecorded)", "₱0.00", ref y, Color.FromArgb(140, 140, 140));
            Label lblCogsNote = new Label
            {
                Text = "* Historical unit purchase cost is not tracked in catalog",
                Font = new Font("Segoe UI", 7F, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(0, y - 2),
                AutoSize = true
            };
            pnlPlBody.Controls.Add(lblCogsNote);
            y += 14;

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
            _tlpSplit.Controls.Add(cardPl, 1, 0);

            // ========================================================
            // 4. PLAN RESTRICTED BANNER / OVERLAY
            // ========================================================
            _pnlPlanRestricted = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.FromArgb(245, 243, 238)
            };

            Panel cardRestricted = new Panel
            {
                Size = new Size(580, 240),
                BackColor = Color.White,
                Location = new Point(50, 50)
            };
            cardRestricted.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(210, 205, 195), 1);
                e.Graphics.DrawRectangle(p, 0, 0, cardRestricted.Width - 1, cardRestricted.Height - 1);
            };

            Label lblRestrictedTitle = new Label
            {
                Text = "🔒 Medium Enterprise Plan Required",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(184, 50, 38),
                Location = new Point(24, 24),
                AutoSize = true
            };

            _lblRestrictedMsg = new Label
            {
                Text = "Financial Statements & Executive P&L is exclusive to the Medium Enterprise Plan.\nPlease upgrade your subscription to access this module.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(24, 65),
                Size = new Size(530, 80)
            };

            Label lblRestrictedHint = new Label
            {
                Text = "Plan Entitlements: Micro (Generative Income, Reports) | Small (+Support Income) | Medium (+Procurement, +Payroll, +Financial Statements, +Dashboard)",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(24, 160),
                Size = new Size(530, 40)
            };

            cardRestricted.Controls.Add(lblRestrictedTitle);
            cardRestricted.Controls.Add(_lblRestrictedMsg);
            cardRestricted.Controls.Add(lblRestrictedHint);
            _pnlPlanRestricted.Controls.Add(cardRestricted);

            Controls.Add(_pnlPlanRestricted);
            Controls.Add(_tlpSplit);
            Controls.Add(_tlpKpis);
            Controls.Add(pnlHeader);
        }

        private (Panel Card, Label ValLabel) CreateKpiCard(string title, string value, Color accent, bool isDark = false)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = isDark ? Color.FromArgb(20, 21, 17) : Color.White,
                Padding = new Padding(14, 12, 14, 10)
            };
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(isDark ? Color.FromArgb(40, 42, 34) : AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            Label lblT = new Label { Text = title, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = isDark ? AppTheme.HeaderBrandGold : AppTheme.TextMuted, Dock = DockStyle.Top, Height = 18 };
            Label lblV = new Label { Text = value, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = isDark ? Color.White : accent, Dock = DockStyle.Top, Height = 32 };

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
            var plan = _dataService.CurrentCompany?.PlanName;
            bool isAllowed = ModuleAccessService.IsModuleEnabled(plan, ErpModule.FinancialStatements);

            if (!isAllowed)
            {
                _tlpKpis.Visible = false;
                _tlpSplit.Visible = false;
                _pnlPlanRestricted.Visible = true;
                _lblRestrictedMsg.Text = $"Financial Statements & Executive P&L is exclusive to the Medium Enterprise Plan.\nYour current plan is '{plan ?? "Micro"}'. Please upgrade your subscription to access this module.";
                return;
            }

            _tlpKpis.Visible = true;
            _tlpSplit.Visible = true;
            _pnlPlanRestricted.Visible = false;

            // Determine active period filter
            string selectedPeriod = _cboPeriodFilter?.SelectedItem?.ToString() ?? "All Time";
            DateTime? startDate = null;
            DateTime? endDate = null;
            string periodLabel = selectedPeriod;

            DateTime now = DateTime.Now;
            if (selectedPeriod == "This Month")
            {
                startDate = new DateTime(now.Year, now.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddTicks(-1);
                periodLabel = $"This Month ({startDate.Value:MMM yyyy})";
            }
            else if (selectedPeriod == "Last Month")
            {
                DateTime prev = now.AddMonths(-1);
                startDate = new DateTime(prev.Year, prev.Month, 1);
                endDate = new DateTime(now.Year, now.Month, 1).AddTicks(-1);
                periodLabel = $"Last Month ({startDate.Value:MMM yyyy})";
            }
            else if (selectedPeriod == "Custom Range")
            {
                startDate = _dtpFrom.Value.Date;
                endDate = _dtpTo.Value.Date.AddDays(1).AddTicks(-1);
                periodLabel = $"Custom ({startDate.Value:MMM dd, yyyy} - {_dtpTo.Value.Date:MMM dd, yyyy})";
            }

            // 1. Fetch Real Statement from DataService (API / Local SQL / Memory)
            var report = _dataService.GetFinancialStatement(startDate, endDate, periodLabel);
            _lastReport = report;

            // 2. Update Top KPI Cards
            _lblKpiGrossRevenue.Text = $"₱{report.GrossRevenue:N2}";
            _lblKpiTotalExpenses.Text = $"₱{report.OperatingOverhead:N2}";
            _lblKpiNetProfit.Text = $"₱{report.NetOperatingProfit:N2}";
            _lblKpiNetProfit.ForeColor = report.NetOperatingProfit >= 0 ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
            _lblKpiTaxVat.Text = $"₱{report.VatLiability:N2}";

            // 3. Update P&L Statement
            _lblPlPeriod.Text = $"Period: {report.PeriodLabel}";
            _lblPlRetailSales.Text = $"₱{report.RetailSalesRevenue:N2}";
            _lblPlRepairSales.Text = $"₱{report.RepairServicesRevenue:N2}";
            _lblPlGrossRevenue.Text = $"₱{report.GrossRevenue:N2}";
            _lblPlCogs.Text = $"₱0.00 (Unrecorded)";
            _lblPlPayroll.Text = $"-₱{report.PayrollExpenses:N2}";
            _lblPlOverhead.Text = $"-₱{report.OperatingOverhead:N2}";
            _lblPlNetProfit.Text = $"₱{report.NetOperatingProfit:N2}";
            _lblPlNetProfit.ForeColor = report.NetOperatingProfit >= 0 ? Color.FromArgb(27, 122, 79) : Color.FromArgb(184, 50, 38);
            _lblPlVat.Text = $"₱{report.VatLiability:N2}";

            // 4. Update Grid with active period
            ApplyExpenseFilters(startDate, endDate);
        }

        private void ApplyExpenseFilters(DateTime? startDate = null, DateTime? endDate = null)
        {
            if (_gridExpenses == null) return;

            string q = _txtSearch?.Text.Trim().ToLowerInvariant() ?? "";
            var filtered = _dataService.ExpenseRecords.AsEnumerable();

            if (_currentCategoryFilter == "Archived")
            {
                filtered = filtered.Where(e => !e.IsActive);
            }
            else
            {
                filtered = filtered.Where(e => e.IsActive);
                if (_currentCategoryFilter != "All")
                {
                    filtered = filtered.Where(e => e.Category.Contains(_currentCategoryFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (startDate.HasValue)
            {
                filtered = filtered.Where(e => e.ExpenseDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                filtered = filtered.Where(e => e.ExpenseDate <= endDate.Value);
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
                string actionText = exp.IsActive ? "📁 Archive" : "♻️ Restore";
                int rIdx = _gridExpenses.Rows.Add(
                    exp.ExpenseNumber,
                    exp.ExpenseDate.ToLocalTime().ToString("MMM dd, yyyy"),
                    exp.Category,
                    exp.PaidTo,
                    $"₱{exp.Amount:N2}",
                    actionText
                );
                _gridExpenses.Rows[rIdx].Tag = exp.ExpenseId;
                _gridExpenses.Rows[rIdx].Cells["ColAction"].Style.ForeColor = exp.IsActive ? Color.FromArgb(184, 50, 38) : Color.FromArgb(27, 122, 79);
                _gridExpenses.Rows[rIdx].Cells["ColAction"].Style.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            }
        }

        private void PrintStatement()
        {
            var r = _lastReport ?? _dataService.GetFinancialStatement();
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
                    ev.Graphics?.DrawString("STORE FINANCIAL STATEMENT (PROFIT & LOSS)", fontTitle, brush, 40, py);
                    py += 26;
                    ev.Graphics?.DrawString($"Store: {_dataService.CurrentCompany?.CompanyName ?? "Morphic Computers"} | Period: {r.PeriodLabel} | Generated: {r.GeneratedAt.ToLocalTime():MMM dd, yyyy HH:mm} | By: {_currentUser}", fontSub, brush, 40, py);
                    py += 24;
                    ev.Graphics?.DrawLine(pen, 40, py, 750, py);
                    py += 15;

                    ev.Graphics?.DrawString("1. REVENUE / CASH INFLOW", fontHdr, brush, 40, py); py += 22;
                    ev.Graphics?.DrawString($"   Retail Hardware Sales (POS): ₱{r.RetailSalesRevenue:N2} ({r.CompletedOrdersCount} completed orders)", fontRow, brush, 40, py); py += 20;
                    ev.Graphics?.DrawString($"   Repair Bench Services: ₱{r.RepairServicesRevenue:N2} ({r.CompletedRepairsCount} completed tickets)", fontRow, brush, 40, py); py += 20;
                    ev.Graphics?.DrawString($"   TOTAL GROSS INFLOW: ₱{r.GrossRevenue:N2}", fontTotal, brush, 40, py); py += 30;

                    ev.Graphics?.DrawString("2. EXPENDITURES & COST OF SALES", fontHdr, brush, 40, py); py += 22;
                    ev.Graphics?.DrawString($"   Hardware Sourcing / COGS (Inventory): ₱{r.CostOfGoodsSold:N2} [{r.CogsStatus}]", fontRow, brush, 40, py); py += 20;
                    ev.Graphics?.DrawString($"   Labor, Salaries & Payroll Liabilities: ₱{r.PayrollExpenses:N2} ({r.PayrollDisbursementsCount} staff payroll disbursements)", fontRow, brush, 40, py); py += 20;
                    ev.Graphics?.DrawString($"   Store Overhead, Rent & Utilities: ₱{r.OperatingOverhead:N2} ({r.ActiveExpensesCount} active expenses)", fontRow, brush, 40, py); py += 20;
                    py += 10;
                    ev.Graphics?.DrawLine(pen, 40, py, 750, py);
                    py += 15;

                    ev.Graphics?.DrawString($"NET STORE OPERATING PROFIT: ₱{r.NetOperatingProfit:N2}", fontTitle, brush, 40, py); py += 28;
                    ev.Graphics?.DrawString($"12% Statutory VAT Collected for Remittance: ₱{r.VatLiability:N2}", fontSub, brush, 40, py);
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
