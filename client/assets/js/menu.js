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

function getKeyLog() {
  const command = document.getElementById("keylog-cmd");
  if (command.dataset.state.trim().toLowerCase() === "off") {
    // Keylog is OFF → ask to turn ON
    sendCommand("PLACE HOLDER 1");
    command.dataset.state = "on";
    command.textContent = "Stop"; // show "Stop" because keylog is now ON
  } else {
    sendCommand("PLACE HOLDER 2");
    command.dataset.state = "off";
    command.textContent = "Start"; // show "Start" because keylog is now OFF
  }
}

function toggleWebcam() {
  const command = document.getElementById("webcam-cmd");

  // Webcam is ON when the text says "Off"
  const webcamIsOn = command.dataset.state.trim().toLowerCase() === "on";

  if (!webcamIsOn) {
    // Webcam is OFF → ask to turn ON
    if (confirm("CẢNH BÁO: Bạn có muốn mở camera?")) {
      sendCommand("CMD_CAM_ON");
      command.dataset.state = "on";
      command.textContent = "Off"; // show "Off" because webcam is now ON
    }
  } else {
    // Webcam is ON → turn OFF
    sendCommand("CMD_CAM_OFF");
    command.dataset.state = "off";
    command.textContent = "On"; // show "On" because webcam is now OFF
  }
}

function restartCmd() {
  if (confirm("CẢNH BÁO: Bạn có chắc muốn khởi động lại máy Agent?"))
    sendCommand("CMD_RESTART");
}

function shutdownCmd() {
  if (confirm("CẢNH BÁO: Bạn có chắc muốn TẮT NGUỒN máy Agent?"))
    sendCommand("CMD_SHUTDOWN");
}
