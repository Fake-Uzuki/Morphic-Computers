using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class NewApprovalRequestDialog : Form
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly string _currentUser;

        private ComboBox _cboType = null!;
        private TextBox _txtTitle = null!;
        private NumericUpDown _numAmount = null!;
        private TextBox _txtReason = null!;

        public ApprovalRequest? CreatedRequest { get; private set; }

        public NewApprovalRequestDialog(string currentUser = "Staff")
        {
            _currentUser = currentUser;

            Text = "Submit Manager Approval Request";
            Size = new Size(520, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.AppBackground;

            InitializeForm();
        }

        private void InitializeForm()
        {
            Controls.Clear();

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = AppTheme.HeaderBg
            };

            Label lblTitle = new Label
            {
                Text = "📝 SUBMIT APPROVAL REQUEST",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 10),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = $"Requested by: {_currentUser} • Forwarded to Store Manager / Admin for review",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 168, 158),
                Location = new Point(20, 32),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Card Body
            SunshineCard card = new SunshineCard
            {
                Location = new Point(20, 75),
                Size = new Size(465, 340),
                Padding = new Padding(18),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            int y = 14;

            // Request Type
            card.Controls.Add(CreateLabel("REQUEST CATEGORY *", 15, y));
            card.Controls.Add(CreateLabel("AMOUNT INVOLVED (₱)", 240, y));
            y += 20;

            _cboType = new ComboBox
            {
                Location = new Point(15, y),
                Width = 210,
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboType.Items.AddRange(new object[] { "VoidTransaction", "CustomDiscount", "InventoryWriteOff", "WarrantyOverride" });
            _cboType.SelectedIndex = 0;

            _numAmount = new NumericUpDown
            {
                Location = new Point(240, y),
                Width = 210,
                DecimalPlaces = 2,
                Maximum = 1000000,
                Font = AppTheme.BodyFont,
                Value = 0
            };
            card.Controls.Add(_cboType);
            card.Controls.Add(_numAmount);
            y += 35;

            // Title
            card.Controls.Add(CreateLabel("REQUEST TITLE / SUBJECT *", 15, y));
            y += 20;
            _txtTitle = new TextBox { Location = new Point(15, y), Width = 435, Font = AppTheme.BodyFont };
            card.Controls.Add(_txtTitle);
            y += 35;

            // Reason & Justification
            card.Controls.Add(CreateLabel("JUSTIFICATION & OPERATIONAL REASON *", 15, y));
            y += 20;
            _txtReason = new TextBox
            {
                Location = new Point(15, y),
                Width = 435,
                Height = 110,
                Multiline = true,
                Font = AppTheme.BodyFont,
                ScrollBars = ScrollBars.Vertical
            };
            card.Controls.Add(_txtReason);

            // Bottom Buttons
            SunshineButton btnSubmit = new SunshineButton
            {
                Text = "Submit to Manager Queue",
                IsPrimary = true,
                Location = new Point(245, 430),
                Size = new Size(240, 42)
            };
            btnSubmit.Click += (s, e) => SubmitRequest();

            SunshineButton btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(125, 430),
                Size = new Size(110, 42)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(pnlHeader);
            Controls.Add(card);
            Controls.Add(btnSubmit);
            Controls.Add(btnCancel);
            AcceptButton = btnSubmit;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private void SubmitRequest()
        {
            string title = _txtTitle.Text.Trim();
            string reason = _txtReason.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a title for the approval request.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtTitle.Focus();
                return;
            }

            if (string.IsNullOrEmpty(reason))
            {
                MessageBox.Show("Please describe the operational reason for this request.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtReason.Focus();
                return;
            }

            var request = new ApprovalRequest
            {
                RequestType = _cboType.SelectedItem?.ToString() ?? "VoidTransaction",
                Title = title,
                ReasonDescription = reason,
                RequestedBy = _currentUser,
                RequestedAmount = _numAmount.Value,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _dataService.AddApprovalRequest(request);
            CreatedRequest = request;

            MessageBox.Show($"Approval ticket {request.RequestNumber} submitted to manager queue.", "Request Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
