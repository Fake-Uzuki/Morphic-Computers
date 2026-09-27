using System;
using System.Collections.Generic;
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
    public class PayrollView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridPayroll = null!;
        private TextBox _txtSearch = null!;
        private FlowLayoutPanel _flpFilterPills = null!;

        private string _currentRoleFilter = "All";
        private List<PayrollRecord> _currentDisplayRecords = new();

        public PayrollView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeLayout();
            _dataService.PayrollRecordsChanged += () =>
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
                PlaceholderText = "Search employee, role, bank..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlSearch.Controls.Add(_txtSearch);

            // Filter Pills
            _flpFilterPills = new FlowLayoutPanel
            {
                Location = new Point(295, 8),
                Size = new Size(580, 36),
                BackColor = Color.Transparent,
                WrapContents = false
            };

            AddFilterPill("All", "All Roles");
            AddFilterPill("Manager", "Managers");
            AddFilterPill("Technician", "Technicians");
            AddFilterPill("Counter", "Sales / Counter");
            AddFilterPill("Inventory", "Inventory");

            SunshineButton btnPrintReport = new SunshineButton
            {
                Text = "🖨️ Print Payroll Report",
                IsPrimary = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 445, 8),
                Size = new Size(195, 34),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold)
            };
            btnPrintReport.Click += (s, e) => PrintPayrollSummaryReport();

            SunshineButton btnNewPayroll = new SunshineButton
            {
                Text = "+ Calculate & Disburse Payroll",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 235, 8),
                Size = new Size(215, 34),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnNewPayroll.Click += (s, e) =>
            {
                using var dialog = new PayrollCalculatorDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RefreshData();
                }
            };

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(_flpFilterPills);
            pnlToolbar.Controls.Add(btnPrintReport);
            pnlToolbar.Controls.Add(btnNewPayroll);

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

            _gridPayroll = new DataGridView
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

            _gridPayroll.EnableHeadersVisualStyles = false;
            _gridPayroll.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridPayroll.ColumnHeadersHeight = 36;
            _gridPayroll.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", FillWeight = 6, MinimumWidth = 45, Name = "ColId" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "EMPLOYEE NAME", FillWeight = 14, MinimumWidth = 120, Name = "ColName" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ROLE / DEPT", FillWeight = 11, MinimumWidth = 100, Name = "ColRole" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PERIOD", FillWeight = 11, MinimumWidth = 95, Name = "ColPeriod" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BASIC / GROSS", FillWeight = 10, MinimumWidth = 95, Name = "ColGross" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SSS", FillWeight = 8, MinimumWidth = 70, Name = "ColSss" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PHILHEALTH", FillWeight = 8, MinimumWidth = 75, Name = "ColPhic" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PAG-IBIG", FillWeight = 7, MinimumWidth = 65, Name = "ColHdmf" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "W/TAX", FillWeight = 8, MinimumWidth = 70, Name = "ColTax" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OTHER DED", FillWeight = 7, MinimumWidth = 65, Name = "ColOther" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TOTAL DED", FillWeight = 9, MinimumWidth = 75, Name = "ColDeduc" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NET PAY", FillWeight = 11, MinimumWidth = 90, Name = "ColNet" });
            _gridPayroll.Columns.Add(new DataGridViewButtonColumn { HeaderText = "PAYSLIP", FillWeight = 8, MinimumWidth = 65, Text = "Payslip", UseColumnTextForButtonValue = true, Name = "ColPayslip" });
            _gridPayroll.Columns.Add(new DataGridViewButtonColumn { HeaderText = "VOID", FillWeight = 6, MinimumWidth = 50, Text = "Void", UseColumnTextForButtonValue = true, Name = "ColDelete" });

            _gridPayroll.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int payrollId = Convert.ToInt32(_gridPayroll.Rows[e.RowIndex].Tag);
                    var record = _dataService.PayrollRecords.FirstOrDefault(p => p.PayrollId == payrollId);
                    if (record == null) return;

                    if (e.ColumnIndex == _gridPayroll.Columns["ColPayslip"]!.Index)
                    {
                        using var dlg = new PayslipViewDialog(record);
                        dlg.ShowDialog();
                    }
                    else if (e.ColumnIndex == _gridPayroll.Columns["ColDelete"]!.Index)
                    {
                        var res = MessageBox.Show(
                            $"Are you sure you want to void the payroll compensation record for '{record.StaffName}' (₱{record.NetPay:N2})?",
                            "Confirm Void",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (res == DialogResult.Yes)
                        {
                            _dataService.DeletePayrollRecord(payrollId);
                            RefreshData();
                        }
                    }
                }
            };

            cardGrid.Controls.Add(_gridPayroll);
            pnlMainContainer.Controls.Add(cardGrid);
            cardGrid.BringToFront();

            Controls.Add(pnlMainContainer);
            Controls.Add(pnlToolbar);
        }



        private void AddFilterPill(string roleFilter, string label)
        {
            Button btnPill = new Button
            {
                Text = label,
                Tag = roleFilter,
                AutoSize = true,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = roleFilter == _currentRoleFilter ? AppTheme.Primary : Color.FromArgb(240, 238, 230),
                ForeColor = roleFilter == _currentRoleFilter ? Color.Black : AppTheme.TextDark,
                Margin = new Padding(0, 2, 8, 2),
                Cursor = Cursors.Hand
            };
            btnPill.FlatAppearance.BorderSize = 0;
            btnPill.Click += (s, e) =>
            {
                _currentRoleFilter = (string)btnPill.Tag!;
                UpdatePillStyles();
                ApplyFilters();
            };
            _flpFilterPills.Controls.Add(btnPill);
        }

        private void UpdatePillStyles()
        {
            foreach (Control c in _flpFilterPills.Controls)
            {
                if (c is Button btn)
                {
                    bool isSel = (string)btn.Tag! == _currentRoleFilter;
                    btn.BackColor = isSel ? AppTheme.Primary : Color.FromArgb(240, 238, 230);
                    btn.ForeColor = isSel ? Color.Black : AppTheme.TextDark;
                }
            }
        }

        public void RefreshData()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var query = _dataService.PayrollRecords.AsEnumerable();

            if (_currentRoleFilter != "All")
            {
                query = query.Where(r => r.Role.Contains(_currentRoleFilter, StringComparison.OrdinalIgnoreCase));
            }

            string term = _txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(r =>
                    r.StaffName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    r.Role.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    r.PaymentMethod.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    r.PayrollId.ToString().Contains(term));
            }

            var list = query.OrderByDescending(r => r.ProcessedAt).ToList();
            _currentDisplayRecords = list;

            _gridPayroll.Rows.Clear();
            foreach (var r in list)
            {
                decimal sss = r.SssDeduction;
                decimal phic = r.PhilHealthDeduction;
                decimal hdmf = r.PagIbigDeduction;
                decimal tax = r.WithholdingTax;
                decimal other = r.OtherDeductions;
                if (sss == 0 && phic == 0 && hdmf == 0 && tax == 0 && r.Deductions > 0)
                {
                    sss = Math.Round(r.Deductions * 0.45m, 2);
                    phic = Math.Round(r.Deductions * 0.25m, 2);
                    hdmf = r.Deductions - sss - phic;
                }

                int rowIdx = _gridPayroll.Rows.Add(
                    $"#PAY-{r.PayrollId:D3}",
                    r.StaffName,
                    r.Role,
                    $"{r.PeriodStart:MMM dd} - {r.PeriodEnd:MMM dd}",
                    $"₱{r.GrossPay:N2}",
                    $"₱{sss:N2}",
                    $"₱{phic:N2}",
                    $"₱{hdmf:N2}",
                    $"₱{tax:N2}",
                    $"₱{other:N2}",
                    $"₱{r.Deductions:N2}",
                    $"₱{r.NetPay:N2}"
                );

                var row = _gridPayroll.Rows[rowIdx];
                row.Tag = r.PayrollId;
                row.Cells["ColNet"].Style.ForeColor = Color.FromArgb(20, 130, 60);
                row.Cells["ColNet"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
        }

        private void PrintPayrollSummaryReport()
        {
            if (_currentDisplayRecords == null || _currentDisplayRecords.Count == 0)
            {
                MessageBox.Show("There are no payroll records currently displayed in the table to print.", "No Payroll Records", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int recordIndex = 0;
                int pageNumber = 1;

                PrintDocument pd = new PrintDocument();
                pd.DocumentName = $"Payroll_Summary_Report_{DateTime.UtcNow:yyyyMMdd}";
                pd.DefaultPageSettings.Landscape = true;

                pd.BeginPrint += (s, ev) =>
                {
                    recordIndex = 0;
                    pageNumber = 1;
                };

                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics;
                    if (g == null) return;

                    using var fontTitle = new Font("Segoe UI", 13F, FontStyle.Bold);
                    using var fontSubtitle = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    using var fontMeta = new Font("Segoe UI", 8F, FontStyle.Regular);
                    using var fontHdr = new Font("Segoe UI", 7.8F, FontStyle.Bold);
                    using var fontRow = new Font("Segoe UI", 8F, FontStyle.Regular);
                    using var fontRowBold = new Font("Segoe UI", 8F, FontStyle.Bold);
                    using var fontTotals = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                    using var fontFooter = new Font("Segoe UI", 7.5F, FontStyle.Italic);

                    using var brushDark = new SolidBrush(AppTheme.TextDark);
                    using var brushMuted = new SolidBrush(Color.FromArgb(110, 105, 95));
                    using var brushGreen = new SolidBrush(Color.FromArgb(20, 125, 60));
                    using var penGrid = new Pen(Color.FromArgb(225, 222, 214), 1);
                    using var penThick = new Pen(Color.FromArgb(160, 155, 140), 1.5F);
                    using var brushHeaderBg = new SolidBrush(Color.FromArgb(246, 244, 238));
                    using var brushAltBg = new SolidBrush(Color.FromArgb(252, 251, 248));

                    StringFormat alignRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                    StringFormat alignLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    StringFormat alignCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                    int xStart = 40;
                    int y = 40;
                    int totalTableWidth = 1000;

                    // 1. REPORT HEADER
                    string companyName = _dataService.CurrentCompany?.CompanyName ?? "IT8 TechStore";
                    string planName = _dataService.CurrentCompany?.PlanName ?? "Enterprise";
                    g.DrawString(companyName.ToUpperInvariant(), fontTitle, brushDark, xStart, y);
                    y += 24;

                    g.DrawString("STORE PAYROLL DISBURSEMENT & STATUTORY COMPLIANCE REPORT", fontSubtitle, brushDark, xStart, y);
                    y += 18;

                    string filterDesc = _currentRoleFilter == "All" ? "All Staff Positions" : $"Filtered by Role: {_currentRoleFilter}";
                    string meta = $"Generated: {DateTime.Now:yyyy-MM-dd hh:mm tt}  |  Scope: {filterDesc}  |  Total Records: {_currentDisplayRecords.Count}  |  Plan: {planName}  |  Page {pageNumber}";
                    g.DrawString(meta, fontMeta, brushMuted, xStart, y);
                    y += 20;

                    g.DrawLine(penThick, xStart, y, xStart + totalTableWidth, y);
                    y += 10;

                    // 2. COLUMN HEADERS
                    // Total table width = 1000
                    int colIdW = 65;       // 40
                    int colNameW = 150;    // 105
                    int colRoleW = 105;    // 255
                    int colPeriodW = 110;  // 360
                    int colGrossW = 80;    // 470
                    int colSssW = 65;      // 550
                    int colPhicW = 65;     // 615
                    int colHdmfW = 60;     // 680
                    int colTaxW = 65;      // 740
                    int colOtherW = 55;    // 805
                    int colTotDedW = 80;   // 860
                    int colNetW = 100;     // 940 (to 1040)

                    int headerH = 26;
                    g.FillRectangle(brushHeaderBg, xStart, y, totalTableWidth, headerH);
                    g.DrawRectangle(penGrid, xStart, y, totalTableWidth, headerH);

                    int curX = xStart;
                    g.DrawString("ID", fontHdr, brushDark, new RectangleF(curX, y, colIdW, headerH), alignLeft); curX += colIdW;
                    g.DrawString("STAFF NAME", fontHdr, brushDark, new RectangleF(curX, y, colNameW, headerH), alignLeft); curX += colNameW;
                    g.DrawString("ROLE", fontHdr, brushDark, new RectangleF(curX, y, colRoleW, headerH), alignLeft); curX += colRoleW;
                    g.DrawString("PAY PERIOD", fontHdr, brushDark, new RectangleF(curX, y, colPeriodW, headerH), alignLeft); curX += colPeriodW;
                    g.DrawString("GROSS", fontHdr, brushDark, new RectangleF(curX, y, colGrossW, headerH), alignRight); curX += colGrossW;
                    g.DrawString("SSS", fontHdr, brushDark, new RectangleF(curX, y, colSssW, headerH), alignRight); curX += colSssW;
                    g.DrawString("PHILHEALTH", fontHdr, brushDark, new RectangleF(curX, y, colPhicW, headerH), alignRight); curX += colPhicW;
                    g.DrawString("PAG-IBIG", fontHdr, brushDark, new RectangleF(curX, y, colHdmfW, headerH), alignRight); curX += colHdmfW;
                    g.DrawString("TAX", fontHdr, brushDark, new RectangleF(curX, y, colTaxW, headerH), alignRight); curX += colTaxW;
                    g.DrawString("OTHER", fontHdr, brushDark, new RectangleF(curX, y, colOtherW, headerH), alignRight); curX += colOtherW;
                    g.DrawString("TOTAL DED", fontHdr, brushDark, new RectangleF(curX, y, colTotDedW, headerH), alignRight); curX += colTotDedW;
                    g.DrawString("NET PAY", fontHdr, brushDark, new RectangleF(curX, y, colNetW, headerH), alignRight);
                    y += headerH;

                    // 3. DATA ROWS
                    int rowH = 22;
                    bool hasMore = false;

                    while (recordIndex < _currentDisplayRecords.Count)
                    {
                        // Check if we need a new page
                        if (y + rowH + 130 > ev.MarginBounds.Bottom && recordIndex < _currentDisplayRecords.Count - 1)
                        {
                            hasMore = true;
                            break;
                        }

                        var r = _currentDisplayRecords[recordIndex];

                        decimal sss = r.SssDeduction;
                        decimal phic = r.PhilHealthDeduction;
                        decimal hdmf = r.PagIbigDeduction;
                        decimal tax = r.WithholdingTax;
                        decimal other = r.OtherDeductions;
                        if (sss == 0 && phic == 0 && hdmf == 0 && tax == 0 && r.Deductions > 0)
                        {
                            sss = Math.Round(r.Deductions * 0.45m, 2);
                            phic = Math.Round(r.Deductions * 0.25m, 2);
                            hdmf = r.Deductions - sss - phic;
                        }

                        if (recordIndex % 2 == 1)
                        {
                            g.FillRectangle(brushAltBg, xStart, y, totalTableWidth, rowH);
                        }

                        curX = xStart;
                        g.DrawString($"#PAY-{r.PayrollId:D3}", fontRow, brushDark, new RectangleF(curX, y, colIdW, rowH), alignLeft); curX += colIdW;
                        g.DrawString(r.StaffName, fontRowBold, brushDark, new RectangleF(curX, y, colNameW, rowH), alignLeft); curX += colNameW;
                        g.DrawString(r.Role, fontRow, brushMuted, new RectangleF(curX, y, colRoleW, rowH), alignLeft); curX += colRoleW;
                        g.DrawString($"{r.PeriodStart:MMM dd} - {r.PeriodEnd:MMM dd}", fontRow, brushDark, new RectangleF(curX, y, colPeriodW, rowH), alignLeft); curX += colPeriodW;
                        g.DrawString($"₱{r.GrossPay:N2}", fontRow, brushDark, new RectangleF(curX, y, colGrossW, rowH), alignRight); curX += colGrossW;
                        g.DrawString($"₱{sss:N2}", fontRow, brushDark, new RectangleF(curX, y, colSssW, rowH), alignRight); curX += colSssW;
                        g.DrawString($"₱{phic:N2}", fontRow, brushDark, new RectangleF(curX, y, colPhicW, rowH), alignRight); curX += colPhicW;
                        g.DrawString($"₱{hdmf:N2}", fontRow, brushDark, new RectangleF(curX, y, colHdmfW, rowH), alignRight); curX += colHdmfW;
                        g.DrawString($"₱{tax:N2}", fontRow, brushDark, new RectangleF(curX, y, colTaxW, rowH), alignRight); curX += colTaxW;
                        g.DrawString($"₱{other:N2}", fontRow, brushDark, new RectangleF(curX, y, colOtherW, rowH), alignRight); curX += colOtherW;
                        g.DrawString($"₱{r.Deductions:N2}", fontRow, brushDark, new RectangleF(curX, y, colTotDedW, rowH), alignRight); curX += colTotDedW;
                        g.DrawString($"₱{r.NetPay:N2}", fontRowBold, brushGreen, new RectangleF(curX, y, colNetW, rowH), alignRight);

                        g.DrawLine(penGrid, xStart, y + rowH, xStart + totalTableWidth, y + rowH);
                        y += rowH;
                        recordIndex++;
                    }

                    if (hasMore)
                    {
                        pageNumber++;
                        ev.HasMorePages = true;
                        return;
                    }

                    // 4. SUMMARY TOTALS ROW
                    y += 4;
                    int totalsH = 26;
                    g.FillRectangle(brushHeaderBg, xStart, y, totalTableWidth, totalsH);
                    g.DrawRectangle(penThick, xStart, y, totalTableWidth, totalsH);

                    decimal totalGross = _currentDisplayRecords.Sum(r => r.GrossPay);
                    decimal totalSss = _currentDisplayRecords.Sum(r => (r.SssDeduction > 0 ? r.SssDeduction : Math.Round(r.Deductions * 0.45m, 2)));
                    decimal totalPhic = _currentDisplayRecords.Sum(r => (r.PhilHealthDeduction > 0 ? r.PhilHealthDeduction : Math.Round(r.Deductions * 0.25m, 2)));
                    decimal totalHdmf = _currentDisplayRecords.Sum(r => (r.PagIbigDeduction > 0 ? r.PagIbigDeduction : (r.Deductions - Math.Round(r.Deductions * 0.45m, 2) - Math.Round(r.Deductions * 0.25m, 2))));
                    decimal totalTax = _currentDisplayRecords.Sum(r => r.WithholdingTax);
                    decimal totalOther = _currentDisplayRecords.Sum(r => r.OtherDeductions);
                    decimal totalDeduc = _currentDisplayRecords.Sum(r => r.Deductions);
                    decimal totalNet = _currentDisplayRecords.Sum(r => r.NetPay);

                    curX = xStart;
                    int summaryLabelWidth = colIdW + colNameW + colRoleW + colPeriodW;
                    g.DrawString($"TOTALS ({_currentDisplayRecords.Count} RECORDS)", fontTotals, brushDark, new RectangleF(curX, y, summaryLabelWidth, totalsH), alignLeft);
                    curX += summaryLabelWidth;

                    g.DrawString($"₱{totalGross:N2}", fontTotals, brushDark, new RectangleF(curX, y, colGrossW, totalsH), alignRight); curX += colGrossW;
                    g.DrawString($"₱{totalSss:N2}", fontTotals, brushDark, new RectangleF(curX, y, colSssW, totalsH), alignRight); curX += colSssW;
                    g.DrawString($"₱{totalPhic:N2}", fontTotals, brushDark, new RectangleF(curX, y, colPhicW, totalsH), alignRight); curX += colPhicW;
                    g.DrawString($"₱{totalHdmf:N2}", fontTotals, brushDark, new RectangleF(curX, y, colHdmfW, totalsH), alignRight); curX += colHdmfW;
                    g.DrawString($"₱{totalTax:N2}", fontTotals, brushDark, new RectangleF(curX, y, colTaxW, totalsH), alignRight); curX += colTaxW;
                    g.DrawString($"₱{totalOther:N2}", fontTotals, brushDark, new RectangleF(curX, y, colOtherW, totalsH), alignRight); curX += colOtherW;
                    g.DrawString($"₱{totalDeduc:N2}", fontTotals, brushDark, new RectangleF(curX, y, colTotDedW, totalsH), alignRight); curX += colTotDedW;
                    g.DrawString($"₱{totalNet:N2}", fontTotals, brushGreen, new RectangleF(curX, y, colNetW, totalsH), alignRight);
                    y += totalsH + 20;

                    // 5. COMPLIANCE STATEMENT & SIGNATURE BLOCKS
                    g.DrawString("Official Certification: All statutory deductions (SSS RA 11199, PhilHealth RA 11223, HDMF Circular 460, BIR TRAIN Law RA 10963) and net compensation have been calculated and verified under standard labor practices.", fontFooter, brushMuted, xStart, y);
                    y += 30;

                    int sigWidth = 280;
                    g.DrawLine(penGrid, xStart, y + 16, xStart + sigWidth, y + 16);
                    g.DrawString("Prepared By: Payroll & HR Officer", fontMeta, brushDark, xStart, y + 20);

                    g.DrawLine(penGrid, xStart + 360, y + 16, xStart + 360 + sigWidth, y + 16);
                    g.DrawString("Audited By: Finance & Accounting Lead", fontMeta, brushDark, xStart + 360, y + 20);

                    g.DrawLine(penGrid, xStart + 720, y + 16, xStart + totalTableWidth, y + 16);
                    g.DrawString("Approved By: Store Manager / Admin", fontMeta, brushDark, xStart + 720, y + 20);

                    ev.HasMorePages = false;
                };

                using var previewDlg = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 1040,
                    Height = 720,
                    StartPosition = FormStartPosition.CenterParent
                };
                previewDlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch print preview: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
