using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class RepairIntakeDialog : Form
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly RepairTicket? _targetTicket;

        private ComboBox _cboCustomer = null!;
        private TextBox _txtPhone = null!;
        private TextBox _txtEmail = null!;
        private ComboBox _cboDeviceType = null!;
        private TextBox _txtBrandModel = null!;
        private TextBox _txtSerial = null!;
        private TextBox _txtIssue = null!;
        private ComboBox _cboTechnician = null!;
        private NumericUpDown _numLabor = null!;
        private NumericUpDown _numParts = null!;
        private NumericUpDown _numDeposit = null!;
        private Label _lblTotal = null!;

        public RepairTicket? CreatedOrUpdatedTicket { get; private set; }

        public RepairIntakeDialog(RepairTicket? ticket = null)
        {
            _targetTicket = ticket;

            Text = _targetTicket == null ? "New Repair Job Order Intake" : $"Edit Repair Ticket - {_targetTicket.TicketNumber}";
            Size = new Size(560, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.AppBackground;

            InitializeForm();

            if (_targetTicket != null)
            {
                _cboCustomer.Text = _targetTicket.CustomerName;
                _txtPhone.Text = _targetTicket.CustomerPhone ?? "";
                _txtEmail.Text = _targetTicket.CustomerEmail ?? "";
                _cboDeviceType.SelectedItem = _targetTicket.DeviceType;
                _txtBrandModel.Text = _targetTicket.DeviceBrandModel;
                _txtSerial.Text = _targetTicket.SerialNumber ?? "";
                _txtIssue.Text = _targetTicket.ReportedIssue;
                _cboTechnician.SelectedItem = _targetTicket.AssignedTechnician ?? "Lead Tech Alex";
                _numLabor.Value = Math.Min(_targetTicket.LaborFee, _numLabor.Maximum);
                _numParts.Value = Math.Min(_targetTicket.PartsCost, _numParts.Maximum);
                _numDeposit.Value = Math.Min(_targetTicket.DepositAmount, _numDeposit.Maximum);
                UpdateTotalSummary();
            }
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
                Text = _targetTicket == null ? "🔧 SERVICE BENCH INTAKE" : "🔧 EDIT REPAIR TICKET",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 10),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Register customer device, report symptom & initialize job order",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 168, 158),
                Location = new Point(20, 32),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Body Card
            SunshineCard card = new SunshineCard
            {
                Location = new Point(20, 75),
                Size = new Size(504, 500),
                Padding = new Padding(16),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            int y = 10;

            // Customer Name & Phone
            card.Controls.Add(CreateLabel("CUSTOMER NAME *", 15, y));
            card.Controls.Add(CreateLabel("CONTACT NUMBER *", 265, y));
            y += 20;

            _cboCustomer = new ComboBox
            {
                Location = new Point(15, y),
                Width = 235,
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            foreach (var c in _dataService.Customers.Where(cust => cust.IsActive))
            {
                if (!string.IsNullOrWhiteSpace(c.CustomerName))
                    _cboCustomer.Items.Add(c.CustomerName);
            }
            _cboCustomer.SelectedIndexChanged += (s, e) =>
            {
                string sel = _cboCustomer.Text;
                var cust = _dataService.Customers.FirstOrDefault(c => c.CustomerName.Equals(sel, StringComparison.OrdinalIgnoreCase));
                if (cust != null)
                {
                    if (!string.IsNullOrWhiteSpace(cust.ContactNumber)) _txtPhone.Text = cust.ContactNumber;
                    if (!string.IsNullOrWhiteSpace(cust.EmailAddress)) _txtEmail.Text = cust.EmailAddress;
                }
            };

            _txtPhone = new TextBox { Location = new Point(265, y), Width = 220, Font = AppTheme.BodyFont };
            card.Controls.Add(_cboCustomer);
            card.Controls.Add(_txtPhone);
            y += 35;

            // Email & Device Type
            card.Controls.Add(CreateLabel("EMAIL ADDRESS", 15, y));
            card.Controls.Add(CreateLabel("DEVICE CATEGORY *", 265, y));
            y += 20;

            _txtEmail = new TextBox { Location = new Point(15, y), Width = 235, Font = AppTheme.BodyFont };
            _cboDeviceType = new ComboBox
            {
                Location = new Point(265, y),
                Width = 220,
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboDeviceType.Items.AddRange(new object[] { "Desktop PC", "Laptop", "Graphics Card (GPU)", "Motherboard", "Power Supply (PSU)", "Mobile / Tablet", "Gaming Console" });
            _cboDeviceType.SelectedIndex = 0;
            card.Controls.Add(_txtEmail);
            card.Controls.Add(_cboDeviceType);
            y += 35;

            // Brand / Model & Serial Number
            card.Controls.Add(CreateLabel("BRAND & MODEL DETAILS *", 15, y));
            card.Controls.Add(CreateLabel("SERIAL NUMBER / S/N", 265, y));
            y += 20;

            _txtBrandModel = new TextBox { Location = new Point(15, y), Width = 235, Font = AppTheme.BodyFont };
            _txtSerial = new TextBox { Location = new Point(265, y), Width = 220, Font = AppTheme.BodyFont };
            card.Controls.Add(_txtBrandModel);
            card.Controls.Add(_txtSerial);
            y += 35;

            // Reported Issue
            card.Controls.Add(CreateLabel("CUSTOMER REPORTED ISSUE / SYMPTOMS *", 15, y));
            y += 20;
            _txtIssue = new TextBox
            {
                Location = new Point(15, y),
                Width = 470,
                Height = 65,
                Multiline = true,
                Font = AppTheme.BodyFont,
                ScrollBars = ScrollBars.Vertical
            };
            card.Controls.Add(_txtIssue);
            y += 75;

            // Technician Assignment (Parts Supplier removed per design)
            card.Controls.Add(CreateLabel("ASSIGNED TECHNICIAN", 15, y));
            y += 20;
            _cboTechnician = new ComboBox
            {
                Location = new Point(15, y),
                Width = 470,
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboTechnician.Items.AddRange(new object[] { "Lead Tech Alex", "Tech Justin", "Tech Ryan", "Bench Queue (Unassigned)" });
            _cboTechnician.SelectedIndex = 0;

            card.Controls.Add(_cboTechnician);
            y += 35;

            // Pricing & Billing
            card.Controls.Add(CreateLabel("EST. LABOR (₱)", 15, y));
            card.Controls.Add(CreateLabel("PARTS COST (₱)", 180, y));
            card.Controls.Add(CreateLabel("DEPOSIT (₱)", 345, y));
            y += 20;

            _numLabor = new NumericUpDown { Location = new Point(15, y), Width = 145, DecimalPlaces = 2, Maximum = 100000, Font = AppTheme.BodyFont, Value = 1000 };
            _numParts = new NumericUpDown { Location = new Point(180, y), Width = 145, DecimalPlaces = 2, Maximum = 200000, Font = AppTheme.BodyFont, Value = 0 };
            _numDeposit = new NumericUpDown { Location = new Point(345, y), Width = 140, DecimalPlaces = 2, Maximum = 100000, Font = AppTheme.BodyFont, Value = 500 };

            _numLabor.ValueChanged += (s, e) => UpdateTotalSummary();
            _numParts.ValueChanged += (s, e) => UpdateTotalSummary();
            _numDeposit.ValueChanged += (s, e) => UpdateTotalSummary();

            card.Controls.Add(_numLabor);
            card.Controls.Add(_numParts);
            card.Controls.Add(_numDeposit);
            y += 35;

            // Total / Balance summary pill
            _lblTotal = new Label
            {
                Location = new Point(15, y),
                Width = 470,
                Height = 32,
                BackColor = AppTheme.GoldPillBg,
                ForeColor = AppTheme.GoldPillText,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(_lblTotal);
            UpdateTotalSummary();

            // Bottom Buttons
            SunshineButton btnSave = new SunshineButton
            {
                Text = _targetTicket == null ? "Create Job Order" : "Update Job Order",
                IsPrimary = true,
                Location = new Point(275, 590),
                Size = new Size(248, 42)
            };
            btnSave.Click += (s, e) => SaveTicket();

            SunshineButton btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(155, 590),
                Size = new Size(110, 42)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(pnlHeader);
            Controls.Add(card);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            AcceptButton = btnSave;
        }

        private void UpdateTotalSummary()
        {
            decimal total = _numLabor.Value + _numParts.Value;
            decimal balance = Math.Max(0, total - _numDeposit.Value);
            _lblTotal.Text = $"Estimated Total: ₱{total:N2}   |   Deposit: ₱{_numDeposit.Value:N2}   |   Balance Due: ₱{balance:N2}";
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

        private void SaveTicket()
        {
            string customer = _cboCustomer.Text.Trim();
            string phone = _txtPhone.Text.Trim();
            string brand = _txtBrandModel.Text.Trim();
            string issue = _txtIssue.Text.Trim();
            string partsSupplier = _targetTicket?.PartsSupplier ?? "In-House Stock";

            if (string.IsNullOrEmpty(customer))
            {
                MessageBox.Show("Please enter or select the customer's name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cboCustomer.Focus();
                return;
            }

            if (string.IsNullOrEmpty(brand))
            {
                MessageBox.Show("Please enter the device brand and model.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBrandModel.Focus();
                return;
            }

            if (string.IsNullOrEmpty(issue))
            {
                MessageBox.Show("Please describe the reported issue or symptom.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtIssue.Focus();
                return;
            }

            if (_targetTicket == null)
            {
                var newTicket = new RepairTicket
                {
                    CustomerName = customer,
                    CustomerPhone = phone,
                    CustomerEmail = _txtEmail.Text.Trim(),
                    DeviceType = _cboDeviceType.SelectedItem?.ToString() ?? "Desktop PC",
                    DeviceBrandModel = brand,
                    SerialNumber = _txtSerial.Text.Trim(),
                    ReportedIssue = issue,
                    AssignedTechnician = _cboTechnician.SelectedItem?.ToString() ?? "Lead Tech Alex",
                    PartsSupplier = partsSupplier,
                    Status = "Received",
                    LaborFee = _numLabor.Value,
                    PartsCost = _numParts.Value,
                    DepositAmount = _numDeposit.Value,
                    CreatedAt = DateTime.UtcNow,
                    EstimatedCompletionDate = DateTime.UtcNow.AddDays(2)
                };

                _dataService.AddRepairTicket(newTicket);
                CreatedOrUpdatedTicket = newTicket;
            }
            else
            {
                _targetTicket.CustomerName = customer;
                _targetTicket.CustomerPhone = phone;
                _targetTicket.CustomerEmail = _txtEmail.Text.Trim();
                _targetTicket.DeviceType = _cboDeviceType.SelectedItem?.ToString() ?? "Desktop PC";
                _targetTicket.DeviceBrandModel = brand;
                _targetTicket.SerialNumber = _txtSerial.Text.Trim();
                _targetTicket.ReportedIssue = issue;
                _targetTicket.AssignedTechnician = _cboTechnician.SelectedItem?.ToString() ?? "Lead Tech Alex";
                _targetTicket.PartsSupplier = partsSupplier;
                _targetTicket.LaborFee = _numLabor.Value;
                _targetTicket.PartsCost = _numParts.Value;
                _targetTicket.DepositAmount = _numDeposit.Value;

                _dataService.SaveRepairsToLocalCache();
                _dataService.RepairTicketsChanged?.Invoke();
                CreatedOrUpdatedTicket = _targetTicket;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
