using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Dialogs
{
    public class UpgradeSubscriptionDialog : Form
    {
        public bool Upgraded { get; private set; }

        public UpgradeSubscriptionDialog(string featureName)
        {
            Text = "Feature Restricted - Upgrade Required";
            Size = new Size(520, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.AppBackground;

            InitializeLayout(featureName);
        }

        private void InitializeLayout(string featureName)
        {
            SunshineCard card = new SunshineCard
            {
                Location = new Point(20, 20),
                Size = new Size(465, 275),
                Padding = new Padding(24)
            };

            Label lblTitle = new Label
            {
                Text = "FEATURE ACCESS RESTRICTED",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 110, 10),
                Location = new Point(20, 20),
                AutoSize = true
            };

            Label lblFeature = new Label
            {
                Text = $"Module: {featureName}",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 52),
                AutoSize = true
            };

            Label lblDescription = new Label
            {
                Text = "This module is not included in the Micro Company subscription plan. " +
                       "Your current plan includes Dashboard, Products & Stock Inventory, and Point of Sale (POS). " +
                       "To unlock Repair Job Orders and Supplier Management, upgrade to the Small Business tier.",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 82),
                Size = new Size(420, 65)
            };

            Label lblPlanStatus = new Label
            {
                Text = "Current Tier: Micro (Company A)  |  Required Tier: Small Business",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 110, 10),
                Location = new Point(20, 155),
                AutoSize = true
            };

            SunshineButton btnUpgrade = new SunshineButton
            {
                Text = "Simulate Upgrade to Small Business",
                IsPrimary = true,
                Location = new Point(20, 200),
                Size = new Size(270, 42)
            };

            btnUpgrade.Click += (s, e) =>
            {
                var company = DataService.Instance.Companies.Find(c => c.CompanyId == DataService.Instance.ActiveCompanyId);
                if (company != null)
                {
                    company.PlanName = "SmallBusiness";
                }
                Upgraded = true;
                MessageBox.Show(
                    "Subscription successfully upgraded to Small Business!\n\nThe requested feature is now unlocked.",
                    "Plan Updated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                DialogResult = DialogResult.OK;
                Close();
            };

            SunshineButton btnClose = new SunshineButton
            {
                Text = "Close",
                IsPrimary = false,
                Location = new Point(310, 200),
                Size = new Size(130, 42)
            };
            btnClose.Click += (s, e) => Close();

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblFeature);
            card.Controls.Add(lblDescription);
            card.Controls.Add(lblPlanStatus);
            card.Controls.Add(btnUpgrade);
            card.Controls.Add(btnClose);

            Controls.Add(card);
        }
    }
}
