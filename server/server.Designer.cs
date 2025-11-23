namespace WindowsAgentGUI
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Button placeholderButton;

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
            this.placeholderButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // placeholderButton
            // 
            this.placeholderButton.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.placeholderButton.Location = new System.Drawing.Point(60, 40);
            this.placeholderButton.Name = "placeholderButton";
            this.placeholderButton.Size = new System.Drawing.Size(180, 50);
            this.placeholderButton.TabIndex = 0;
            this.placeholderButton.Text = "Placeholder Button";
            this.placeholderButton.UseVisualStyleBackColor = true;
            this.placeholderButton.Click += new System.EventHandler(this.placeholderButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 130);
            this.Controls.Add(this.placeholderButton);
            this.Name = "Form1";
            this.Text = "Windows Agent";
            this.ResumeLayout(false);
        }
    }
}
