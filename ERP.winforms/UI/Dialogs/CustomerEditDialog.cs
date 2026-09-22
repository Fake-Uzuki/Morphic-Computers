using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class CustomerEditDialog : Form
    {
        private readonly Customer? _targetCustomer;
        private readonly DataService _dataService = DataService.Instance;

        private TextBox _txtName = null!;
        private TextBox _txtPhone = null!;
        private TextBox _txtEmail = null!;
        private TextBox _txtAddress = null!;

        public Customer? ResultCustomer { get; private set; }

        public CustomerEditDialog(Customer? customer = null)
        {
            _targetCustomer = customer;

            Text = _targetCustomer == null ? "Register New Customer" : $"Edit Customer - {_targetCustomer.CustomerName}";
            Size = new Size(500, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.AppBackground;

            InitializeForm();

            if (_targetCustomer != null)
            {
                _txtName.Text = _targetCustomer.CustomerName;
                _txtPhone.Text = _targetCustomer.ContactNumber ?? "";
                _txtEmail.Text = _targetCustomer.EmailAddress ?? "";
                _txtAddress.Text = _targetCustomer.Address ?? "";
            }
        }

        private void InitializeForm()
        {
            Controls.Clear();

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = AppTheme.HeaderBg
            };

            Label lblTitle = new Label
            {
                Text = _targetCustomer == null ? "👤 NEW CUSTOMER PROFILE" : "👤 EDIT CUSTOMER PROFILE",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 12),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);

            // Card Body
            SunshineCard card = new SunshineCard
            {
                Location = new Point(20, 75),
                Size = new Size(445, 300),
                Padding = new Padding(18),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            int y = 14;

            // Name
            card.Controls.Add(CreateLabel("CUSTOMER / CORPORATE NAME *", 15, y));
            y += 20;
            _txtName = new TextBox { Location = new Point(15, y), Width = 410, Font = AppTheme.BodyFont };
            card.Controls.Add(_txtName);
            y += 35;

            // Phone & Email
            card.Controls.Add(CreateLabel("PRIMARY PHONE NUMBER *", 15, y));
            card.Controls.Add(CreateLabel("EMAIL ADDRESS", 225, y));
            y += 20;
            _txtPhone = new TextBox { Location = new Point(15, y), Width = 200, Font = AppTheme.BodyFont };
            _txtEmail = new TextBox { Location = new Point(225, y), Width = 200, Font = AppTheme.BodyFont };
            card.Controls.Add(_txtPhone);
            card.Controls.Add(_txtEmail);
            y += 35;

            // Address (Davao City Only)
            card.Controls.Add(CreateLabel("DELIVERY / BILLING ADDRESS (DAVAO CITY ONLY) *", 15, y));
            y += 20;
            _txtAddress = new TextBox
            {
                Location = new Point(15, y),
                Width = 410,
                Height = 65,
                Multiline = true,
                Font = AppTheme.BodyFont,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "e.g., Door 3, Bajada, Davao City"
            };
            card.Controls.Add(_txtAddress);

            // Bottom Buttons
            SunshineButton btnSave = new SunshineButton
            {
                Text = _targetCustomer == null ? "Save Customer" : "Update Profile",
                IsPrimary = true,
                Location = new Point(255, 390),
                Size = new Size(210, 42)
            };
            btnSave.Click += (s, e) => SaveCustomer();

            SunshineButton btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(135, 390),
                Size = new Size(110, 42)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(pnlHeader);
            Controls.Add(card);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            AcceptButton = btnSave;
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

        private void SaveCustomer()
        {
            string name = _txtName.Text.Trim();
            string phone = _txtPhone.Text.Trim();
            string address = _txtAddress.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter the customer name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                address = "Poblacion District, Davao City";
            }
            else if (!address.Contains("Davao", StringComparison.OrdinalIgnoreCase))
            {
                address = $"{address}, Davao City";
            }

            if (_targetCustomer == null)
            {
                var newCust = new Customer
                {
                    CustomerName = name,
                    ContactNumber = phone,
                    EmailAddress = _txtEmail.Text.Trim(),
                    Address = address,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dataService.AddCustomer(newCust);
                ResultCustomer = newCust;
            }
            else
            {
                _targetCustomer.CustomerName = name;
                _targetCustomer.ContactNumber = phone;
                _targetCustomer.EmailAddress = _txtEmail.Text.Trim();
                _targetCustomer.Address = address;
                _dataService.UpdateCustomer(_targetCustomer);
                ResultCustomer = _targetCustomer;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
