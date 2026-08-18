using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IT8_TechStore.Models;
using IT8_TechStore.Services;
using IT8_TechStore.Theme;
using IT8_TechStore.UI.Components;
using IT8_TechStore.UI.Dialogs;

namespace IT8_TechStore.UI.Views
{
    public class ProductsView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private TextBox _txtSearch = null!;
        private ComboBox _cboCategoryFilter = null!;
        private DataGridView _gridProducts = null!;
        private Label _lblTotalItems = null!;

        public ProductsView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;
            InitializeView();
        }

        private void InitializeView()
        {
            SuspendLayout();
            Controls.Clear();

            // 1. Top Header Banner
            Label lblHeader = new Label
            {
                Text = "Products & Stock Inventory",
                Font = AppTheme.HeaderFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(20, 16),
                AutoSize = true
            };

            Label lblSubHeader = new Label
            {
                Text = "Manage computer hardware, laptops, peripherals, and stock levels in SSMS.",
                Font = AppTheme.BodyFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 48),
                AutoSize = true
            };

            Controls.Add(lblHeader);
            Controls.Add(lblSubHeader);

            // 2. Control Toolbar Panel (Search, Filter, Action Buttons)
            SunshineCard cardToolbar = new SunshineCard
            {
                Location = new Point(20, 80),
                Size = new Size(ClientSize.Width - 40, 68),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12)
            };

            Label lblSearch = new Label
            {
                Text = "🔍 Search:",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(14, 22),
                AutoSize = true
            };

            _txtSearch = new TextBox
            {
                Location = new Point(90, 18),
                Size = new Size(200, 26),
                Font = AppTheme.BodyFont,
                BackColor = AppTheme.InputBackground,
                ForeColor = AppTheme.TextDark
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();

            Label lblFilter = new Label
            {
                Text = "Category:",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(305, 22),
                AutoSize = true
            };

            _cboCategoryFilter = new ComboBox
            {
                Location = new Point(380, 18),
                Size = new Size(180, 26),
                Font = AppTheme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.InputBackground,
                ForeColor = AppTheme.TextDark
            };
            _cboCategoryFilter.Items.Add("All Categories");
            foreach (var cat in _dataService.Categories)
            {
                _cboCategoryFilter.Items.Add(cat.Name);
            }
            _cboCategoryFilter.SelectedIndex = 0;
            _cboCategoryFilter.SelectedIndexChanged += (s, e) => ApplyFilters();

            // Toolbar Buttons
            SunshineButton btnAdd = new SunshineButton
            {
                Text = "➕ Add Product",
                IsPrimary = true,
                Location = new Point(cardToolbar.Width - 420, 14),
                Size = new Size(130, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAdd.Click += BtnAdd_Click;

            SunshineButton btnEdit = new SunshineButton
            {
                Text = "✏️ Edit",
                IsPrimary = false,
                Location = new Point(cardToolbar.Width - 280, 14),
                Size = new Size(90, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnEdit.Click += BtnEdit_Click;

            SunshineButton btnDelete = new SunshineButton
            {
                Text = "🗑️ Delete",
                IsPrimary = false,
                Location = new Point(cardToolbar.Width - 180, 14),
                Size = new Size(95, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnDelete.Click += BtnDelete_Click;

            SunshineButton btnRefresh = new SunshineButton
            {
                Text = "🔄 Refresh",
                IsPrimary = false,
                Location = new Point(cardToolbar.Width - 75, 14),
                Size = new Size(65, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.Click += (s, e) => { _dataService.LoadFromDatabase(); ApplyFilters(); };

            cardToolbar.Controls.Add(lblSearch);
            cardToolbar.Controls.Add(_txtSearch);
            cardToolbar.Controls.Add(lblFilter);
            cardToolbar.Controls.Add(_cboCategoryFilter);
            cardToolbar.Controls.Add(btnAdd);
            cardToolbar.Controls.Add(btnEdit);
            cardToolbar.Controls.Add(btnDelete);
            cardToolbar.Controls.Add(btnRefresh);

            Controls.Add(cardToolbar);

            // 3. Products DataGridView Table Container
            SunshineCard cardGridContainer = new SunshineCard
            {
                Location = new Point(20, 160),
                Size = new Size(ClientSize.Width - 40, 520),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12)
            };

            _lblTotalItems = new Label
            {
                Text = "Showing 0 items in stock",
                Font = AppTheme.BodyBoldFont,
                ForeColor = AppTheme.TextDark,
                Location = new Point(14, 12),
                AutoSize = true
            };

            _gridProducts = new DataGridView
            {
                Location = new Point(14, 40),
                Size = new Size(cardGridContainer.Width - 28, cardGridContainer.Height - 54),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
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

            // Custom Header & Cell Formatting
            _gridProducts.EnableHeadersVisualStyles = false;
            _gridProducts.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
            _gridProducts.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridProducts.ColumnHeadersDefaultCellStyle.Font = AppTheme.BodyBoldFont;
            _gridProducts.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.Primary;
            _gridProducts.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridProducts.ColumnHeadersHeight = 36;
            _gridProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            _gridProducts.DefaultCellStyle.BackColor = AppTheme.CardBackground;
            _gridProducts.DefaultCellStyle.ForeColor = AppTheme.TextDark;
            _gridProducts.DefaultCellStyle.Font = AppTheme.BodyFont;
            _gridProducts.DefaultCellStyle.SelectionBackColor = AppTheme.CardHover;
            _gridProducts.DefaultCellStyle.SelectionForeColor = AppTheme.TextDark;
            _gridProducts.RowTemplate.Height = 34;

            _gridProducts.CellFormatting += GridProducts_CellFormatting;

            cardGridContainer.Controls.Add(_lblTotalItems);
            cardGridContainer.Controls.Add(_gridProducts);
            Controls.Add(cardGridContainer);

            ApplyFilters();

            ResumeLayout(false);
        }

        private void ApplyFilters()
        {
            string search = _txtSearch.Text.Trim().ToLower();
            string category = _cboCategoryFilter.SelectedItem?.ToString() ?? "All Categories";

            var filtered = _dataService.Products.Where(p =>
                (string.IsNullOrEmpty(search) ||
                 p.Name.ToLower().Contains(search) ||
                 p.SKU.ToLower().Contains(search) ||
                 p.Description.ToLower().Contains(search)) &&
                (category == "All Categories" || p.CategoryName == category)
            ).ToList();

            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("SKU Code", typeof(string));
            dt.Columns.Add("Product Name", typeof(string));
            dt.Columns.Add("Category", typeof(string));
            dt.Columns.Add("Price ($)", typeof(string));
            dt.Columns.Add("Stock Qty", typeof(int));
            dt.Columns.Add("Stock Status", typeof(string));
            dt.Columns.Add("Description", typeof(string));

            foreach (var p in filtered)
            {
                dt.Rows.Add(p.Id, p.SKU, p.Name, p.CategoryName, $"${p.Price:N2}", p.StockQuantity, p.StockStatus, p.Description);
            }

            _gridProducts.DataSource = dt;
            _lblTotalItems.Text = $"Showing {filtered.Count} products in stock inventory";

            var colId = _gridProducts.Columns["ID"];
            if (colId != null) colId.Visible = false;
        }

        private void GridProducts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Highlight Stock Status Column cells with soft color badges
            if (_gridProducts.Columns[e.ColumnIndex].Name == "Stock Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                if (status.StartsWith("In Stock"))
                {
                    e.CellStyle!.BackColor = Color.FromArgb(235, 250, 235);
                    e.CellStyle.ForeColor = Color.FromArgb(30, 100, 30);
                }
                else if (status.StartsWith("Low Stock"))
                {
                    e.CellStyle!.BackColor = AppTheme.CardHover;
                    e.CellStyle.ForeColor = AppTheme.TextDark;
                }
                else if (status.StartsWith("Out of Stock"))
                {
                    e.CellStyle!.BackColor = Color.FromArgb(255, 230, 230);
                    e.CellStyle.ForeColor = Color.FromArgb(160, 30, 30);
                }
            }
        }

        private Product? GetSelectedProduct()
        {
            if (_gridProducts.SelectedRows.Count == 0) return null;
            var row = _gridProducts.SelectedRows[0];
            if (row.Cells["ID"].Value is int id)
            {
                return _dataService.Products.FirstOrDefault(p => p.Id == id);
            }
            return null;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var dlg = new ProductEditForm();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ApplyFilters();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedProduct();
            if (selected == null)
            {
                MessageBox.Show("Please select a product from the table to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new ProductEditForm(selected);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ApplyFilters();
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedProduct();
            if (selected == null)
            {
                MessageBox.Show("Please select a product from the table to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete '{selected.Name}' (SKU: {selected.SKU}) from SSMS database?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _dataService.DeleteProduct(selected.Id);
                ApplyFilters();
            }
        }
    }
}
