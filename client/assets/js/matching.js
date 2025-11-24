let socket = null;

function connectToWebSocket(ip) {
  const wsUrl = `ws://${ip}:8080`;

  // Đóng kết nối cũ nếu có
  if (socket) {
    socket.close();
  }

  log(`Đang khởi tạo kết nối tới: <b>${wsUrl}</b>...`);

  try {
    socket = new WebSocket(wsUrl);

    // 1. KHI KẾT NỐI THÀNH CÔNG
    socket.onopen = () => {
      statusText.innerText = "Đã kết nối: " + ip;
      statusText.style.color = "green";
      log("✅ Socket Connected Successfully!");

      // GỬI TÍN HIỆU ĐĂNG KÝ WEB CLIENT
      // (Server Node.js cần biết đây là Web Dashboard chứ không phải Agent)
      socket.send("REGISTER_WEB");
      log("📤 Sent identification: REGISTER_WEB");

      // Mở khóa nút Next
      nextBtn.disabled = false;
      nextBtn.style.opacity = "1";
      nextBtn.style.cursor = "pointer";
    };

    // 2. KHI NHẬN TIN NHẮN TỪ SERVER
    socket.onmessage = (event) => {
      const data = event.data;
      // 2.1. Nếu là ảnh Screenshot (Giả sử C# gửi về bắt đầu bằng "IMG_BASE64:")
      if (data.startsWith("IMG_BASE64:")) {
        log("📸 Đã nhận được ảnh chụp màn hình!", true);
        // Tạo popup hoặc chèn ảnh vào div để xem
        const base64Image = data.replace("IMG_BASE64:", "");
        // Hiển thị ảnh (Code ví dụ)
        const imgWindow = window.open("");
        imgWindow.document.write(
          `<img src="data:image/png;base64,${base64Image}" />`
        );
      }
      // 2.2. Nếu là danh sách Process (Giả sử dữ liệu dạng JSON String)
      else if (data.startsWith("LIST_PROC:")) {
        const content = data.replace("LIST_PROC:", "");
        log("📄 Danh sách tiến trình: <br>" + content, true);
      }
      // 2.3. Tin nhắn thường
      else {
        log("📥 Phản hồi từ Agent: " + data, true);
      }
    };

    // 3. KHI MẤT KẾT NỐI
    socket.onclose = (event) => {
      statusText.innerText = "Mất kết nối";
      statusText.style.color = "red";
      log(`❌ Socket Disconnected (Code: ${event.code})`);

      // Khóa nút Next
      nextBtn.disabled = true;
      nextBtn.style.opacity = "0.5";
      nextBtn.style.cursor = "not-allowed";
    };

    // 4. KHI CÓ LỖI
    socket.onerror = (error) => {
      log("⚠️ Socket Error. Kiểm tra lại IP hoặc Server.");
      console.error("WebSocket Error:", error);
    };
  } catch (e) {
    log("Lỗi khởi tạo: " + e.message);
  }
}

function connectButtonClicked() {
  const ip = ipInput.value.trim();

  if (!ip) {
    alert("Vui lòng nhập địa chỉ IP!");
    return;
  }

  statusText.innerText = "Đang kết nối...";
  statusText.style.color = "orange";

  // Gọi hàm kết nối thật
  connectToWebSocket(ip);
}
