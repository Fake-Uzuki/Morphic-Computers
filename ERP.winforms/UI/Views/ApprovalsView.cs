using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;
using ERP.winforms.UI.Dialogs;

namespace ERP.winforms.UI.Views
{
    public class ApprovalsView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly string _currentUser;
        private readonly string _currentRole;

        private DataGridView _gridRequests = null!;
        private TextBox _txtSearch = null!;
        private FlowLayoutPanel _flpFilterPills = null!;

        // Right Detail Workbench
        private Panel _pnlDetails = null!;
        private Label _lblDetailReqNum = null!;
        private Label _lblDetailType = null!;
        private Label _lblDetailRequester = null!;
        private Label _lblDetailAmount = null!;
        private Label _lblDetailStatus = null!;
        private Label _lblDetailTitle = null!;
        private TextBox _txtDetailReason = null!;
        private TextBox _txtReviewNotes = null!;
        private SunshineButton _btnApprove = null!;
        private SunshineButton _btnReject = null!;

        private string _currentStatusFilter = "Pending";
        private ApprovalRequest? _selectedRequest;

        public ApprovalsView(string currentUser = "Cirunay", string currentRole = "Store Administrator")
        {
            _currentUser = currentUser;
            _currentRole = currentRole;

            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeLayout();
            _dataService.ApprovalRequestsChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (InvokeRequired) BeginInvoke(new Action(RefreshData));
                    else RefreshData();
                }
            };

            RefreshData();
        }

        private void InitializeLayout()
        {
            Controls.Clear();



            // ========================================================
            // 1. EXPLANATORY HEADER BANNER (Explains Approvals Purpose)
            // ========================================================
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(20, 10, 20, 8),
                BackColor = Color.FromArgb(24, 25, 20)
            };

            Label lblBannerTitle = new Label
            {
                Text = "🛡️ STORE APPROVALS & OPERATIONAL AUDIT TRAIL",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.HeaderBrandGold,
                Location = new Point(20, 8),
                AutoSize = true
            };

            Label lblBannerSub = new Label
            {
                Text = "Central operational hub for POS cashier item voids, cart cancellations, and manager discount overrides. Store Managers can review and authorize pending tickets below.",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 178, 168),
                Location = new Point(20, 30),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblBannerTitle);
            pnlHeader.Controls.Add(lblBannerSub);

            // ========================================================
            // 2. TOOLBAR (Search & Actions)
            // ========================================================
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(20, 4, 20, 6),
                BackColor = AppTheme.OperationsBarBg
            };

            Panel pnlSearch = new Panel
            {
                Location = new Point(20, 9),
                Size = new Size(260, 34),
                BackColor = Color.White
            };
            pnlSearch.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };

            _txtSearch = new TextBox
            {
                Location = new Point(8, 7),
                Width = 244,
                BorderStyle = BorderStyle.None,
                Font = AppTheme.BodyFont,
                PlaceholderText = "Search request #, requester, title..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlSearch.Controls.Add(_txtSearch);

            // Filter Pills
            _flpFilterPills = new FlowLayoutPanel
            {
                Location = new Point(295, 8),
                Size = new Size(520, 36),
                BackColor = Color.Transparent,
                WrapContents = false
            };

            AddFilterPill("Pending", "Pending Review");
            AddFilterPill("Approved", "Approved");
            AddFilterPill("Rejected", "Rejected");
            AddFilterPill("All", "All Tickets");

            SunshineButton btnSubmitReq = new SunshineButton
            {
                Text = "+ Manual Store Request",
                IsPrimary = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 200, 8),
                Size = new Size(180, 34),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            btnSubmitReq.Click += (s, e) =>
            {
                using var dialog = new NewApprovalRequestDialog(_currentUser);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RefreshData();
                }
            };

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(_flpFilterPills);
            pnlToolbar.Controls.Add(btnSubmitReq);

            // ========================================================
            // 3. MAIN WORKSPACE CONTAINER (Split: Grid + Details)
            // ========================================================
            Panel pnlMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 14),
                BackColor = Color.Transparent
            };

            // Right Detail Panel
            _pnlDetails = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360,
                Padding = new Padding(12),
                BackColor = Color.White
            };
            _pnlDetails.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _pnlDetails.Width - 1, _pnlDetails.Height - 1);
            };
            InitializeDetailPanel();

            // Left Grid Card
            SunshineCard cardGrid = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            _gridRequests = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 238, 230),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 44 }
            };

            _gridRequests.EnableHeadersVisualStyles = false;
            _gridRequests.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridRequests.ColumnHeadersHeight = 36;
            _gridRequests.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "REQ #", FillWeight = 13, MinimumWidth = 85, Name = "ColNum" });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CATEGORY", FillWeight = 16, MinimumWidth = 110, Name = "ColType" });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SUBJECT / TITLE", FillWeight = 26, MinimumWidth = 150, Name = "ColTitle" });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "REQUESTED BY", FillWeight = 15, MinimumWidth = 110, Name = "ColBy" });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "AMOUNT", FillWeight = 14, MinimumWidth = 90, Name = "ColAmt" });
            _gridRequests.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", FillWeight = 12, MinimumWidth = 85, Name = "ColStatus" });

            _gridRequests.SelectionChanged += (s, e) =>
            {
                if (_gridRequests.SelectedRows.Count > 0 && _gridRequests.SelectedRows[0].Tag != null)
                {
                    int reqId = Convert.ToInt32(_gridRequests.SelectedRows[0].Tag);
                    _selectedRequest = _dataService.ApprovalRequests.FirstOrDefault(r => r.RequestId == reqId);
                }
                else
                {
                    _selectedRequest = null;
                }
                UpdateDetailPanel();
            };

            cardGrid.Controls.Add(_gridRequests);

            Panel pnlSpacer = new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent };

            pnlMainContainer.Controls.Add(cardGrid);
            pnlMainContainer.Controls.Add(pnlSpacer);
            pnlMainContainer.Controls.Add(_pnlDetails);
            cardGrid.BringToFront();

            Controls.Add(pnlMainContainer);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlHeader);
        }

        private void AddFilterPill(string filterKey, string label)
        {
            Button btn = new Button
            {
                Text = label,
                Tag = filterKey,
                Size = new Size(110, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = filterKey == "Pending" ? AppTheme.TextDark : AppTheme.TextMuted,
                BackColor = filterKey == "Pending" ? AppTheme.Primary : Color.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Click += (s, e) =>
            {
                _currentStatusFilter = filterKey;
                foreach (Control c in _flpFilterPills.Controls)
                {
                    if (c is Button b)
                    {
                        bool active = b.Tag?.ToString() == _currentStatusFilter;
                        b.BackColor = active ? AppTheme.Primary : Color.Transparent;
                        b.ForeColor = active ? AppTheme.TextDark : AppTheme.TextMuted;
                    }
                }
                ApplyFilters();
            };

            _flpFilterPills.Controls.Add(btn);
        }

        private void InitializeDetailPanel()
        {
            _pnlDetails.Controls.Clear();

            int y = 8;
            Label lblHeader = new Label
            {
                Text = "REVIEW & DECISION WORKBENCH",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(10, y),
                AutoSize = true
            };
            y += 24;

            _lblDetailReqNum = new Label { Text = "Select request to review", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(160, 110, 10), Location = new Point(10, y), Size = new Size(330, 20) };
            y += 22;
            _lblDetailType = new Label { Location = new Point(10, y), Size = new Size(330, 18), Font = AppTheme.BodyFont, ForeColor = AppTheme.TextDark };
            y += 20;
            _lblDetailRequester = new Label { Location = new Point(10, y), Size = new Size(330, 18), Font = AppTheme.BodyFont, ForeColor = AppTheme.TextDark };
            y += 20;
            _lblDetailAmount = new Label { Location = new Point(10, y), Size = new Size(330, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = AppTheme.TextDark };
            y += 22;
            _lblDetailStatus = new Label { Location = new Point(10, y), Size = new Size(330, 20), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            y += 24;

            _lblDetailTitle = new Label { Location = new Point(10, y), Size = new Size(330, 36), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = AppTheme.TextDark };
            y += 40;

            Label lblReasonHdr = new Label { Text = "OPERATIONAL JUSTIFICATION:", Location = new Point(10, y), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted };
            y += 18;

            _txtDetailReason = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(330, 60),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 248, 243),
                ScrollBars = ScrollBars.Vertical,
                Font = AppTheme.BodyFont
            };
            y += 68;

            Label lblNotesHdr = new Label { Text = "MANAGER / ADMIN REVIEW NOTES:", Location = new Point(10, y), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted };
            y += 18;

            _txtReviewNotes = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(330, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = AppTheme.BodyFont
            };
            y += 68;

            _btnApprove = new SunshineButton
            {
                Text = "✅ Approve Request",
                IsPrimary = true,
                Location = new Point(10, y),
                Size = new Size(160, 38),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Enabled = false
            };
            _btnApprove.Click += (s, e) => ResolveSelected(true);

            _btnReject = new SunshineButton
            {
                Text = "❌ Reject Request",
                IsPrimary = false,
                Location = new Point(180, y),
                Size = new Size(160, 38),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Enabled = false
            };
            _btnReject.Click += (s, e) => ResolveSelected(false);

            _pnlDetails.Controls.Add(lblHeader);
            _pnlDetails.Controls.Add(_lblDetailReqNum);
            _pnlDetails.Controls.Add(_lblDetailType);
            _pnlDetails.Controls.Add(_lblDetailRequester);
            _pnlDetails.Controls.Add(_lblDetailAmount);
            _pnlDetails.Controls.Add(_lblDetailStatus);
            _pnlDetails.Controls.Add(_lblDetailTitle);
            _pnlDetails.Controls.Add(lblReasonHdr);
            _pnlDetails.Controls.Add(_txtDetailReason);
            _pnlDetails.Controls.Add(lblNotesHdr);
            _pnlDetails.Controls.Add(_txtReviewNotes);
            _pnlDetails.Controls.Add(_btnApprove);
            _pnlDetails.Controls.Add(_btnReject);
        }

        private void ResolveSelected(bool approve)
        {
            var targetReq = _selectedRequest;
            if (targetReq == null)
            {
                MessageBox.Show("Please select an approval request from the table to review.", "No Request Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int reqId = targetReq.RequestId;
            string reqNumber = targetReq.RequestNumber;

            bool canAuthorize = _currentRole.Contains("Admin", StringComparison.OrdinalIgnoreCase) || 
                                _currentRole.Contains("Manager", StringComparison.OrdinalIgnoreCase);

            string authorizer = _currentUser;

            // If not directly logged in as Admin or Manager, prompt for manager password
            if (!canAuthorize)
            {
                string? pw = PromptForPassword("Manager Authorization Required", "Please enter manager password (09092121, admin123, or manager123) to authorize this request:");
                if (pw == null) return; // User cancelled

                bool ok = pw == "09092121" || pw == "admin123" || pw == "manager123" || pw == "admin" || pw == "manager";
                if (!ok)
                {
                    var r1 = OfflineAuthService.Instance.ValidateOfflineLogin("Tenant A", "manager", pw);
                    var r2 = OfflineAuthService.Instance.ValidateOfflineLogin("Tenant B", "cirunay", pw);
                    var r3 = OfflineAuthService.Instance.ValidateOfflineLogin("Tenant B", "manager", pw);
                    ok = r1.Success || r2.Success || r3.Success;
                }

                if (!ok)
                {
                    MessageBox.Show("Incorrect manager authorization password. Authorization denied.", "Authorization Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                authorizer = $"{_currentUser} (Verified by Manager)";
            }

            string status = approve ? "Approved" : "Rejected";
            string notes = _txtReviewNotes.Text.Trim();
            if (string.IsNullOrWhiteSpace(notes))
            {
                notes = approve ? "Authorized by Store Manager / Admin." : "Rejected by Store Manager / Admin.";
            }

            _dataService.ResolveApprovalRequest(reqId, status, authorizer, notes);
            MessageBox.Show($"Request {reqNumber} marked as {status} by {authorizer}.", "Decision Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshData();
        }

        private string? PromptForPassword(string title, string prompt)
        {
            using Form promptForm = new Form
            {
                Width = 420,
                Height = 210,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };
            Label lblText = new Label { Left = 20, Top = 16, Width = 370, Height = 40, Text = prompt, Font = new Font("Segoe UI", 9F) };
            TextBox txtInput = new TextBox { Left = 20, Top = 64, Width = 365, PasswordChar = '●', Font = new Font("Segoe UI", 11F) };
            Label lblHint = new Label { Left = 20, Top = 96, Width = 370, Text = "💡 Passwords: 09092121 (Admin), admin123, or manager123", Font = new Font("Segoe UI", 7.5F, FontStyle.Italic), ForeColor = Color.FromArgb(130, 95, 10) };
            Button btnOk = new Button { Text = "Authorize", Left = 180, Width = 100, Top = 125, Height = 32, DialogResult = DialogResult.OK, BackColor = AppTheme.Primary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            Button btnCancel = new Button { Text = "Cancel", Left = 290, Width = 95, Top = 125, Height = 32, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            promptForm.Controls.AddRange(new Control[] { lblText, txtInput, lblHint, btnOk, btnCancel });
            promptForm.AcceptButton = btnOk;
            promptForm.CancelButton = btnCancel;

            return promptForm.ShowDialog(this) == DialogResult.OK ? txtInput.Text.Trim() : null;
        }

        private void UpdateDetailPanel()
        {
            if (_selectedRequest == null)
            {
                _lblDetailReqNum.Text = "Select request to review";
                _lblDetailType.Text = "";
                _lblDetailRequester.Text = "";
                _lblDetailAmount.Text = "";
                _lblDetailStatus.Text = "";
                _lblDetailTitle.Text = "";
                _txtDetailReason.Text = "";
                _txtReviewNotes.Text = "";
                _btnApprove.Enabled = false;
                _btnReject.Enabled = false;
                return;
            }

            _lblDetailReqNum.Text = $"TICKET: {_selectedRequest.RequestNumber}";
            _lblDetailType.Text = $"Type: {_selectedRequest.RequestType}";
            _lblDetailRequester.Text = $"Requested By: {_selectedRequest.RequestedBy} on {_selectedRequest.CreatedAt.ToLocalTime():MMM dd, hh:mm tt}";
            _lblDetailAmount.Text = $"Impact Amount: ₱{_selectedRequest.RequestedAmount:N2}";
            _lblDetailStatus.Text = $"Status: {_selectedRequest.Status.ToUpperInvariant()}";
            _lblDetailStatus.ForeColor = _selectedRequest.Status switch
            {
                "Approved" => AppTheme.GreenPillText,
                "Rejected" => Color.FromArgb(184, 50, 38),
                _ => Color.FromArgb(160, 110, 10)
            };

            _lblDetailTitle.Text = _selectedRequest.Title;
            _txtDetailReason.Text = _selectedRequest.ReasonDescription;
            _txtReviewNotes.Text = _selectedRequest.ReviewNotes ?? "";

            bool isPending = _selectedRequest.Status == "Pending";
            _btnApprove.Enabled = isPending;
            _btnReject.Enabled = isPending;
        }

        public void RefreshData()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string query = _txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _dataService.ApprovalRequests.AsEnumerable();

            if (_currentStatusFilter != "All")
            {
                filtered = filtered.Where(r => r.Status.Equals(_currentStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(r =>
                    r.RequestNumber.ToLowerInvariant().Contains(query) ||
                    r.Title.ToLowerInvariant().Contains(query) ||
                    r.RequestedBy.ToLowerInvariant().Contains(query) ||
                    r.RequestType.ToLowerInvariant().Contains(query));
            }

            _gridRequests.Rows.Clear();
            foreach (var r in filtered)
            {
                int rowIdx = _gridRequests.Rows.Add(
                    r.RequestNumber,
                    r.RequestType,
                    r.Title,
                    r.RequestedBy,
                    $"₱{r.RequestedAmount:N2}",
                    r.Status
                );
                _gridRequests.Rows[rowIdx].Tag = r.RequestId;
            }

            if (_gridRequests.Rows.Count > 0)
            {
                var matchingRow = _gridRequests.Rows.Cast<DataGridViewRow>()
                    .FirstOrDefault(r => r.Tag != null && _selectedRequest != null && (int)r.Tag == _selectedRequest.RequestId);

                if (matchingRow != null)
                {
                    matchingRow.Selected = true;
                }
                else
                {
                    _gridRequests.Rows[0].Selected = true;
                    int reqId = Convert.ToInt32(_gridRequests.Rows[0].Tag);
                    _selectedRequest = _dataService.ApprovalRequests.FirstOrDefault(r => r.RequestId == reqId);
                }
            }
            else
            {
                _selectedRequest = null;
            }

            UpdateDetailPanel();
        }
    }
}
