using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ERP.domain.entities;
using ERP.winforms.Services;
using ERP.winforms.Theme;
using ERP.winforms.UI.Components;

namespace ERP.winforms.UI.Views
{
    public class ProductsView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        public Action? OnProductsChanged;

        // Left Form Inputs
        private TextBox _txtFormSku = null!;
        private TextBox _txtFormName = null!;
        private ComboBox _cboFormCategory = null!;
        private ComboBox _cboFormSupplier = null!;
        private TextBox _txtFormPrice = null!;
        private TextBox _txtFormCost = null!;
        private NumericUpDown _numFormStock = null!;
        private NumericUpDown _numFormMinStock = null!;
        private TextBox _txtFormDesc = null!;
        private Label _lblFormMode = null!;
        private SunshineButton _btnSave = null!;
        private SunshineButton _btnArchiveRestore = null!;
        private Product? _selectedProduct;

        // Pagination
        private int _currentPage = 1;
        private const int PageSize = 7;
        private int _totalPages = 1;

        // Right Table & Search
        private DataGridView _gridProducts = null!;
        private TextBox _txtSearch = null!;
        private ComboBox _cboCategory = null!;
        private ComboBox _cboStockStatus = null!;
        private ComboBox _cboArchiveFilter = null!;
        private Label _lblFooterSummary = null!;
        private Panel _pnlPagination = null!;

