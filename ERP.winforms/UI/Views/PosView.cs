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
    public class PosView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly List<CartItem> _cart = new();

        public Action? OnOrderCompleted;

        private FlowLayoutPanel _flpProducts = null!;
        private FlowLayoutPanel _flpCategories = null!;
        private DataGridView _gridCart = null!;
        private Label _lblSubtotal = null!;
        private Label _lblDiscount = null!;
        private Label _lblTax = null!;
        private Label _lblGrandTotal = null!;
        private Label _lblCartSummary = null!;
        private SunshineButton _btnCheckout = null!;

        private ComboBox _cboCustomer = null!;
        private Customer? _selectedCustomer;
        private decimal _appliedDiscount = 0;
        private string _discountReason = "";

        private string _selectedCategory = "All";
        private string _searchQuery = "";

        public PosView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            InitializeLayout();

            _dataService.CategoriesChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => PopulateCategoryTabs()));
                    }
                    else
                    {
                        PopulateCategoryTabs();
                    }
                }
            };

            _dataService.CustomersChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => PopulatePosCustomers()));
                    }
                    else
                    {
                        PopulatePosCustomers();
                    }
                }
            };
        }

        private void InitializeLayout()
        {
            SuspendLayout();
            Controls.Clear();

            // Split 2 columns: 62% Catalog on Left, 38% Active Cart on Right
            TableLayoutPanel tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16, 12, 16, 12)
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));

            // ==========================================
            // LEFT: PRODUCT SEARCH & HARDWARE CATALOG
            // ==========================================
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };

            // 1. Search Bar with empty placeholder (clean) and [Fast Scanner] button
            Panel pnlSearch = new Panel { Dock = DockStyle.Top, Height = 40 };

            TextBox txtSearch = new TextBox
            {
                PlaceholderText = "", // Removed placeholder text as requested
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Location = new Point(0, 6),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Width = pnlLeft.Width - 140
            };
            txtSearch.TextChanged += (s, e) =>
            {
                _searchQuery = txtSearch.Text.Trim();
                PopulateCatalog();
            };

            SunshineButton btnScanner = new SunshineButton
            {
                Text = "Fast Scanner",
                IsPrimary = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlLeft.Width - 130, 4),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnScanner.Click += (s, e) => MessageBox.Show("Barcode optical scanner ready. Scan any hardware box.", "Scanner Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);

            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(btnScanner);

            // 2. Category Filter Tabs Row
            _flpCategories = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                WrapContents = false,
                AutoScroll = false,
                Margin = new Padding(0, 4, 0, 8)
            };
            PopulateCategoryTabs();

            // 3. FlowLayoutPanel for hardware cards
            _flpProducts = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = AppTheme.AppBackground,
                Padding = new Padding(0, 6, 0, 0)
            };
            _flpProducts.Resize += (s, e) => AdjustProductCardSizes();

            pnlLeft.Controls.Add(_flpProducts);
            pnlLeft.Controls.Add(_flpCategories);
            pnlLeft.Controls.Add(pnlSearch);

            tlpMain.Controls.Add(pnlLeft, 0, 0);

            // ==========================================
            // RIGHT: ACTIVE CASHIER CART
            // ==========================================
            SunshineCard cardCart = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            // 1. Cart Header: Title & TRANS #
            Panel pnlCartHeader = new Panel { Dock = DockStyle.Top, Height = 36 };
            Label lblCartTitle = new Label { Text = "Active Cashier Cart", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(0, 4), AutoSize = true };

            Panel pnlTrans = new Panel { Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(cardCart.Width - 140, 4), Size = new Size(115, 26), BackColor = Color.FromArgb(20, 21, 17) };
            Label lblTrans = new Label { Text = $"TRANS #{DateTime.Now:fff}", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.Primary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlTrans.Controls.Add(lblTrans);

            pnlCartHeader.Controls.Add(lblCartTitle);
            pnlCartHeader.Controls.Add(pnlTrans);

            // 2. Customer Selection Bar
            Panel pnlCustomer = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(254, 252, 245), Margin = new Padding(0, 4, 0, 6) };
            pnlCustomer.Paint += (s, e) =>
            {
                using Pen pen = new Pen(Color.FromArgb(235, 230, 218), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlCustomer.Width - 1, pnlCustomer.Height - 1);
            };

            Label lblCustPrompt = new Label { Text = "Client:", Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(8, 10), AutoSize = true };

            _cboCustomer = new ComboBox
            {
                Location = new Point(70, 7),
                Width = cardCart.Width - 95,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            PopulatePosCustomers();
            _cboCustomer.SelectedIndexChanged += (s, e) =>
            {
                string sel = _cboCustomer.SelectedItem?.ToString() ?? "";
                if (sel.StartsWith("Walk-in", StringComparison.OrdinalIgnoreCase))
                {
                    _selectedCustomer = null;
                }
                else
                {
                    _selectedCustomer = _dataService.Customers.FirstOrDefault(c =>
                        sel.StartsWith(c.CustomerName, StringComparison.OrdinalIgnoreCase));
                }
            };

            pnlCustomer.Controls.Add(lblCustPrompt);
            pnlCustomer.Controls.Add(_cboCustomer);

            // 3. Cart DataGridView
            _gridCart = new DataGridView
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
                RowTemplate = { Height = 42 }
            };

            _gridCart.EnableHeadersVisualStyles = false;
            _gridCart.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridCart.ColumnHeadersHeight = 32;
            _gridCart.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM DESCRIPTION", FillWeight = 42, Name = "ColDesc" });
            _gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PRICE", FillWeight = 18, Name = "ColPrice" });
            _gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY", FillWeight = 12, Name = "ColQty" });
            _gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TOTAL (PHP)", FillWeight = 18, Name = "ColTotal" });
            _gridCart.Columns.Add(new DataGridViewButtonColumn { HeaderText = "ACTION", FillWeight = 14, Name = "ColRemove" });

            _gridCart.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && _gridCart.Columns["ColRemove"] != null && e.ColumnIndex == _gridCart.Columns["ColRemove"]!.Index)
                {
                    if (e.RowIndex < _cart.Count)
                    {
                        _cart.RemoveAt(e.RowIndex);
                        UpdateCartTotals();
                    }
                }
            };

            // 4. Cart Sub-note: Items count and Clear Cart button
            Panel pnlNote = new Panel { Dock = DockStyle.Bottom, Height = 26 };
            _lblCartSummary = new Label { Text = "Cart Items: 0 lines (0 units)", Font = new Font("Segoe UI", 7.5F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(0, 4), AutoSize = true };

            Button btnClearCart = new Button
            {
                Text = "🗑️ Clear Cart",
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.FromArgb(184, 50, 38),
                BackColor = Color.FromArgb(254, 242, 242),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(cardCart.Width - 110, 2),
                Size = new Size(95, 22),
                Cursor = Cursors.Hand
            };
            btnClearCart.FlatAppearance.BorderColor = Color.FromArgb(240, 200, 200);
            btnClearCart.Click += (s, e) =>
            {
                if (_cart.Count == 0) return;
                var res = MessageBox.Show("Remove all items from the current cart?", "Clear Cart", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    _cart.Clear();
                    UpdateCartTotals();
                }
            };

            pnlNote.Controls.Add(_lblCartSummary);
            pnlNote.Controls.Add(btnClearCart);

            // 5. Financial Summary & Totals in PHP with Manager-Authorized Discount
            Panel pnlTotals = new Panel { Dock = DockStyle.Bottom, Height = 175 };

            Label lblSubtotalTxt = new Label { Text = "Subtotal", Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(0, 6), AutoSize = true };
            _lblSubtotal = new Label { Text = "₱0.00", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(cardCart.Width - 140, 6), Size = new Size(130, 18), TextAlign = ContentAlignment.MiddleRight };

            Label lblDiscountTxt = new Label { Text = "Discount", Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), ForeColor = Color.FromArgb(184, 50, 38), Location = new Point(0, 28), AutoSize = true };

            Button btnDiscountAction = new Button
            {
                Text = "🏷️ Apply Discount",
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 110, 10),
                BackColor = Color.FromArgb(254, 250, 238),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(62, 26),
                Size = new Size(116, 22),
                Cursor = Cursors.Hand
            };
            btnDiscountAction.FlatAppearance.BorderColor = Color.FromArgb(235, 215, 170);
            btnDiscountAction.Click += (s, e) => OpenDiscountDialog();

            _lblDiscount = new Label { Text = "-₱0.00", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(184, 50, 38), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(cardCart.Width - 140, 28), Size = new Size(130, 18), TextAlign = ContentAlignment.MiddleRight };

            Label lblTaxTxt = new Label { Text = "Tax (12% VAT)", Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(0, 50), AutoSize = true };
            _lblTax = new Label { Text = "₱0.00", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(cardCart.Width - 140, 50), Size = new Size(130, 18), TextAlign = ContentAlignment.MiddleRight };

            Panel pnlLine = new Panel { Location = new Point(0, 74), Size = new Size(cardCart.Width - 20, 1), BackColor = Color.FromArgb(225, 220, 208), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            Label lblPayable = new Label { Text = "AMOUNT PAYABLE", Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(0, 80), AutoSize = true };
            Label lblTotalTxt = new Label { Text = "TOTAL:", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(0, 94), AutoSize = true };
            _lblGrandTotal = new Label { Text = "₱0.00", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(cardCart.Width - 220, 88), Size = new Size(210, 36), TextAlign = ContentAlignment.MiddleRight };

            // Quick payment method pills: Cash & Card (POS)
            Panel pnlQuickPay = new Panel { Location = new Point(0, 134), Size = new Size(cardCart.Width - 20, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            string[] payOpts = { "Cash", "Card (POS)" };
            int qx = 0;
            int qWidth = (cardCart.Width - 36) / 2;
            foreach (var opt in payOpts)
            {
                Button btnQ = new Button { Text = opt, Location = new Point(qx, 0), Size = new Size(qWidth, 28), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), BackColor = Color.White };
                btnQ.FlatAppearance.BorderColor = Color.FromArgb(220, 215, 205);
                btnQ.Click += (s, e) => Checkout(opt);
                pnlQuickPay.Controls.Add(btnQ);
                qx += qWidth + 8;
            }

            pnlTotals.Controls.Add(lblSubtotalTxt);
            pnlTotals.Controls.Add(_lblSubtotal);
            pnlTotals.Controls.Add(lblDiscountTxt);
            pnlTotals.Controls.Add(btnDiscountAction);
            pnlTotals.Controls.Add(_lblDiscount);
            pnlTotals.Controls.Add(lblTaxTxt);
            pnlTotals.Controls.Add(_lblTax);
            pnlTotals.Controls.Add(pnlLine);
            pnlTotals.Controls.Add(lblPayable);
            pnlTotals.Controls.Add(lblTotalTxt);
            pnlTotals.Controls.Add(_lblGrandTotal);
            pnlTotals.Controls.Add(pnlQuickPay);

            // 6. Action Footer: [Clear Cart] [Hold] [Checkout / Pay (F12)]
            Panel pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 46, Margin = new Padding(0, 8, 0, 0) };

            SunshineButton btnClear = new SunshineButton
            {
                Text = "Clear Cart",
                IsPrimary = false,
                Location = new Point(0, 6),
                Size = new Size(95, 38),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnClear.Click += (s, e) =>
            {
                _cart.Clear();
                UpdateCartTotals();
            };

            SunshineButton btnHold = new SunshineButton
            {
                Text = "Hold",
                IsPrimary = false,
                Location = new Point(102, 6),
                Size = new Size(70, 38),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnHold.Click += (s, e) => MessageBox.Show("Cart order held as active pending session.", "Order Held", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _btnCheckout = new SunshineButton
            {
                Text = "Checkout / Pay",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(180, 6),
                Size = new Size(cardCart.Width - 190, 38),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _btnCheckout.Click += (s, e) => Checkout("Cash");

            pnlActions.Controls.Add(btnClear);
            pnlActions.Controls.Add(btnHold);
            pnlActions.Controls.Add(_btnCheckout);

            cardCart.Controls.Add(_gridCart);
            cardCart.Controls.Add(pnlNote);
            cardCart.Controls.Add(pnlTotals);
            cardCart.Controls.Add(pnlActions);
            cardCart.Controls.Add(pnlCustomer);
            cardCart.Controls.Add(pnlCartHeader);

            tlpMain.Controls.Add(cardCart, 1, 0);

            Controls.Add(tlpMain);
            ResumeLayout(false);

            PopulateCatalog();
        }

        public void PopulateCatalog()
        {
            if (_flpProducts == null) return;
            _flpProducts.SuspendLayout();
            _flpProducts.Controls.Clear();

            var prods = _dataService.Products.Where(p => p.IsActive);

            if (_selectedCategory != "All")
            {
                prods = prods.Where(p => p.CategoryName.Equals(_selectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                prods = prods.Where(p => p.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                         p.ProductCode.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var prod in prods)
            {
                _flpProducts.Controls.Add(CreateHardwareCard(prod));
            }

            _flpProducts.ResumeLayout(true);
        }

        private int CalculateCardWidth()
        {
            if (_flpProducts == null) return 240;
            int scrollWidth = SystemInformation.VerticalScrollBarWidth + 14;
            int usableWidth = _flpProducts.ClientSize.Width - scrollWidth;
            if (usableWidth < 280) return 240;
            int cols = Math.Max(2, (usableWidth + 10) / 235);
            int cardWidth = (usableWidth - ((cols - 1) * 10)) / cols;
            return Math.Max(180, cardWidth);
        }

        private void AdjustProductCardSizes()
        {
            if (_flpProducts == null || _flpProducts.Controls.Count == 0) return;
            int newWidth = CalculateCardWidth();
            _flpProducts.SuspendLayout();
            foreach (Control ctrl in _flpProducts.Controls)
            {
                if (ctrl is Panel p)
                {
                    p.Width = newWidth;
                    foreach (Control inner in p.Controls)
                    {
                        if (inner.Name == "StockBadge")
                        {
                            inner.Location = new Point(newWidth - inner.Width - 8, inner.Location.Y);
                        }
                        else if (inner.Name == "AddBtn")
                        {
                            inner.Location = new Point(newWidth - inner.Width - 8, inner.Location.Y);
                        }
                        else if (inner.Name == "Title" || inner.Name == "Specs")
                        {
                            inner.Width = newWidth - 16;
                        }
                    }
                }
            }
            _flpProducts.ResumeLayout(true);
        }

        private Panel CreateHardwareCard(Product prod)
        {
            int cardWidth = CalculateCardWidth();
            Panel card = new Panel
            {
                Size = new Size(cardWidth, 160),
                Margin = new Padding(0, 0, 10, 10),
                BackColor = Color.White
            };
            card.Paint += (s, e) =>
            {
                using Pen pen = new Pen(AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // Top Row: SKU code on left, stock badge on right
            Label lblSku = new Label { Text = prod.ProductCode, Font = new Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(8, 8), AutoSize = true };

            Panel pnlStockBadge = new Panel
            {
                Name = "StockBadge",
                Location = new Point(cardWidth - 92, 6),
                Size = new Size(84, 20),
                BackColor = prod.StockQuantity > 2 ? AppTheme.GreenPillBg : AppTheme.AmberPillBg
            };
            Label lblStockBadge = new Label
            {
                Text = prod.StockQuantity > 2 ? $"● {prod.StockQuantity} In Stock" : $"● {prod.StockQuantity} Low Stock",
                Font = new Font("Segoe UI", 6.5F, FontStyle.Bold),
                ForeColor = prod.StockQuantity > 2 ? AppTheme.GreenPillText : AppTheme.AmberPillText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlStockBadge.Controls.Add(lblStockBadge);

            // Title
            Label lblTitle = new Label
            {
                Name = "Title",
                Text = prod.Name,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(8, 30),
                Size = new Size(cardWidth - 16, 38),
                AutoEllipsis = true
            };

            // Specs / Description
            Label lblSpecs = new Label
            {
                Name = "Specs",
                Text = string.IsNullOrWhiteSpace(prod.Description) ? "Hardware Component SKU" : prod.Description,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(8, 70),
                Size = new Size(cardWidth - 16, 28),
                AutoEllipsis = true
            };

            // Bottom: MSRP Tender on left, [+ Add] button on right
            Label lblMsrp = new Label { Text = "MSRP Tender", Font = new Font("Segoe UI", 7F, FontStyle.Regular), ForeColor = AppTheme.TextMuted, Location = new Point(8, 104), AutoSize = true };
            Label lblPrice = new Label { Text = $"₱{prod.Price:N2}", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(8, 118), AutoSize = true };

            SunshineButton btnAdd = new SunshineButton
            {
                Name = "AddBtn",
                Text = "+ Add",
                IsPrimary = prod.StockQuantity > 0,
                Location = new Point(cardWidth - 78, 114),
                Size = new Size(70, 30),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnAdd.Click += (s, e) => AddToCart(prod);

            card.Controls.Add(lblSku);
            card.Controls.Add(pnlStockBadge);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblSpecs);
            card.Controls.Add(lblMsrp);
            card.Controls.Add(lblPrice);
            card.Controls.Add(btnAdd);

            return card;
        }

        private void AddToCart(Product prod)
        {
            if (prod.StockQuantity <= 0)
            {
                MessageBox.Show("This item is currently out of stock!", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var item = _cart.FirstOrDefault(c => c.ProductId == prod.ProductId);
            if (item != null)
            {
                if (item.Quantity >= prod.StockQuantity)
                {
                    MessageBox.Show("Requested quantity exceeds available inventory.", "Stock Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                item.Quantity++;
            }
            else
            {
                _cart.Add(new CartItem { ProductId = prod.ProductId, ProductName = prod.Name, UnitPrice = prod.Price, Quantity = 1 });
            }

            UpdateCartTotals();
        }

        private void UpdateCartTotals()
        {
            _gridCart.Rows.Clear();

            decimal subtotal = 0;
            int totalUnits = 0;

            foreach (var item in _cart)
            {
                subtotal += item.TotalPrice;
                totalUnits += item.Quantity;
                int rIdx = _gridCart.Rows.Add(item.ProductName, $"₱{item.UnitPrice:N2}", item.Quantity, $"₱{item.TotalPrice:N2}", "❌ Remove");
                var r = _gridCart.Rows[rIdx];
                r.Cells["ColRemove"].Style.ForeColor = Color.FromArgb(184, 50, 38);
                r.Cells["ColRemove"].Style.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            }

            if (_appliedDiscount > subtotal) _appliedDiscount = subtotal;

            decimal netSubtotal = Math.Max(0, subtotal - _appliedDiscount);
            decimal tax = Math.Round(netSubtotal * 0.12m, 2);
            decimal total = netSubtotal + tax;

            _lblSubtotal.Text = $"₱{subtotal:N2}";
            if (_lblDiscount != null) _lblDiscount.Text = $"-₱{_appliedDiscount:N2}";
            _lblTax.Text = $"₱{tax:N2}";
            _lblGrandTotal.Text = $"₱{total:N2}";
            _lblCartSummary.Text = $"Cart Items: {_cart.Count} lines ({totalUnits} units)";
        }

        private void OpenDiscountDialog()
        {
            decimal subtotal = _cart.Sum(c => c.TotalPrice);
            if (subtotal <= 0)
            {
                MessageBox.Show("Please add hardware items to the cart before applying a discount.", "Cart Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new ApplyDiscountDialog(subtotal, _appliedDiscount);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _appliedDiscount = dlg.DiscountAmount;
                _discountReason = dlg.DiscountReason;
                UpdateCartTotals();
                MessageBox.Show($"Discount of ₱{_appliedDiscount:N2} ({_discountReason}) authorized by manager and applied to current cart!", "Discount Authorized", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PopulatePosCustomers()
        {
            if (_cboCustomer == null) return;
            string current = _cboCustomer.SelectedItem?.ToString() ?? "Walk-in Customer (Standard)";
            _cboCustomer.Items.Clear();
            _cboCustomer.Items.Add("Walk-in Customer (Standard)");
            foreach (var c in _dataService.Customers.Where(cust => cust.IsActive))
            {
                string display = string.IsNullOrWhiteSpace(c.CustomerCode) ? c.CustomerName : $"{c.CustomerName} ({c.CustomerCode})";
                _cboCustomer.Items.Add(display);
            }
            int idx = _cboCustomer.FindStringExact(current);
            _cboCustomer.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void Checkout(string paymentMethod)
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("Please add hardware items to the cart before checking out.", "Empty Cart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal subtotal = _cart.Sum(c => c.TotalPrice);
            decimal netSubtotal = Math.Max(0, subtotal - _appliedDiscount);
            decimal tax = Math.Round(netSubtotal * 0.12m, 2);
            decimal total = netSubtotal + tax;

            string custName = _selectedCustomer != null ? _selectedCustomer.CustomerName : "Walk-in Customer";

            Order order = new Order
            {
                CompanyId = _dataService.ActiveCompanyId,
                CustomerName = custName,
                CreatedAt = DateTime.Now,
                Items = new List<CartItem>(_cart),
                Subtotal = subtotal,
                Tax = tax,
                Discount = _appliedDiscount,
                TotalAmount = total,
                PaymentMethod = paymentMethod
            };

            using var receipt = new ReceiptForm(order);
            if (receipt.ShowDialog(this) == DialogResult.OK)
            {
                _dataService.ProcessOrder(order);
                _cart.Clear();
                _appliedDiscount = 0;
                _discountReason = "";
                UpdateCartTotals();
                PopulateCatalog();
                OnOrderCompleted?.Invoke();
            }
        }

        private void PopulateCategoryTabs()
        {
            if (_flpCategories == null) return;
            _flpCategories.SuspendLayout();
            _flpCategories.Controls.Clear();

            var catList = new List<string> { "All Items" };
            catList.AddRange(_dataService.Categories.Select(c => c.Name));

            // If selected category no longer exists, fall back to All
            if (_selectedCategory != "All" && !_dataService.Categories.Any(c => c.Name.Equals(_selectedCategory, StringComparison.OrdinalIgnoreCase)))
            {
                _selectedCategory = "All";
            }

            foreach (var c in catList)
            {
                bool isSel = (c == "All Items" && _selectedCategory == "All") || (c == _selectedCategory);
                Button btnC = new Button
                {
                    Text = c,
                    AutoSize = true,
                    Height = 28,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    ForeColor = isSel ? Color.White : AppTheme.TextDark,
                    BackColor = isSel ? Color.FromArgb(20, 21, 17) : Color.White,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 2, 6, 2)
                };
                btnC.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
                string catName = c;
                btnC.Click += (s, e) =>
                {
                    _selectedCategory = catName == "All Items" ? "All" : catName;
                    foreach (Control ctrl in _flpCategories.Controls)
                    {
                        if (ctrl is Button b)
                        {
                            bool sel = (b.Text == catName);
                            b.BackColor = sel ? Color.FromArgb(20, 21, 17) : Color.White;
                            b.ForeColor = sel ? Color.White : AppTheme.TextDark;
                        }
                    }
                    PopulateCatalog();
                };
                _flpCategories.Controls.Add(btnC);
            }

            _flpCategories.ResumeLayout(true);
        }

        public void RefreshCatalog()
        {
            PopulateCategoryTabs();
            PopulateCatalog();
        }
    }
}
