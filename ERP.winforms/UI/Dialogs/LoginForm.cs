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
    public class LoginForm : Form
    {
        private readonly DataService _dataService = DataService.Instance;

        public string AuthenticatedUser { get; private set; } = "Cirunay";
        public string AuthenticatedRole { get; private set; } = "Store Administrator";
        public Company SelectedCompany { get; private set; } = null!;

        private TextBox _txtCompany = null!;
        private TextBox _txtUsername = null!;
        private TextBox _txtPassword = null!;
        private Label _lblError = null!;

        private SunshineButton _btnLogin = null!;

        public LoginForm()
        {
            Text = "Sign In - Multi-Tenant ERP Cloud";
            Size = new Size(470, 560);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.HeaderBg;

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // Top Header Brand Icon & Titles
            Label lblLogo = new Label
            {
                Text = "M",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                BackColor = AppTheme.HeaderBrandGold,
                Location = new Point(207, 24),
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblTitle = new Label
            {
                Text = "Morphic Cloud ERP",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 76),
                Size = new Size(414, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblSubtitle = new Label
            {
                Text = "Multi-Tenant Enterprise Portal  •  Sign In",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 168, 158),
                Location = new Point(20, 108),
                Size = new Size(414, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Card Container for Input Fields
            SunshineCard card = new SunshineCard
            {
                Location = new Point(36, 138),
                Size = new Size(382, 360),
                Padding = new Padding(22),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            int y = 14;

            // 1. Company Name Input Box (Validated, empty, no pre-written words)
            Label lblCompany = new Label
            {
                Text = "COMPANY *",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, y),
                AutoSize = true
            };
            y += 20;

            _txtCompany = new TextBox
            {
                Text = "", // completely empty
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(20, y),
                Width = 340
            };
            y += 36;

            // 2. Username Input Box (Empty, no pre-written words)
            Label lblUser = new Label
            {
                Text = "USERNAME *",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, y),
                AutoSize = true
            };
            y += 20;

            _txtUsername = new TextBox
            {
                Text = "", // completely empty
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(20, y),
                Width = 340
            };
            y += 36;

            // 3. Password Input Box (Empty, masked, no pre-written words)
            Label lblPass = new Label
            {
                Text = "PASSWORD *",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, y),
                AutoSize = true
            };
            y += 20;

            _txtPassword = new TextBox
            {
                Text = "", // completely empty
                PasswordChar = '●',
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(20, y),
                Width = 340
            };
            y += 34;

            // Error label
            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(184, 50, 38),
                Location = new Point(20, y),
                Size = new Size(340, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            y += 26;

            // Sign In Button
            _btnLogin = new SunshineButton
            {
                Text = "Sign In",
                IsPrimary = true,
                Location = new Point(20, y),
                Size = new Size(340, 42),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            _btnLogin.Click += async (s, e) => await AttemptLoginAsync();

            card.Controls.Add(lblCompany);
            card.Controls.Add(_txtCompany);
            card.Controls.Add(lblUser);
            card.Controls.Add(_txtUsername);
            card.Controls.Add(lblPass);
            card.Controls.Add(_txtPassword);
            card.Controls.Add(_lblError);
            card.Controls.Add(_btnLogin);

            Controls.Add(lblLogo);
            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(card);

            AcceptButton = _btnLogin;
        }

        private async System.Threading.Tasks.Task AttemptLoginAsync()
        {
            _lblError.Text = "";

            string companyInput = _txtCompany.Text.Trim();
            string username = _txtUsername.Text.Trim();
            string password = _txtPassword.Text;

            // 1. Validate Company Input
            if (string.IsNullOrEmpty(companyInput))
            {
                _lblError.Text = "Please enter your company name.";
                _txtCompany.Focus();
                return;
            }

            // Resolve known company or use input directly for API authentication
            Company? matchedCompany = _dataService.Companies.FirstOrDefault(c =>
                c.CompanyName.Equals(companyInput, StringComparison.OrdinalIgnoreCase) ||
                c.CompanyCode.Equals(companyInput, StringComparison.OrdinalIgnoreCase));

            string targetCompanyName = matchedCompany?.CompanyName ?? companyInput;

            // 2. Validate Username and Password
            if (string.IsNullOrEmpty(username))
            {
                _lblError.Text = "Please enter your username.";
                _txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                _lblError.Text = "Please enter your password.";
                _txtPassword.Focus();
                return;
            }

            _btnLogin.Enabled = false;
            _btnLogin.Text = "Signing In...";
            Cursor = Cursors.WaitCursor;

            // 3. Authenticate against ERP.api asynchronously if network is available
            bool isOnline = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (isOnline)
            {
                try
                {
                    var loginResult = await ApiClient.Instance.LoginAsync(targetCompanyName, username, password);

                    if (loginResult != null && loginResult.Success)
                    {
                        AuthenticatedUser = loginResult.Username;
                        AuthenticatedRole = loginResult.Role;
                        SelectedCompany = new Company
                        {
                            CompanyId = loginResult.CompanyId,
                            CompanyCode = loginResult.CompanyCode,
                            CompanyName = loginResult.CompanyName,
                            PlanName = loginResult.PlanName
                        };

                        _dataService.ActiveCompanyId = loginResult.CompanyId;
                        _dataService.CurrentCompany = SelectedCompany;
                        _dataService.LoadFromDatabase();

                        // Securely cache credentials in machine DPAPI-encrypted vault for offline operations
                        OfflineAuthService.Instance.CacheSuccessfulLogin(loginResult, password);

                        DialogResult = DialogResult.OK;
                        Close();
                        return;
                    }

                    // If API returned a specific rejection (e.g. invalid credentials or company not found)
                    if (loginResult != null && !string.IsNullOrEmpty(loginResult.Message) && !loginResult.Message.StartsWith("API Connection Error"))
                    {
                        _lblError.Text = loginResult.Message;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"API Login Exception: {ex.Message}");
                }
                finally
                {
                    _btnLogin.Enabled = true;
                    _btnLogin.Text = "Sign In";
                    Cursor = Cursors.Default;
                }
            }

            // 4. Secure Offline Authentication via DPAPI Salted Hash Vault
            var offlineResult = OfflineAuthService.Instance.ValidateOfflineLogin(companyInput, username, password);
            if (offlineResult.Success && offlineResult.Credential != null)
            {
                var cred = offlineResult.Credential;
                AuthenticatedUser = cred.DisplayName;
                AuthenticatedRole = cred.Role;
                SelectedCompany = new Company
                {
                    CompanyId = cred.CompanyId,
                    CompanyCode = cred.CompanyCode,
                    CompanyName = cred.CompanyName,
                    PlanName = cred.PlanName
                };

                _dataService.ActiveCompanyId = cred.CompanyId;
                _dataService.CurrentCompany = SelectedCompany;
                _dataService.LoadFromDatabase();

                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _lblError.Text = !string.IsNullOrEmpty(offlineResult.Message)
                ? offlineResult.Message
                : "Invalid username or password. Please try again.";
        }
    }
}