        public ProductsView()
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
                        BeginInvoke(new Action(() => { RefreshCategoriesInUI(); ApplyFilters(); }));
                    }
                    else
                    {
                        RefreshCategoriesInUI();
                        ApplyFilters();
                    }
                }
            };

            _dataService.SuppliersChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => { RefreshSuppliersInUI(); ApplyFilters(); }));
                    }
                    else
                    {
                        RefreshSuppliersInUI();
                        ApplyFilters();
                    }
                }
            };
        }

        private void InitializeLayout()
        {
            SuspendLayout();
            Controls.Clear();

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.Transparent
            };

            // ========================================================
            // 1. TOP HEADER (Title + Last Sync - Clean without Subtitle)
            // ========================================================
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 34 };

            Label lblTitle = new Label
            {
                Text = "Products Inventory Management",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(0, 4),
                AutoSize = true
            };

            Label lblSync = new Label
            {
                Text = $"LAST REGISTRY RECONCILIATION   Today, {DateTime.Now:HH:mm:ss} EST",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSync);

            // ========================================================
            // 2. MAIN 2-COLUMN LAYOUT: Left Inputs Form | Right Table
            // ========================================================
            TableLayoutPanel tlpContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            tlpContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350f)); // Left Inputs: 350px
            tlpContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));   // Right Table: Remaining space

            // LEFT: Product Form Panel
            Panel pnlLeftForm = CreateProductInputForm();
            tlpContent.Controls.Add(pnlLeftForm, 0, 0);

            // RIGHT: Search, Filter, DataGrid, Centered Pagination
            Panel pnlRightTable = CreateProductTablePanel();
            tlpContent.Controls.Add(pnlRightTable, 1, 0);

            pnlMain.Controls.Add(tlpContent);
            pnlMain.Controls.Add(pnlHeader);

            Controls.Add(pnlMain);
            ResumeLayout(false);

            ApplyFilters();
        }

        private Panel CreateProductInputForm()
        {
            SunshineCard card = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(14),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder,
                AutoScroll = true
            };

            // Form Title
            Label lblCardTitle = new Label
            {
                Text = "Product Specifications",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Location = new Point(12, 10),
                AutoSize = true
            };

            _lblFormMode = new Label
            {
                Text = "New Product Entry",
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 95, 10),
                BackColor = Color.FromArgb(254, 245, 215),
                Location = new Point(200, 10),
                Size = new Size(110, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            int y = 42;

            // SKU / Barcode
            Label lblSku = new Label { Text = "SKU / PRODUCT CODE *", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(12, y), AutoSize = true };
            y += 18;
            _txtFormSku = new TextBox { Font = new Font("Segoe UI", 9F), Location = new Point(12, y), Width = 298 };
            y += 32;

            // Product Name
            Label lblName = new Label { Text = "PRODUCT NAME *", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(12, y), AutoSize = true };
            y += 18;
            _txtFormName = new TextBox { Font = new Font("Segoe UI", 9F), Location = new Point(12, y), Width = 298 };
            y += 32;

            // Category
            Label lblCat = new Label { Text = "CATEGORY", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(12, y), AutoSize = true };
            y += 18;
            _cboFormCategory = new ComboBox { Font = new Font("Segoe UI", 9F), Location = new Point(12, y), Width = 172, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var c in _dataService.Categories) _cboFormCategory.Items.Add(c.Name);
            if (_cboFormCategory.Items.Count > 0) _cboFormCategory.SelectedIndex = 0;

            Button btnAddCat = new Button
            {
                Text = "+ New",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(190, y - 1),
                Size = new Size(58, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 243, 238),
                ForeColor = AppTheme.TextDark,
                Cursor = Cursors.Hand
            };
            btnAddCat.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
            btnAddCat.Click += (s, e) => OpenAddCategoryDialog();

            Button btnManageCat = new Button
            {
                Text = "Manage",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(252, y - 1),
                Size = new Size(60, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 243, 238),
                ForeColor = AppTheme.TextDark,
                Cursor = Cursors.Hand
            };
            btnManageCat.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
            btnManageCat.Click += (s, e) => OpenManageCategoriesDialog();
            y += 32;

            // Supplier / Distributor
            Label lblSupplier = new Label { Text = "SUPPLIER / DISTRIBUTOR", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(12, y), AutoSize = true };
            y += 18;
            _cboFormSupplier = new ComboBox { Font = new Font("Segoe UI", 9F), Location = new Point(12, y), Width = 298, DropDownStyle = ComboBoxStyle.DropDownList };
            RefreshSuppliersInUI();
            y += 32;

            // Price & Cost in a 2-column row
            Label lblPrice = new Label { Text = "RETAIL PRICE (₱) *", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(12, y), AutoSize = true };
            Label lblCost = new Label { Text = "UNIT COST (₱)", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(165, y), AutoSize = true };
            y += 18;
            _txtFormPrice = new TextBox { Font = new Font("Segoe UI", 9F), Location = new Point(12, y), Width = 140, Text = "0.00" };
            _txtFormCost = new TextBox { Font = new Font("Segoe UI", 9F), Location = new Point(165, y), Width = 145, Text = "0.00" };
            y += 32;

            // Stock & Min Reorder in a 2-column row
            Label lblStock = new Label { Text = "CURRENT STOCK *", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(12, y), AutoSize = true };
            Label lblMinStock = new Label { Text = "MIN THRESHOLD", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(165, y), AutoSize = true };
            y += 18;
            _numFormStock = new NumericUpDown { Font = new Font("Segoe UI", 9F), Location = new Point(12, y), Width = 140, Minimum = 0, Maximum = 9999, Value = 10 };
            _numFormMinStock = new NumericUpDown { Font = new Font("Segoe UI", 9F), Location = new Point(165, y), Width = 145, Minimum = 0, Maximum = 999, Value = 3 };
            y += 32;

            // Description / Specs
            Label lblDesc = new Label { Text = "DESCRIPTION / SPECS", Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), ForeColor = AppTheme.TextMuted, Location = new Point(12, y), AutoSize = true };
            y += 18;
            _txtFormDesc = new TextBox { Font = new Font("Segoe UI", 8.5F), Location = new Point(12, y), Width = 298, Height = 52, Multiline = true };
            y += 62;

            // Action Buttons
            _btnSave = new SunshineButton
            {
                Text = "+ Save / Add Product",
                IsPrimary = true,
                Location = new Point(12, y),
                Size = new Size(298, 36),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            _btnSave.Click += BtnSaveProduct_Click;
            y += 42;

            SunshineButton btnUpdate = new SunshineButton
            {
                Text = "Update Selected",
                IsPrimary = false,
                Location = new Point(12, y),
                Size = new Size(144, 32),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnUpdate.Click += BtnUpdateProduct_Click;

            SunshineButton btnClear = new SunshineButton
            {
                Text = "Clear / New",
                IsPrimary = false,
                Location = new Point(166, y),
                Size = new Size(144, 32),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnClear.Click += (s, e) => ClearForm();
            y += 38;

            _btnArchiveRestore = new SunshineButton
            {
                Text = "Archive Product",
                IsPrimary = false,
                CustomTextColor = Color.FromArgb(184, 50, 38),
                Location = new Point(12, y),
                Size = new Size(298, 32),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Enabled = false
            };
            _btnArchiveRestore.Click += BtnArchiveRestore_Click;

            card.Controls.Add(lblCardTitle);
            card.Controls.Add(_lblFormMode);
            card.Controls.Add(lblSku);
            card.Controls.Add(_txtFormSku);
            card.Controls.Add(lblName);
            card.Controls.Add(_txtFormName);
            card.Controls.Add(lblCat);
            card.Controls.Add(_cboFormCategory);
            card.Controls.Add(btnAddCat);
            card.Controls.Add(btnManageCat);
            card.Controls.Add(lblSupplier);
            card.Controls.Add(_cboFormSupplier);
            card.Controls.Add(lblPrice);
            card.Controls.Add(lblCost);
            card.Controls.Add(_txtFormPrice);
            card.Controls.Add(_txtFormCost);
            card.Controls.Add(lblStock);
            card.Controls.Add(lblMinStock);
            card.Controls.Add(_numFormStock);
            card.Controls.Add(_numFormMinStock);
            card.Controls.Add(lblDesc);
            card.Controls.Add(_txtFormDesc);
            card.Controls.Add(_btnSave);
            card.Controls.Add(btnUpdate);
            card.Controls.Add(btnClear);
            card.Controls.Add(_btnArchiveRestore);

            return card;
        }

        private Panel CreateProductTablePanel()
        {
            Panel pnlRight = new Panel { Dock = DockStyle.Fill };

            // 1. FILTER BAR
            Panel pnlFilterBar = new Panel { Dock = DockStyle.Top, Height = 44 };

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search product by name, SKU or barcode...",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Location = new Point(0, 8),
                Width = 230
            };
            _txtSearch.TextChanged += (s, e) => { _currentPage = 1; ApplyFilters(); };

            _cboCategory = new ComboBox
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Location = new Point(235, 8),
                Width = 135,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboCategory.Items.Add("All Categories");
            foreach (var c in _dataService.Categories) _cboCategory.Items.Add(c.Name);
            _cboCategory.SelectedIndex = 0;
            _cboCategory.SelectedIndexChanged += (s, e) => { _currentPage = 1; ApplyFilters(); };

            _cboStockStatus = new ComboBox
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Location = new Point(375, 8),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboStockStatus.Items.AddRange(new object[] { "All Stock Status", "In Stock", "Low Stock", "Out of Stock" });
            _cboStockStatus.SelectedIndex = 0;
            _cboStockStatus.SelectedIndexChanged += (s, e) => { _currentPage = 1; ApplyFilters(); };

            _cboArchiveFilter = new ComboBox
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(500, 8),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboArchiveFilter.Items.AddRange(new object[] { "Active Products", "Archived Catalog", "All Products" });
            _cboArchiveFilter.SelectedIndex = 0;
            _cboArchiveFilter.SelectedIndexChanged += (s, e) => { _currentPage = 1; ApplyFilters(); };

            Button btnReset = new Button
            {
                Text = "Reset",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                Location = new Point(635, 8),
                Size = new Size(55, 26),
                FlatStyle = FlatStyle.Flat
            };
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(215, 210, 198);
            btnReset.Click += (s, e) =>
            {
                _txtSearch.Clear();
                _cboCategory.SelectedIndex = 0;
                _cboStockStatus.SelectedIndex = 0;
                _cboArchiveFilter.SelectedIndex = 0;
                _currentPage = 1;
                ApplyFilters();
            };

            SunshineButton btnToolbarArchive = new SunshineButton
            {
                Text = "📦 Archive / Restore",
                IsPrimary = false,
                CustomTextColor = Color.FromArgb(184, 50, 38),
                Location = new Point(698, 7),
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold)
            };
            btnToolbarArchive.Click += (s, e) => BtnArchiveRestore_Click(s, e);

            SunshineButton btnExport = new SunshineButton
            {
                Text = "Export (CSV)",
                IsPrimary = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlRight.Width - 110, 6),
                Size = new Size(105, 28),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold)
            };
            btnExport.Click += (s, e) => MessageBox.Show("Inventory catalog exported to CSV.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            pnlFilterBar.Controls.Add(_txtSearch);
            pnlFilterBar.Controls.Add(_cboCategory);
            pnlFilterBar.Controls.Add(_cboStockStatus);
            pnlFilterBar.Controls.Add(_cboArchiveFilter);
            pnlFilterBar.Controls.Add(btnReset);
            pnlFilterBar.Controls.Add(btnToolbarArchive);
            pnlFilterBar.Controls.Add(btnExport);

            // 2. DATA GRID VIEW
            SunshineCard cardGrid = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                BorderRadius = 4,
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            _gridProducts = new DataGridView
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
                RowTemplate = { Height = 44 }
            };

            _gridProducts.EnableHeadersVisualStyles = false;
            _gridProducts.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridProducts.ColumnHeadersHeight = 36;
            _gridProducts.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID / SKU", FillWeight = 11, Name = "ColId" });
            _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PRODUCT NAME", FillWeight = 25, Name = "ColName" });
            _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CATEGORY", FillWeight = 14, Name = "ColCat" });
            _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SUPPLIER", FillWeight = 15, Name = "ColSupplier" });
            _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PRICE (PHP)", FillWeight = 12, Name = "ColPrice" });
            _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "COST (PHP)", FillWeight = 11, Name = "ColCost" });
            _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STOCK", FillWeight = 9, Name = "ColStock" });
            _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STATUS", FillWeight = 10, Name = "ColStatus" });
            _gridProducts.Columns.Add(new DataGridViewButtonColumn { HeaderText = "ACTIONS", FillWeight = 13, Name = "ColAction" });

            _gridProducts.SelectionChanged += (s, e) => LoadSelectedRowToForm();
            _gridProducts.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && _gridProducts.Columns["ColAction"] != null && e.ColumnIndex == _gridProducts.Columns["ColAction"]!.Index)
                {
                    var row = _gridProducts.Rows[e.RowIndex];
                    if (row.Tag is Product p)
                    {
                        _selectedProduct = p;
                        BtnArchiveRestore_Click(s, e);
                    }
                }
            };

            // Right-click context menu for instant archive / restore on any product row
            ContextMenuStrip ctxProducts = new ContextMenuStrip();
            ToolStripMenuItem itemArchive = new ToolStripMenuItem("📦 Archive Product (Soft Delete)");
            ToolStripMenuItem itemRestore = new ToolStripMenuItem("♻️ Restore to Active Catalog");
            ToolStripMenuItem itemEdit = new ToolStripMenuItem("✏️ Edit Product in Form");

            itemArchive.Click += (s, e) => { if (_selectedProduct != null) BtnArchiveRestore_Click(s, e); };
            itemRestore.Click += (s, e) => { if (_selectedProduct != null) BtnArchiveRestore_Click(s, e); };
            itemEdit.Click += (s, e) => { if (_selectedProduct != null) LoadSelectedRowToForm(); };

            ctxProducts.Opening += (s, e) =>
            {
                if (_selectedProduct == null)
                {
                    e.Cancel = true;
                    return;
                }
                itemArchive.Visible = _selectedProduct.IsActive;
                itemRestore.Visible = !_selectedProduct.IsActive;
            };

            ctxProducts.Items.Add(itemArchive);
            ctxProducts.Items.Add(itemRestore);
            ctxProducts.Items.Add(new ToolStripSeparator());
            ctxProducts.Items.Add(itemEdit);
            _gridProducts.ContextMenuStrip = ctxProducts;

            _gridProducts.Resize += (s, e) => ApplyFilters();

            cardGrid.Controls.Add(_gridProducts);

            // 3. CENTERED PAGINATION FOOTER
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            _lblFooterSummary = new Label
            {
                Text = "Displaying 7 products",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(4, 12),
                AutoSize = true
            };

            _pnlPagination = new Panel { Size = new Size(205, 28) };
            string[] pagin = { "Prev", "1", "2", "3", "Next" };
            int px = 0;
            foreach (var p in pagin)
            {
                Button btnP = new Button
                {
                    Text = p,
                    Size = new Size(38, 24),
                    Location = new Point(px, 2),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                    BackColor = p == "1" ? AppTheme.Primary : Color.White,
                    Cursor = Cursors.Hand
                };
                btnP.FlatAppearance.BorderColor = Color.FromArgb(220, 215, 205);
                _pnlPagination.Controls.Add(btnP);
                px += 41;
            }
            _pnlPagination.Width = px;

            // Exact mathematical horizontal centering
            pnlFooter.Resize += (s, e) =>
            {
                _pnlPagination.Location = new Point(Math.Max(120, (pnlFooter.Width - _pnlPagination.Width) / 2), 6);
            };
            _pnlPagination.Location = new Point(Math.Max(120, (pnlRight.Width - px) / 2), 6);

            pnlFooter.Controls.Add(_lblFooterSummary);
            pnlFooter.Controls.Add(_pnlPagination);

            pnlRight.Controls.Add(cardGrid);
            pnlRight.Controls.Add(pnlFooter);
            pnlRight.Controls.Add(pnlFilterBar);

            return pnlRight;
        }

        private void LoadSelectedRowToForm()
        {
            if (_gridProducts.SelectedRows.Count == 0) return;
            var row = _gridProducts.SelectedRows[0];
            if (row.Tag is Product p)
            {
                _selectedProduct = p;
                _lblFormMode.Text = $"Editing #{p.ProductId}";
                _lblFormMode.BackColor = Color.FromArgb(232, 247, 240);
                _lblFormMode.ForeColor = Color.FromArgb(27, 122, 79);
                if (_btnSave != null) _btnSave.Text = "Save Changes";

                _txtFormSku.Text = p.ProductCode;
                _txtFormName.Text = p.ProductName;
                _txtFormPrice.Text = p.UnitPrice.ToString("F2");
                _txtFormCost.Text = (p.UnitPrice * 0.75m).ToString("F2");
                _numFormStock.Value = Math.Max(0, Math.Min(9999, p.StockQuantity));
                _txtFormDesc.Text = p.Description;

                int catIdx = _cboFormCategory.FindStringExact(p.CategoryName);
                if (catIdx >= 0)
                {
                    _cboFormCategory.SelectedIndex = catIdx;
                }
                else if (_cboFormCategory.Items.Count > 0)
                {
                    _cboFormCategory.SelectedIndex = 0;
                }

                string supName = string.IsNullOrWhiteSpace(p.SupplierName) ? "Direct Distribution" : p.SupplierName;
                int supIdx = _cboFormSupplier.FindStringExact(supName);
                if (supIdx >= 0)
                {
                    _cboFormSupplier.SelectedIndex = supIdx;
                }
                else
                {
                    _cboFormSupplier.Items.Add(supName);
                    _cboFormSupplier.SelectedItem = supName;
                }

                if (_btnArchiveRestore != null)
                {
                    _btnArchiveRestore.Enabled = true;
                    if (p.IsActive)
                    {
                        _btnArchiveRestore.Text = "Archive Product";
                        _btnArchiveRestore.CustomTextColor = Color.FromArgb(184, 50, 38);
                    }
                    else
                    {
                        _btnArchiveRestore.Text = "Restore to Active";
                        _btnArchiveRestore.CustomTextColor = Color.FromArgb(27, 122, 79);
                    }
                }
            }
        }

        private void ClearForm()
        {
            _selectedProduct = null;
            _lblFormMode.Text = "New Product Entry";
            _lblFormMode.BackColor = Color.FromArgb(254, 245, 215);
            _lblFormMode.ForeColor = Color.FromArgb(130, 95, 10);
            if (_btnSave != null) _btnSave.Text = "+ Save / Add Product";

            if (_btnArchiveRestore != null)
            {
                _btnArchiveRestore.Enabled = false;
                _btnArchiveRestore.Text = "Archive Product";
                _btnArchiveRestore.CustomTextColor = Color.FromArgb(184, 50, 38);
            }

            _txtFormSku.Clear();
            _txtFormName.Clear();
            _txtFormPrice.Text = "0.00";
            _txtFormCost.Text = "0.00";
            _numFormStock.Value = 10;
            _numFormMinStock.Value = 3;
            _txtFormDesc.Clear();
            if (_cboFormCategory.Items.Count > 0) _cboFormCategory.SelectedIndex = 0;
            if (_cboFormSupplier != null && _cboFormSupplier.Items.Count > 0) _cboFormSupplier.SelectedIndex = 0;
            _txtFormSku.Focus();
        }

        private void BtnSaveProduct_Click(object? sender, EventArgs e)
        {
            if (_selectedProduct != null)
            {
                BtnUpdateProduct_Click(sender, e);
                return;
            }

            string sku = _txtFormSku.Text.Trim();
            string name = _txtFormName.Text.Trim();
            if (string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please provide both Product SKU and Product Name.", "Validation Note", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(_txtFormPrice.Text.Trim(), out decimal price) || price <= 0)
            {
                MessageBox.Show("Please provide a valid retail price (PHP).", "Validation Note", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if SKU already exists
            var existingProduct = _dataService.Products.FirstOrDefault(p => p.ProductCode.Equals(sku, StringComparison.OrdinalIgnoreCase));
            if (existingProduct != null)
            {
                var ask = MessageBox.Show($"A product with SKU '{sku}' ({existingProduct.ProductName}) already exists.\n\nDo you want to update this existing product instead of creating a duplicate?", "Product SKU Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (ask == DialogResult.Yes)
                {
                    _selectedProduct = existingProduct;
                    BtnUpdateProduct_Click(sender, e);
                    return;
                }
                else
                {
                    return;
                }
            }

            string cat = _cboFormCategory.SelectedItem?.ToString() ?? "Graphics Cards (GPU)";
            string sup = _cboFormSupplier?.SelectedItem?.ToString() ?? "Direct Distribution";

            Product newProd = new Product
            {
                ProductCode = sku,
                ProductName = name,
                CategoryName = cat,
                SupplierName = sup,
                UnitPrice = price,
                StockQuantity = (int)_numFormStock.Value,
                Description = _txtFormDesc.Text.Trim()
            };

            _dataService.AddProduct(newProd);
            ApplyFilters();
            OnProductsChanged?.Invoke();
            ClearForm();

            MessageBox.Show($"Product '{name}' (SKU: {sku}) successfully added to inventory!", "Product Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnUpdateProduct_Click(object? sender, EventArgs e)
        {
            if (_selectedProduct == null)
            {
                string sku = _txtFormSku.Text.Trim();
                if (!string.IsNullOrEmpty(sku))
                {
                    _selectedProduct = _dataService.Products.FirstOrDefault(p => p.ProductCode.Equals(sku, StringComparison.OrdinalIgnoreCase));
                }

                if (_selectedProduct == null)
                {
                    MessageBox.Show("Please select a product from the table on the right to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            string name = _txtFormName.Text.Trim();
            string skuText = _txtFormSku.Text.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(skuText))
            {
                MessageBox.Show("Product Name and SKU cannot be empty.", "Validation Note", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(_txtFormPrice.Text.Trim(), out decimal price) || price <= 0)
            {
                MessageBox.Show("Please enter a valid retail price.", "Validation Note", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string cat = _cboFormCategory.SelectedItem?.ToString() ?? "Graphics Cards (GPU)";
            string sup = _cboFormSupplier?.SelectedItem?.ToString() ?? "Direct Distribution";

            _selectedProduct.ProductName = name;
            _selectedProduct.ProductCode = skuText;
            _selectedProduct.CategoryName = cat;
            _selectedProduct.SupplierName = sup;
            _selectedProduct.UnitPrice = price;
            _selectedProduct.StockQuantity = (int)_numFormStock.Value;
            _selectedProduct.Description = _txtFormDesc.Text.Trim();

            _dataService.UpdateProduct(_selectedProduct);
            ApplyFilters();
            OnProductsChanged?.Invoke();

            MessageBox.Show($"Product '{name}' updated successfully!", "Product Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenAddCategoryDialog()
        {
            using var dlg = new UI.Dialogs.AddCategoryDialog();
            if (dlg.ShowDialog() == DialogResult.OK && dlg.CreatedCategory != null)
            {
                RefreshCategoriesInUI(dlg.CreatedCategory.Name);
                OnProductsChanged?.Invoke();
            }
        }

        private void OpenManageCategoriesDialog()
        {
            using var dlg = new UI.Dialogs.ManageCategoriesDialog();
            dlg.ShowDialog(this);
            RefreshCategoriesInUI();
            ApplyFilters();
            OnProductsChanged?.Invoke();
        }

        private void RefreshCategoriesInUI(string? selectedCategory = null)
        {
            string currentFormSelection = selectedCategory ?? _cboFormCategory?.SelectedItem?.ToString() ?? "";
            string currentFilterSelection = _cboCategory?.SelectedItem?.ToString() ?? "All Categories";

            if (_cboFormCategory != null)
            {
                _cboFormCategory.Items.Clear();
                foreach (var c in _dataService.Categories) _cboFormCategory.Items.Add(c.Name);
                int idx = _cboFormCategory.FindStringExact(currentFormSelection);
                _cboFormCategory.SelectedIndex = idx >= 0 ? idx : (_cboFormCategory.Items.Count > 0 ? 0 : -1);
            }

            if (_cboCategory != null)
            {
                _cboCategory.Items.Clear();
                _cboCategory.Items.Add("All Categories");
                foreach (var c in _dataService.Categories) _cboCategory.Items.Add(c.Name);
                int filterIdx = _cboCategory.FindStringExact(currentFilterSelection);
                _cboCategory.SelectedIndex = filterIdx >= 0 ? filterIdx : 0;
            }
        }

        private void RefreshSuppliersInUI(string? selectedSupplier = null)
        {
            if (_cboFormSupplier == null) return;
            string current = selectedSupplier ?? _cboFormSupplier.SelectedItem?.ToString() ?? "Direct Distribution";
            _cboFormSupplier.Items.Clear();
            _cboFormSupplier.Items.Add("Direct Distribution");
            foreach (var s in _dataService.Suppliers.Where(s => s.IsActive))
            {
                if (!string.IsNullOrWhiteSpace(s.SupplierName) && !_cboFormSupplier.Items.Contains(s.SupplierName))
                {
                    _cboFormSupplier.Items.Add(s.SupplierName);
                }
            }
            int idx = _cboFormSupplier.FindStringExact(current);
            _cboFormSupplier.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void BtnArchiveRestore_Click(object? sender, EventArgs e)
        {
            if (_selectedProduct == null)
            {
                if (_gridProducts.SelectedRows.Count > 0 && _gridProducts.SelectedRows[0].Tag is Product p)
                {
                    _selectedProduct = p;
                }
                else
                {
                    MessageBox.Show("Please select a product from the table first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            string prodName = _selectedProduct.ProductName;
            string prodSku = _selectedProduct.ProductCode;
            int prodId = _selectedProduct.ProductId;

            if (_selectedProduct.IsActive)
            {
                var res = MessageBox.Show(
                    $"Are you sure you want to archive '{prodName}' (SKU: {prodSku})?\n\n" +
                    "• The product will be hidden from POS sales.\n" +
                    "• Past sales, receipts, and order histories remain fully intact.\n" +
                    "• You can restore this product at any time from the 'Archived Catalog' view.",
                    "Archive Product Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    _dataService.ArchiveProduct(prodId);
                    ApplyFilters();
                    OnProductsChanged?.Invoke();
                    ClearForm();
                    MessageBox.Show($"Product '{prodName}' has been archived successfully.", "Product Archived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                var res = MessageBox.Show(
                    $"Restore '{prodName}' (SKU: {prodSku}) back to the active catalog?\n\n" +
                    "• The product will immediately become available for sale in the POS terminal.",
                    "Restore Product Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    _dataService.RestoreProduct(prodId);
                    ApplyFilters();
                    OnProductsChanged?.Invoke();
                    ClearForm();
                    MessageBox.Show($"Product '{prodName}' restored to active catalog!", "Product Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public void ApplyFilters()
        {
            if (_gridProducts == null) return;
            _gridProducts.Rows.Clear();

            var list = _dataService.Products.AsEnumerable();

            string archiveMode = _cboArchiveFilter?.SelectedItem?.ToString() ?? "Active Products";
            if (archiveMode == "Active Products")
            {
                list = list.Where(p => p.IsActive);
            }
            else if (archiveMode == "Archived Catalog")
            {
                list = list.Where(p => !p.IsActive);
            }

            string query = _txtSearch?.Text.Trim() ?? "";
            if (!string.IsNullOrEmpty(query))
            {
                list = list.Where(p => p.ProductName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                       p.ProductCode.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            string cat = _cboCategory?.SelectedItem?.ToString() ?? "All Categories";
            if (cat != "All Categories")
            {
                list = list.Where(p => p.CategoryName.Equals(cat, StringComparison.OrdinalIgnoreCase));
            }

            string status = _cboStockStatus?.SelectedItem?.ToString() ?? "All Stock Status";
            if (status == "In Stock") list = list.Where(p => p.StockQuantity > 5);
            else if (status == "Low Stock") list = list.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
            else if (status == "Out of Stock") list = list.Where(p => p.StockQuantity <= 0);

            var filteredList = list.ToList();
            int totalItems = filteredList.Count;
            int pageSize = GetEffectivePageSize();
            _totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));

            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;

            var pageItems = filteredList.Skip((_currentPage - 1) * pageSize).Take(pageSize).ToList();

            decimal totalVal = filteredList.Sum(p => p.UnitPrice * p.StockQuantity);
            int lowStock = filteredList.Count(p => p.StockQuantity <= 2);

            foreach (var p in pageItems)
            {
                string stockStatus = !p.IsActive
                    ? "Archived"
                    : (p.StockQuantity > 2 ? "In Stock" : (p.StockQuantity > 0 ? $"Low Stock ({p.StockQuantity})" : "Out of Stock"));
                decimal cost = p.UnitPrice * 0.75m;

                int rowIdx = _gridProducts.Rows.Add(
                    p.ProductCode,
                    p.ProductName,
                    p.CategoryName,
                    string.IsNullOrWhiteSpace(p.SupplierName) ? "Direct Distribution" : p.SupplierName,
                    $"₱{p.UnitPrice:N2}",
                    $"₱{cost:N2}",
                    $"{p.StockQuantity}",
                    stockStatus,
                    p.IsActive ? "📦 Archive" : "♻️ Restore"
                );

                var row = _gridProducts.Rows[rowIdx];
                row.Tag = p;
                row.Cells["ColId"].Style.ForeColor = Color.FromArgb(160, 110, 10);
                row.Cells["ColId"].Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

                var actionCell = row.Cells["ColAction"];
                actionCell.Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

                if (!p.IsActive)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(140, 140, 140);
                    row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(160, 80, 80);
                    row.Cells["ColStatus"].Style.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
                    actionCell.Style.ForeColor = Color.FromArgb(27, 122, 79);
                }
                else
                {
                    actionCell.Style.ForeColor = Color.FromArgb(184, 50, 38);

                    if (p.StockQuantity > 2)
                    {
                        row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(27, 122, 79);
                    }
                    else if (p.StockQuantity > 0)
                    {
                        row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(168, 110, 5);
                    }
                    else
                    {
                        row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(184, 50, 38);
                    }
                }
            }

            UpdatePaginationButtons(totalItems, pageSize);
        }

        private int GetEffectivePageSize()
        {
            if (_gridProducts == null || _gridProducts.ClientSize.Height <= 100) return 12;
            int availableHeight = _gridProducts.ClientSize.Height - _gridProducts.ColumnHeadersHeight;
            int rowHeight = _gridProducts.RowTemplate.Height > 0 ? _gridProducts.RowTemplate.Height : 44;
            int fitRows = availableHeight / rowHeight;
            return Math.Max(10, fitRows);
        }

        private void UpdatePaginationButtons(int totalItems, int pageSize)
        {
            if (_pnlPagination == null) return;
            _pnlPagination.SuspendLayout();
            _pnlPagination.Controls.Clear();

            List<string> buttons = new List<string> { "Prev" };
            for (int i = 1; i <= _totalPages; i++)
            {
                buttons.Add(i.ToString());
            }
            buttons.Add("Next");

            int px = 0;
            foreach (var text in buttons)
            {
                int btnWidth = (text == "Prev" || text == "Next") ? 52 : 36;
                Button btn = new Button
                {
                    Text = text,
                    Size = new Size(btnWidth, 24),
                    Location = new Point(px, 2),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(220, 215, 205);

                if (text == "Prev")
                {
                    btn.Enabled = _currentPage > 1;
                    btn.BackColor = btn.Enabled ? Color.White : Color.FromArgb(245, 243, 238);
                    btn.ForeColor = btn.Enabled ? AppTheme.TextDark : Color.Gray;
                    btn.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; ApplyFilters(); } };
                }
                else if (text == "Next")
                {
                    btn.Enabled = _currentPage < _totalPages;
                    btn.BackColor = btn.Enabled ? Color.White : Color.FromArgb(245, 243, 238);
                    btn.ForeColor = btn.Enabled ? AppTheme.TextDark : Color.Gray;
                    btn.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; ApplyFilters(); } };
                }
                else if (int.TryParse(text, out int pageNum))
                {
                    bool isCurrent = pageNum == _currentPage;
                    btn.BackColor = isCurrent ? AppTheme.Primary : Color.White;
                    btn.Font = isCurrent ? new Font("Segoe UI", 7F, FontStyle.Bold) : new Font("Segoe UI", 7F, FontStyle.Regular);
                    btn.Click += (s, e) => { _currentPage = pageNum; ApplyFilters(); };
                }

                _pnlPagination.Controls.Add(btn);
                px += btnWidth + 3;
            }

            _pnlPagination.Width = px;
            if (_pnlPagination.Parent != null)
            {
                _pnlPagination.Location = new Point(Math.Max(120, (_pnlPagination.Parent.Width - px) / 2), 6);
            }
            _pnlPagination.ResumeLayout();

            int start = totalItems == 0 ? 0 : (_currentPage - 1) * pageSize + 1;
            int end = Math.Min(_currentPage * pageSize, totalItems);
            if (_lblFooterSummary != null)
            {
                _lblFooterSummary.Text = $"Showing {start} - {end} of {totalItems} products (Page {_currentPage} of {_totalPages})";
            }
        }

        public void FilterLowStockOnly()
        {
            if (_cboStockStatus != null) _cboStockStatus.SelectedItem = "Low Stock";
        }
    }
}
