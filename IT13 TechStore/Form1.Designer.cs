namespace IT8_TechStore
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1463, 1040);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(1168, 851);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Morphic Computers - Inventory & POS Operations Center";
            Load += Form1_Load;
            ResumeLayout(false);
        }
    }
}
