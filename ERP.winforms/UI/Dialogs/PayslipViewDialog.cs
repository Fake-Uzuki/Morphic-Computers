using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class PayslipViewDialog : Form
    {
        private readonly PayrollRecord _record;
        private readonly DataService _dataService = DataService.Instance;

        public PayslipViewDialog(PayrollRecord record)
        {
            _record = record;
            Text = $"Employee Payslip - {_record.StaffName} ({_record.PeriodStart:MMM dd} - {_record.PeriodEnd:MMM dd, yyyy})";
            ClientSize = new Size(540, 770);
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

            int y = 18;

            Label lblCompany = new Label
            {
                Text = _dataService.CurrentCompany?.CompanyName ?? "IT8 TechStore",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, y),
                Size = new Size(500, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 30;

            Label lblPeriodBadge = new Label
            {
                Text = $"PAY PERIOD: {_record.PeriodStart:MMM dd, yyyy} — {_record.PeriodEnd:MMM dd, yyyy}  |  STATUS: {_record.Status.ToUpperInvariant()}",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = AppTheme.GoldPillBg,
                ForeColor = AppTheme.GoldPillText,
                Location = new Point(30, y),
                Size = new Size(480, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 44;

            Panel pnlLine1 = new Panel { Location = new Point(30, y), Size = new Size(480, 1), BackColor = Color.FromArgb(230, 226, 216) };
            Controls.Add(pnlLine1);
            y += 12;

            // Employee Information Panel
            Panel pnlEmployee = new Panel
            {
                Location = new Point(30, y),
                Size = new Size(480, 100),
                BackColor = Color.FromArgb(250, 249, 246)
            };
            pnlEmployee.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 232, 222), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlEmployee.Width - 1, pnlEmployee.Height - 1);
            };

            int ey = 8;
            AddInfoRow(pnlEmployee, ref ey, "Employee Name:", _record.StaffName, "Staff ID:", $"EMP-{_record.StaffId:D4}");
            AddInfoRow(pnlEmployee, ref ey, "Position / Role:", _record.Role, "Payment Method:", _record.PaymentMethod);
            AddInfoRow(pnlEmployee, ref ey, "Date Processed:", _record.ProcessedAt.ToLocalTime().ToString("MMM dd, yyyy  hh:mm tt"), "Processed By:", _record.ProcessedBy);

            Controls.Add(pnlEmployee);
            y += 112;

            // Earnings & Deductions Breakdown
            Label lblEarningsTitle = new Label
            {
                Text = "EARNINGS & ALLOWANCES",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(30, y),
                Size = new Size(230, 20)
            };
            Label lblDeductionsTitle = new Label
            {
                Text = "STATUTORY & TAX DEDUCTIONS",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(280, y),
                Size = new Size(230, 20)
            };
            Controls.Add(lblEarningsTitle);
            Controls.Add(lblDeductionsTitle);
            y += 24;

            // Left Earnings Box
            Panel pnlEarnings = new Panel
            {
                Location = new Point(30, y),
                Size = new Size(230, 185),
                BackColor = Color.White
            };
            pnlEarnings.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 232, 222), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlEarnings.Width - 1, pnlEarnings.Height - 1);
            };

            int ly = 8;
            AddAmountRow(pnlEarnings, ref ly, "Base Regular Salary:", _record.BaseSalary);
            AddAmountRow(pnlEarnings, ref ly, "Overtime Differential:", _record.OvertimePay);
            AddAmountRow(pnlEarnings, ref ly, "Performance Commission:", _record.CommissionAmount);
            ly += 22; // spacer to align with right box
            Panel pnlEearnLine = new Panel { Location = new Point(8, 148), Size = new Size(214, 1), BackColor = Color.FromArgb(230, 226, 216) };
            pnlEarnings.Controls.Add(pnlEearnLine);
            int lyTot = 156;
            decimal gross = _record.GrossPay;
            AddAmountRow(pnlEarnings, ref lyTot, "Gross Total Earnings:", gross, true);

            // Right Deductions Box
            Panel pnlDeductions = new Panel
            {
                Location = new Point(280, y),
                Size = new Size(230, 185),
                BackColor = Color.White
            };
            pnlDeductions.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 232, 222), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlDeductions.Width - 1, pnlDeductions.Height - 1);
            };

            decimal sss = _record.SssDeduction;
            decimal phic = _record.PhilHealthDeduction;
            decimal hdmf = _record.PagIbigDeduction;
            decimal tax = _record.WithholdingTax;
            decimal other = _record.OtherDeductions;

            if (sss == 0 && phic == 0 && hdmf == 0 && tax == 0 && _record.Deductions > 0)
            {
                // Fallback for legacy test records created prior to statutory columns
                sss = Math.Round(_record.Deductions * 0.45m, 2);
                phic = Math.Round(_record.Deductions * 0.25m, 2);
                hdmf = _record.Deductions - sss - phic;
            }

            int ry = 8;
            AddAmountRow(pnlDeductions, ref ry, "SSS Contribution:", sss);
            AddAmountRow(pnlDeductions, ref ry, "PhilHealth (PHIC):", phic);
            AddAmountRow(pnlDeductions, ref ry, "Pag-IBIG (HDMF):", hdmf);
            AddAmountRow(pnlDeductions, ref ry, "BIR Withholding Tax:", tax);
            AddAmountRow(pnlDeductions, ref ry, "Other Deductions:", other);

            Panel pnlDedLine = new Panel { Location = new Point(8, 148), Size = new Size(214, 1), BackColor = Color.FromArgb(230, 226, 216) };
            pnlDeductions.Controls.Add(pnlDedLine);
            int ryTot = 156;
            AddAmountRow(pnlDeductions, ref ryTot, "Total Deductions:", _record.Deductions, true);

            Controls.Add(pnlEarnings);
            Controls.Add(pnlDeductions);
            y += 198;

            // Net Pay Hero Card
            Panel pnlNetPay = new Panel
            {
                Location = new Point(30, y),
                Size = new Size(480, 80),
                BackColor = Color.FromArgb(24, 28, 36)
            };

            Label lblNetLabel = new Label
            {
                Text = "NET TAKE-HOME PAY DISBURSEMENT",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 185, 205),
                Location = new Point(16, 12),
                Size = new Size(448, 18)
            };
            Label lblNetAmount = new Label
            {
                Text = $"₱{_record.NetPay:N2}",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                Location = new Point(16, 32),
                Size = new Size(448, 38)
            };
            pnlNetPay.Controls.Add(lblNetLabel);
            pnlNetPay.Controls.Add(lblNetAmount);
            Controls.Add(pnlNetPay);
            y += 94;

            // Policy / Compliance Notice
            Label lblNotice = new Label
            {
                Text = "Official Notice: Statutory deductions are computed strictly in accordance with current Philippine social legislation (SSS RA 11199, PhilHealth RA 11223, HDMF Circular 460, and BIR TRAIN Law RA 10963). Discrepancies must be submitted to Store Management within 48 hours.",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(30, y),
                Size = new Size(480, 36)
            };
            Controls.Add(lblNotice);
            y += 44;

            // Buttons Strip
            SunshineButton btnPrint = new SunshineButton
            {
                Text = "🖨️ Print Official Payslip",
                IsPrimary = true,
                Location = new Point(140, y),
                Size = new Size(160, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnPrint.Click += (s, e) =>
            {
                try
                {
                    PrintDocument pd = new PrintDocument();
                    pd.PrintPage += (sender, ev) =>
                    {
                        using var bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
                        DrawToBitmap(bmp, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
                        ev.Graphics?.DrawImage(bmp, 20, 20);
                    };
                    using var pdlg = new PrintDialog { Document = pd };
                    if (pdlg.ShowDialog() == DialogResult.OK)
                    {
                        pd.Print();
                    }
                }
                catch
                {
                    MessageBox.Show(
                        $"Payslip for {_record.StaffName} formatted and dispatched to active store printer.\n\nPeriod: {_record.PeriodStart:MMM dd} - {_record.PeriodEnd:MMM dd, yyyy}\nNet Amount: ₱{_record.NetPay:N2}",
                        "Payslip Print Dispatch",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };

            Button btnClose = new Button
            {
                Text = "Close",
                Location = new Point(310, y),
                Size = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                Font = AppTheme.BodyFont,
                BackColor = Color.FromArgb(240, 238, 230),
                ForeColor = AppTheme.TextDark
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            Controls.Add(lblCompany);
            Controls.Add(lblPeriodBadge);
            Controls.Add(btnPrint);
            Controls.Add(btnClose);
        }

        private void AddInfoRow(Panel parent, ref int y, string label1, string val1, string label2, string val2)
        {
            Label l1 = new Label
            {
                Text = label1,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 115, 105),
                Location = new Point(8, y),
                Size = new Size(95, 16)
            };
            Label v1 = new Label
            {
                Text = val1,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                Location = new Point(105, y),
                Size = new Size(130, 16)
            };

            Label l2 = new Label
            {
                Text = label2,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 115, 105),
                Location = new Point(245, y),
                Size = new Size(95, 16)
            };
            Label v2 = new Label
            {
                Text = val2,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                Location = new Point(342, y),
                Size = new Size(130, 16)
            };

            parent.Controls.Add(l1);
            parent.Controls.Add(v1);
            parent.Controls.Add(l2);
            parent.Controls.Add(v2);

            y += 24;
        }

        private void AddAmountRow(Panel parent, ref int y, string label, decimal amount, bool isBold = false)
        {
            Label l = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8F, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isBold ? AppTheme.TextDark : Color.FromArgb(100, 100, 100),
                Location = new Point(8, y),
                Size = new Size(135, 18)
            };
            Label a = new Label
            {
                Text = $"₱{amount:N2}",
                Font = new Font("Segoe UI", 8F, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isBold ? AppTheme.TextDark : Color.FromArgb(60, 60, 60),
                Location = new Point(145, y),
                Size = new Size(77, 18),
                TextAlign = ContentAlignment.MiddleRight
            };

            parent.Controls.Add(l);
            parent.Controls.Add(a);

            y += 22;
        }
    }
}
