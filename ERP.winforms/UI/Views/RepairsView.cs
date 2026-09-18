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
    public class RepairsView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridRepairs = null!;
        private TextBox _txtSearch = null!;
        private FlowLayoutPanel _flpFilterPills = null!;

        // Metric Card Labels
        private Label _lblMetricTotal = null!;
        private Label _lblMetricBench = null!;
        private Label _lblMetricReady = null!;
        private Label _lblMetricRevenue = null!;

        // Right Detail Panel Controls
        private Panel _pnlDetails = null!;
        private Label _lblDetailTicket = null!;
        private Label _lblDetailCustomer = null!;
        private Label _lblDetailDevice = null!;
        private Label _lblDetailStatus = null!;
        private Label _lblDetailPricing = null!;
        private TextBox _txtDetailNotes = null!;

        private string _currentStatusFilter = "All";
        private RepairTicket? _selectedTicket;

        public RepairsView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;

            InitializeLayout();
            _dataService.RepairTicketsChanged += () =>
            {
                if (InvokeRequired) Invoke(new Action(RefreshData));
                else RefreshData();
            };

            RefreshData();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // ========================================================
            // 1. TOP METRICS STRIP (Height: 95px)
            // ========================================================
            Panel pnlMetrics = new Panel
            {
                Dock = DockStyle.Top,
                Height = 95,
                Padding = new Padding(20, 14, 20, 10),
                BackColor = Color.Transparent
            };

            int cardW = 280;
            int cardH = 75;
            int gap = 16;
            int x = 20;

            var cardTotal = CreateMetricCard("TOTAL REPAIR JOBS", "0", "All time tickets registered", x, cardW, cardH, out _lblMetricTotal);
            x += cardW + gap;
            var cardBench = CreateMetricCard("ACTIVE ON BENCH", "0", "Diagnosing or awaiting parts", x, cardW, cardH, out _lblMetricBench);
            x += cardW + gap;
            var cardReady = CreateMetricCard("READY FOR PICKUP", "0", "Repairs completed, ready for release", x, cardW, cardH, out _lblMetricReady);
            x += cardW + gap;
            var cardRev = CreateMetricCard("REPAIR REVENUE", "₱0.00", "Labor fees & parts billed", x, cardW, cardH, out _lblMetricRevenue);

            pnlMetrics.Controls.Add(cardTotal);
            pnlMetrics.Controls.Add(cardBench);
            pnlMetrics.Controls.Add(cardReady);
            pnlMetrics.Controls.Add(cardRev);

            // ========================================================
            // 2. FILTER & ACTION TOOLBAR (Height: 52px)
            // ========================================================
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(20, 4, 20, 6),
                BackColor = AppTheme.OperationsBarBg
            };

            // Search Box
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
                PlaceholderText = "Search ticket #, customer, model..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlSearch.Controls.Add(_txtSearch);

            // Filter Pills (All, Diagnosing, InRepair, AwaitingParts, ReadyForPickup, Completed)
            _flpFilterPills = new FlowLayoutPanel
            {
                Location = new Point(295, 8),
                Size = new Size(620, 36),
                BackColor = Color.Transparent,
                WrapContents = false
            };

            AddFilterPill("All", "All Tickets");
            AddFilterPill("Diagnosing", "Diagnosing");
            AddFilterPill("InRepair", "In Repair");
            AddFilterPill("AwaitingParts", "Waiting Parts");
            AddFilterPill("ReadyForPickup", "Ready for Pickup");
            AddFilterPill("Completed", "Completed");

            // "+ New Repair Ticket" Action Button
            SunshineButton btnNewTicket = new SunshineButton
            {
                Text = "+ New Repair Ticket (F3)",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 230, 8),
                Size = new Size(190, 34),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnNewTicket.Click += (s, e) => OpenNewTicketDialog();

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(_flpFilterPills);
            pnlToolbar.Controls.Add(btnNewTicket);

            // ========================================================
            // 3. MAIN WORKSPACE CONTAINER (Split: Grid + Details)
            // ========================================================
            Panel pnlMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 14),
                BackColor = Color.Transparent
            };

            // Right Detail Action Workbench Card
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

            _gridRepairs = new DataGridView
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

            _gridRepairs.EnableHeadersVisualStyles = false;
            _gridRepairs.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridRepairs.ColumnHeadersHeight = 36;
            _gridRepairs.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridRepairs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TICKET #", FillWeight = 14, Name = "ColTicket" });
            _gridRepairs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CUSTOMER", FillWeight = 16, Name = "ColCust" });
            _gridRepairs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DEVICE / MODEL", FillWeight = 18, Name = "ColDevice" });
            _gridRepairs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SYMPTOMS", FillWeight = 18, Name = "ColIssue" });
            _gridRepairs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TECHNICIAN", FillWeight = 12, Name = "ColTech" });
            _gridRepairs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", FillWeight = 11, Name = "ColStatus" });
            _gridRepairs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TOTAL (PHP)", FillWeight = 11, Name = "ColTotal" });

            _gridRepairs.SelectionChanged += (s, e) =>
            {
                if (_gridRepairs.SelectedRows.Count > 0)
                {
                    int ticketId = Convert.ToInt32(_gridRepairs.SelectedRows[0].Tag);
                    _selectedTicket = _dataService.RepairTickets.FirstOrDefault(t => t.RepairTicketId == ticketId);
                    UpdateDetailPanel();
                }
            };

            cardGrid.Controls.Add(_gridRepairs);

            // Spacing panel between grid and details
            Panel pnlSpacer = new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent };

            pnlMainContainer.Controls.Add(cardGrid);
            pnlMainContainer.Controls.Add(pnlSpacer);
            pnlMainContainer.Controls.Add(_pnlDetails);

            Controls.Add(pnlMainContainer);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlMetrics);
        }

        private Panel CreateMetricCard(string title, string val, string sub, int x, int w, int h, out Label valLabel)
        {
            Panel card = new Panel
            {
                Location = new Point(x, 10),
                Size = new Size(w, h),
                BackColor = Color.White
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(14, 10),
                AutoSize = true
            };

            valLabel = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(12, 26),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = sub,
                Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                ForeColor = AppTheme.TextSubtle,
                Location = new Point(14, 54),
                AutoSize = true
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(valLabel);
            card.Controls.Add(lblSub);
            return card;
        }

        private void AddFilterPill(string filterKey, string label)
        {
            Button btn = new Button
            {
                Text = label,
                Tag = filterKey,
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = filterKey == "All" ? AppTheme.TextDark : AppTheme.TextMuted,
                BackColor = filterKey == "All" ? AppTheme.Primary : Color.Transparent,
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
                Text = "TICKET WORKBENCH",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(10, y),
                AutoSize = true
            };
            y += 24;

            _lblDetailTicket = new Label
            {
                Text = "Select a repair ticket to inspect",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 110, 10),
                Location = new Point(10, y),
                Size = new Size(330, 20)
            };
            y += 24;

            _lblDetailCustomer = new Label { Location = new Point(10, y), Size = new Size(330, 20), Font = AppTheme.BodyFont, ForeColor = AppTheme.TextDark };
            y += 20;
            _lblDetailDevice = new Label { Location = new Point(10, y), Size = new Size(330, 20), Font = AppTheme.BodyFont, ForeColor = AppTheme.TextDark };
            y += 20;
            _lblDetailStatus = new Label { Location = new Point(10, y), Size = new Size(330, 24), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            y += 26;
            _lblDetailPricing = new Label { Location = new Point(10, y), Size = new Size(330, 24), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = AppTheme.TextDark };
            y += 28;

            Label lblIssueTitle = new Label { Text = "DIAGNOSTIC & SERVICE NOTES:", Location = new Point(10, y), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted };
            y += 18;

            _txtDetailNotes = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(330, 90),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = AppTheme.BodyFont
            };
            y += 96;

            SunshineButton btnSaveNotes = new SunshineButton
            {
                Text = "Save Notes & Tech Update",
                IsPrimary = false,
                Location = new Point(10, y),
                Size = new Size(330, 30),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnSaveNotes.Click += (s, e) =>
            {
                if (_selectedTicket != null)
                {
                    _selectedTicket.DiagnosticNotes = _txtDetailNotes.Text.Trim();
                    _dataService.SaveRepairsToLocalCache();
                    MessageBox.Show("Diagnostic notes updated successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            y += 36;

            Label lblActions = new Label { Text = "ADVANCE REPAIR WORKFLOW STAGE:", Location = new Point(10, y), AutoSize = true, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted };
            y += 20;

            // Workflow advancing buttons
            SunshineButton btnStageDiag = CreateSmallActionBtn("🔍 Diagnosing", 10, y, 160, () => AdvanceStatus("Diagnosing"));
            SunshineButton btnStageParts = CreateSmallActionBtn("⏳ Awaiting Parts", 175, y, 165, () => AdvanceStatus("AwaitingParts"));
            y += 34;
            SunshineButton btnStageRepair = CreateSmallActionBtn("🛠️ In Repair", 10, y, 160, () => AdvanceStatus("InRepair"));
            SunshineButton btnStageReady = CreateSmallActionBtn("✅ Ready for Pickup", 175, y, 165, () => AdvanceStatus("ReadyForPickup"));
            y += 34;
            SunshineButton btnStageDone = CreateSmallActionBtn("🎉 Complete & Release", 10, y, 330, () => AdvanceStatus("Completed"), true);
            y += 38;

            // Print Claim Stub button
            SunshineButton btnPrintStub = new SunshineButton
            {
                Text = "🖨️ View / Print Claim Stub",
                IsPrimary = true,
                Location = new Point(10, y),
                Size = new Size(330, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnPrintStub.Click += (s, e) =>
            {
                if (_selectedTicket != null)
                {
                    using var stubDialog = new RepairClaimStubDialog(_selectedTicket);
                    stubDialog.ShowDialog();
                }
            };

            _pnlDetails.Controls.Add(lblHeader);
            _pnlDetails.Controls.Add(_lblDetailTicket);
            _pnlDetails.Controls.Add(_lblDetailCustomer);
            _pnlDetails.Controls.Add(_lblDetailDevice);
            _pnlDetails.Controls.Add(_lblDetailStatus);
            _pnlDetails.Controls.Add(_lblDetailPricing);
            _pnlDetails.Controls.Add(lblIssueTitle);
            _pnlDetails.Controls.Add(_txtDetailNotes);
            _pnlDetails.Controls.Add(btnSaveNotes);
            _pnlDetails.Controls.Add(lblActions);
            _pnlDetails.Controls.Add(btnStageDiag);
            _pnlDetails.Controls.Add(btnStageParts);
            _pnlDetails.Controls.Add(btnStageRepair);
            _pnlDetails.Controls.Add(btnStageReady);
            _pnlDetails.Controls.Add(btnStageDone);
            _pnlDetails.Controls.Add(btnPrintStub);
        }

        private SunshineButton CreateSmallActionBtn(string text, int x, int y, int w, Action onClick, bool isPrimary = false)
        {
            var btn = new SunshineButton
            {
                Text = text,
                IsPrimary = isPrimary,
                Location = new Point(x, y),
                Size = new Size(w, 28),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void AdvanceStatus(string nextStatus)
        {
            if (_selectedTicket == null) return;
            _selectedTicket.Status = nextStatus;
            _selectedTicket.DiagnosticNotes = _txtDetailNotes.Text.Trim();
            if (nextStatus == "Completed" || nextStatus == "ReadyForPickup")
            {
                _selectedTicket.CompletedAt = DateTime.UtcNow;
            }

            _dataService.SaveRepairsToLocalCache();
            _dataService.RepairTicketsChanged?.Invoke();
            UpdateDetailPanel();
            ApplyFilters();
        }

        private void UpdateDetailPanel()
        {
            if (_selectedTicket == null)
            {
                _lblDetailTicket.Text = "Select a repair ticket to inspect";
                _lblDetailCustomer.Text = "";
                _lblDetailDevice.Text = "";
                _lblDetailStatus.Text = "";
                _lblDetailPricing.Text = "";
                _txtDetailNotes.Text = "";
                return;
            }

            _lblDetailTicket.Text = $"TICKET: {_selectedTicket.TicketNumber}   ({_selectedTicket.AssignedTechnician ?? "Unassigned"})";
            _lblDetailCustomer.Text = $"Customer: {_selectedTicket.CustomerName}   |   Phone: {_selectedTicket.CustomerPhone ?? "N/A"}";
            _lblDetailDevice.Text = $"Device: {_selectedTicket.DeviceBrandModel} ({_selectedTicket.DeviceType})";
            _lblDetailStatus.Text = $"Current Stage: {_selectedTicket.Status.ToUpperInvariant()}";
            _lblDetailStatus.ForeColor = GetStatusColor(_selectedTicket.Status);
            _lblDetailPricing.Text = $"Total: ₱{_selectedTicket.TotalAmount:N2}  (Labor: ₱{_selectedTicket.LaborFee:N2} | Parts: ₱{_selectedTicket.PartsCost:N2}) | Bal: ₱{_selectedTicket.BalanceDue:N2}";
            _txtDetailNotes.Text = _selectedTicket.DiagnosticNotes ?? _selectedTicket.ReportedIssue;
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "ReadyForPickup" => AppTheme.GreenPillText,
                "Completed" => Color.FromArgb(30, 95, 180),
                "InRepair" => Color.FromArgb(160, 110, 10),
                "AwaitingParts" => Color.FromArgb(184, 50, 38),
                _ => AppTheme.TextDark
            };
        }

        public void RefreshData()
        {
            // Update metrics
            var all = _dataService.RepairTickets;
            _lblMetricTotal.Text = all.Count.ToString();
            _lblMetricBench.Text = all.Count(t => t.Status is "Diagnosing" or "InRepair" or "AwaitingParts").ToString();
            _lblMetricReady.Text = all.Count(t => t.Status == "ReadyForPickup").ToString();
            decimal totalRev = all.Sum(t => t.TotalAmount);
            _lblMetricRevenue.Text = $"₱{totalRev:N2}";

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string query = _txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _dataService.RepairTickets.AsEnumerable();

            if (_currentStatusFilter != "All")
            {
                filtered = filtered.Where(t => t.Status.Equals(_currentStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(t =>
                    t.TicketNumber.ToLowerInvariant().Contains(query) ||
                    t.CustomerName.ToLowerInvariant().Contains(query) ||
                    t.DeviceBrandModel.ToLowerInvariant().Contains(query) ||
                    t.ReportedIssue.ToLowerInvariant().Contains(query) ||
                    (t.SerialNumber != null && t.SerialNumber.ToLowerInvariant().Contains(query)));
            }

            _gridRepairs.Rows.Clear();
            foreach (var t in filtered)
            {
                int rowIdx = _gridRepairs.Rows.Add(
                    t.TicketNumber,
                    t.CustomerName,
                    $"{t.DeviceBrandModel} ({t.DeviceType})",
                    t.ReportedIssue,
                    t.AssignedTechnician ?? "Unassigned",
                    t.Status,
                    $"₱{t.TotalAmount:N2}"
                );
                _gridRepairs.Rows[rowIdx].Tag = t.RepairTicketId;
            }

            if (_selectedTicket == null && _gridRepairs.Rows.Count > 0)
            {
                _gridRepairs.Rows[0].Selected = true;
            }
        }

        private void OpenNewTicketDialog()
        {
            using var dialog = new RepairIntakeDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RefreshData();
                if (dialog.CreatedOrUpdatedTicket != null)
                {
                    using var stub = new RepairClaimStubDialog(dialog.CreatedOrUpdatedTicket);
                    stub.ShowDialog();
                }
            }
        }
    }
}
