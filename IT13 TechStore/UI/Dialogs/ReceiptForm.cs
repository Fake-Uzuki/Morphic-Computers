using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IT8_TechStore.Models;
using IT8_TechStore.Services;
using IT8_TechStore.Theme;
using IT8_TechStore.UI.Components;

namespace IT8_TechStore.UI.Dialogs
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
            Size = new Size(460, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.CardBackground;

            InitializeForm();
        }

        private void InitializeForm()
        {
            Controls.Clear();

            // 1. Header Logo & Title
            Label lblLogo = new Label
            {
                Text = "☀️ MORPHIC COMPUTERS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Location = new Point(20, 16),
                Size = new Size(404, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblTagline = new Label
            {
                Text = "Official Sales Receipt & Invoice",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 48),
                Size = new Size(404, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblDivider1 = new Label
            {
                Text = "----------------------------------------------------------------------------------",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.BorderColor,
                Location = new Point(20, 70),
                Size = new Size(404, 15)
            };

            // 2. Receipt Metadata
            Label lblInfo = new Label
            {
                Text = $"Order ID: {_order.Id}\nCustomer: {_order.CustomerName}\nDate: {_order.CreatedAt:g}\nPayment: {_order.PaymentMethod}",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, 88),
                Size = new Size(396, 70)
            };

            // 3. Itemized Purchased List
            ListBox lstItems = new ListBox
            {
                Location = new Point(24, 164),
                Size = new Size(396, 170),
                BackColor = AppTheme.AppBackground,
                ForeColor = AppTheme.TextDark,
                Font = AppTheme.BodyFont,
                BorderStyle = BorderStyle.FixedSingle
            };

            foreach (var item in _order.Items)
            {
                lstItems.Items.Add($"{item.ProductName}  x{item.Quantity}  =  ${item.Subtotal:N2}");
            }

            // 4. Financial Totals
            Label lblTotals = new Label
            {
                Text = $"Subtotal:   ${_order.Subtotal:N2}\nTax (12% VAT):   ${_order.Tax:N2}\nDiscount:   -${_order.Discount:N2}\n----------------------------------------\nGrand Total:   ${_order.TotalAmount:N2}",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, 344),
                Size = new Size(396, 95)
            };

            // 5. Payment Tendered & Change Due Section
            Label lblCashLabel = new Label
            {
                Text = "Cash Tendered ($):",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, 452),
                AutoSize = true
            };

            _txtCashTendered = new TextBox
            {
                Text = _order.TotalAmount.ToString("F2"),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(200, 448),
                Width = 220
            };
            _txtCashTendered.TextChanged += (s, e) => CalculateChange();

            Label lblChangeLabel = new Label
            {
                Text = "Change Due ($):",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(24, 492),
                AutoSize = true
            };

            _lblChangeDue = new Label
            {
                Text = "$0.00",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.Green,
                Location = new Point(200, 490),
                AutoSize = true
            };

            // 6. Complete Payment Button
            _btnCompletePayment = new SunshineButton
            {
                Text = "💳 Complete Transaction & Print Receipt",
                IsPrimary = true,
                Location = new Point(24, 545),
                Width = 396,
                Height = 48,
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
                    _lblChangeDue.Text = $"${change:N2}";
                    _lblChangeDue.ForeColor = Color.Green;
                    _btnCompletePayment.Enabled = true;
                }
                else
                {
                    _lblChangeDue.Text = $"Insufficient (${Math.Abs(change):N2})";
                    _lblChangeDue.ForeColor = Color.Red;
                    _btnCompletePayment.Enabled = false;
                }
            }
            else
            {
                _lblChangeDue.Text = "$0.00";
                _btnCompletePayment.Enabled = false;
            }
        }

        private void BtnCompletePayment_Click(object? sender, EventArgs e)
        {
            // Record order in DataService
            _dataService.Orders.Insert(0, _order);

            // Deduct stock quantities from inventory
            foreach (var item in _order.Items)
            {
                var prod = _dataService.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity = Math.Max(0, prod.StockQuantity - item.Quantity);
                    _dataService.UpdateProduct(prod);
                }
            }

            MessageBox.Show("Payment processed successfully!\nReceipt printed to store system.", "Transaction Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
