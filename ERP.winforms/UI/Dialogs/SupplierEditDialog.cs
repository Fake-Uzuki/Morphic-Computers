using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class SupplierEditDialog : Form
    {
        private readonly Supplier? _targetSupplier;
        private readonly DataService _dataService = DataService.Instance;

        private TextBox _txtName = null!;
        private TextBox _txtContact = null!;
        private TextBox _txtPhone = null!;
        private TextBox _txtEmail = null!;
        private TextBox _txtAddress = null!;

        public Supplier? ResultSupplier { get; private set; }

        public SupplierEditDialog(Supplier? supplier = null)
        {
            _targetSupplier = supplier;

            Text = _targetSupplier == null ? "Add Parts & Hardware Supplier" : $"Edit Supplier - {_targetSupplier.SupplierName}";
            Size = new Size(500, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.AppBackground;

            InitializeForm();

            if (_targetSupplier != null)
            {
                _txtName.Text = _targetSupplier.SupplierName;
                _txtContact.Text = _targetSupplier.ContactPerson ?? "";
                _txtPhone.Text = _targetSupplier.ContactNumber ?? "";
                _txtEmail.Text = _targetSupplier.EmailAddress ?? "";
                _txtAddress.Text = _targetSupplier.Address ?? "";
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
                Text = _targetSupplier == null ? "🚚 NEW SUPPLIER REGISTRATION" : "🚚 EDIT SUPPLIER DETAILS",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 12),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);

            // Card Body
            SunshineCard card = new SunshineCard
            {
                Location = new Point(20, 58),
                Size = new Size(445, 340),
                Padding = new Padding(18),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            int y = 14;

            // Company Name
            card.Controls.Add(CreateLabel("SUPPLIER / DISTRIBUTOR NAME *", 15, y));
            y += 20;
            _txtName = new TextBox { Location = new Point(15, y), Width = 410, Font = AppTheme.BodyFont };
            card.Controls.Add(_txtName);
            y += 35;

            // Contact Person
            card.Controls.Add(CreateLabel("CONTACT PERSON / ACCOUNT MANAGER *", 15, y));
            y += 20;
            _txtContact = new TextBox { Location = new Point(15, y), Width = 410, Font = AppTheme.BodyFont };
            card.Controls.Add(_txtContact);
            y += 35;

            // Phone & Email
            card.Controls.Add(CreateLabel("PHONE NUMBER", 15, y));
            card.Controls.Add(CreateLabel("EMAIL ADDRESS", 225, y));
            y += 20;
            _txtPhone = new TextBox { Location = new Point(15, y), Width = 200, Font = AppTheme.BodyFont };
            _txtEmail = new TextBox { Location = new Point(225, y), Width = 200, Font = AppTheme.BodyFont };
            card.Controls.Add(_txtPhone);
            card.Controls.Add(_txtEmail);
            y += 35;

            // Office / Warehouse Address
            card.Controls.Add(CreateLabel("OFFICE / WAREHOUSE ADDRESS", 15, y));
            y += 20;
            _txtAddress = new TextBox
            {
                Location = new Point(15, y),
                Width = 410,
                Height = 65,
                Multiline = true,
                Font = AppTheme.BodyFont,
                ScrollBars = ScrollBars.Vertical
            };
            card.Controls.Add(_txtAddress);

            // Bottom Buttons
            SunshineButton btnSave = new SunshineButton
            {
                Text = _targetSupplier == null ? "Save Supplier" : "Update Supplier",
                IsPrimary = true,
                Location = new Point(255, 430),
                Size = new Size(210, 42)
            };
            btnSave.Click += (s, e) => SaveSupplier();

            SunshineButton btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(135, 430),
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

        private void SaveSupplier()
        {
            string name = _txtName.Text.Trim();
            string contact = _txtContact.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter the supplier name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(contact))
            {
                MessageBox.Show("Please enter the contact person's name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtContact.Focus();
                return;
            }

            if (_targetSupplier == null)
            {
                var newSup = new Supplier
                {
                    SupplierName = name,
                    ContactPerson = contact,
                    ContactNumber = _txtPhone.Text.Trim(),
                    EmailAddress = _txtEmail.Text.Trim(),
                    Address = _txtAddress.Text.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _dataService.AddSupplier(newSup);
                ResultSupplier = newSup;
            }
            else
            {
                _targetSupplier.SupplierName = name;
                _targetSupplier.ContactPerson = contact;
                _targetSupplier.ContactNumber = _txtPhone.Text.Trim();
                _targetSupplier.EmailAddress = _txtEmail.Text.Trim();
                _targetSupplier.Address = _txtAddress.Text.Trim();
                _dataService.UpdateSupplier(_targetSupplier);
                ResultSupplier = _targetSupplier;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
