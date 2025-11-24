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
            // --- 2. HIỂN THỊ IP MÁY HIỆN TẠI ---
            // --- 2. HIỂN THỊ IP MÁY HIỆN TẠI ---
            string myIP = GetLocalIPAddress();

            // Thay Label bằng TextBox
            TextBox ipBox = new TextBox();
            ipBox.Text = myIP; // Chỉ hiện IP cho dễ copy
            ipBox.Font = new Font("Consolas", 12, FontStyle.Bold);
            ipBox.ForeColor = Color.DarkRed;
            ipBox.BackColor = this.BackColor; // Màu nền trùng màu Form
            ipBox.BorderStyle = BorderStyle.None; // Bỏ viền khung
            ipBox.ReadOnly = true; // Chỉ đọc, không cho sửa
            ipBox.TextAlign = HorizontalAlignment.Center; // Căn giữa chữ
            ipBox.Width = 250; // Cần set chiều rộng vì TextBox không có AutoSize
            ipBox.Top = 15;
            ipBox.Left = (this.ClientSize.Width - ipBox.Width) / 2;

            // Mẹo: Khi click vào thì tự động chọn hết toàn bộ text để copy cho nhanh
            ipBox.Click += (s, e) => ipBox.SelectAll();

            this.Controls.Add(ipBox);

            // Nếu muốn vẫn giữ dòng chữ "My IP Address:" thì tạo thêm 1 Label nhỏ bên cạnh hoặc nối chuỗi vào TextBox tùy bạn.

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
        // --- Thay thế toàn bộ hàm ListenLoop cũ bằng hàm này ---
        private async Task ListenLoop()
        {
            // Tăng buffer lên 4MB cho chắc chắn
            var buffer = new byte[1024 * 1024 * 4];

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

                    // 1. Lọc sạch ký tự Null và khoảng trắng
                    string receivedCmd = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim().Replace("\0", "");

                    // UpdateLog("⬅️ Server: " + receivedCmd); // (Tùy chọn: bỏ comment để debug)

                    // 2. QUAN TRỌNG: Chạy xử lý lệnh trên UI Thread (Main Thread)
                    // Để hàm chụp màn hình (TakeScreenshot) có thể truy cập được Graphics màn hình
                    string responseData = "";

                    this.Invoke(new Action(() =>
                    {
                        // Gọi Logic xử lý
                        responseData = _featureLogic.ProcessCommand(receivedCmd);
                    }));

                    // 3. Gửi phản hồi (nếu có)
                    if (!string.IsNullOrEmpty(responseData))
                    {
                        byte[] sendBytes = Encoding.UTF8.GetBytes(responseData);
                        await ws.SendAsync(new ArraySegment<byte>(sendBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                        // Chỉ log nếu không phải là ảnh (để đỡ spam log)
                        if (!responseData.StartsWith("IMG_BASE64"))
                        {
                            UpdateLog("➡️ Đã gửi trả lời: " + responseData);
                        }
                        else
                        {
                            UpdateLog("📸 Đã gửi dữ liệu ảnh Screenshot.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateLog("❌ Lỗi luồng lắng nghe: " + ex.Message);
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
