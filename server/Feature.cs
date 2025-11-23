using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WindowsAgent
{
    // Class này bây giờ chỉ chứa các hàm chức năng (Logic)
    // Không chứa code mạng (Network) nữa
    public class Feature
    {
        // Hàm xử lý lệnh và trả về kết quả string
        public string ProcessCommand(string cmd)
        {
            switch (cmd)
            {
                case "CMD_GET_PROCESS":
                    return "LIST_PROC:" + GetProcessList();

                case "CMD_SCREENSHOT":
                    return "IMG_BASE64:" + TakeScreenshot();

                case "CMD_GET_APPS":
                    return "LOG: Tính năng lấy danh sách App đang phát triển...";

                case "CMD_PING":
                    return "PONG: Agent vẫn hoạt động!";

                case "CMD_RESTART":
                    Application.Restart();
                    Environment.Exit(0);
                    return "LOG: Restarting..."; // Dòng này có thể không chạy tới kịp

                case "CMD_SHUTDOWN":
                     // Process.Start("shutdown", "/s /t 0"); 
                    return "LOG: Đã nhận lệnh tắt máy.";

                default:
                    return "LOG: Không hiểu lệnh " + cmd;
            }
        }

        private string GetProcessList()
        {
            StringBuilder sb = new StringBuilder();
            try {
                Process[] processList = Process.GetProcesses();
                foreach (Process p in processList)
                {
                    sb.Append(p.ProcessName + " (ID: " + p.Id + ")\n");
                }
            } catch (Exception e) { return "Error: " + e.Message; }
            return sb.ToString();
        }

        private string TakeScreenshot()
        {
            try
            {
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat.Png);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }
    }
}