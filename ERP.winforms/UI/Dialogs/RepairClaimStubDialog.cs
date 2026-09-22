using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class RepairClaimStubDialog : Form
    {
        private readonly RepairTicket _ticket;
        private readonly DataService _dataService = DataService.Instance;

        public RepairClaimStubDialog(RepairTicket ticket)
        {
            _ticket = ticket;
            Text = $"Repair Claim Stub - {_ticket.TicketNumber}";
            ClientSize = new Size(480, 680);
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
                Text = _dataService.CurrentCompany?.CompanyName ?? "Morphic Computers (Tenant B)",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, y),
                Size = new Size(440, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 28;


            Label lblTicketBadge = new Label
            {
                Text = $"JOB ORDER: {_ticket.TicketNumber}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = AppTheme.GoldPillBg,
                ForeColor = AppTheme.GoldPillText,
                Location = new Point(40, y),
                Size = new Size(400, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 44;

            Panel pnlLine1 = new Panel { Location = new Point(30, y), Size = new Size(420, 1), BackColor = Color.FromArgb(230, 226, 216) };
            y += 10;

            // Details table
            Panel pnlDetails = new Panel { Location = new Point(30, y), Size = new Size(420, 350) };
            int dy = 4;

            AddRow(pnlDetails, ref dy, "Date Registered:", _ticket.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy  hh:mm tt"));
            AddRow(pnlDetails, ref dy, "Customer Name:", _ticket.CustomerName);
            AddRow(pnlDetails, ref dy, "Contact Phone:", _ticket.CustomerPhone ?? "N/A");
            AddRow(pnlDetails, ref dy, "Device Category:", _ticket.DeviceType);
            AddRow(pnlDetails, ref dy, "Brand & Model:", _ticket.DeviceBrandModel);
            AddRow(pnlDetails, ref dy, "Serial Number:", _ticket.SerialNumber ?? "N/A");
            AddRow(pnlDetails, ref dy, "Assigned Tech:", _ticket.AssignedTechnician ?? "Lead Tech");
            AddRow(pnlDetails, ref dy, "Current Status:", _ticket.Status);
            AddRow(pnlDetails, ref dy, "Reported Issue:", _ticket.ReportedIssue);

            dy += 6;
            Panel pnlInnerLine = new Panel { Location = new Point(0, dy), Size = new Size(420, 1), BackColor = Color.FromArgb(230, 226, 216) };
            pnlDetails.Controls.Add(pnlInnerLine);
            dy += 10;

            AddRow(pnlDetails, ref dy, "Labor Service Fee:", $"PHP ₱{_ticket.LaborFee:N2}");
            AddRow(pnlDetails, ref dy, "Hardware Parts Cost:", $"PHP ₱{_ticket.PartsCost:N2}");
            AddRow(pnlDetails, ref dy, "Initial Deposit Paid:", $"PHP ₱{_ticket.DepositAmount:N2}");
            AddRow(pnlDetails, ref dy, "TOTAL ESTIMATE:", $"PHP ₱{_ticket.TotalAmount:N2}", true);
            AddRow(pnlDetails, ref dy, "BALANCE DUE:", $"PHP ₱{_ticket.BalanceDue:N2}", true);

            y += 360;

            Label lblDisclaimer = new Label
            {
                Text = "TERMS & CONDITIONS: Please present this claim stub upon claiming your device. " +
                       "30-day warranty applies exclusively to replaced hardware components and documented service labor. " +
                       "Units uncollected after 60 days of completion will be subject to storage fees.",
                Font = new Font("Segoe UI", 7F, FontStyle.Italic),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(30, y),
                Size = new Size(420, 55),
                TextAlign = ContentAlignment.TopCenter
            };
            y += 65;

            // Buttons
            SunshineButton btnPrint = new SunshineButton
            {
                Text = "🖨️ Print Claim Stub",
                IsPrimary = true,
                Location = new Point(245, y),
                Size = new Size(205, 42)
            };
            btnPrint.Click += (s, e) =>
            {
                MessageBox.Show($"Claim stub for {_ticket.TicketNumber} sent to receipt/thermal printer.", "Print Job Queued", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            };

            SunshineButton btnClose = new SunshineButton
            {
                Text = "Close",
                IsPrimary = false,
                Location = new Point(130, y),
                Size = new Size(105, 42)
            };
            btnClose.Click += (s, e) => Close();

            Controls.Add(lblCompany);
            Controls.Add(lblTicketBadge);
            Controls.Add(pnlLine1);
            Controls.Add(pnlDetails);
            Controls.Add(lblDisclaimer);
            Controls.Add(btnPrint);
            Controls.Add(btnClose);
        }

        private void AddRow(Panel container, ref int y, string label, string val, bool isBold = false)
        {
            Label lblKey = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8.5F, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isBold ? AppTheme.TextDark : AppTheme.TextMuted,
                Location = new Point(0, y),
                Size = new Size(160, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblVal = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 8.5F, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isBold ? (label.Contains("BALANCE") ? Color.FromArgb(184, 50, 38) : AppTheme.TextDark) : AppTheme.TextDark,
                Location = new Point(165, y),
                Size = new Size(255, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            container.Controls.Add(lblKey);
            container.Controls.Add(lblVal);
            y += 22;
        }
    }
}
