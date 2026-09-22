using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class ApplyDiscountDialog : Form
    {
        private readonly decimal _subtotal;
        private NumericUpDown _numPercent = null!;
        private NumericUpDown _numFixedAmount = null!;
        private RadioButton _rbPercent = null!;
        private RadioButton _rbFixed = null!;
        private ComboBox _cboReason = null!;
        private TextBox _txtManagerPassword = null!;
        private Label _lblDiscountPreview = null!;
        private Label _lblNewTotalPreview = null!;

        public decimal DiscountAmount { get; private set; }
        public string DiscountReason { get; private set; } = string.Empty;

        public ApplyDiscountDialog(decimal subtotal, decimal currentDiscount = 0)
        {
            _subtotal = Math.Max(0, subtotal);
            DiscountAmount = currentDiscount;

            Text = "Manager Discount Authorization - Morphic POS";
            ClientSize = new Size(480, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            InitializeLayout();
            UpdateCalculations();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // 1. Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = AppTheme.HeaderBg
            };

            Label lblTitle = new Label
            {
                Text = "🏷️ MANAGER DISCOUNT AUTHORIZATION",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 10),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Discounts and promotional rate overrides require manager credential verification.",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 168, 158),
                Location = new Point(20, 34),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // 2. Main Content Card
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };

            int y = 14;

            // Subtotal row
            Label lblSubtotalLabel = new Label
            {
                Text = "CART ORDER SUBTOTAL:",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, y),
                AutoSize = true
            };
            Label lblSubtotalValue = new Label
            {
                Text = $"₱{_subtotal:N2}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(200, y - 3),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblSubtotalLabel);
            pnlBody.Controls.Add(lblSubtotalValue);
            y += 32;

            // Separator
            Panel sep1 = new Panel { Location = new Point(24, y), Size = new Size(432, 1), BackColor = Color.FromArgb(235, 230, 220) };
            pnlBody.Controls.Add(sep1);
            y += 12;

            // Discount Type Option
            Label lblType = new Label { Text = "DISCOUNT SPECIFICATION:", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(24, y), AutoSize = true };
            pnlBody.Controls.Add(lblType);
            y += 22;

            _rbPercent = new RadioButton { Text = "Percentage (%)", Checked = true, Location = new Point(28, y), AutoSize = true, Font = AppTheme.BodyFont };
            _rbFixed = new RadioButton { Text = "Fixed Amount (₱)", Checked = false, Location = new Point(170, y), AutoSize = true, Font = AppTheme.BodyFont };
            _rbPercent.CheckedChanged += (s, e) => ToggleDiscountType();
            pnlBody.Controls.Add(_rbPercent);
            pnlBody.Controls.Add(_rbFixed);
            y += 28;

            _numPercent = new NumericUpDown
            {
                Location = new Point(28, y),
                Width = 120,
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 1,
                Value = 10,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            _numPercent.ValueChanged += (s, e) => UpdateCalculations();

            _numFixedAmount = new NumericUpDown
            {
                Location = new Point(170, y),
                Width = 140,
                Minimum = 0,
                Maximum = Math.Max(100000, _subtotal),
                DecimalPlaces = 2,
                Value = 0,
                Enabled = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            _numFixedAmount.ValueChanged += (s, e) => UpdateCalculations();

            // Quick preset pills
            int px = 320;
            int[] presets = { 5, 10, 15, 20 };
            foreach (int p in presets)
            {
                Button btnP = new Button
                {
                    Text = $"{p}%",
                    Size = new Size(32, 26),
                    Location = new Point(px, y),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                    BackColor = Color.FromArgb(246, 243, 236),
                    Cursor = Cursors.Hand
                };
                btnP.FlatAppearance.BorderColor = Color.FromArgb(220, 215, 205);
                btnP.Click += (s, e) =>
                {
                    _rbPercent.Checked = true;
                    _numPercent.Value = p;
                };
                pnlBody.Controls.Add(btnP);
                px += 35;
            }

            pnlBody.Controls.Add(_numPercent);
            pnlBody.Controls.Add(_numFixedAmount);
            y += 38;

            // Reason Dropdown
            Label lblReason = new Label { Text = "REASON / DISCOUNT CLASSIFICATION:", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(24, y), AutoSize = true };
            pnlBody.Controls.Add(lblReason);
            y += 20;

            _cboReason = new ComboBox
            {
                Location = new Point(28, y),
                Width = 424,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = AppTheme.BodyFont
            };
            _cboReason.Items.AddRange(new object[]
            {
                "VIP / Loyalty Customer Discount",
                "Senior Citizen / PWD Statutory Discount",
                "Promotional Clearance / Sale Promo",
                "Store Manager Discretionary Courtesy",
                "Damaged Box / Display Unit Allowance",
                "Staff & Associate Purchase Privilege"
            });
            _cboReason.SelectedIndex = 0;
            pnlBody.Controls.Add(_cboReason);
            y += 36;

            // Calculation Preview Card
            Panel pnlPreview = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(428, 54),
                BackColor = Color.FromArgb(254, 250, 240)
            };
            pnlPreview.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 215, 170), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlPreview.Width - 1, pnlPreview.Height - 1);
            };

            _lblDiscountPreview = new Label
            {
                Text = "Discount Deduction: -₱0.00",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(184, 50, 38),
                Location = new Point(14, 8),
                AutoSize = true
            };
            _lblNewTotalPreview = new Label
            {
                Text = "Net Subtotal (Before Tax): ₱0.00",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 122, 79),
                Location = new Point(14, 28),
                AutoSize = true
            };
            pnlPreview.Controls.Add(_lblDiscountPreview);
            pnlPreview.Controls.Add(_lblNewTotalPreview);
            pnlBody.Controls.Add(pnlPreview);
            y += 66;

            // Manager Password Input
            Label lblPw = new Label
            {
                Text = "MANAGER'S AUTHORIZATION PASSWORD *",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(184, 50, 38),
                Location = new Point(24, y),
                AutoSize = true
            };
            pnlBody.Controls.Add(lblPw);
            y += 20;

            _txtManagerPassword = new TextBox
            {
                Location = new Point(28, y),
                Width = 424,
                Font = new Font("Segoe UI", 10F),
                UseSystemPasswordChar = true,
                PlaceholderText = "Input manager password for override confirmation..."
            };
            pnlBody.Controls.Add(_txtManagerPassword);
            y += 28;

            Label lblPwHelp = new Label
            {
                Text = "💡 Authorized manager passwords: 09092121 (Admin), admin123, or manager123",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(130, 95, 10),
                Location = new Point(28, y),
                Size = new Size(424, 18)
            };
            pnlBody.Controls.Add(lblPwHelp);
            y += 26;

            // Bottom Action Buttons
            SunshineButton btnApply = new SunshineButton
            {
                Text = "🔓 Authorize & Apply Discount",
                IsPrimary = true,
                Location = new Point(220, y),
                Size = new Size(232, 40),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnApply.Click += (s, e) => ConfirmAuthorization();

            SunshineButton btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(110, y),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlBody.Controls.Add(btnApply);
            pnlBody.Controls.Add(btnCancel);

            Controls.Add(pnlBody);
            Controls.Add(pnlHeader);
            AcceptButton = btnApply;
        }

        private void ToggleDiscountType()
        {
            if (_rbPercent.Checked)
            {
                _numPercent.Enabled = true;
                _numFixedAmount.Enabled = false;
            }
            else
            {
                _numPercent.Enabled = false;
                _numFixedAmount.Enabled = true;
            }
            UpdateCalculations();
        }

        private void UpdateCalculations()
        {
            decimal deduction = 0;
            if (_rbPercent.Checked)
            {
                deduction = Math.Round(_subtotal * (_numPercent.Value / 100m), 2);
            }
            else
            {
                deduction = Math.Min(_subtotal, _numFixedAmount.Value);
            }

            decimal net = Math.Max(0, _subtotal - deduction);
            if (_lblDiscountPreview != null) _lblDiscountPreview.Text = $"Discount Deduction: -₱{deduction:N2}";
            if (_lblNewTotalPreview != null) _lblNewTotalPreview.Text = $"Net Subtotal (Before Tax): ₱{net:N2}";
        }

        private void ConfirmAuthorization()
        {
            string pw = _txtManagerPassword.Text.Trim();
            if (string.IsNullOrEmpty(pw))
            {
                MessageBox.Show("Please enter the manager's authorization password.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtManagerPassword.Focus();
                return;
            }

            // Verify manager credential
            bool isAuthorized = pw == "09092121" || pw == "admin123" || pw == "manager123" || pw == "admin" || pw == "manager";

            if (!isAuthorized)
            {
                var mgrResult = OfflineAuthService.Instance.ValidateOfflineLogin("Tenant A", "manager", pw);
                var adminResult = OfflineAuthService.Instance.ValidateOfflineLogin("Tenant A", "admin", pw);
                var cirunayResult = OfflineAuthService.Instance.ValidateOfflineLogin("Tenant B", "cirunay", pw);
                var mgrB = OfflineAuthService.Instance.ValidateOfflineLogin("Tenant B", "manager", pw);
                var adminB = OfflineAuthService.Instance.ValidateOfflineLogin("Tenant B", "admin", pw);
                isAuthorized = mgrResult.Success || adminResult.Success || cirunayResult.Success || mgrB.Success || adminB.Success;
            }

            if (!isAuthorized)
            {
                MessageBox.Show("Manager authorization failed. The password provided is incorrect.", "Authorization Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtManagerPassword.SelectAll();
                _txtManagerPassword.Focus();
                return;
            }

            // Calculate final deduction
            if (_rbPercent.Checked)
            {
                DiscountAmount = Math.Round(_subtotal * (_numPercent.Value / 100m), 2);
            }
            else
            {
                DiscountAmount = Math.Min(_subtotal, _numFixedAmount.Value);
            }

            DiscountReason = _cboReason.SelectedItem?.ToString() ?? "Manager Authorized Discount";

            // Log approved discount to Approvals module
            DataService.Instance.ApprovalRequests.Insert(0, new domain.entities.ApprovalRequest
            {
                RequestId = (DataService.Instance.ApprovalRequests.Count > 0 ? DataService.Instance.ApprovalRequests.Max(r => r.RequestId) : 0) + 1,
                CompanyId = DataService.Instance.ActiveCompanyId,
                RequestNumber = $"DISC-{DateTime.Now:yyMMdd}-{new Random().Next(100, 999)}",
                RequestType = "POS Discount Override",
                Title = $"Discount Authorized: ₱{DiscountAmount:N2} ({DiscountReason})",
                RequestedAmount = DiscountAmount,
                RequestedBy = "POS Cashier",
                ReviewedBy = "Manager Override Password",
                Status = "Approved",
                ReasonDescription = $"Manager authorized {DiscountReason} deduction of ₱{DiscountAmount:N2} on order subtotal ₱{_subtotal:N2}.",
                ReviewNotes = "Manager credential verified and override authorized.",
                CreatedAt = DateTime.UtcNow,
                ResolvedAt = DateTime.UtcNow
            });
            DataService.Instance.SaveApprovalsToLocalCache();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
