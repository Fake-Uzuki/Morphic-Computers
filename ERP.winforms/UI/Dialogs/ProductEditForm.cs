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
        private ComboBox? _cboSupplier;
        private NumericUpDown _numPrice = null!;
        private NumericUpDown _numStock = null!;
        private TextBox _txtDescription = null!;
        private SunshineButton _btnSave = null!;
        private SunshineButton _btnCancel = null!;

        public ProductEditForm(Product? product = null)
        {
            _targetProduct = product;

            Text = _targetProduct == null ? "Add New Product" : "Edit Product Details";
            Size = new Size(460, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.CardBackground;

            InitializeForm();

            if (_targetProduct != null)
            {
                PopulateFields(_targetProduct);
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
            Label lblCode = new Label { Text = "Product Code (SKU) *:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _txtCode = new TextBox { Font = AppTheme.BodyFont, Location = new Point(20, y + 18), Width = 404 };
            y += 48;

            // Product Name
            Label lblName = new Label { Text = "Product Name / Model *:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _txtName = new TextBox { Font = AppTheme.BodyFont, Location = new Point(20, y + 18), Width = 404 };
            y += 48;

            // Category
            Label lblCategory = new Label { Text = "Category *:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
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

            // Supplier / Vendor (Only for Small Business or higher plans)
            bool hasSupplierModule = _dataService.CurrentCompany != null && !string.Equals(_dataService.CurrentCompany.PlanName, "Micro", StringComparison.OrdinalIgnoreCase);
            Label? lblSupplier = null;
            if (hasSupplierModule)
            {
                lblSupplier = new Label { Text = "Supplier / Vendor:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
                _cboSupplier = new ComboBox { Font = AppTheme.BodyFont, Location = new Point(20, y + 18), Width = 404, DropDownStyle = ComboBoxStyle.DropDownList };
                _cboSupplier.Items.Add("(None / Unassigned)");
                foreach (var sup in _dataService.Suppliers)
                {
                    if (!_cboSupplier.Items.Contains(sup.SupplierName))
                        _cboSupplier.Items.Add(sup.SupplierName);
                }
                _cboSupplier.SelectedIndex = 0;
                y += 48;
            }

            // Price & Stock
            Label lblPrice = new Label { Text = "Unit Price (₱) *:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _numPrice = new NumericUpDown { Font = AppTheme.BodyFont, Location = new Point(20, y + 18), Width = 195, DecimalPlaces = 2, Maximum = 1000000, Value = 0.00m };

            Label lblStock = new Label { Text = "Initial Stock Qty:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(229, y), AutoSize = true };
            _numStock = new NumericUpDown { Font = AppTheme.BodyFont, Location = new Point(229, y + 18), Width = 195, Maximum = 100000, Value = 0 };
            y += 48;

            // Description
            Label lblDesc = new Label { Text = "Specifications / Description:", Font = AppTheme.SmallFont, ForeColor = AppTheme.TextMuted, Location = new Point(20, y), AutoSize = true };
            _txtDescription = new TextBox
            {
                Font = AppTheme.BodyFont,
                Location = new Point(20, y + 18),
                Size = new Size(404, 75),
                Multiline = true
            };
            y += 100;

            // Action Buttons
            _btnSave = new SunshineButton
            {
                Text = _targetProduct == null ? "Add Product" : "Save Changes",
                IsPrimary = true,
                Location = new Point(20, y),
                Width = 195,
                Height = 44
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(229, y),
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
            if (hasSupplierModule && lblSupplier != null && _cboSupplier != null)
            {
                Controls.Add(lblSupplier);
                Controls.Add(_cboSupplier);
            }
            Controls.Add(lblPrice);
            Controls.Add(_numPrice);
            Controls.Add(lblStock);
            Controls.Add(_numStock);
            Controls.Add(lblDesc);
            Controls.Add(_txtDescription);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);

            ClientSize = new Size(444, y + 64);
        }

        private void PopulateFields(Product prod)
        {
            _txtCode.Text = prod.ProductCode;
            _txtName.Text = prod.ProductName;
            int catIdx = _cboCategory.FindStringExact(prod.CategoryName);
            if (catIdx >= 0) _cboCategory.SelectedIndex = catIdx;

            if (_cboSupplier != null && !string.IsNullOrWhiteSpace(prod.SupplierName))
            {
                int sIdx = _cboSupplier.FindStringExact(prod.SupplierName);
                if (sIdx >= 0) _cboSupplier.SelectedIndex = sIdx;
            }

            _numPrice.Value = Math.Min(prod.UnitPrice, _numPrice.Maximum);
            _numStock.Value = Math.Min(prod.StockQuantity, _numStock.Maximum);
            _txtDescription.Text = prod.Description;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string code = _txtCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Please enter a Product Code (SKU)!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtCode.Focus();
                return;
            }

            string name = _txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a product name / model!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            if (_cboCategory.SelectedItem == null || string.IsNullOrWhiteSpace(_cboCategory.SelectedItem.ToString()))
            {
                MessageBox.Show("Please select or create a product category!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cboCategory.Focus();
                return;
            }
            string cat = _cboCategory.SelectedItem.ToString()!;

            string? sup = null;
            if (_cboSupplier != null && _cboSupplier.SelectedItem != null)
            {
                string selectedSup = _cboSupplier.SelectedItem.ToString()!;
                if (!selectedSup.Equals("(None / Unassigned)", StringComparison.OrdinalIgnoreCase))
                {
                    sup = selectedSup;
                }
            }

            if (_numPrice.Value <= 0)
            {
                MessageBox.Show("Please enter a valid Unit Price greater than ₱0.00!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _numPrice.Focus();
                return;
            }

            if (_targetProduct == null)
            {
                if (_dataService.Products.Any(p => p.ProductCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"Cannot add product. A product with SKU / ID '{code}' already exists. Duplicate IDs are not allowed.", "Duplicate Product ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Product newProd = new Product
                {
                    ProductCode = code,
                    ProductName = name,
                    CategoryName = cat,
                    SupplierName = sup,
                    UnitPrice = _numPrice.Value,
                    StockQuantity = (int)_numStock.Value,
                    Description = _txtDescription.Text.Trim()
                };
                _dataService.AddProduct(newProd);
            }
            else
            {
                if (_dataService.Products.Any(p => p.ProductId != _targetProduct.ProductId && p.ProductCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"Cannot update product. Another product with SKU / ID '{code}' already exists.", "Duplicate Product ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _targetProduct.ProductCode = code;
                _targetProduct.ProductName = name;
                _targetProduct.CategoryName = cat;
                _targetProduct.SupplierName = sup;
                _targetProduct.UnitPrice = _numPrice.Value;
                _targetProduct.StockQuantity = (int)_numStock.Value;
                _targetProduct.Description = _txtDescription.Text.Trim();
                _dataService.UpdateProduct(_targetProduct);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
