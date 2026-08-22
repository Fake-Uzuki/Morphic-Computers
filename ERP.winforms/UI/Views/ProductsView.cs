using System;
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
    public class ProductsView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridProducts = null!;
        private TextBox _txtSearch = null!;
        private ComboBox _cboCategoryFilter = null!;
        private Label _lblStatusCount = null!;

        public ProductsView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            InitializeView();
        }

        private void InitializeView()
        {
            SuspendLayout();
            Controls.Clear();

            Label lblTitle = new Label
            {
                Text = "Products & Inventory Management",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 16),
                AutoSize = true
            };

            _lblStatusCount = new Label
            {
                Text = "Showing 0 items in stock",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 48),
                AutoSize = true
            };

            _txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Search product by name or category...",
                Font = AppTheme.BodyFont,
                Location = new Point(20, 80),
                Width = 280
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();

            _cboCategoryFilter = new ComboBox
            {
                Font = AppTheme.BodyFont,
                Location = new Point(310, 80),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboCategoryFilter.Items.Add("All Categories");
            foreach (var cat in _dataService.Categories)
            {
                _cboCategoryFilter.Items.Add(cat.Name);
            }
            _cboCategoryFilter.SelectedIndex = 0;
            _cboCategoryFilter.SelectedIndexChanged += (s, e) => ApplyFilters();

            SunshineButton btnAdd = new SunshineButton
            {
                Text = "➕ Add Product",
                IsPrimary = true,
                Location = new Point(500, 78),
                Width = 140,
                Height = 36,
                Font = AppTheme.BodyBoldFont
            };
            btnAdd.Click += BtnAdd_Click;

            SunshineButton btnEdit = new SunshineButton
            {
                Text = "✏️ Edit Selected",
                IsPrimary = false,
                Location = new Point(650, 78),
                Width = 140,
                Height = 36,
                Font = AppTheme.BodyBoldFont
            };
            btnEdit.Click += BtnEdit_Click;

            SunshineButton btnDelete = new SunshineButton
            {
                Text = "🗑️ Delete Selected",
                IsPrimary = false,
                Location = new Point(800, 78),
                Width = 150,
                Height = 36,
                Font = AppTheme.BodyBoldFont
            };
            btnDelete.Click += BtnDelete_Click;

            Controls.Add(lblTitle);
            Controls.Add(_lblStatusCount);
            Controls.Add(_txtSearch);
            Controls.Add(_cboCategoryFilter);
            Controls.Add(btnAdd);
            Controls.Add(btnEdit);
            Controls.Add(btnDelete);

            SunshineCard cardGrid = new SunshineCard
            {
                Location = new Point(20, 126),
                Size = new Size(ClientSize.Width - 40, ClientSize.Height - 146),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12)
            };

            _gridProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.CardBackground,
                BorderStyle = BorderStyle.None,
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

            _gridProducts.EnableHeadersVisualStyles = false;
            _gridProducts.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
            _gridProducts.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridProducts.ColumnHeadersDefaultCellStyle.Font = AppTheme.BodyBoldFont;
            _gridProducts.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.Primary;
            _gridProducts.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridProducts.ColumnHeadersHeight = 38;

            _gridProducts.DefaultCellStyle.BackColor = AppTheme.CardBackground;
            _gridProducts.DefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridProducts.DefaultCellStyle.Font = AppTheme.BodyFont;
            _gridProducts.DefaultCellStyle.SelectionBackColor = AppTheme.CardHover;
            _gridProducts.DefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridProducts.RowTemplate.Height = 36;

            cardGrid.Controls.Add(_gridProducts);
            Controls.Add(cardGrid);

            ApplyFilters();

            ResumeLayout(false);
        }

        public void ApplyFilters()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Product Name", typeof(string));
            dt.Columns.Add("Category", typeof(string));
            dt.Columns.Add("Price ($)", typeof(string));
            dt.Columns.Add("Stock Quantity", typeof(int));
            dt.Columns.Add("Stock Status", typeof(string));

            string query = _txtSearch?.Text ?? "";
            string selCat = _cboCategoryFilter?.SelectedItem?.ToString() ?? "All Categories";

            var prods = _dataService.Products.AsEnumerable();

            if (selCat != "All Categories")
            {
                prods = prods.Where(p => p.CategoryName == selCat);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                prods = prods.Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.CategoryName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            var list = prods.ToList();
            foreach (var p in list)
            {
                dt.Rows.Add(p.Id, p.Name, p.CategoryName, $"${p.Price:N2}", p.StockQuantity, p.StockStatus);
            }

            _gridProducts.DataSource = dt;
            _lblStatusCount.Text = $"Showing {list.Count} items in inventory";
        }

        public void OpenAddProductDialog()
        {
            using var dlg = new ProductEditForm();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ApplyFilters();
            }
        }

        private Product? GetSelectedProduct()
        {
            if (_gridProducts.CurrentRow == null || _gridProducts.CurrentRow.Cells[0].Value == null) return null;
            int id = Convert.ToInt32(_gridProducts.CurrentRow.Cells[0].Value);
            return _dataService.Products.FirstOrDefault(p => p.Id == id);
        }

        private void BtnAdd_Click(object? sender, EventArgs e) => OpenAddProductDialog();

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedProduct();
            if (selected != null)
            {
                using var dlg = new ProductEditForm(selected);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    ApplyFilters();
                }
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedProduct();
            if (selected != null)
            {
                if (MessageBox.Show($"Are you sure you want to delete '{selected.Name}' from inventory?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _dataService.DeleteProduct(selected.Id);
                    ApplyFilters();
                }
            }
        }
    }
}
