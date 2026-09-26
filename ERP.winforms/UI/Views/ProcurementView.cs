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
    public class ProcurementView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridPOs = null!;
        private TextBox _txtSearch = null!;
        private Label _lblTotalPoCount = null!;
        private Label _lblPendingDelivery = null!;
        private Label _lblTotalSpend = null!;
        private Label _lblSupplierCount = null!;

        public ProcurementView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = false;

            InitializeLayout();

            _dataService.PurchaseOrdersChanged += () =>
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(new Action(RefreshGrid));
                }
                else
                {
                    RefreshGrid();
                }
            };
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
                Text = "Medium Enterprise Logistics  |  Component Inbound Orders & Supplier Shipments",
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
            Panel pnlCard1 = CreateKpiCard("TOTAL PURCHASE ORDERS", "0", "All logged POs", startX, cardW, out _lblTotalPoCount);
            startX += cardW + cardGap;

            // KPI 2: Pending Delivery
            Panel pnlCard2 = CreateKpiCard("IN TRANSIT / PENDING", "0 Active", "Awaiting fulfillment", startX, cardW, out _lblPendingDelivery);
            startX += cardW + cardGap;

            // KPI 3: Total Spend
            Panel pnlCard3 = CreateKpiCard("TOTAL COMMITMENT", "₱0", "Allocated procurement capital", startX, cardW, out _lblTotalSpend);
            startX += cardW + cardGap;

            // KPI 4: Connected Suppliers
            int supplierCount = _dataService.Suppliers.Count;
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

            SunshineButton btnCancel = new SunshineButton
            {
                Text = "✕ Cancel Purchase Order",
                IsPrimary = false,
                Location = new Point(720, 9),
                Size = new Size(190, 34),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold)
            };
            btnCancel.Click += BtnCancel_Click;

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(btnNewPo);
            pnlToolbar.Controls.Add(btnReceive);
            pnlToolbar.Controls.Add(btnCancel);

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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false
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
            var orders = _dataService.PurchaseOrders;
            if (string.IsNullOrEmpty(query))
            {
                _gridPOs.DataSource = orders.ToList();
            }
            else
            {
                _gridPOs.DataSource = orders
                    .Where(o => (o.PoNumber != null && o.PoNumber.ToLower().Contains(query)) ||
                                (o.SupplierName != null && o.SupplierName.ToLower().Contains(query)) ||
                                (o.ItemDescription != null && o.ItemDescription.ToLower().Contains(query)) ||
                                (o.Status != null && o.Status.ToLower().Contains(query)))
                    .ToList();
            }
        }

        public void RefreshGrid()
        {
            var orders = _dataService.PurchaseOrders;
            _gridPOs.DataSource = null;
            _gridPOs.DataSource = orders.ToList();

            if (_lblTotalPoCount != null) _lblTotalPoCount.Text = orders.Count.ToString();
            if (_lblPendingDelivery != null) _lblPendingDelivery.Text = $"{orders.Count(o => o.Status != "Received" && o.Status != "Cancelled")} Active";
            if (_lblTotalSpend != null) _lblTotalSpend.Text = $"₱{orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount):N0}";
            if (_lblSupplierCount != null) _lblSupplierCount.Text = $"{_dataService.Suppliers.Count} Vendors";
        }

        private class SupplierComboItem
        {
            public int SupplierId { get; set; }
            public string SupplierName { get; set; } = string.Empty;
            public override string ToString() => SupplierName;
        }

        private class ProductComboItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
            public override string ToString() => ProductName;
        }

        private void BtnNewPo_Click(object? sender, EventArgs e)
        {
            using var dlg = new Form
            {
                Text = "Create Purchase Order",
                Size = new Size(460, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            int y = 16;
            Label lblSup = new Label { Text = "Supplier / Vendor *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            ComboBox cboSup = new ComboBox { Location = new Point(24, y), Width = 390, DropDownStyle = ComboBoxStyle.DropDownList };
            
            var suppliers = _dataService.Suppliers;
            if (suppliers.Count > 0)
            {
                foreach (var s in suppliers)
                {
                    cboSup.Items.Add(new SupplierComboItem { SupplierId = s.SupplierId, SupplierName = s.SupplierName });
                }
            }
            if (cboSup.Items.Count > 0) cboSup.SelectedIndex = 0;
            y += 34;

            Label lblProd = new Label { Text = "Select Existing Product (Optional)", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            ComboBox cboProd = new ComboBox { Location = new Point(24, y), Width = 390, DropDownStyle = ComboBoxStyle.DropDownList };
            cboProd.Items.Add(new ProductComboItem { ProductId = 0, ProductName = "-- Custom Component --", UnitPrice = 0 });
            foreach (var p in _dataService.Products)
            {
                cboProd.Items.Add(new ProductComboItem { ProductId = p.ProductId, ProductName = p.ProductName, UnitPrice = p.UnitPrice });
            }
            cboProd.SelectedIndex = 0;
            y += 34;

            Label lblItem = new Label { Text = "Component Description *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtItem = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F) };
            y += 34;

            cboProd.SelectedIndexChanged += (s, ev) =>
            {
                if (cboProd.SelectedItem is ProductComboItem selectedProd && selectedProd.ProductId > 0)
                {
                    txtItem.Text = selectedProd.ProductName;
                }
            };

            Label lblQty = new Label { Text = "Quantity *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            NumericUpDown nudQty = new NumericUpDown { Location = new Point(24, y), Width = 180, Minimum = 1, Maximum = 10000, Value = 10 };
            y += 34;

            Label lblCost = new Label { Text = "Unit Cost (₱) *", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            NumericUpDown nudCost = new NumericUpDown { Location = new Point(24, y), Width = 180, Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, Value = 5000 };
            y += 34;

            Label lblNotes = new Label { Text = "Order Notes / Remarks", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
            y += 20;
            TextBox txtNotes = new TextBox { Location = new Point(24, y), Width = 390, Font = new Font("Segoe UI", 9.5F) };
            y += 40;

            SunshineButton btnSave = new SunshineButton
            {
                Text = "Submit Purchase Order",
                IsPrimary = true,
                Location = new Point(24, y),
                Size = new Size(190, 36)
            };

            btnSave.Click += (s, ev) =>
            {
                if (cboSup.SelectedItem is not SupplierComboItem selectedSup || selectedSup.SupplierId <= 0)
                {
                    MessageBox.Show("Please select a valid supplier/vendor.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtItem.Text))
                {
                    MessageBox.Show("Please enter component description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int qty = (int)nudQty.Value;
                decimal unitCost = nudCost.Value;
                int? prodId = (cboProd.SelectedItem is ProductComboItem p && p.ProductId > 0) ? p.ProductId : null;

                var newPo = new PurchaseOrder
                {
                    SupplierId = selectedSup.SupplierId,
                    SupplierName = selectedSup.SupplierName,
                    OrderDate = DateTime.UtcNow,
                    Status = "In Transit",
                    TotalAmount = qty * unitCost,
                    Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim(),
                    CreatedBy = "Admin",
                    ApprovedBy = "Marcus V. (Manager)",
                    IsActive = true,
                    Items = new List<PurchaseOrderItem>
                    {
                        new PurchaseOrderItem
                        {
                            ProductId = prodId,
                            ItemDescription = txtItem.Text.Trim(),
                            Quantity = qty,
                            UnitCost = unitCost,
                            TotalAmount = qty * unitCost
                        }
                    }
                };

                bool success = _dataService.AddPurchaseOrder(newPo);
                if (success)
                {
                    RefreshGrid();
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                else
                {
                    MessageBox.Show("Failed to save purchase order. Please check input values or system connection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.Add(lblSup);
            dlg.Controls.Add(cboSup);
            dlg.Controls.Add(lblProd);
            dlg.Controls.Add(cboProd);
            dlg.Controls.Add(lblItem);
            dlg.Controls.Add(txtItem);
            dlg.Controls.Add(lblQty);
            dlg.Controls.Add(nudQty);
            dlg.Controls.Add(lblCost);
            dlg.Controls.Add(nudCost);
            dlg.Controls.Add(lblNotes);
            dlg.Controls.Add(txtNotes);
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

            var selected = _gridPOs.SelectedRows[0].DataBoundItem as PurchaseOrder;
            if (selected == null) return;

            if (selected.Status == "Received")
            {
                MessageBox.Show("This purchase order has already been received.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selected.Status == "Cancelled")
            {
                MessageBox.Show("Cannot receive a cancelled purchase order.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selected.Status = "Received";
            selected.ReceivedDate = DateTime.UtcNow;

            bool success = _dataService.UpdatePurchaseOrder(selected);
            if (success)
            {
                RefreshGrid();
                MessageBox.Show($"Shipment for {selected.PoNumber} marked as Received.", "Fulfillment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to update purchase order status.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (_gridPOs.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a purchase order to cancel.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = _gridPOs.SelectedRows[0].DataBoundItem as PurchaseOrder;
            if (selected == null) return;

            if (selected.Status == "Cancelled" || !selected.IsActive)
            {
                MessageBox.Show("This purchase order has already been cancelled.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to cancel purchase order {selected.PoNumber}?", "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            bool success = _dataService.TogglePurchaseOrderArchive(selected.PurchaseOrderId);
            if (success)
            {
                RefreshGrid();
                MessageBox.Show($"Purchase order {selected.PoNumber} has been cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to cancel purchase order.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
