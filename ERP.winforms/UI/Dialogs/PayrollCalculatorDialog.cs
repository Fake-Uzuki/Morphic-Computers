using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.domain.services;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class PayrollCalculatorDialog : Form
    {
        private readonly DataService _dataService = DataService.Instance;

        private ComboBox _cboStaff = null!;
        private DateTimePicker _dtpStart = null!;
        private DateTimePicker _dtpEnd = null!;
        private NumericUpDown _numBase = null!;
        private NumericUpDown _numOvertime = null!;
        private NumericUpDown _numCommission = null!;
        private NumericUpDown _numOtherDeductions = null!;
        private ComboBox _cboPaymentMethod = null!;

        // Statutory breakdown preview labels
        private Label _lblSssVal = null!;
        private Label _lblPhicVal = null!;
        private Label _lblHdmfVal = null!;
        private Label _lblTaxVal = null!;
        private Label _lblTotalDedVal = null!;
        private Label _lblGrossVal = null!;
        private Label _lblNetCalculated = null!;

        public PayrollCalculatorDialog()
        {
            Text = "Store Payroll Calculator & Statutory Compensation Processor";
            ClientSize = new Size(540, 720);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            InitializeForm();
        }

        private void InitializeForm()
        {
            Controls.Clear();

            int y = 16;

            Label lblTitle = new Label
            {
                Text = "Calculate & Disburse Staff Compensation",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, y),
                Size = new Size(490, 26)
            };
            y += 28;

            Panel pnlDivider = new Panel { Location = new Point(24, y), Size = new Size(490, 1), BackColor = Color.FromArgb(230, 226, 216) };
            Controls.Add(pnlDivider);
            y += 14;

            // 1. Staff Selector
            Label lblStaff = new Label { Text = "Staff Member *", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(200, 18) };
            y += 20;
            _cboStaff = new ComboBox
            {
                Location = new Point(24, y),
                Size = new Size(490, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = AppTheme.BodyFont
            };

            var staffList = _dataService.StaffMembers.Where(s => s.IsActive).ToList();
            foreach (var staff in staffList)
            {
                _cboStaff.Items.Add($"{staff.FullName}  ({staff.Role} - {staff.PositionTitle})");
            }
            if (_cboStaff.Items.Count > 0) _cboStaff.SelectedIndex = 0;

            _cboStaff.SelectedIndexChanged += (s, e) =>
            {
                if (_cboStaff.SelectedIndex >= 0 && _cboStaff.SelectedIndex < staffList.Count)
                {
                    var sel = staffList[_cboStaff.SelectedIndex];
                    _numBase.Value = Math.Max(0, sel.MonthlySalary > 0 ? sel.MonthlySalary : 18000m);
                    UpdateNetCalculation();
                }
            };

            Controls.Add(lblStaff);
            Controls.Add(_cboStaff);
            y += 36;

            // 2. Pay Period Dates (Start & End)
            Label lblDates = new Label { Text = "Pay Period Range (Bi-Monthly or Monthly) *", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(300, 18) };
            y += 20;

            var startDefault = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            _dtpStart = new DateTimePicker
            {
                Location = new Point(24, y),
                Size = new Size(235, 26),
                Format = DateTimePickerFormat.Short,
                Value = startDefault,
                Font = AppTheme.BodyFont
            };

            _dtpEnd = new DateTimePicker
            {
                Location = new Point(279, y),
                Size = new Size(235, 26),
                Format = DateTimePickerFormat.Short,
                Value = startDefault.AddMonths(1).AddDays(-1),
                Font = AppTheme.BodyFont
            };

            Controls.Add(lblDates);
            Controls.Add(_dtpStart);
            Controls.Add(_dtpEnd);
            y += 36;

            // 3. Financial Inputs (Base & Overtime)
            Label lblBase = new Label { Text = "Base Monthly Pay / Salary (₱) *", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(230, 18) };
            Label lblOvertime = new Label { Text = "Overtime Differential (₱)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(279, y), Size = new Size(230, 18) };
            y += 20;

            _numBase = new NumericUpDown
            {
                Location = new Point(24, y),
                Size = new Size(235, 26),
                DecimalPlaces = 2,
                Maximum = 1000000m,
                Value = staffList.Count > 0 ? (staffList[0].MonthlySalary > 0 ? staffList[0].MonthlySalary : 20000m) : 20000m,
                Font = AppTheme.BodyFont
            };
            _numBase.ValueChanged += (s, e) => UpdateNetCalculation();

            _numOvertime = new NumericUpDown
            {
                Location = new Point(279, y),
                Size = new Size(235, 26),
                DecimalPlaces = 2,
                Maximum = 500000m,
                Value = 0m,
                Font = AppTheme.BodyFont
            };
            _numOvertime.ValueChanged += (s, e) => UpdateNetCalculation();

            Controls.Add(lblBase);
            Controls.Add(lblOvertime);
            Controls.Add(_numBase);
            Controls.Add(_numOvertime);
            y += 36;

            // 4. Commissions & Other Deductions
            Label lblComm = new Label { Text = "Sales & Tech Commission (₱)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(230, 18) };
            Label lblOtherDed = new Label { Text = "Other Voluntary Deductions / Advances (₱)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(279, y), Size = new Size(235, 18) };
            y += 20;

            _numCommission = new NumericUpDown
            {
                Location = new Point(24, y),
                Size = new Size(235, 26),
                DecimalPlaces = 2,
                Maximum = 500000m,
                Value = 0m,
                Font = AppTheme.BodyFont
            };
            _numCommission.ValueChanged += (s, e) => UpdateNetCalculation();

            _numOtherDeductions = new NumericUpDown
            {
                Location = new Point(279, y),
                Size = new Size(235, 26),
                DecimalPlaces = 2,
                Maximum = 500000m,
                Value = 0m,
                Font = AppTheme.BodyFont
            };
            _numOtherDeductions.ValueChanged += (s, e) => UpdateNetCalculation();

            Controls.Add(lblComm);
            Controls.Add(lblOtherDed);
            Controls.Add(_numCommission);
            Controls.Add(_numOtherDeductions);
            y += 36;

            // 5. Philippine Statutory Deductions Real-Time Breakdown Card
            Label lblStatutoryHdr = new Label
            {
                Text = "Philippine Statutory Deductions & Tax Preview (Auto-Calculated)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 85, 105),
                Location = new Point(24, y),
                Size = new Size(490, 18)
            };
            y += 20;

            Panel pnlStatutory = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(490, 96),
                BackColor = Color.FromArgb(248, 249, 251)
            };
            pnlStatutory.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(225, 230, 238), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlStatutory.Width - 1, pnlStatutory.Height - 1);
            };

            // Grid inside panel: Row 1 (Gross, SSS, PhilHealth), Row 2 (Pag-IBIG, W/Tax, Total Deductions)
            int colW = 158;
            AddPreviewItem(pnlStatutory, 8, 8, "GROSS COMPENSATION", out _lblGrossVal, colW);
            AddPreviewItem(pnlStatutory, 8 + colW, 8, "SSS (RA 11199)", out _lblSssVal, colW);
            AddPreviewItem(pnlStatutory, 8 + colW * 2, 8, "PHILHEALTH (RA 11223)", out _lblPhicVal, colW);

            AddPreviewItem(pnlStatutory, 8, 50, "PAG-IBIG (HDMF 460)", out _lblHdmfVal, colW);
            AddPreviewItem(pnlStatutory, 8 + colW, 50, "WITHHOLDING TAX (TRAIN)", out _lblTaxVal, colW);
            AddPreviewItem(pnlStatutory, 8 + colW * 2, 50, "TOTAL DEDUCTIONS", out _lblTotalDedVal, colW, isBold: true);

            Controls.Add(lblStatutoryHdr);
            Controls.Add(pnlStatutory);
            y += 104;

            // 6. Payment Method
            Label lblMethod = new Label { Text = "Disbursement Channel *", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(220, 18) };
            y += 20;

            _cboPaymentMethod = new ComboBox
            {
                Location = new Point(24, y),
                Size = new Size(490, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = AppTheme.BodyFont
            };
            _cboPaymentMethod.Items.AddRange(new object[]
            {
                "Bank Transfer (BDO Unibank)",
                "Bank Transfer (BPI)",
                "Bank Transfer (Metrobank)",
                "Cash Counter Envelope",
                "GCash Corporate Disbursement",
                "Maya Enterprise"
            });
            _cboPaymentMethod.SelectedIndex = 0;

            Controls.Add(lblMethod);
            Controls.Add(_cboPaymentMethod);
            y += 36;

            // 7. Net Take-Home Calculated Banner
            Panel pnlNet = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(490, 68),
                BackColor = Color.FromArgb(24, 28, 36)
            };

            Label lblNetHdr = new Label
            {
                Text = "NET TAKE-HOME COMPENSATION (AFTER STATUTORY & TAX DEDUCTIONS)",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 175, 195),
                Location = new Point(14, 8),
                Size = new Size(460, 16)
            };

            _lblNetCalculated = new Label
            {
                Text = "₱0.00",
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                Location = new Point(14, 24),
                Size = new Size(460, 36)
            };

            pnlNet.Controls.Add(lblNetHdr);
            pnlNet.Controls.Add(_lblNetCalculated);
            Controls.Add(pnlNet);
            y += 78;

            // Buttons
            SunshineButton btnSave = new SunshineButton
            {
                Text = "✓ Process & Disburse Payroll",
                IsPrimary = true,
                Location = new Point(24, y),
                Size = new Size(270, 38),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSave.Click += OnProcessPayroll;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(306, y),
                Size = new Size(100, 38),
                FlatStyle = FlatStyle.Flat,
                Font = AppTheme.BodyFont,
                BackColor = Color.FromArgb(240, 238, 230),
                ForeColor = AppTheme.TextDark
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.Add(lblTitle);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);

            UpdateNetCalculation();
        }

        private void AddPreviewItem(Panel parent, int x, int y, string label, out Label valLabel, int width, bool isBold = false)
        {
            Label lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 130, 145),
                Location = new Point(x, y),
                Size = new Size(width - 8, 14)
            };
            valLabel = new Label
            {
                Text = "₱0.00",
                Font = new Font("Segoe UI", 9.5F, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isBold ? Color.FromArgb(180, 40, 30) : AppTheme.TextDark,
                Location = new Point(x, y + 15),
                Size = new Size(width - 8, 20)
            };
            parent.Controls.Add(lbl);
            parent.Controls.Add(valLabel);
        }

        private void UpdateNetCalculation()
        {
            var calc = PayrollCalculationService.Calculate(
                _numBase.Value,
                _numOvertime.Value,
                _numCommission.Value,
                _numOtherDeductions.Value);

            if (_lblGrossVal != null) _lblGrossVal.Text = $"₱{calc.GrossPay:N2}";
            if (_lblSssVal != null) _lblSssVal.Text = $"₱{calc.SssDeduction:N2}";
            if (_lblPhicVal != null) _lblPhicVal.Text = $"₱{calc.PhilHealthDeduction:N2}";
            if (_lblHdmfVal != null) _lblHdmfVal.Text = $"₱{calc.PagIbigDeduction:N2}";
            if (_lblTaxVal != null) _lblTaxVal.Text = $"₱{calc.WithholdingTax:N2}";
            if (_lblTotalDedVal != null) _lblTotalDedVal.Text = $"₱{calc.TotalDeductions:N2}";
            if (_lblNetCalculated != null) _lblNetCalculated.Text = $"₱{calc.NetPay:N2}";
        }

        private void OnProcessPayroll(object? sender, EventArgs e)
        {
            var staffList = _dataService.StaffMembers.Where(s => s.IsActive).ToList();
            if (_cboStaff.SelectedIndex < 0 || _cboStaff.SelectedIndex >= staffList.Count)
            {
                MessageBox.Show("Please select an active staff member.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var staff = staffList[_cboStaff.SelectedIndex];

            var calc = PayrollCalculationService.Calculate(
                _numBase.Value,
                _numOvertime.Value,
                _numCommission.Value,
                _numOtherDeductions.Value);

            var record = new PayrollRecord
            {
                CompanyId = _dataService.ActiveCompanyId,
                StaffId = staff.StaffId,
                StaffName = staff.FullName,
                Role = staff.Role,
                PeriodStart = _dtpStart.Value.Date,
                PeriodEnd = _dtpEnd.Value.Date,
                BaseSalary = _numBase.Value,
                OvertimePay = _numOvertime.Value,
                CommissionAmount = _numCommission.Value,
                SssDeduction = calc.SssDeduction,
                PhilHealthDeduction = calc.PhilHealthDeduction,
                PagIbigDeduction = calc.PagIbigDeduction,
                WithholdingTax = calc.WithholdingTax,
                OtherDeductions = calc.OtherDeductions,
                Deductions = calc.TotalDeductions,
                Status = "Paid",
                PaymentMethod = _cboPaymentMethod.SelectedItem?.ToString() ?? "Bank Transfer",
                ProcessedAt = DateTime.UtcNow,
                ProcessedBy = "Store Manager"
            };

            bool ok = _dataService.AddPayrollRecord(record);
            if (!ok)
            {
                MessageBox.Show("Failed to record payroll disbursement. Please check network/database connectivity or plan permissions.", "Disbursement Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(
                $"Payroll compensation successfully disbursed for {record.StaffName}!\n\n" +
                $"Gross Pay: ₱{record.GrossPay:N2}\n" +
                $"SSS: ₱{record.SssDeduction:N2}\n" +
                $"PhilHealth: ₱{record.PhilHealthDeduction:N2}\n" +
                $"Pag-IBIG: ₱{record.PagIbigDeduction:N2}\n" +
                $"Withholding Tax: ₱{record.WithholdingTax:N2}\n" +
                $"Total Deductions: ₱{record.Deductions:N2}\n" +
                $"Net Take-Home: ₱{record.NetPay:N2}\n" +
                $"Channel: {record.PaymentMethod}",
                "Disbursement Recorded",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
