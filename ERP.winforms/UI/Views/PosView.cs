using System;
using System.Collections.Generic;
using System.Data;
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

        private string _selectedCategory = "All";
        private string _searchQuery = "";

        private FlowLayoutPanel _flpProducts = null!;
        private DataGridView _gridCart = null!;
        private TextBox _txtCustomerName = null!;
        private TextBox _txtDiscount = null!;
        private Label _lblSubtotal = null!;
        private Label _lblTax = null!;
        private Label _lblDiscount = null!;
        private Label _lblGrandTotal = null!;
        private SunshineButton _btnCheckout = null!;

        public PosView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            InitializeView();
        }

        private void InitializeView()
        {
            SuspendLayout();
            Controls.Clear();

            TableLayoutPanel tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16)
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));

            Panel pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };

            Label lblPosTitle = new Label
            {
                Text = "Point of Sale (POS Terminal)",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(0, 4),
                AutoSize = true
            };

            TextBox txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Search products by name...",
                Font = AppTheme.BodyFont,
                Location = new Point(0, 44),
                Width = 320
            };
            txtSearch.TextChanged += (s, e) =>
            {
                _searchQuery = txtSearch.Text;
                PopulateProductsGrid();
            };

            FlowLayoutPanel flpCategoryTabs = new FlowLayoutPanel
            {
                Location = new Point(0, 82),
                Size = new Size(pnlLeft.Width, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = false
            };

            Button btnAll = CreateCategoryTabButton("All Items", "All");
            flpCategoryTabs.Controls.Add(btnAll);

            foreach (var cat in _dataService.Categories)
            {
                flpCategoryTabs.Controls.Add(CreateCategoryTabButton($"{cat.Icon} {cat.Name}", cat.Name));
            }

            _flpProducts = new FlowLayoutPanel
            {
                Location = new Point(0, 130),
                Size = new Size(pnlLeft.Width, pnlLeft.Height - 130),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = AppTheme.AppBackground
            };

            pnlLeft.Controls.Add(lblPosTitle);
            pnlLeft.Controls.Add(txtSearch);
            pnlLeft.Controls.Add(flpCategoryTabs);
            pnlLeft.Controls.Add(_flpProducts);

            tlpMain.Controls.Add(pnlLeft, 0, 0);

            SunshineCard cardCart = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0),
                Padding = new Padding(14)
            };

            Label lblCartTitle = new Label
            {
                Text = "🛒 Active Cashier Cart",
                Font = AppTheme.SubheaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(14, 12),
                AutoSize = true
            };

            Label lblCustomer = new Label
            {
                Text = "Customer Name:",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(14, 44),
                AutoSize = true
            };

            _txtCustomerName = new TextBox
            {
                Text = "Walk-in Customer",
                Font = AppTheme.BodyFont,
                Location = new Point(14, 64),
                Width = cardCart.Width - 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _gridCart = new DataGridView
            {
                Location = new Point(14, 100),
                Size = new Size(cardCart.Width - 28, cardCart.Height - 340),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = AppTheme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = AppTheme.BorderColor,
                MultiSelect = false
            };

            _gridCart.EnableHeadersVisualStyles = false;
            _gridCart.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
            _gridCart.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridCart.ColumnHeadersDefaultCellStyle.Font = AppTheme.BodyBoldFont;
            _gridCart.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.Primary;
            _gridCart.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridCart.ColumnHeadersHeight = 32;

            _gridCart.DefaultCellStyle.BackColor = AppTheme.CardBackground;
            _gridCart.DefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridCart.DefaultCellStyle.Font = AppTheme.BodyFont;
            _gridCart.DefaultCellStyle.SelectionBackColor = AppTheme.CardHover;
            _gridCart.DefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridCart.RowTemplate.Height = 32;

            _gridCart.CellClick += GridCart_CellClick;

            Panel pnlSummary = new Panel
            {
                Location = new Point(14, cardCart.Height - 230),
                Size = new Size(cardCart.Width - 28, 215),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = AppTheme.AppBackground,
                Padding = new Padding(10)
            };

            _lblSubtotal = new Label { Text = "Subtotal: $0.00", Font = AppTheme.BodyFont, ForeColor = AppTheme.TextDark, Location = new Point(10, 8), AutoSize = true };
            _lblTax = new Label { Text = "Tax (12% VAT): $0.00", Font = AppTheme.BodyFont, ForeColor = AppTheme.TextDark, Location = new Point(10, 32), AutoSize = true };

            Label lblDiscountLabel = new Label { Text = "Discount ($):", Font = AppTheme.BodyFont, ForeColor = AppTheme.TextDark, Location = new Point(10, 58), AutoSize = true };
            _txtDiscount = new TextBox { Text = "0", Font = AppTheme.BodyFont, Location = new Point(110, 54), Width = 80 };
            _txtDiscount.TextChanged += (s, e) => UpdateCartTotals();

            _lblDiscount = new Label { Text = "-$0.00", Font = AppTheme.BodyFont, ForeColor = Color.DarkRed, Location = new Point(200, 58), AutoSize = true };
            _lblGrandTotal = new Label { Text = "TOTAL: $0.00", Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = AppTheme.TextDark, Location = new Point(10, 92), AutoSize = true };

            SunshineButton btnClear = new SunshineButton
            {
                Text = "🗑️ Clear Cart",
                IsPrimary = false,
                Location = new Point(10, 145),
                Width = 120,
                Height = 42,
                Font = AppTheme.BodyBoldFont
            };
            btnClear.Click += (s, e) =>
            {
                _cart.Clear();
                UpdateCartTotals();
            };

            _btnCheckout = new SunshineButton
            {
                Text = "💳 Checkout & Pay",
                IsPrimary = true,
                Location = new Point(140, 145),
                Width = pnlSummary.Width - 150,
                Height = 42,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = AppTheme.BodyBoldFont
            };
            _btnCheckout.Click += BtnCheckout_Click;

            pnlSummary.Controls.Add(_lblSubtotal);
            pnlSummary.Controls.Add(_lblTax);
            pnlSummary.Controls.Add(lblDiscountLabel);
            pnlSummary.Controls.Add(_txtDiscount);
            pnlSummary.Controls.Add(_lblDiscount);
            pnlSummary.Controls.Add(_lblGrandTotal);
            pnlSummary.Controls.Add(btnClear);
            pnlSummary.Controls.Add(_btnCheckout);

            cardCart.Controls.Add(lblCartTitle);
            cardCart.Controls.Add(lblCustomer);
            cardCart.Controls.Add(_txtCustomerName);
            cardCart.Controls.Add(_gridCart);
            cardCart.Controls.Add(pnlSummary);

            tlpMain.Controls.Add(cardCart, 1, 0);

            Controls.Add(tlpMain);

            PopulateProductsGrid();
            UpdateCartTotals();

            ResumeLayout(false);
        }

        private Button CreateCategoryTabButton(string text, string categoryName)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Font = AppTheme.SmallFont,
                BackColor = _selectedCategory == categoryName ? AppTheme.Primary : AppTheme.CardBackground,
                ForeColor = AppTheme.TextDark,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderColor = AppTheme.BorderColor;

            btn.Click += (s, e) =>
            {
                _selectedCategory = categoryName;
                PopulateProductsGrid();
            };

            return btn;
        }

        public void PopulateProductsGrid()
        {
            _flpProducts.Controls.Clear();

            var prods = _dataService.Products.AsEnumerable();

            if (_selectedCategory != "All")
            {
                prods = prods.Where(p => p.CategoryName == _selectedCategory);
            }

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                prods = prods.Where(p => p.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) || p.CategoryName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var prod in prods)
            {
                _flpProducts.Controls.Add(CreateProductCard(prod));
            }
        }

        private SunshineCard CreateProductCard(Product prod)
        {
            SunshineCard card = new SunshineCard
            {
                Size = new Size(185, 140),
                Margin = new Padding(0, 0, 10, 10),
                Padding = new Padding(10),
                Cursor = Cursors.Hand
            };

            Label lblName = new Label
            {
                Text = prod.Name,
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(8, 8),
                Size = new Size(168, 38),
                AutoEllipsis = true
            };

            Label lblPrice = new Label
            {
                Text = $"${prod.Price:N2}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Location = new Point(8, 48),
                AutoSize = true
            };

            Label lblStock = new Label
            {
                Text = prod.StockQuantity > 0 ? $"Qty: {prod.StockQuantity}" : "Out of Stock",
                Font = AppTheme.SmallFont,
                ForeColor = prod.StockQuantity > 0 ? AppTheme.TextMuted : Color.Red,
                Location = new Point(8, 74),
                AutoSize = true
            };

            SunshineButton btnAdd = new SunshineButton
            {
                Text = "➕ Add",
                IsPrimary = prod.StockQuantity > 0,
                Enabled = prod.StockQuantity > 0,
                Location = new Point(8, 98),
                Size = new Size(168, 32),
                Font = AppTheme.SmallFont
            };

            EventHandler addAction = (s, e) => AddToCart(prod);
            btnAdd.Click += addAction;
            card.Click += addAction;
            lblName.Click += addAction;
            lblPrice.Click += addAction;
            lblStock.Click += addAction;

            card.Controls.Add(lblName);
            card.Controls.Add(lblPrice);
            card.Controls.Add(lblStock);
            card.Controls.Add(btnAdd);

            return card;
        }

        private void AddToCart(Product prod)
        {
            if (prod.StockQuantity <= 0)
            {
                MessageBox.Show($"{prod.Name} is currently out of stock!", "Stock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var item = _cart.FirstOrDefault(c => c.ProductId == prod.Id);
            if (item != null)
            {
                if (item.Quantity + 1 > prod.StockQuantity)
                {
                    MessageBox.Show($"Cannot add more {prod.Name}. Only {prod.StockQuantity} available in stock!", "Stock Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                item.Quantity++;
            }
            else
            {
                _cart.Add(new CartItem
                {
                    ProductId = prod.Id,
                    ProductName = prod.Name,
                    UnitPrice = prod.Price,
                    Quantity = 1
                });
            }

            UpdateCartTotals();
        }

        private void GridCart_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _cart.Count) return;

            if (e.ColumnIndex == 4)
            {
                _cart.RemoveAt(e.RowIndex);
                UpdateCartTotals();
            }
        }

        private void UpdateCartTotals()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Item Name", typeof(string));
            dt.Columns.Add("Price", typeof(string));
            dt.Columns.Add("Qty", typeof(int));
            dt.Columns.Add("Total", typeof(string));

            foreach (var item in _cart)
            {
                dt.Rows.Add(item.ProductName, $"${item.UnitPrice:N2}", item.Quantity, $"${item.Subtotal:N2}");
            }
            _gridCart.DataSource = dt;

            decimal subtotal = _cart.Sum(c => c.Subtotal);
            decimal tax = subtotal * 0.12m;

            decimal discount = 0;
            if (decimal.TryParse(_txtDiscount.Text, out decimal dVal))
            {
                discount = dVal;
            }

            decimal grandTotal = Math.Max(0, subtotal + tax - discount);

            _lblSubtotal.Text = $"Subtotal: ${subtotal:N2}";
            _lblTax.Text = $"Tax (12% VAT): ${tax:N2}";
            _lblDiscount.Text = $"-$" + discount.ToString("N2");
            _lblGrandTotal.Text = $"TOTAL: ${grandTotal:N2}";

            _btnCheckout.Enabled = _cart.Count > 0;
        }

        private void BtnCheckout_Click(object? sender, EventArgs e)
        {
            if (_cart.Count == 0) return;

            decimal subtotal = _cart.Sum(c => c.Subtotal);
            decimal tax = subtotal * 0.12m;
            decimal discount = 0;
            if (decimal.TryParse(_txtDiscount.Text, out decimal dVal)) discount = dVal;
            decimal grandTotal = Math.Max(0, subtotal + tax - discount);

            Order order = new Order
            {
                CustomerName = string.IsNullOrWhiteSpace(_txtCustomerName.Text) ? "Walk-in Customer" : _txtCustomerName.Text,
                Items = new List<CartItem>(_cart),
                Subtotal = subtotal,
                Tax = tax,
                Discount = discount,
                TotalAmount = grandTotal,
                CreatedAt = DateTime.Now,
                PaymentMethod = "Cash"
            };

            using var receiptDlg = new ReceiptForm(order);
            if (receiptDlg.ShowDialog() == DialogResult.OK)
            {
                _cart.Clear();
                _txtDiscount.Text = "0";
                UpdateCartTotals();
                PopulateProductsGrid();
            }
        }
    }
}
