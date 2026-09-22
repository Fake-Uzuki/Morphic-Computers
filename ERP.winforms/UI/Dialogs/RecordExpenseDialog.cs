using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class RecordExpenseDialog : Form
    {
        private readonly string _currentUser;
        private ComboBox _cboCategory = null!;
        private NumericUpDown _numAmount = null!;
        private TextBox _txtDescription = null!;
        private TextBox _txtPaidTo = null!;
        private ComboBox _cboPaymentMethod = null!;
        private TextBox _txtReceiptRef = null!;

        public ExpenseRecord? CreatedExpense { get; private set; }

        public RecordExpenseDialog(string currentUser = "Admin")
        {
            _currentUser = currentUser;

            Text = "Record Operating Expense - Morphic Computers";
            ClientSize = new Size(520, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // 1. Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Color.FromArgb(24, 25, 20)
            };

            Label lblTitle = new Label
            {
                Text = "💸 RECORD STORE OPERATING EXPENSE",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 10),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = $"Recorded by: {_currentUser}  •  Deducted from Store Gross Margin in Monthly P&L",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 168, 158),
                Location = new Point(20, 34),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // 2. Form Body
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };

            int y = 14;

            // Category & Amount
            Label lblCat = new Label { Text = "EXPENSE CLASSIFICATION *", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            Label lblAmt = new Label { Text = "AMOUNT (PHP) *", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(184, 50, 38), Location = new Point(270, y), AutoSize = true };
            y += 20;

            _cboCategory = new ComboBox
            {
                Location = new Point(20, y),
                Size = new Size(235, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            _cboCategory.Items.AddRange(new object[]
            {
                "Store Commercial Rent",
                "Electricity & Power",
                "Internet & Telecom",
                "Repair Bench Supplies",
                "Store Packaging & Supplies",
                "Hardware Tools & Equipment",
                "Marketing & Promotion",
                "Logistics & Courier Delivery",
                "Miscellaneous Operational Expense"
            });
            _cboCategory.SelectedIndex = 0;

            _numAmount = new NumericUpDown
            {
                Location = new Point(270, y),
                Size = new Size(225, 28),
                Minimum = 1,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Value = 1000,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            pnlBody.Controls.Add(lblCat);
            pnlBody.Controls.Add(_cboCategory);
            pnlBody.Controls.Add(lblAmt);
            pnlBody.Controls.Add(_numAmount);
            y += 40;

            // Paid To / Vendor
            Label lblVendor = new Label { Text = "BENEFICIARY / VENDOR / PAYEE *", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            y += 20;

            _txtPaidTo = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(475, 28),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "e.g. Davao Light & Power Co., Mindanao Mall Property, etc."
            };
            pnlBody.Controls.Add(lblVendor);
            pnlBody.Controls.Add(_txtPaidTo);
            y += 40;

            // Description / Justification
            Label lblDesc = new Label { Text = "EXPENSE DESCRIPTION & OPERATIONAL PURPOSE *", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            y += 20;

            _txtDescription = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(475, 54),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Describe the business necessity of this expenditure..."
            };
            pnlBody.Controls.Add(lblDesc);
            pnlBody.Controls.Add(_txtDescription);
            y += 66;

            // Payment Method & Receipt Reference
            Label lblPay = new Label { Text = "PAYMENT CHANNEL *", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            Label lblRef = new Label { Text = "OFFICIAL RECEIPT / INVOICE REF #", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(270, y), AutoSize = true };
            y += 20;

            _cboPaymentMethod = new ComboBox
            {
                Location = new Point(20, y),
                Size = new Size(235, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            _cboPaymentMethod.Items.AddRange(new object[] { "Cash", "Bank Transfer", "Online Banking", "Company Check" });
            _cboPaymentMethod.SelectedIndex = 0;

            _txtReceiptRef = new TextBox
            {
                Location = new Point(270, y),
                Size = new Size(225, 28),
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "e.g. OR-99214"
            };
            pnlBody.Controls.Add(lblPay);
            pnlBody.Controls.Add(_cboPaymentMethod);
            pnlBody.Controls.Add(lblRef);
            pnlBody.Controls.Add(_txtReceiptRef);
            y += 50;

            // Action Buttons
            SunshineButton btnSave = new SunshineButton
            {
                Text = "💾 Record & Deduct Expense",
                IsPrimary = true,
                Location = new Point(250, y),
                Size = new Size(245, 42),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSave.Click += (s, e) => SaveExpense();

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(135, y),
                Size = new Size(100, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(220, 215, 205);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlBody.Controls.Add(btnSave);
            pnlBody.Controls.Add(btnCancel);

            Controls.Add(pnlBody);
            Controls.Add(pnlHeader);
            AcceptButton = btnSave;
        }

        private void SaveExpense()
        {
            string vendor = _txtPaidTo.Text.Trim();
            if (string.IsNullOrWhiteSpace(vendor))
            {
                MessageBox.Show("Please enter the vendor or payee for this expenditure.", "Vendor Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPaidTo.Focus();
                return;
            }

            string desc = _txtDescription.Text.Trim();
            if (string.IsNullOrWhiteSpace(desc))
            {
                MessageBox.Show("Please provide a brief description of this expense.", "Description Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDescription.Focus();
                return;
            }

            CreatedExpense = new ExpenseRecord
            {
                CompanyId = DataService.Instance.ActiveCompanyId,
                Category = _cboCategory.SelectedItem?.ToString() ?? "Miscellaneous",
                Amount = _numAmount.Value,
                PaidTo = vendor,
                Description = desc,
                PaymentMethod = _cboPaymentMethod.SelectedItem?.ToString() ?? "Cash",
                ReceiptRef = string.IsNullOrWhiteSpace(_txtReceiptRef.Text) ? "N/A" : _txtReceiptRef.Text.Trim(),
                RecordedBy = _currentUser,
                ExpenseDate = DateTime.UtcNow
            };

            DataService.Instance.AddExpense(CreatedExpense);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
