using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Views
{
    public class PurchaseOrderItem
    {
        public string PoNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalAmount => Quantity * UnitCost;
        public string Status { get; set; } = "Pending";
        public string ApprovedBy { get; set; } = "Procurement Lead";
        public DateTime? ReceivedDate { get; set; }
    }

    public class ProcurementView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridPOs = null!;
        private TextBox _txtSearch = null!;
        private Label _lblTotalPoCount = null!;
        private Label _lblPendingDelivery = null!;
        private Label _lblTotalSpend = null!;
        private Label _lblSupplierCount = null!;

        private readonly List<PurchaseOrderItem> _orders = new();

        public ProcurementView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeDefaultPOs();
            InitializeLayout();
        }

        private void InitializeDefaultPOs()
        {
            _orders.Clear();
            _orders.Add(new PurchaseOrderItem
            {
                PoNumber = "PO-2026-001",
                SupplierName = "Asus Republic of Gamers Corp",
                OrderDate = DateTime.UtcNow.AddDays(-10),
                ItemDescription = "ROG Strix RTX 4080 Super OC (16GB)",
                Quantity = 5,
                UnitCost = 54000m,
                Status = "Received",
                ApprovedBy = "Marcus V. (Manager)",
                ReceivedDate = DateTime.UtcNow.AddDays(-2)
            });
            _orders.Add(new PurchaseOrderItem
            {
                PoNumber = "PO-2026-002",
                SupplierName = "Corsair Memory Philippines",
                OrderDate = DateTime.UtcNow.AddDays(-4),
                ItemDescription = "Vengeance RGB DDR5 32GB (2x16GB) 6000MHz",
                Quantity = 15,
                UnitCost = 6200m,
                Status = "In Transit",
                ApprovedBy = "Marcus V. (Manager)",
                ReceivedDate = null
            });
            _orders.Add(new PurchaseOrderItem
            {
                PoNumber = "PO-2026-003",
                SupplierName = "Samsung Semiconductor",
                OrderDate = DateTime.UtcNow.AddDays(-1),
                ItemDescription = "990 PRO NVMe M.2 SSD 2TB",
                Quantity = 10,
                UnitCost = 8900m,
                Status = "Pending",
                ApprovedBy = "Admin",
                ReceivedDate = null
            });
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // 1. TOP HEADER BANNER
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(24, 12, 24, 10),
                BackColor = AppTheme.HeaderBg
            };

            Label lblTitle = new Label
            {
                Text = "Supply Chain & Procurement Management",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 10),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Medium Enterprise Logistics  |  Component Inbound Orders & Supplier Shipments (Scaffolded / Local Preview)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(24, 36),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // 2. KPI METRICS CARDS PANEL
            Panel pnlKpis = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(24, 10, 24, 10),
                BackColor = Color.White
            };

            int cardW = 240;
            int cardGap = 16;
            int startX = 24;

            // KPI 1: Total Purchase Orders
            Panel pnlCard1 = CreateKpiCard("TOTAL PURCHASE ORDERS", _orders.Count.ToString(), "All logged POs", startX, cardW, out _lblTotalPoCount);
            startX += cardW + cardGap;

            // KPI 2: Pending Delivery
            Panel pnlCard2 = CreateKpiCard("IN TRANSIT / PENDING", $"{_orders.Count(o => o.Status != "Received")} Active", "Awaiting fulfillment", startX, cardW, out _lblPendingDelivery);
            startX += cardW + cardGap;

            // KPI 3: Total Spend
            decimal totalSpend = _orders.Sum(o => o.TotalAmount);
            Panel pnlCard3 = CreateKpiCard("TOTAL COMMITMENT", $"₱{totalSpend:N0}", "Allocated procurement capital", startX, cardW, out _lblTotalSpend);
            startX += cardW + cardGap;

            // KPI 4: Connected Suppliers
            int supplierCount = _dataService.Suppliers.Count > 0 ? _dataService.Suppliers.Count : 4;
            Panel pnlCard4 = CreateKpiCard("ACTIVE SUPPLIERS", $"{supplierCount} Vendors", "Contracted distributors", startX, cardW, out _lblSupplierCount);

            pnlKpis.Controls.Add(pnlCard1);
            pnlKpis.Controls.Add(pnlCard2);
            pnlKpis.Controls.Add(pnlCard3);
            pnlKpis.Controls.Add(pnlCard4);

            // 3. TOOLBAR (Search & Actions)
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(24, 8, 24, 8),
                BackColor = AppTheme.OperationsBarBg
            };

            Panel pnlSearch = new Panel
            {
                Location = new Point(24, 9),
                Size = new Size(280, 34),
                BackColor = Color.White
            };
            pnlSearch.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(220, 215, 205), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };

            _txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                Location = new Point(10, 8),
                Width = 260
            };
            _txtSearch.TextChanged += (s, e) => FilterPOs();
            pnlSearch.Controls.Add(_txtSearch);

            SunshineButton btnNewPo = new SunshineButton
            {
                Text = "+ Create Purchase Order",
                IsPrimary = true,
                Location = new Point(320, 9),
                Size = new Size(190, 34),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold)
            };
            btnNewPo.Click += BtnNewPo_Click;

            SunshineButton btnReceive = new SunshineButton
            {
                Text = "✓ Mark Shipment Received",
                IsPrimary = false,
                Location = new Point(520, 9),
                Size = new Size(190, 34),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold)
            };
            btnReceive.Click += BtnReceive_Click;

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(btnNewPo);
            pnlToolbar.Controls.Add(btnReceive);

            // 4. MAIN DATA GRID
            Panel pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 12, 24, 16),
                BackColor = AppTheme.AppBackground
            };

            _gridPOs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 238, 232),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 40 },
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _gridPOs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 244, 239);
            _gridPOs.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridPOs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _gridPOs.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            _gridPOs.DefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridPOs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(244, 234, 185);
            _gridPOs.DefaultCellStyle.SelectionForeColor = AppTheme.TextDark;

            _gridPOs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PO NUMBER", DataPropertyName = "PoNumber", FillWeight = 60 });
            _gridPOs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "VENDOR / SUPPLIER", DataPropertyName = "SupplierName", FillWeight = 110 });
            _gridPOs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEMS DESCRIPTION", DataPropertyName = "ItemDescription", FillWeight = 130 });
            _gridPOs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY", DataPropertyName = "Quantity", FillWeight = 35 });
            _gridPOs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "UNIT COST", DataPropertyName = "UnitCost", FillWeight = 50, DefaultCellStyle = { Format = "₱#,##0.00" } });
            _gridPOs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TOTAL COST", DataPropertyName = "TotalAmount", FillWeight = 60, DefaultCellStyle = { Format = "₱#,##0.00" } });
            _gridPOs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", DataPropertyName = "Status", FillWeight = 50 });
            _gridPOs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ORDER DATE", DataPropertyName = "OrderDate", FillWeight = 55, DefaultCellStyle = { Format = "yyyy-MM-dd" } });

            pnlGridContainer.Controls.Add(_gridPOs);

            Controls.Add(pnlGridContainer);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlKpis);
            Controls.Add(pnlHeader);

            RefreshGrid();
        }

        private Panel CreateKpiCard(string title, string value, string subtitle, int x, int width, out Label lblVal)
        {
            Panel card = new Panel
            {
                Location = new Point(x, 8),
                Size = new Size(width, 68),
                BackColor = Color.FromArgb(248, 247, 243)
            };
            card.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(230, 226, 218), 1);
                e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };

            Label lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(12, 8),
                AutoSize = true
            };

            lblVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(12, 24),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 7.2F, FontStyle.Regular),
                ForeColor = Color.FromArgb(140, 135, 125),
                Location = new Point(12, 48),
                AutoSize = true
            };

            card.Controls.Add(lblT);
            card.Controls.Add(lblVal);
            card.Controls.Add(lblSub);

            return card;
        }

        private void FilterPOs()
        {
            string query = _txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                _gridPOs.DataSource = _orders.ToList();
            }
            else
            {
                _gridPOs.DataSource = _orders
                    .Where(o => o.PoNumber.ToLower().Contains(query) ||
                                o.SupplierName.ToLower().Contains(query) ||
                                o.ItemDescription.ToLower().Contains(query) ||
                                o.Status.ToLower().Contains(query))
                    .ToList();
            }
        }

        private void RefreshGrid()
        {
            _gridPOs.DataSource = null;
            _gridPOs.DataSource = _orders.ToList();

            if (_lblTotalPoCount != null) _lblTotalPoCount.Text = _orders.Count.ToString();
            if (_lblPendingDelivery != null) _lblPendingDelivery.Text = $"{_orders.Count(o => o.Status != "Received")} Active";
            if (_lblTotalSpend != null) _lblTotalSpend.Text = $"₱{_orders.Sum(o => o.TotalAmount):N0}";
        }

        private void BtnNewPo_Click(object? sender, EventArgs e)
        {
            using var dlg = new Form
            {
                Text = "Create Purchase Order",
                Size = new Size(420, 380),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            int y = 16;
            Label lblSup = new Label { Text = "Supplier / Vendor *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            ComboBox cboSup = new ComboBox { Location = new Point(24, y), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            if (_dataService.Suppliers.Count > 0)
            {
                foreach (var s in _dataService.Suppliers) cboSup.Items.Add(s.SupplierName);
            }
            else
            {
                cboSup.Items.AddRange(new object[] { "Asus Republic of Gamers Corp", "Corsair Memory Philippines", "Samsung Semiconductor", "Intel Microelectronics" });
            }
            if (cboSup.Items.Count > 0) cboSup.SelectedIndex = 0;
            y += 34;

            Label lblItem = new Label { Text = "Component Description *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtItem = new TextBox { Location = new Point(24, y), Width = 350, Font = new Font("Segoe UI", 9.5F) };
            y += 34;

            Label lblQty = new Label { Text = "Quantity *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            NumericUpDown nudQty = new NumericUpDown { Location = new Point(24, y), Width = 160, Minimum = 1, Maximum = 1000, Value = 10 };
            y += 34;

            Label lblCost = new Label { Text = "Unit Cost (₱) *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            NumericUpDown nudCost = new NumericUpDown { Location = new Point(24, y), Width = 160, Minimum = 1, Maximum = 500000, Value = 5000 };
            y += 40;

            SunshineButton btnSave = new SunshineButton
            {
                Text = "Submit Purchase Order",
                IsPrimary = true,
                Location = new Point(24, y),
                Size = new Size(180, 36)
            };
            btnSave.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtItem.Text))
                {
                    MessageBox.Show("Please enter component description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _orders.Insert(0, new PurchaseOrderItem
                {
                    PoNumber = $"PO-2026-{_orders.Count + 1:D3}",
                    SupplierName = cboSup.SelectedItem?.ToString() ?? "Vendor",
                    ItemDescription = txtItem.Text.Trim(),
                    Quantity = (int)nudQty.Value,
                    UnitCost = nudCost.Value,
                    Status = "In Transit",
                    OrderDate = DateTime.UtcNow,
                    ApprovedBy = "Admin"
                });
                RefreshGrid();
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.Add(lblSup);
            dlg.Controls.Add(cboSup);
            dlg.Controls.Add(lblItem);
            dlg.Controls.Add(txtItem);
            dlg.Controls.Add(lblQty);
            dlg.Controls.Add(nudQty);
            dlg.Controls.Add(lblCost);
            dlg.Controls.Add(nudCost);
            dlg.Controls.Add(btnSave);

            dlg.ShowDialog(this);
        }

        private void BtnReceive_Click(object? sender, EventArgs e)
        {
            if (_gridPOs.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a purchase order to receive.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = _gridPOs.SelectedRows[0].DataBoundItem as PurchaseOrderItem;
            if (selected == null) return;

            if (selected.Status == "Received")
            {
                MessageBox.Show("This purchase order has already been received.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            selected.Status = "Received";
            selected.ReceivedDate = DateTime.UtcNow;
            RefreshGrid();
            MessageBox.Show($"Shipment for {selected.PoNumber} ({selected.ItemDescription}) marked as Received.", "Fulfillment", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
