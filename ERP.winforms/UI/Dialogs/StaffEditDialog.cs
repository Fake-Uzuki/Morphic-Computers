using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class StaffEditDialog : Form
    {
        private readonly StaffMember? _targetStaff;
        private readonly DataService _dataService = DataService.Instance;

        private TextBox _txtName = null!;
        private TextBox _txtUsername = null!;
        private TextBox _txtPassword = null!;
        private ComboBox _cboRole = null!;
        private TextBox _txtPosition = null!;
        private TextBox _txtEmail = null!;
        private TextBox _txtPhone = null!;
        private NumericUpDown _numHourlyRate = null!;
        private NumericUpDown _numMonthlySalary = null!;

        public StaffMember? ResultStaff { get; private set; }

        public StaffEditDialog(StaffMember? staff = null)
        {
            _targetStaff = staff;

            Text = _targetStaff == null ? "Register Employee / Staff Member" : $"Edit Staff - {_targetStaff.FullName}";
            Size = new Size(520, 630);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.AppBackground;

            InitializeForm();

            if (_targetStaff != null)
            {
                _txtName.Text = _targetStaff.FullName;
                _txtUsername.Text = _targetStaff.Username;
                _txtPassword.Text = string.IsNullOrEmpty(_targetStaff.InitialPassword) ? "staff123" : _targetStaff.InitialPassword;
                _cboRole.SelectedItem = _targetStaff.Role;
                _txtPosition.Text = _targetStaff.PositionTitle;
                _txtEmail.Text = _targetStaff.Email ?? "";
                _txtPhone.Text = _targetStaff.PhoneNumber ?? "";
                _numHourlyRate.Value = Math.Min(_targetStaff.HourlyRate, _numHourlyRate.Maximum);
                _numMonthlySalary.Value = Math.Min(_targetStaff.MonthlySalary, _numMonthlySalary.Maximum);
            }
            else
            {
                _txtPassword.Text = "staff123";
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
                Text = _targetStaff == null ? "👥 REGISTER STAFF MEMBER" : "👥 EDIT STAFF PROFILE",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 10),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Manage employee roles, access privileges and compensation rates",
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
                Size = new Size(465, 390),
                Padding = new Padding(18),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            int y = 14;

            // Full Name & Username
            card.Controls.Add(CreateLabel("FULL NAME *", 15, y));
            card.Controls.Add(CreateLabel("LOGIN USERNAME *", 240, y));
            y += 20;

            _txtName = new TextBox { Location = new Point(15, y), Width = 210, Font = AppTheme.BodyFont };
            _txtUsername = new TextBox { Location = new Point(240, y), Width = 210, Font = AppTheme.BodyFont };
            card.Controls.Add(_txtName);
            card.Controls.Add(_txtUsername);
            y += 35;

            // Password & Role
            card.Controls.Add(CreateLabel("LOGIN PASSWORD * (Used to Sign In)", 15, y));
            card.Controls.Add(CreateLabel("SYSTEM ROLE *", 240, y));
            y += 20;

            _txtPassword = new TextBox { Location = new Point(15, y), Width = 210, Font = AppTheme.BodyFont };
            _cboRole = new ComboBox
            {
                Location = new Point(240, y),
                Width = 210,
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboRole.Items.AddRange(new object[] { "Store Administrator", "Store Manager", "Hardware Technician", "Cashier Operations" });
            _cboRole.SelectedIndex = 2; // Default Hardware Tech

            card.Controls.Add(_txtPassword);
            card.Controls.Add(_cboRole);
            y += 35;

            // Position & Phone
            card.Controls.Add(CreateLabel("JOB POSITION TITLE *", 15, y));
            card.Controls.Add(CreateLabel("PHONE NUMBER", 240, y));
            y += 20;

            _txtPosition = new TextBox { Location = new Point(15, y), Width = 210, Font = AppTheme.BodyFont, Text = "Hardware Technician" };
            _txtPhone = new TextBox { Location = new Point(240, y), Width = 210, Font = AppTheme.BodyFont };
            _cboRole.SelectedIndexChanged += (s, e) =>
            {
                if (_targetStaff == null)
                {
                    _txtPosition.Text = _cboRole.SelectedItem?.ToString() ?? "Hardware Technician";
                }
            };

            card.Controls.Add(_txtPosition);
            card.Controls.Add(_txtPhone);
            y += 35;

            // Email & Monthly Salary
            card.Controls.Add(CreateLabel("EMAIL ADDRESS", 15, y));
            card.Controls.Add(CreateLabel("MONTHLY SALARY BASE (₱)", 240, y));
            y += 20;

            _txtEmail = new TextBox { Location = new Point(15, y), Width = 210, Font = AppTheme.BodyFont };
            _numMonthlySalary = new NumericUpDown { Location = new Point(240, y), Width = 210, DecimalPlaces = 2, Maximum = 500000, Font = AppTheme.BodyFont, Value = 25000 };
            card.Controls.Add(_txtEmail);
            card.Controls.Add(_numMonthlySalary);
            y += 35;

            // Hourly Rate & Password Hint
            card.Controls.Add(CreateLabel("HOURLY RATE (₱)", 15, y));
            y += 20;
            _numHourlyRate = new NumericUpDown { Location = new Point(15, y), Width = 210, DecimalPlaces = 2, Maximum = 10000, Font = AppTheme.BodyFont, Value = 150 };
            card.Controls.Add(_numHourlyRate);

            Label lblPwdHint = new Label
            {
                Text = "💡 Staff sign in at the login screen using their assigned Username and Password.",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 110, 20),
                Location = new Point(15, y + 26),
                Size = new Size(435, 18)
            };
            card.Controls.Add(lblPwdHint);

            // Bottom Buttons
            SunshineButton btnSave = new SunshineButton
            {
                Text = _targetStaff == null ? "Save Staff Profile" : "Update Profile",
                IsPrimary = true,
                Location = new Point(265, 480),
                Size = new Size(220, 42)
            };
            btnSave.Click += (s, e) => SaveStaff();

            SunshineButton btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(145, 480),
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

        private void SaveStaff()
        {
            string name = _txtName.Text.Trim();
            string username = _txtUsername.Text.Trim();
            string password = _txtPassword.Text.Trim();
            string role = _cboRole.SelectedItem?.ToString() ?? "Hardware Technician";
            string pos = _txtPosition.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter the staff member's full name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a username for this employee.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                password = "staff123";
            }

            if (_targetStaff == null)
            {
                var newStaff = new StaffMember
                {
                    FullName = name,
                    Username = username,
                    InitialPassword = password,
                    Role = role,
                    PositionTitle = pos,
                    PhoneNumber = _txtPhone.Text.Trim(),
                    Email = _txtEmail.Text.Trim(),
                    HourlyRate = _numHourlyRate.Value,
                    MonthlySalary = _numMonthlySalary.Value,
                    IsActive = true,
                    HiredDate = DateTime.UtcNow
                };

                _dataService.AddStaffMember(newStaff);
                ResultStaff = newStaff;
            }
            else
            {
                _targetStaff.FullName = name;
                _targetStaff.Username = username;
                _targetStaff.InitialPassword = password;
                _targetStaff.Role = role;
                _targetStaff.PositionTitle = pos;
                _targetStaff.PhoneNumber = _txtPhone.Text.Trim();
                _targetStaff.Email = _txtEmail.Text.Trim();
                _targetStaff.HourlyRate = _numHourlyRate.Value;
                _targetStaff.MonthlySalary = _numMonthlySalary.Value;

                _dataService.UpdateStaffMember(_targetStaff);
                ResultStaff = _targetStaff;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
