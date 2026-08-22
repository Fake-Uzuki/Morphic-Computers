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

            Text = _targetProduct == null ? "➕ Add New Product" : "✏️ Edit Product Details";
            Size = new Size(460, 480);
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
        }

        private void InitializeForm()
        {
            Controls.Clear();

            Label lblTitle = new Label
            {
                Text = _targetProduct == null ? "Add New Inventory Product" : "Edit Product Information",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 16),
                AutoSize = true
            };

            // Product Name
            Label lblName = new Label { Text = "Product Name *", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(20, 60), AutoSize = true };
            _txtName = new TextBox { Font = AppTheme.BodyFont, Location = new Point(20, 82), Width = 404 };

            // Category
            Label lblCat = new Label { Text = "Category *", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(20, 122), AutoSize = true };
            _cboCategory = new ComboBox { Font = AppTheme.BodyFont, Location = new Point(20, 144), Width = 404, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var cat in _dataService.Categories)
            {
                _cboCategory.Items.Add(cat.Name);
            }
            if (_cboCategory.Items.Count > 0) _cboCategory.SelectedIndex = 0;

            // Unit Price
            Label lblPrice = new Label { Text = "Unit Price ($) *", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(20, 184), AutoSize = true };
            _numPrice = new NumericUpDown
            {
                Font = AppTheme.BodyFont,
                Location = new Point(20, 206),
                Width = 190,
                DecimalPlaces = 2,
                Maximum = 1000000m,
                Value = 99.99m
            };

            // Stock Quantity
            Label lblStock = new Label { Text = "Initial Stock Qty *", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(234, 184), AutoSize = true };
            _numStock = new NumericUpDown
            {
                Font = AppTheme.BodyFont,
                Location = new Point(234, 206),
                Width = 190,
                Maximum = 10000,
                Value = 10
            };

            // Description
            Label lblDesc = new Label { Text = "Description (Optional)", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(20, 246), AutoSize = true };
            _txtDescription = new TextBox
            {
                Font = AppTheme.BodyFont,
                Location = new Point(20, 268),
                Size = new Size(404, 80),
                Multiline = true
            };

            // Action Buttons
            _btnSave = new SunshineButton
            {
                Text = _targetProduct == null ? "➕ Add Product" : "💾 Save Changes",
                IsPrimary = true,
                Location = new Point(20, 370),
                Width = 195,
                Height = 44
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new SunshineButton
            {
                Text = "❌ Cancel",
                IsPrimary = false,
                Location = new Point(229, 370),
                Width = 195,
                Height = 44
            };
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.Add(lblTitle);
            Controls.Add(lblName);
            Controls.Add(_txtName);
            Controls.Add(lblCat);
            Controls.Add(_cboCategory);
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

            if (_targetProduct == null)
            {
                Product newProd = new Product
                {
                    Name = _txtName.Text.Trim(),
                    CategoryName = _cboCategory.SelectedItem?.ToString() ?? "General",
                    Price = _numPrice.Value,
                    StockQuantity = (int)_numStock.Value,
                    Description = _txtDescription.Text.Trim()
                };
                _dataService.AddProduct(newProd);
            }
            else
            {
                _targetProduct.Name = _txtName.Text.Trim();
                _targetProduct.CategoryName = _cboCategory.SelectedItem?.ToString() ?? "General";
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
