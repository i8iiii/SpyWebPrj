using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;

namespace server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // Create a new button
            Button startButton = new Button();
            startButton.Text = "Start";
            startButton.Font = new Font("Segoe UI", 14, FontStyle.Bold);

            // Colors and style
            startButton.BackColor = Color.FromArgb(52, 152, 219); // nice blue
            startButton.ForeColor = Color.White;
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.FlatAppearance.BorderColor = Color.White;
            startButton.FlatAppearance.BorderSize = 0;  // no border

            // Size
            startButton.Width = 200;
            startButton.Height = 60;

            // Center on the form
            startButton.Left = (this.ClientSize.Width - startButton.Width) / 2;
            startButton.Top = (this.ClientSize.Height - startButton.Height) / 2;

            // Rounded corners
            startButton.Region = Region.FromHrgn(
                CreateRoundRectRgn(0, 0, startButton.Width, startButton.Height, 20, 20)
            );

            // Click event
            startButton.Click += (s, e) =>
            {
                MessageBox.Show("Button clicked!");
            };

            // Add the button to the form
            this.Controls.Add(startButton);
        }

        // Rounded corner function
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );
    }
}
