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
    public class ProductEditForm : Form
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly Product? _targetProduct;

        private TextBox _txtCode = null!;
        private TextBox _txtName = null!;
        private ComboBox _cboCategory = null!;
        private NumericUpDown _numPrice = null!;
        private NumericUpDown _numStock = null!;
        private TextBox _txtDescription = null!;
        private SunshineButton _btnSave = null!;
        private SunshineButton _btnCancel = null!;

        public ProductEditForm(Product? product = null)
        {
            _targetProduct = product;

            Text = _targetProduct == null ? "Add New Product" : "Edit Product Details";
            Size = new Size(460, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.CardBackground;

            InitializeForm();

            if (_targetProduct != null)
            {
                _txtCode.Text = _targetProduct.ProductCode;
                _txtName.Text = _targetProduct.ProductName;
                int cIdx = _cboCategory.FindStringExact(_targetProduct.CategoryName);
                if (cIdx >= 0) _cboCategory.SelectedIndex = cIdx;
                else if (_cboCategory.Items.Count > 0) _cboCategory.SelectedIndex = 0;
                _numPrice.Value = Math.Min(_targetProduct.UnitPrice, _numPrice.Maximum);
                _numStock.Value = Math.Min(_targetProduct.StockQuantity, _numStock.Maximum);
                _txtDescription.Text = _targetProduct.Description;
            }

            _dataService.CategoriesChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    Action act = () =>
                    {
                        string cur = _cboCategory.SelectedItem?.ToString() ?? "";
                        _cboCategory.Items.Clear();
                        foreach (var c in _dataService.Categories) _cboCategory.Items.Add(c.Name);
                        int idx = _cboCategory.FindStringExact(cur);
                        _cboCategory.SelectedIndex = idx >= 0 ? idx : (_cboCategory.Items.Count > 0 ? 0 : -1);
                    };
                    if (InvokeRequired) BeginInvoke(act); else act();
                }
            };
        }

        private void InitializeForm()
        {
            Controls.Clear();

            Label lblTitle = new Label
            {
                Text = _targetProduct == null ? "NEW PRODUCT ENTRY" : "UPDATE PRODUCT DETAILS",
                Font = AppTheme.SubheaderFont,
                ForeColor = Color.FromArgb(160, 110, 10),
                Location = new Point(20, 16),
                AutoSize = true
            };

            int y = 50;

            // Product Code
            Label lblCode = new Label { Text = "Product Code (SKU):", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _txtCode = new TextBox { Font = AppTheme.BodyFont, Location = new Point(20, y + 18), Width = 404 };
            y += 48;

            // Product Name
            Label lblName = new Label { Text = "Product Name / Model:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _txtName = new TextBox { Font = AppTheme.BodyFont, Location = new Point(20, y + 18), Width = 404 };
            y += 48;

            // Category
            Label lblCategory = new Label { Text = "Category:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _cboCategory = new ComboBox { Font = AppTheme.BodyFont, Location = new Point(20, y + 18), Width = 270, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var cat in _dataService.Categories)
            {
                _cboCategory.Items.Add(cat.Name);
            }
            if (_cboCategory.Items.Count > 0) _cboCategory.SelectedIndex = 0;

            Button btnAddCat = new Button
            {
                Text = "+ New",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(296, y + 17),
                Size = new Size(58, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 243, 238),
                ForeColor = AppTheme.TextDark,
                Cursor = Cursors.Hand
            };
            btnAddCat.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
            btnAddCat.Click += (s, e) =>
            {
                using var dlg = new AddCategoryDialog();
                if (dlg.ShowDialog() == DialogResult.OK && dlg.CreatedCategory != null)
                {
                    _cboCategory.Items.Clear();
                    foreach (var c in _dataService.Categories) _cboCategory.Items.Add(c.Name);
                    int idx = _cboCategory.FindStringExact(dlg.CreatedCategory.Name);
                    if (idx >= 0) _cboCategory.SelectedIndex = idx;
                }
            };

            Button btnManageCat = new Button
            {
                Text = "Manage",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(358, y + 17),
                Size = new Size(66, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 243, 238),
                ForeColor = AppTheme.TextDark,
                Cursor = Cursors.Hand
            };
            btnManageCat.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
            btnManageCat.Click += (s, e) =>
            {
                using var dlg = new ManageCategoriesDialog();
                dlg.ShowDialog(this);
                string cur = _cboCategory.SelectedItem?.ToString() ?? "";
                _cboCategory.Items.Clear();
                foreach (var c in _dataService.Categories) _cboCategory.Items.Add(c.Name);
                int idx = _cboCategory.FindStringExact(cur);
                _cboCategory.SelectedIndex = idx >= 0 ? idx : (_cboCategory.Items.Count > 0 ? 0 : -1);
            };
            y += 48;

            // Price & Stock
            Label lblPrice = new Label { Text = "Unit Price (₱):", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _numPrice = new NumericUpDown { Font = AppTheme.BodyFont, Location = new Point(20, y + 18), Width = 195, DecimalPlaces = 2, Maximum = 100000, Value = 10.00m };

            Label lblStock = new Label { Text = "Initial Stock Qty:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(229, y), AutoSize = true };
            _numStock = new NumericUpDown { Font = AppTheme.BodyFont, Location = new Point(229, y + 18), Width = 195, Maximum = 10000, Value = 10 };
            y += 48;

            // Description
            Label lblDesc = new Label { Text = "Specifications / Description:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _txtDescription = new TextBox
            {
                Font = AppTheme.BodyFont,
                Location = new Point(20, y + 18),
                Size = new Size(404, 80),
                Multiline = true
            };

            // Action Buttons
            _btnSave = new SunshineButton
            {
                Text = _targetProduct == null ? "Add Product" : "Save Changes",
                IsPrimary = true,
                Location = new Point(20, 370),
                Width = 195,
                Height = 44
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(229, 370),
                Width = 195,
                Height = 44
            };
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.Add(lblTitle);
            Controls.Add(lblCode);
            Controls.Add(_txtCode);
            Controls.Add(lblName);
            Controls.Add(_txtName);
            Controls.Add(lblCategory);
            Controls.Add(_cboCategory);
            Controls.Add(btnAddCat);
            Controls.Add(btnManageCat);
            Controls.Add(lblPrice);
            Controls.Add(_numPrice);
            Controls.Add(lblStock);
            Controls.Add(_numStock);
            Controls.Add(lblDesc);
            Controls.Add(_txtDescription);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
        }

        private void PopulateFields(Product prod)
        {
            _txtName.Text = prod.Name;
            int catIdx = _cboCategory.Items.IndexOf(prod.CategoryName);
            if (catIdx >= 0) _cboCategory.SelectedIndex = catIdx;
            _numPrice.Value = Math.Min(prod.Price, _numPrice.Maximum);
            _numStock.Value = Math.Min(prod.StockQuantity, _numStock.Maximum);
            _txtDescription.Text = prod.Description;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Please enter a valid product name!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string code = string.IsNullOrWhiteSpace(_txtCode.Text) ? $"PRD{DateTime.Now:fff}" : _txtCode.Text.Trim();
            string cat = _cboCategory.SelectedItem?.ToString() ?? "Graphics Cards (GPU)";

            if (_targetProduct == null)
            {
                Product newProd = new Product
                {
                    ProductCode = code,
                    Name = _txtName.Text.Trim(),
                    CategoryName = cat,
                    Price = _numPrice.Value,
                    StockQuantity = (int)_numStock.Value,
                    Description = _txtDescription.Text.Trim()
                };
                _dataService.AddProduct(newProd);
            }
            else
            {
                _targetProduct.ProductCode = code;
                _targetProduct.Name = _txtName.Text.Trim();
                _targetProduct.CategoryName = cat;
                _targetProduct.Price = _numPrice.Value;
                _targetProduct.StockQuantity = (int)_numStock.Value;
                _targetProduct.Description = _txtDescription.Text.Trim();
                _dataService.UpdateProduct(_targetProduct);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
