using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
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
        private Label ipLabel; // Label để hiển thị IP
        private ClientWebSocket ws;
        
        // Khai báo Feature class
        private Feature _featureLogic;

        public MainButtonForm()
        {
            // --- 1. CẤU HÌNH FORM ---
            this.Text = "Windows Agent - Monitor";
            this.Width = 400;
            this.Height = 350;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            // --- 2. HIỂN THỊ IP MÁY HIỆN TẠI ---
            string myIP = GetLocalIPAddress();

            ipLabel = new Label();
            ipLabel.Text = "My IP Address: " + myIP;
            ipLabel.Font = new Font("Consolas", 12, FontStyle.Bold);
            ipLabel.ForeColor = Color.DarkRed;
            ipLabel.AutoSize = true;
            ipLabel.Top = 15;
            // Căn giữa Label IP
            ipLabel.Left = (this.ClientSize.Width - 250) / 2; 
            this.Controls.Add(ipLabel);

            // --- 3. NÚT KẾT NỐI ---
            openServerButton = new Button();
            openServerButton.Text = "KẾT NỐI GATEWAY"; 
            openServerButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            openServerButton.BackColor = Color.FromArgb(52, 152, 219);
            openServerButton.ForeColor = Color.White;
            openServerButton.FlatStyle = FlatStyle.Flat;
            openServerButton.Width = 200; 
            openServerButton.Height = 50;
            openServerButton.Left = (this.ClientSize.Width - openServerButton.Width) / 2;
            openServerButton.Top = 50;

            // Bo tròn nút
             openServerButton.Region = Region.FromHrgn(
                CreateRoundRectRgn(0, 0, openServerButton.Width, openServerButton.Height, 20, 20)
            );

            openServerButton.Click += async (s, e) =>
            {
                openServerButton.Enabled = false; 
                openServerButton.Text = "Đang kết nối...";
                openServerButton.BackColor = Color.Gray;
                await ConnectToGatewayAsync();
            };
            this.Controls.Add(openServerButton);

            // --- 4. LOG BOX ---
            logBox = new TextBox();
            logBox.Multiline = true;
            logBox.Width = 350;
            logBox.Height = 180;
            logBox.Left = 20;
            logBox.Top = 120;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.ReadOnly = true;
            logBox.BackColor = Color.Black;
            logBox.ForeColor = Color.LimeGreen; // Giao diện hacker
            logBox.Font = new Font("Consolas", 9);
            this.Controls.Add(logBox);

            // KHỞI TẠO LOGIC FEATURE
            _featureLogic = new Feature();
        }

        // --- HÀM LẤY ĐỊA CHỈ IP CỦA MÁY NÀY ---
        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    // Chỉ lấy IPv4 (dạng 192.168.x.x)
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "Không tìm thấy IP";
            }
            catch
            {
                return "Lỗi mạng";
            }
        }

        // --- LOGIC KẾT NỐI SOCKET ---
        private async Task ConnectToGatewayAsync()
        {
            try
            {
                ws = new ClientWebSocket();
                
                // LƯU Ý: Nếu Gateway chạy ở máy khác, hãy nhập IP của máy chạy Node.js vào đây
                // Ví dụ: ws://192.168.1.15:8080
                var gatewayUri = new Uri("ws://127.0.0.1:8080"); 
                
                UpdateLog("⏳ Đang tìm Gateway tại " + gatewayUri + "...");
                await ws.ConnectAsync(gatewayUri, CancellationToken.None);
                
                UpdateLog("✅ Đã kết nối thành công!");
                openServerButton.Text = "ĐÃ KẾT NỐI";
                openServerButton.BackColor = Color.Green;

                // Gửi tin nhắn đăng ký nhận diện là AGENT
                var msg = Encoding.UTF8.GetBytes("REGISTER_AGENT");
                await ws.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
                UpdateLog("📩 Đã gửi tín hiệu nhận diện (REGISTER_AGENT)");

                // Bắt đầu lắng nghe lệnh từ Web gửi về
                _ = Task.Run(ListenLoop);
            }
            catch (Exception ex)
            {
                UpdateLog("❌ Lỗi kết nối: " + ex.Message);
                openServerButton.Enabled = true;
                openServerButton.Text = "THỬ LẠI";
                openServerButton.BackColor = Color.Red;
            }
        }

        // --- VÒNG LẶP LẮNG NGHE LỆNH ---
        private async Task ListenLoop()
        {
            var buffer = new byte[1024 * 1024 * 2]; // Buffer 2MB (để chứa ảnh nếu cần nhận)
            
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        UpdateLog("🔌 Server Gateway đã ngắt kết nối");
                        break;
                    }

                    string receivedCmd = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    UpdateLog("⬅️ Server ra lệnh: " + receivedCmd);

                    // === XỬ LÝ LỆNH VÀ TRẢ KẾT QUẢ ===
                    // Gọi class Feature để thực hiện
                    string responseData = _featureLogic.ProcessCommand(receivedCmd);

                    if (!string.IsNullOrEmpty(responseData))
                    {
                        byte[] sendBytes = Encoding.UTF8.GetBytes(responseData);
                        await ws.SendAsync(new ArraySegment<byte>(sendBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        UpdateLog("➡️ Đã gửi kết quả (" + sendBytes.Length + " bytes)");
                    }
                }
                catch (Exception ex)
                {
                    UpdateLog("❌ Lỗi khi đang lắng nghe: " + ex.Message);
                    break;
                }
            }
        }

        // Hàm update log an toàn (Thread-safe)
        private void UpdateLog(string msg)
        {
            if (logBox.InvokeRequired)
            {
                logBox.Invoke(new Action(() => logBox.AppendText(msg + "\r\n")));
            }
            else
            {
                logBox.AppendText(msg + "\r\n");
            }
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainButtonForm());
        }
    }
}
