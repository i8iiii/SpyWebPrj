using System;
using System.Drawing;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsAgent
{
    public class MainButtonForm : Form
    {
        private Button openServerButton;
        private TextBox logBox;
        private ClientWebSocket ws;

        public MainButtonForm()
        {
            // Form properties
            this.Text = "Windows Agent";
            this.Width = 400;
            this.Height = 300;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Log box
            logBox = new TextBox();
            logBox.Multiline = true;
            logBox.Width = 350;
            logBox.Height = 150;
            logBox.Left = 20;
            logBox.Top = 120;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.ReadOnly = true;
            this.Controls.Add(logBox);

            // Open Server button
            openServerButton = new Button();
            openServerButton.Text = "Open Server";
            openServerButton.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            openServerButton.BackColor = Color.FromArgb(52, 152, 219);
            openServerButton.ForeColor = Color.White;
            openServerButton.FlatStyle = FlatStyle.Flat;
            openServerButton.FlatAppearance.BorderSize = 0;
            openServerButton.Width = 200;
            openServerButton.Height = 60;
            openServerButton.Left = (this.ClientSize.Width - openServerButton.Width) / 2;
            openServerButton.Top = 40;

            // Rounded corners
            openServerButton.Region = Region.FromHrgn(
                CreateRoundRectRgn(0, 0, openServerButton.Width, openServerButton.Height, 20, 20)
            );

            openServerButton.Click += async (s, e) =>
            {
                await ConnectToGatewayAsync();
            };

            this.Controls.Add(openServerButton);
        }

        private async Task ConnectToGatewayAsync()
        {
            try
            {
                ws = new ClientWebSocket();
                // Replace with your Node.js gateway IP/port
                var gatewayUri = new Uri("ws://localhost:8080"); 
                await ws.ConnectAsync(gatewayUri, CancellationToken.None);
                logBox.AppendText("✅ Connected to Gateway\n");

                // Register as Agent
                var msg = Encoding.UTF8.GetBytes("REGISTER_AGENT");
                await ws.SendAsync(msg, WebSocketMessageType.Text, true, CancellationToken.None);
                logBox.AppendText("📩 REGISTER_AGENT sent\n");

                // Start listening for messages
                _ = Task.Run(ListenLoop);
            }
            catch (Exception ex)
            {
                logBox.AppendText("❌ Error: " + ex.Message + "\n");
            }
        }

        private async Task ListenLoop()
        {
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        logBox.Invoke(() => logBox.AppendText("🔌 Disconnected from Gateway\n"));
                        break;
                    }

                    var received = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    logBox.Invoke(() => logBox.AppendText("⬅️ " + received + "\n"));

                    // Here you can parse commands, e.g., take screenshot, etc.
                }
                catch (Exception ex)
                {
                    logBox.Invoke(() => logBox.AppendText("❌ Listen error: " + ex.Message + "\n"));
                    break;
                }
            }
        }

        // Rounded corners function
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainButtonForm());
        }
    }
}
