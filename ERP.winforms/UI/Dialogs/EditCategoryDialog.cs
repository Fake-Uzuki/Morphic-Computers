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
    public class EditCategoryDialog : Form
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly Category _category;

        private TextBox _txtName = null!;
        private TextBox _txtDesc = null!;
        private SunshineButton _btnSave = null!;
        private SunshineButton _btnCancel = null!;

        public bool HasChanges { get; private set; }

        public EditCategoryDialog(Category category)
        {
            _category = category ?? throw new ArgumentNullException(nameof(category));

            Text = "Edit Product Category";
            Size = new Size(440, 310);
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
                Text = "EDIT CATEGORY",
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
                Width = 384,
                Text = _category.Name
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
                Width = 384,
                Text = _category.Description ?? ""
            };
            y += 60;

            // Action Buttons
            _btnSave = new SunshineButton
            {
                Text = "Save Changes",
                IsPrimary = true,
                Location = new Point(20, y),
                Width = 185,
                Height = 40
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new SunshineButton
            {
                Text = "Cancel",
                IsPrimary = false,
                Location = new Point(219, y),
                Width = 185,
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
            string newName = _txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Please enter a valid category name.", "Validation Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            // Check if duplicate with another category
            if (_dataService.Categories.Any(c => c.Id != _category.Id && c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"Another category named '{newName}' already exists.", "Duplicate Category", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtName.Focus();
                return;
            }

            string newDesc = _txtDesc.Text.Trim();
            bool success = _dataService.UpdateCategory(_category.Id, newName, newDesc);
            if (success)
            {
                HasChanges = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Failed to update category. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
