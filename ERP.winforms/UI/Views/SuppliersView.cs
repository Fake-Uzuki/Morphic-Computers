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
    public class SuppliersView : UserControl
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridSuppliers = null!;
        private TextBox _txtSearch = null!;

        public SuppliersView()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.AppBackground;
            AutoScroll = true;

            InitializeLayout();
            _dataService.SuppliersChanged += () =>
            {
                if (InvokeRequired) Invoke(new Action(RefreshData));
                else RefreshData();
            };

            RefreshData();
        }

        private void InitializeLayout()
        {
            Controls.Clear();



            // ========================================================
            // 2. TOOLBAR (Search & Actions)
            // ========================================================
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(20, 4, 20, 6),
                BackColor = AppTheme.OperationsBarBg
            };

            Panel pnlSearch = new Panel
            {
                Location = new Point(20, 9),
                Size = new Size(300, 34),
                BackColor = Color.White
            };
            pnlSearch.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };

            _txtSearch = new TextBox
            {
                Location = new Point(8, 7),
                Width = 284,
                BorderStyle = BorderStyle.None,
                Font = AppTheme.BodyFont,
                PlaceholderText = "Search supplier, contact, address..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlSearch.Controls.Add(_txtSearch);

            SunshineButton btnAddSupplier = new SunshineButton
            {
                Text = "+ Add Supplier",
                IsPrimary = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 170, 8),
                Size = new Size(150, 34),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAddSupplier.Click += (s, e) =>
            {
                using var dialog = new SupplierEditDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RefreshData();
                }
            };

            pnlToolbar.Controls.Add(pnlSearch);
            pnlToolbar.Controls.Add(btnAddSupplier);

            // ========================================================
            // 3. MAIN TABLE CONTAINER
            // ========================================================
            Panel pnlMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 14),
                BackColor = Color.Transparent
            };

            SunshineCard cardGrid = new SunshineCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                CustomBgColor = Color.White,
                CustomBorderColor = AppTheme.CardBorder
            };

            _gridSuppliers = new DataGridView
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

            _gridSuppliers.EnableHeadersVisualStyles = false;
            _gridSuppliers.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _gridSuppliers.ColumnHeadersHeight = 36;
            _gridSuppliers.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(6, 0, 0, 0)
            };

            _gridSuppliers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CODE", FillWeight = 10, Name = "ColCode" });
            _gridSuppliers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SUPPLIER / DISTRIBUTOR NAME", FillWeight = 24, Name = "ColName" });
            _gridSuppliers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CONTACT PERSON", FillWeight = 18, Name = "ColContact" });
            _gridSuppliers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PHONE", FillWeight = 14, Name = "ColPhone" });
            _gridSuppliers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "EMAIL", FillWeight = 16, Name = "ColEmail" });
            _gridSuppliers.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OFFICE / WAREHOUSE", FillWeight = 20, Name = "ColAddress" });
            _gridSuppliers.Columns.Add(new DataGridViewButtonColumn { HeaderText = "EDIT", FillWeight = 8, Text = "Edit", UseColumnTextForButtonValue = true, Name = "ColEdit" });
            _gridSuppliers.Columns.Add(new DataGridViewButtonColumn { HeaderText = "DELETE", FillWeight = 8, Text = "Delete", UseColumnTextForButtonValue = true, Name = "ColDelete" });

            _gridSuppliers.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    int supplierId = Convert.ToInt32(_gridSuppliers.Rows[e.RowIndex].Tag);
                    var sup = _dataService.Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);
                    if (sup == null) return;

                    if (e.ColumnIndex == _gridSuppliers.Columns["ColEdit"]!.Index)
                    {
                        using var editDlg = new SupplierEditDialog(sup);
                        if (editDlg.ShowDialog() == DialogResult.OK)
                        {
                            RefreshData();
                        }
                    }
                    else if (e.ColumnIndex == _gridSuppliers.Columns["ColDelete"]!.Index)
                    {
                        var res = MessageBox.Show(
                            $"Are you sure you want to remove supplier '{sup.SupplierName}'?",
                            "Confirm Deletion",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (res == DialogResult.Yes)
                        {
                            _dataService.DeleteSupplier(supplierId);
                            RefreshData();
                        }
                    }
                }
            };

            cardGrid.Controls.Add(_gridSuppliers);
            pnlMainContainer.Controls.Add(cardGrid);

            Controls.Add(pnlMainContainer);
            Controls.Add(pnlToolbar);
        }

        public void RefreshData()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string query = _txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = _dataService.Suppliers.AsEnumerable();

            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(s =>
                    s.SupplierName.ToLowerInvariant().Contains(query) ||
                    s.SupplierCode.ToLowerInvariant().Contains(query) ||
                    (s.ContactPerson != null && s.ContactPerson.ToLowerInvariant().Contains(query)) ||
                    (s.Address != null && s.Address.ToLowerInvariant().Contains(query)) ||
                    (s.EmailAddress != null && s.EmailAddress.ToLowerInvariant().Contains(query)));
            }

            _gridSuppliers.Rows.Clear();
            foreach (var s in filtered)
            {
                int rowIdx = _gridSuppliers.Rows.Add(
                    s.SupplierCode,
                    s.SupplierName,
                    s.ContactPerson ?? "N/A",
                    s.ContactNumber ?? "N/A",
                    s.EmailAddress ?? "N/A",
                    s.Address ?? "N/A"
                );
                _gridSuppliers.Rows[rowIdx].Tag = s.SupplierId;
            }
        }
    }
}
