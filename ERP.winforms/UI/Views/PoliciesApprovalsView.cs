using System;
using System.Drawing;
using System.Windows.Forms;
using ERP.winforms.Theme;

namespace ERP.winforms.UI.Views
{
    public enum PoliciesApprovalsTab
    {
        WorkflowAndApproval,
        TermsPoliciesAgreements
    }

    public class PoliciesApprovalsView : UserControl
    {
        private readonly ApprovalsView _approvalsView;
        private readonly PoliciesView _policiesView;

        private Panel _pnlSubNav = null!;
        private Panel _pnlContainer = null!;
        private Button _btnTabApprovals = null!;
        private Button _btnTabPolicies = null!;
        private PoliciesApprovalsTab _activeTab = PoliciesApprovalsTab.WorkflowAndApproval;

        public PoliciesApprovalsView(ApprovalsView approvalsView, PoliciesView policiesView)
        {
            _approvalsView = approvalsView;
            _policiesView = policiesView;

            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // Top sub-navigation bar
            _pnlSubNav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = AppTheme.HeaderBg,
                Padding = new Padding(20, 6, 20, 6)
            };

            _btnTabApprovals = new Button
            {
                Text = "🛡️ Workflow & Approval",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(20, 6),
                Size = new Size(185, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Primary,
                ForeColor = AppTheme.TextDark,
                Cursor = Cursors.Hand
            };
            _btnTabApprovals.FlatAppearance.BorderSize = 0;
            _btnTabApprovals.Click += (s, e) => SelectTab(PoliciesApprovalsTab.WorkflowAndApproval);

            _btnTabPolicies = new Button
            {
                Text = "📄 Terms, Policies & Agreements",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(215, 6),
                Size = new Size(245, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 42, 34),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnTabPolicies.FlatAppearance.BorderSize = 0;
            _btnTabPolicies.Click += (s, e) => SelectTab(PoliciesApprovalsTab.TermsPoliciesAgreements);

            _pnlSubNav.Controls.Add(_btnTabApprovals);
            _pnlSubNav.Controls.Add(_btnTabPolicies);

            // Container Panel
            _pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.AppBackground
            };

            Controls.Add(_pnlContainer);
            Controls.Add(_pnlSubNav);

            SelectTab(PoliciesApprovalsTab.WorkflowAndApproval);
        }

        public void SelectTab(PoliciesApprovalsTab tab)
        {
            _activeTab = tab;

            if (_activeTab == PoliciesApprovalsTab.WorkflowAndApproval)
            {
                _btnTabApprovals.BackColor = AppTheme.Primary;
                _btnTabApprovals.ForeColor = AppTheme.TextDark;

                _btnTabPolicies.BackColor = Color.FromArgb(40, 42, 34);
                _btnTabPolicies.ForeColor = Color.White;

                _pnlContainer.Controls.Clear();
                _approvalsView.Dock = DockStyle.Fill;
                _pnlContainer.Controls.Add(_approvalsView);
                _approvalsView.RefreshData();
            }
            else
            {
                _btnTabPolicies.BackColor = AppTheme.Primary;
                _btnTabPolicies.ForeColor = AppTheme.TextDark;

                _btnTabApprovals.BackColor = Color.FromArgb(40, 42, 34);
                _btnTabApprovals.ForeColor = Color.White;

                _pnlContainer.Controls.Clear();
                _policiesView.Dock = DockStyle.Fill;
                _pnlContainer.Controls.Add(_policiesView);
                _policiesView.RefreshData();
            }
        }

        public void RefreshData()
        {
            if (_activeTab == PoliciesApprovalsTab.WorkflowAndApproval)
            {
                _approvalsView.RefreshData();
            }
            else
            {
                _policiesView.RefreshData();
            }
        }
    }
}
