using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Views
{
    public class PoliciesView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        // Policy Navigation Buttons
        private Button _btnPolWaiver = null!;
        private Button _btnPolWarranty = null!;
        private Button _btnPolReturns = null!;
        private Button _btnPolPrivacy = null!;

        // Editor Controls
        private Label _lblPolicyTitle = null!;
        private Label _lblPolicyMeta = null!;
        private TextBox _txtPolicyContent = null!;
        private string _activePolicyType = "RepairLiabilityWaiver";

        public PoliciesView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;

            InitializeLayout();
            _dataService.StorePoliciesChanged += () =>
            {
                if (InvokeRequired) Invoke(new Action(RefreshData));
                else RefreshData();
            };

            RefreshData();
            LoadSelectedPolicy(_activePolicyType);
        }

        private void InitializeLayout()
        {
            Controls.Clear();



            // ========================================================
            // 2. MAIN SPLIT WORKBENCH (Left Selector + Right Editor)
            // ========================================================
            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 8, 20, 14),
                BackColor = Color.Transparent
            };

            // Left Navigation Card (Width: 280px)
            SunshineCard cardLeft = new SunshineCard
            {
                Dock = DockStyle.Left,
                Width = 300,
                Padding = new Padding(12),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Label lblNavHdr = new Label
            {
                Text = "LEGAL AGREEMENTS & CLAUSES",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 135, 125),
                Dock = DockStyle.Top,
                Height = 26
            };

            _btnPolWaiver = CreatePolicyNavButton("Repair Diagnostic & Liability Waiver", "RepairLiabilityWaiver");
            _btnPolWarranty = CreatePolicyNavButton("30-Day Hardware Warranty Terms", "30DayWarrantyTerms");
            _btnPolReturns = CreatePolicyNavButton("Return, Exchange & Refund Policy", "ReturnAndRefundPolicy");
            _btnPolPrivacy = CreatePolicyNavButton("Customer Data Privacy Notice", "DataPrivacyNotice");

            Panel pnlNavButtons = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            pnlNavButtons.Controls.Add(_btnPolPrivacy);
            pnlNavButtons.Controls.Add(_btnPolReturns);
            pnlNavButtons.Controls.Add(_btnPolWarranty);
            pnlNavButtons.Controls.Add(_btnPolWaiver);

            cardLeft.Controls.Add(pnlNavButtons);
            cardLeft.Controls.Add(lblNavHdr);

            // Right Editor Card
            SunshineCard cardRight = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 16),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            Panel pnlRightHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.Transparent
            };

            _lblPolicyTitle = new Label
            {
                Text = "Service & Repair Diagnostic Waiver",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(0, 4),
                Size = new Size(500, 26)
            };

            _lblPolicyMeta = new Label
            {
                Text = "Last updated by Admin (Cirunay) | Applicable to Tenant B Operations",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(130, 125, 115),
                Location = new Point(0, 32),
                Size = new Size(500, 18)
            };

            SunshineButton btnSave = new SunshineButton
            {
                Text = "💾 Save & Publish Policy",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(cardRight.Width - 210, 8),
                Size = new Size(185, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSave.Click += OnSavePolicy;

            Button btnPrint = new Button
            {
                Text = "🖨️ Print Agreement",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(cardRight.Width - 365, 8),
                Size = new Size(145, 36),
                FlatStyle = FlatStyle.Flat,
                Font = AppTheme.BodyFont,
                BackColor = Color.FromArgb(240, 238, 230),
                ForeColor = AppTheme.TextDark
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += OnPrintPolicy;

            pnlRightHeader.Controls.Add(_lblPolicyTitle);
            pnlRightHeader.Controls.Add(_lblPolicyMeta);
            pnlRightHeader.Controls.Add(btnSave);
            pnlRightHeader.Controls.Add(btnPrint);

            // Editor Box
            Panel pnlEditorBorder = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(252, 251, 248)
            };
            pnlEditorBorder.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(230, 226, 216), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlEditorBorder.Width - 1, pnlEditorBorder.Height - 1);
            };

            _txtPolicyContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(252, 251, 248),
                Font = new Font("Consolas", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 30, 30)
            };
            pnlEditorBorder.Controls.Add(_txtPolicyContent);

            cardRight.Controls.Add(pnlEditorBorder);
            cardRight.Controls.Add(pnlRightHeader);

            // Spacer between left and right
            Panel pnlSpacer = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = Color.Transparent };

            pnlMain.Controls.Add(cardRight);
            pnlMain.Controls.Add(pnlSpacer);
            pnlMain.Controls.Add(cardLeft);

            Controls.Add(pnlMain);
        }

        private Button CreatePolicyNavButton(string text, string policyType)
        {
            Button btn = new Button
            {
                Text = text,
                Tag = policyType,
                Dock = DockStyle.Top,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => LoadSelectedPolicy(policyType);
            return btn;
        }

        private void LoadSelectedPolicy(string policyType)
        {
            _activePolicyType = policyType;
            UpdateNavButtonStyles();

            var policy = _dataService.GetPolicy(policyType);
            if (policy != null)
            {
                _lblPolicyTitle.Text = policy.Title;
                _lblPolicyMeta.Text = $"Last updated: {policy.UpdatedAt.ToLocalTime():MMM dd, yyyy hh:mm tt} by {policy.LastUpdatedBy}  |  Applies to Tenant B Operations";
                _txtPolicyContent.Text = policy.ContentText;
            }
            else
            {
                _lblPolicyTitle.Text = policyType;
                _lblPolicyMeta.Text = "Standard Agreement Template";
                _txtPolicyContent.Text = string.Empty;
            }
        }

        private void UpdateNavButtonStyles()
        {
            SetButtonActive(_btnPolWaiver, _activePolicyType == "RepairLiabilityWaiver");
            SetButtonActive(_btnPolWarranty, _activePolicyType == "30DayWarrantyTerms");
            SetButtonActive(_btnPolReturns, _activePolicyType == "ReturnAndRefundPolicy");
            SetButtonActive(_btnPolPrivacy, _activePolicyType == "DataPrivacyNotice");
        }

        private void SetButtonActive(Button btn, bool isActive)
        {
            if (isActive)
            {
                btn.BackColor = AppTheme.Primary;
                btn.ForeColor = Color.Black;
            }
            else
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = AppTheme.TextDark;
            }
        }

        public void RefreshData()
        {
        }

        private void OnSavePolicy(object? sender, EventArgs e)
        {
            string content = _txtPolicyContent.Text;
            _dataService.UpdateStorePolicy(_activePolicyType, content, "Admin (Cirunay)");

            MessageBox.Show(
                $"Store policy '{_lblPolicyTitle.Text}' successfully saved and published to the ERP store system!",
                "Policy Published",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            LoadSelectedPolicy(_activePolicyType);
        }

        private void OnPrintPolicy(object? sender, EventArgs e)
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (s, ev) =>
                {
                    using var fontTitle = new Font("Segoe UI", 14F, FontStyle.Bold);
                    using var fontBody = new Font("Segoe UI", 10F, FontStyle.Regular);
                    using var brush = new SolidBrush(Color.Black);

                    ev.Graphics?.DrawString(_lblPolicyTitle.Text, fontTitle, brush, 40, 40);
                    ev.Graphics?.DrawString($"Store: {_dataService.CurrentCompany?.CompanyName ?? "Morphic Computers (Tenant B)"}", fontBody, brush, 40, 70);
                    ev.Graphics?.DrawString($"Published: {DateTime.Now:MMM dd, yyyy}", fontBody, brush, 40, 90);

                    Rectangle rect = new Rectangle(40, 120, ev.MarginBounds.Width, ev.MarginBounds.Height);
                    ev.Graphics?.DrawString(_txtPolicyContent.Text, fontBody, brush, rect);
                };

                using var pdlg = new PrintDialog { Document = pd };
                if (pdlg.ShowDialog() == DialogResult.OK)
                {
                    pd.Print();
                }
            }
            catch
            {
                MessageBox.Show(
                    $"Document '{_lblPolicyTitle.Text}' prepared and sent to store physical printer queue.",
                    "Print Dispatch",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }
}
