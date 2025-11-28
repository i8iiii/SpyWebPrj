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
        private ClientWebSocket ws;
        private Feature _featureLogic;

        public MainButtonForm()
        {
            // 1. Khởi tạo Logic
            _featureLogic = new Feature();

            // 2. Đăng ký sự kiện: Khi Feature có dữ liệu (ảnh, video, log) -> Gửi Socket
            _featureLogic.OnDataReady += async (data) => 
            {
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    try {
                        byte[] bytes = Encoding.UTF8.GetBytes(data);
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        
                        // Log hiển thị trên Form Agent (để debug)
                        if (data.StartsWith("IMG_BASE64")) UpdateLog("📸 Đã gửi frame ảnh.");
                        else if (data.StartsWith("VIDEO_DATA")) UpdateLog("🎥 Đã gửi file video.");
                        else UpdateLog("➡️ " + data);
                    }
                    catch { UpdateLog("❌ Lỗi gửi Socket."); }
                }
            };

            // 3. UI Setup
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "Agent Monitor";
            this.Width = 450; this.Height = 150;
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // IP Box
            TextBox ipBox = new TextBox { Text = GetLocalIPAddress(), Top = 15, Left = 75, Width = 280, ReadOnly = true, TextAlign = HorizontalAlignment.Center };
            this.Controls.Add(ipBox);

            // Connect Button
            openServerButton = new Button { Text = "CONNECT", Top = 50, Left = 125, Width = 180, Height = 40, BackColor = Color.LightBlue };
            openServerButton.Click += async (s, e) => {
                openServerButton.Enabled = false;
                await ConnectToGatewayAsync();
            };
            this.Controls.Add(openServerButton);

            // Log Box
            logBox = new TextBox { Multiline = true, Top = 110, Left = 20, Width = 390, Height = 230, ScrollBars = ScrollBars.Vertical, BackColor = Color.Black, ForeColor = Color.Lime };
            this.Controls.Add(logBox);
        }

        private async Task ConnectToGatewayAsync()
        {
            try {
                ws = new ClientWebSocket();
                ws.Options.SetBuffer(1024 * 1024 * 10, 1024 * 1024 * 10); // Tăng buffer 10MB để gửi video
                
                // Thay đổi IP Gateway nếu cần (ví dụ 192.168.1.x)
                // Lưu ý: Dùng IP tĩnh nếu Server ở máy khác, GetLocalIPAddress() chỉ đúng nếu chạy chung máy
                string serverIp = GetLocalIPAddress(); // Hoặc GetLocalIPAddress()
                await ws.ConnectAsync(new Uri($"ws://{serverIp}:8080"), CancellationToken.None);

                UpdateLog("✅ Kết nối thành công!");
                openServerButton.BackColor = Color.Green;
                
                // Đăng ký danh tính
                byte[] msg = Encoding.UTF8.GetBytes("REGISTER_AGENT");
                await ws.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);

                _ = Task.Run(ListenLoop);
            }
            catch (Exception ex) {
                UpdateLog("❌ Lỗi kết nối: " + ex.Message, true);
                openServerButton.Enabled = true;
            }
        }

        private async Task ListenLoop()
        {
            var buffer = new byte[1024 * 1024 * 4];
            while (ws.State == WebSocketState.Open)
            {
                try {
                    var res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (res.MessageType == WebSocketMessageType.Close) break;

                    string cmd = Encoding.UTF8.GetString(buffer, 0, res.Count).Trim().Replace("\0", "");
                    
                    // Xử lý lệnh trên UI Thread
                    this.Invoke(new Action(() => {
                        UpdateLog("⬅️ Lệnh: " + cmd);
                        string reply = _featureLogic.ProcessCommand(cmd);
                        
                        if (!string.IsNullOrEmpty(reply)) {
                            byte[] b = Encoding.UTF8.GetBytes(reply);
                            ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Text, true, CancellationToken.None);
                            if (!reply.StartsWith("IMG_BASE64")) UpdateLog("➡️ Trả lời: " + reply);
                        }
                    }));
                } catch { break; }
            }
            UpdateLog("🔌 Mất kết nối.");
            this.Invoke(new Action(() => { openServerButton.Enabled = true; openServerButton.BackColor = Color.Red; }));
        }

        private void UpdateLog(string msg, bool isAdminLog = false) {
            if (logBox.InvokeRequired) logBox.Invoke(new Action(() => logBox.AppendText(msg + "\r\n")), isAdminLog.ToString());
            else logBox.AppendText(msg + "\r\n");
        }

        public static string GetLocalIPAddress() {
            try {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList) if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
            } catch {}
            return "127.0.0.1";
        }

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainButtonForm());
        }
    }
}