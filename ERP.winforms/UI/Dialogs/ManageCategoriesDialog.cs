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
    public class ManageCategoriesDialog : Form
    {
        private readonly DataService _dataService = DataService.Instance;

        private DataGridView _gridCategories = null!;
        private SunshineButton _btnAdd = null!;
        private SunshineButton _btnEdit = null!;
        private SunshineButton _btnDelete = null!;
        private SunshineButton _btnClose = null!;
        private Label _lblStatusInfo = null!;

        public bool HasChanges { get; private set; }

        public ManageCategoriesDialog()
        {
            Text = "Manage Product Categories";
            Size = new Size(680, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.CardBackground;

            InitializeLayout();
            LoadCategories();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(20, 14, 20, 0)
            };

            Label lblTitle = new Label
            {
                Text = "CATEGORY MANAGEMENT",
                Font = AppTheme.SubheaderFont,
                ForeColor = Color.FromArgb(160, 110, 10),
                Location = new Point(20, 12),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Manage product categories. Default system presets are protected and cannot be deleted.",
                Font = new Font("Segoe UI", 8.25F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, 36),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Bottom Actions Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(20, 10, 20, 10)
            };

            _btnAdd = new SunshineButton
            {
                Text = "+ New Category",
                IsPrimary = true,
                Location = new Point(20, 10),
                Size = new Size(135, 36)
            };
            _btnAdd.Click += BtnAdd_Click;

            _btnEdit = new SunshineButton
            {
                Text = "✏️ Rename / Edit",
                IsPrimary = false,
                Location = new Point(165, 10),
                Size = new Size(135, 36),
                Enabled = false
            };
            _btnEdit.Click += BtnEdit_Click;

            _btnDelete = new SunshineButton
            {
                Text = "🗑️ Delete",
                IsPrimary = false,
                Location = new Point(310, 10),
                Size = new Size(100, 36),
                Enabled = false
            };
            _btnDelete.Click += BtnDelete_Click;

            _btnClose = new SunshineButton
            {
                Text = "Close",
                IsPrimary = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlBottom.Width - 120, 10),
                Size = new Size(100, 36)
            };
            _btnClose.Click += (s, e) => { DialogResult = HasChanges ? DialogResult.OK : DialogResult.Cancel; Close(); };

            pnlBottom.Controls.Add(_btnAdd);
            pnlBottom.Controls.Add(_btnEdit);
            pnlBottom.Controls.Add(_btnDelete);
            pnlBottom.Controls.Add(_btnClose);

            // Status Bar / Info Panel
            Panel pnlStatus = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                Padding = new Padding(20, 4, 20, 4),
                BackColor = Color.FromArgb(248, 246, 240)
            };
            _lblStatusInfo = new Label
            {
                Text = "Select a category to edit or delete.",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlStatus.Controls.Add(_lblStatusInfo);

            // DataGridView for Categories
            _gridCategories = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(235, 230, 220),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 38 }
            };

            _gridCategories.EnableHeadersVisualStyles = false;
            _gridCategories.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBg,
                ForeColor = AppTheme.GridHeaderText,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Padding = new Padding(8, 0, 0, 0)
            };
            _gridCategories.ColumnHeadersHeight = 36;
            _gridCategories.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = AppTheme.TextDark,
                SelectionBackColor = AppTheme.GridRowSelected,
                SelectionForeColor = AppTheme.TextDark,
                Padding = new Padding(8, 0, 0, 0)
            };

            _gridCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CATEGORY NAME", FillWeight = 32, Name = "ColName" });
            _gridCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DESCRIPTION", FillWeight = 36, Name = "ColDesc" });
            _gridCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PRODUCTS", FillWeight = 16, Name = "ColCount" });
            _gridCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TYPE", FillWeight = 16, Name = "ColType" });

            _gridCategories.SelectionChanged += GridCategories_SelectionChanged;

            Panel pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 4, 20, 6)
            };
            pnlCenter.Controls.Add(_gridCategories);

            Controls.Add(pnlCenter);
            Controls.Add(pnlStatus);
            Controls.Add(pnlBottom);
            Controls.Add(pnlHeader);
        }

        private void LoadCategories()
        {
            _gridCategories.Rows.Clear();

            var categories = _dataService.Categories.OrderBy(c => c.Id).ToList();
            foreach (var c in categories)
            {
                int prodCount = _dataService.Products.Count(p => p.CategoryName.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
                bool isPreset = _dataService.IsDefaultPreset(c.Name);
                string typeBadge = isPreset ? "[Preset]" : "[Custom]";
                string prodText = prodCount == 1 ? "1 product" : $"{prodCount} products";

                int rowIndex = _gridCategories.Rows.Add(c.Name, c.Description ?? "", prodText, typeBadge);
                var row = _gridCategories.Rows[rowIndex];
                row.Tag = c;

                if (isPreset)
                {
                    row.Cells["ColType"].Style.ForeColor = Color.FromArgb(160, 110, 10);
                    row.Cells["ColType"].Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                }
                else
                {
                    row.Cells["ColType"].Style.ForeColor = Color.FromArgb(30, 140, 60);
                    row.Cells["ColType"].Style.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                }
            }

            UpdateActionButtons();
        }

        private void GridCategories_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            if (_gridCategories.SelectedRows.Count == 0 || !(_gridCategories.SelectedRows[0].Tag is Category cat))
            {
                _btnEdit.Enabled = false;
                _btnDelete.Enabled = false;
                _lblStatusInfo.Text = "Select a category to edit or delete.";
                _lblStatusInfo.ForeColor = AppTheme.TextMuted;
                return;
            }

            bool isPreset = _dataService.IsDefaultPreset(cat.Name);
            int prodCount = _dataService.Products.Count(p => p.CategoryName.Equals(cat.Name, StringComparison.OrdinalIgnoreCase));

            _btnEdit.Enabled = true;

            if (isPreset)
            {
                _btnDelete.Enabled = false;
                _lblStatusInfo.Text = $"'{cat.Name}' is a default system preset and cannot be deleted. ({prodCount} active products)";
                _lblStatusInfo.ForeColor = Color.FromArgb(140, 100, 10);
            }
            else
            {
                _btnDelete.Enabled = true;
                _lblStatusInfo.Text = $"Custom category '{cat.Name}' selected. ({prodCount} active products)";
                _lblStatusInfo.ForeColor = AppTheme.TextDark;
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var dlg = new AddCategoryDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.CreatedCategory != null)
            {
                HasChanges = true;
                LoadCategories();

                // Select the new category
                foreach (DataGridViewRow row in _gridCategories.Rows)
                {
                    if (row.Tag is Category c && c.Id == dlg.CreatedCategory.Id)
                    {
                        row.Selected = true;
                        break;
                    }
                }
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_gridCategories.SelectedRows.Count == 0 || !(_gridCategories.SelectedRows[0].Tag is Category cat)) return;

            using var dlg = new EditCategoryDialog(cat);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.HasChanges)
            {
                HasChanges = true;
                LoadCategories();
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_gridCategories.SelectedRows.Count == 0 || !(_gridCategories.SelectedRows[0].Tag is Category cat)) return;

            if (_dataService.IsDefaultPreset(cat.Name))
            {
                MessageBox.Show("Default system preset categories are protected and cannot be deleted.",
                    "Protected Category", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int productCount = _dataService.Products.Count(p => p.CategoryName.Equals(cat.Name, StringComparison.OrdinalIgnoreCase));
            const string fallback = "Graphics Cards (GPU)";

            string message = productCount > 0
                ? $"Category '{cat.Name}' is currently used by {productCount} product(s).\n\nDeleting this category will automatically reassign those {productCount} product(s) to '{fallback}'.\n\nAre you sure you want to permanently delete this category?"
                : $"Are you sure you want to permanently delete custom category '{cat.Name}'?";

            var confirm = MessageBox.Show(message, "Confirm Category Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            bool success = _dataService.DeleteCategory(cat.Id, fallback);
            if (success)
            {
                HasChanges = true;
                LoadCategories();
                MessageBox.Show($"Category '{cat.Name}' was deleted successfully." + (productCount > 0 ? $"\n{productCount} product(s) reassigned to '{fallback}'." : ""),
                    "Category Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to delete category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (HasChanges)
            {
                DialogResult = DialogResult.OK;
            }
        }
    }
}
