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
    public class ReceiptForm : Form
    {
        private readonly Order _order;
        private readonly DataService _dataService = DataService.Instance;

        private TextBox _txtCashTendered = null!;
        private Label _lblChangeDue = null!;
        private SunshineButton _btnCompletePayment = null!;

        public ReceiptForm(Order order)
        {
            _order = order;
            Text = "Receipt Checkout & Payment - Morphic Computers";
            ClientSize = new Size(450, 635);
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

            Label lblLogo = new Label
            {
                Text = "TENANT A",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 110, 10),
                Location = new Point(20, 18),
                Size = new Size(410, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblTagline = new Label
            {
                Text = "Official Sales Receipt & Invoice",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 48),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblDivider1 = new Label
            {
                Text = "----------------------------------------------------------------------------------",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.BorderColor,
                Location = new Point(20, 70),
                Size = new Size(410, 15)
            };

            Label lblInfo = new Label
            {
                Text = $"Order ID: {_order.Id}\nCustomer: {_order.CustomerName}\nDate: {_order.CreatedAt:g}\nPayment: {_order.PaymentMethod}",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, 88),
                Size = new Size(402, 70)
            };

            ListBox lstItems = new ListBox
            {
                Location = new Point(24, 164),
                Size = new Size(402, 165),
                BackColor = AppTheme.AppBackground,
                ForeColor = AppTheme.TextDark,
                Font = AppTheme.BodyFont,
                BorderStyle = BorderStyle.FixedSingle
            };

            foreach (var item in _order.Items)
            {
                lstItems.Items.Add($"{item.ProductName}  x{item.Quantity}  =  ₱{item.TotalPrice:N2}");
            }

            Label lblTotals = new Label
            {
                Text = $"Subtotal:   ₱{_order.Subtotal:N2}\nTax (12% VAT):   ₱{_order.Tax:N2}\nDiscount:   -₱{_order.Discount:N2}\n----------------------------------------\nGrand Total:   ₱{_order.TotalAmount:N2}",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, 338),
                Size = new Size(402, 105)
            };

            Label lblCashLabel = new Label
            {
                Text = "Cash Tendered (₱):",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, 458),
                AutoSize = true
            };

            _txtCashTendered = new TextBox
            {
                Text = _order.TotalAmount.ToString("F2"),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(190, 452),
                Width = 236
            };
            _txtCashTendered.TextChanged += (s, e) => CalculateChange();

            Label lblChangeLabel = new Label
            {
                Text = "Change Due (₱):",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, 498),
                AutoSize = true
            };

            _lblChangeDue = new Label
            {
                Text = "₱0.00",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.Green,
                Location = new Point(190, 496),
                AutoSize = true
            };

            _btnCompletePayment = new SunshineButton
            {
                Text = "Complete Transaction / Print Receipt",
                IsPrimary = true,
                Location = new Point(24, 545),
                Width = 402,
                Height = 46,
                Font = AppTheme.BodyBoldFont
            };
            _btnCompletePayment.Click += BtnCompletePayment_Click;

            Controls.Add(lblLogo);
            Controls.Add(lblTagline);
            Controls.Add(lblDivider1);
            Controls.Add(lblInfo);
            Controls.Add(lstItems);
            Controls.Add(lblTotals);
            Controls.Add(lblCashLabel);
            Controls.Add(_txtCashTendered);
            Controls.Add(lblChangeLabel);
            Controls.Add(_lblChangeDue);
            Controls.Add(_btnCompletePayment);

            CalculateChange();
        }

        private void CalculateChange()
        {
            if (decimal.TryParse(_txtCashTendered.Text, out decimal cash))
            {
                decimal change = cash - _order.TotalAmount;
                if (change >= 0)
                {
                    _lblChangeDue.Text = $"₱{change:N2}";
                    _lblChangeDue.ForeColor = Color.Green;
                    _btnCompletePayment.Enabled = true;
                }
                else
                {
                    _lblChangeDue.Text = $"Insufficient (₱{Math.Abs(change):N2})";
                    _lblChangeDue.ForeColor = Color.Red;
                    _btnCompletePayment.Enabled = false;
                }
            }
            else
            {
                _lblChangeDue.Text = "₱0.00";
                _btnCompletePayment.Enabled = false;
            }
        }

        private void BtnCompletePayment_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Payment processed successfully!\nReceipt printed to store system.", "Transaction Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
