// Hàm gửi lệnh sang Server -> Agent
function sendCommand(cmd) {
  if (socket && socket.readyState === WebSocket.OPEN) {
    socket.send(cmd);
    log("host@admin:~$ " + cmd, true); // Giả lập giao diện dòng lệnh
  } else {
    alert("Chưa kết nối tới Server! Vui lòng kiểm tra lại.");
  }
}


function getProcessCmd() {
  sendCommand("CMD_GET_PROCESS");
}

function getScreenshotCmd() {
  sendCommand("CMD_SCREENSHOT");
}

function getAppsCmd() {
  sendCommand("CMD_GET_APPS");
}

function getPingCmd() {
  sendCommand("CMD_PING");
}

function restartCmd() {
  if (confirm("CẢNH BÁO: Bạn có chắc muốn khởi động lại máy Agent?"))
    sendCommand("CMD_RESTART");
}

function shutdownCmd() {
  if (confirm("CẢNH BÁO: Bạn có chắc muốn TẮT NGUỒN máy Agent?"))
    sendCommand("CMD_SHUTDOWN");
}
