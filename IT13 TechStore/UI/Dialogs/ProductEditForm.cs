using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IT8_TechStore.Models;
using IT8_TechStore.Services;
using IT8_TechStore.Theme;
using IT8_TechStore.UI.Components;

namespace IT8_TechStore.UI.Dialogs
{
    public class ProductEditForm : Form
    {
        private readonly DataService _dataService = DataService.Instance;

        private TextBox _txtName = null!;
        private ComboBox _cboCategory = null!;
        private TextBox _txtPrice = null!;
        private NumericUpDown _numStock = null!;
        private TextBox _txtDescription = null!;

        public Product? TargetProduct { get; private set; }

        public ProductEditForm(Product? existingProduct = null)
        {
            TargetProduct = existingProduct;
            InitializeComponent();

            if (TargetProduct != null)
            {
                Text = "✏️ Edit Product - Morphic Computers";
                PopulateExistingData();
            }
            else
            {
                Text = "➕ Add New Product - Morphic Computers";
            }
        }

        private void InitializeComponent()
        {
            Size = new Size(500, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.AppBackground;
            Padding = new Padding(20);

            // Title Banner
            Label lblHeader = new Label
            {
                Text = TargetProduct == null ? "Add New Product" : "Edit Product Details",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 16),
                AutoSize = true
            };

            // Form Fields Layout Container
            Panel pnlFields = new Panel
            {
                Location = new Point(20, 54),
                Size = new Size(444, 340),
                BackColor = AppTheme.CardBackground,
                Padding = new Padding(16)
            };

            int y = 18;

            // 1. Name Field
            Label lblName = new Label { Text = "Product Name:", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(16, y), AutoSize = true };
            _txtName = new TextBox { Location = new Point(140, y - 3), Size = new Size(280, 26), Font = AppTheme.BodyFont, BackColor = AppTheme.InputBackground, ForeColor = AppTheme.TextDark };
            y += 46;

            // 2. Category Field
            Label lblCategory = new Label { Text = "Category:", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(16, y), AutoSize = true };
            _cboCategory = new ComboBox { Location = new Point(140, y - 3), Size = new Size(280, 26), Font = AppTheme.BodyFont, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = AppTheme.InputBackground, ForeColor = AppTheme.TextDark };
            foreach (var cat in _dataService.Categories)
            {
                _cboCategory.Items.Add(cat.Name);
            }
            if (_cboCategory.Items.Count > 0) _cboCategory.SelectedIndex = 0;
            y += 46;

            // 3. Price Field
            Label lblPrice = new Label { Text = "Price ($):", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(16, y), AutoSize = true };
            _txtPrice = new TextBox { Location = new Point(140, y - 3), Size = new Size(280, 26), Font = AppTheme.BodyFont, BackColor = AppTheme.InputBackground, ForeColor = AppTheme.TextDark, Text = "0.00" };
            y += 46;

            // 4. Stock Quantity Field
            Label lblStock = new Label { Text = "Stock Quantity:", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(16, y), AutoSize = true };
            _numStock = new NumericUpDown { Location = new Point(140, y - 3), Size = new Size(280, 26), Font = AppTheme.BodyFont, Minimum = 0, Maximum = 10000, Value = 10, BackColor = AppTheme.InputBackground, ForeColor = AppTheme.TextDark };
            y += 46;

            // 5. Description Field
            Label lblDesc = new Label { Text = "Description:", Font = AppTheme.BodyBoldFont, ForeColor = AppTheme.TextDark, Location = new Point(16, y), AutoSize = true };
            _txtDescription = new TextBox { Location = new Point(140, y - 3), Size = new Size(280, 80), Font = AppTheme.BodyFont, Multiline = true, BackColor = AppTheme.InputBackground, ForeColor = AppTheme.TextDark, ScrollBars = ScrollBars.Vertical };

            pnlFields.Controls.Add(lblName); pnlFields.Controls.Add(_txtName);
            pnlFields.Controls.Add(lblCategory); pnlFields.Controls.Add(_cboCategory);
            pnlFields.Controls.Add(lblPrice); pnlFields.Controls.Add(_txtPrice);
            pnlFields.Controls.Add(lblStock); pnlFields.Controls.Add(_numStock);
            pnlFields.Controls.Add(lblDesc); pnlFields.Controls.Add(_txtDescription);

            // Bottom Dialog Action Buttons
            SunshineButton btnSave = new SunshineButton
            {
                Text = "💾 Save Product",
                IsPrimary = true,
                Location = new Point(210, 404),
                Size = new Size(130, 38)
            };
            btnSave.Click += BtnSave_Click;

            SunshineButton btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(350, 404),
                Size = new Size(114, 38)
            };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.Add(lblHeader);
            Controls.Add(pnlFields);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
        }

        private void PopulateExistingData()
        {
            if (TargetProduct == null) return;
            _txtName.Text = TargetProduct.Name;
            _cboCategory.SelectedItem = TargetProduct.CategoryName;
            _txtPrice.Text = TargetProduct.Price.ToString("F2");
            _numStock.Value = Math.Min(TargetProduct.StockQuantity, _numStock.Maximum);
            _txtDescription.Text = TargetProduct.Description;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Please enter a valid product name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            if (!decimal.TryParse(_txtPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Please enter a valid numeric unit price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPrice.Focus();
                return;
            }

            if (TargetProduct == null)
            {
                TargetProduct = new Product
                {
                    SKU = $"MC-{DateTime.Now:mmss}{new Random().Next(10, 99)}", // System internal code
                    Name = _txtName.Text.Trim(),
                    CategoryName = _cboCategory.SelectedItem?.ToString() ?? "General",
                    Price = price,
                    StockQuantity = (int)_numStock.Value,
                    Description = _txtDescription.Text.Trim()
                };
                _dataService.AddProduct(TargetProduct);
            }
            else
            {
                TargetProduct.Name = _txtName.Text.Trim();
                TargetProduct.CategoryName = _cboCategory.SelectedItem?.ToString() ?? "General";
                TargetProduct.Price = price;
                TargetProduct.StockQuantity = (int)_numStock.Value;
                TargetProduct.Description = _txtDescription.Text.Trim();
                _dataService.UpdateProduct(TargetProduct);
            }

            DialogResult = DialogResult.OK;
        }
    }
}
