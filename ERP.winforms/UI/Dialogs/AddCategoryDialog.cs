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
    public class AddCategoryDialog : Form
    {
        private readonly DataService _dataService = DataService.Instance;

        private TextBox _txtName = null!;
        private TextBox _txtDesc = null!;
        private SunshineButton _btnSave = null!;
        private SunshineButton _btnCancel = null!;

        public Category? CreatedCategory { get; private set; }

        public AddCategoryDialog()
        {
            Text = "Add Product Category";
            Size = new Size(420, 290);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.CardBackground;

            InitializeForm();
        }

        private void InitializeForm()
        {
            Controls.Clear();

            Label lblTitle = new Label
            {
                Text = "NEW PRODUCT CATEGORY",
                Font = AppTheme.SubheaderFont,
                ForeColor = Color.FromArgb(160, 110, 10),
                Location = new Point(20, 16),
                AutoSize = true
            };

            int y = 50;

            // Category Name
            Label lblName = new Label
            {
                Text = "Category Name *",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, y),
                AutoSize = true
            };
            _txtName = new TextBox
            {
                Font = AppTheme.BodyFont,
                Location = new Point(20, y + 18),
                Width = 364
            };
            y += 52;

            // Description
            Label lblDesc = new Label
            {
                Text = "Description (Optional)",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, y),
                AutoSize = true
            };
            _txtDesc = new TextBox
            {
                Font = AppTheme.BodyFont,
                Location = new Point(20, y + 18),
                Width = 364
            };
            y += 60;

            // Buttons
            _btnSave = new SunshineButton
            {
                Text = "+ Save Category",
                IsPrimary = true,
                Location = new Point(20, y),
                Width = 175,
                Height = 40
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(209, y),
                Width = 175,
                Height = 40
            };
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblTitle);
            Controls.Add(lblName);
            Controls.Add(_txtName);
            Controls.Add(lblDesc);
            Controls.Add(_txtDesc);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string name = _txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a valid category name.", "Validation Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            if (_dataService.Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"A category named '{name}' already exists.", "Duplicate Category", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtName.Focus();
                return;
            }

            var newCat = new Category
            {
                Name = name,
                Description = _txtDesc.Text.Trim(),
                CompanyId = _dataService.ActiveCompanyId
            };

            _dataService.AddCategory(newCat);
            CreatedCategory = newCat;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
