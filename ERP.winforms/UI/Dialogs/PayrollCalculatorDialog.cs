using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
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
        private NumericUpDown _numDeductions = null!;
        private ComboBox _cboPaymentMethod = null!;
        private Label _lblNetCalculated = null!;

        public PayrollCalculatorDialog()
        {
            Text = "Store Payroll Calculator & Compensation Processor";
            ClientSize = new Size(520, 620);
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
                Size = new Size(470, 26)
            };
            y += 28;


            Panel pnlDivider = new Panel { Location = new Point(24, y), Size = new Size(470, 1), BackColor = Color.FromArgb(230, 226, 216) };
            Controls.Add(pnlDivider);
            y += 14;

            // 1. Staff Selector
            Label lblStaff = new Label { Text = "Staff Member *", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(200, 18) };
            y += 20;
            _cboStaff = new ComboBox
            {
                Location = new Point(24, y),
                Size = new Size(470, 28),
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
                Size = new Size(225, 26),
                Format = DateTimePickerFormat.Short,
                Value = startDefault,
                Font = AppTheme.BodyFont
            };

            _dtpEnd = new DateTimePicker
            {
                Location = new Point(269, y),
                Size = new Size(225, 26),
                Format = DateTimePickerFormat.Short,
                Value = startDefault.AddDays(14),
                Font = AppTheme.BodyFont
            };

            Controls.Add(lblDates);
            Controls.Add(_dtpStart);
            Controls.Add(_dtpEnd);
            y += 36;

            // 3. Financial Inputs (Base & Overtime)
            Label lblBase = new Label { Text = "Base Pay / Salary (₱)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(220, 18) };
            Label lblOvertime = new Label { Text = "Overtime Differential (₱)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(269, y), Size = new Size(220, 18) };
            y += 20;

            _numBase = new NumericUpDown
            {
                Location = new Point(24, y),
                Size = new Size(225, 26),
                DecimalPlaces = 2,
                Maximum = 1000000m,
                Value = staffList.Count > 0 ? (staffList[0].MonthlySalary > 0 ? staffList[0].MonthlySalary : 20000m) : 20000m,
                Font = AppTheme.BodyFont
            };
            _numBase.ValueChanged += (s, e) => UpdateNetCalculation();

            _numOvertime = new NumericUpDown
            {
                Location = new Point(269, y),
                Size = new Size(225, 26),
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

            // 4. Commissions & Deductions
            Label lblComm = new Label { Text = "Sales & Tech Commission (₱)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(220, 18) };
            Label lblDeduc = new Label { Text = "Statutory & Tax Deductions (₱)", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(269, y), Size = new Size(220, 18) };
            y += 20;

            _numCommission = new NumericUpDown
            {
                Location = new Point(24, y),
                Size = new Size(225, 26),
                DecimalPlaces = 2,
                Maximum = 500000m,
                Value = 1500m,
                Font = AppTheme.BodyFont
            };
            _numCommission.ValueChanged += (s, e) => UpdateNetCalculation();

            _numDeductions = new NumericUpDown
            {
                Location = new Point(269, y),
                Size = new Size(225, 26),
                DecimalPlaces = 2,
                Maximum = 500000m,
                Value = 1200m,
                Font = AppTheme.BodyFont
            };
            _numDeductions.ValueChanged += (s, e) => UpdateNetCalculation();

            Controls.Add(lblComm);
            Controls.Add(lblDeduc);
            Controls.Add(_numCommission);
            Controls.Add(_numDeductions);
            y += 36;

            // 5. Payment Method
            Label lblMethod = new Label { Text = "Disbursement Channel *", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(24, y), Size = new Size(220, 18) };
            y += 20;

            _cboPaymentMethod = new ComboBox
            {
                Location = new Point(24, y),
                Size = new Size(470, 28),
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
            y += 38;

            // 6. Net Take-Home Calculated Banner
            Panel pnlNet = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(470, 72),
                BackColor = Color.FromArgb(24, 28, 36)
            };

            Label lblNetHdr = new Label
            {
                Text = "NET TAKE-HOME COMPENSATION",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 175, 195),
                Location = new Point(14, 10),
                Size = new Size(440, 16)
            };

            _lblNetCalculated = new Label
            {
                Text = "₱0.00",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                Location = new Point(14, 26),
                Size = new Size(440, 36)
            };

            pnlNet.Controls.Add(lblNetHdr);
            pnlNet.Controls.Add(_lblNetCalculated);
            Controls.Add(pnlNet);
            y += 86;

            // Buttons
            SunshineButton btnSave = new SunshineButton
            {
                Text = "✓ Process & Disburse Payroll",
                IsPrimary = true,
                Location = new Point(24, y),
                Size = new Size(260, 38),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSave.Click += OnProcessPayroll;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(296, y),
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

        private void UpdateNetCalculation()
        {
            decimal net = (_numBase.Value + _numOvertime.Value + _numCommission.Value) - _numDeductions.Value;
            _lblNetCalculated.Text = $"₱{net:N2}";
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
                Deductions = _numDeductions.Value,
                Status = "Paid",
                PaymentMethod = _cboPaymentMethod.SelectedItem?.ToString() ?? "Bank Transfer",
                ProcessedAt = DateTime.UtcNow,
                ProcessedBy = "Store Manager"
            };

            _dataService.AddPayrollRecord(record);

            MessageBox.Show(
                $"Payroll compensation successfully disbursed for {record.StaffName}!\n\nNet Amount: ₱{record.NetPay:N2}\nChannel: {record.PaymentMethod}",
                "Disbursement Recorded",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
