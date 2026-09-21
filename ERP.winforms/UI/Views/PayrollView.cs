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
    public class PayrollView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridPayroll = null!;
        private TextBox _txtSearch = null!;
        private FlowLayoutPanel _flpFilterPills = null!;

        private string _currentRoleFilter = "All";

        public PayrollView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeLayout();
            _dataService.PayrollRecordsChanged += () =>
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

            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", FillWeight = 8, MinimumWidth = 50, Name = "ColId" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "EMPLOYEE NAME", FillWeight = 18, MinimumWidth = 140, Name = "ColName" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ROLE / DEPARTMENT", FillWeight = 16, MinimumWidth = 130, Name = "ColRole" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PAY PERIOD", FillWeight = 17, MinimumWidth = 130, Name = "ColPeriod" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BASE SALARY", FillWeight = 12, MinimumWidth = 90, Name = "ColBase" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "COMMISSION & OT", FillWeight = 14, MinimumWidth = 100, Name = "ColComm" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DEDUCTIONS", FillWeight = 11, MinimumWidth = 90, Name = "ColDeduc" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NET TAKE-HOME", FillWeight = 14, MinimumWidth = 100, Name = "ColNet" });
            _gridPayroll.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CHANNEL", FillWeight = 14, MinimumWidth = 100, Name = "ColChannel" });
            _gridPayroll.Columns.Add(new DataGridViewButtonColumn { HeaderText = "PAYSLIP", FillWeight = 9, MinimumWidth = 70, Text = "Payslip", UseColumnTextForButtonValue = true, Name = "ColPayslip" });
            _gridPayroll.Columns.Add(new DataGridViewButtonColumn { HeaderText = "VOID", FillWeight = 7, MinimumWidth = 60, Text = "Void", UseColumnTextForButtonValue = true, Name = "ColDelete" });

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

            _gridPayroll.Rows.Clear();
            foreach (var r in list)
            {
                int rowIdx = _gridPayroll.Rows.Add(
                    $"#PAY-{r.PayrollId:D3}",
                    r.StaffName,
                    r.Role,
                    $"{r.PeriodStart:MMM dd} - {r.PeriodEnd:MMM dd, yyyy}",
                    $"₱{r.BaseSalary:N2}",
                    $"₱{(r.CommissionAmount + r.OvertimePay):N2}",
                    $"₱{r.Deductions:N2}",
                    $"₱{r.NetPay:N2}",
                    r.PaymentMethod
                );

                var row = _gridPayroll.Rows[rowIdx];
                row.Tag = r.PayrollId;
                row.Cells["ColNet"].Style.ForeColor = Color.FromArgb(20, 130, 60);
                row.Cells["ColNet"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
        }
    }
}
