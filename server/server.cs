using System;
using System.Windows.Forms;

namespace WindowsAgentGUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void placeholderButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Button clicked (placeholder)");
        }
    }
}
