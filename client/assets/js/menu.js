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




function toggleListAppPanel() {
  const panel = document.querySelector(".list-app-panel");
  const mainPanel = document.querySelector(".button-panel");
  if (panel.style.display === "none" || panel.style.display === "") {
    panel.style.display = "flex";
    mainPanel.style.display = "none";
  } else {
    panel.style.display = "none";
    mainPanel.style.display = "block";
  }
}

function toggleListProcessPanel() {
  const mainPanel = document.querySelector(".button-panel");
  const panel = document.querySelector(".list-process-panel");
  if (panel.style.display === "none" || panel.style.display === "") {
    panel.style.display = "flex";
    mainPanel.style.display = "none";
  } else {
    panel.style.display = "none";
    mainPanel.style.display = "block";
  }
}

function getKeyLog() {
  const command = document.getElementById("keylog-cmd");
  if (command.dataset.state.trim().toLowerCase() === "off") {
    // Keylog is OFF → ask to turn ON
    // TODO: START KEY LOGGING
    sendCommand("PLACE HOLDER 1");
    command.dataset.state = "on";
    command.textContent = "Stop"; 
  } else {
    // TODO: STOP KEY LOGGING
    sendCommand("PLACE HOLDER 2");
    command.dataset.state = "off";
    command.textContent = "Start";
  }
}

// TODO:
// CMD_CAM_ON_<<DURATION>>
// CMD_CAM_OFF
function toggleWebcam() {
  const command = document.getElementById("webcam-cmd");
  const durationInput = document.getElementById("webcam-duration");
  const duration = parseInt(durationInput.value, 10);

  // Webcam is ON when the text says "Off"
  const webcamIsOn = command.dataset.state.trim().toLowerCase() === "on";

  if (!webcamIsOn) {
    // Webcam is OFF → ask to turn ON
    if (confirm("CẢNH BÁO: Bạn có muốn mở camera?")) {
      sendCommand(`CMD_CAM_ON_${isNaN(duration) ? 10 : duration}`);
      // IF DURATION IS 10 (seconds) CMD = CMD_CAM_ON_10
      command.dataset.state = "on";
      command.textContent = "Off"; // show "Off" because webcam is now ON
      durationInput.disabled = true;
    }
  } else {
    // Webcam is ON → turn OFF
    sendCommand("CMD_CAM_OFF");
    durationInput.value = "";
    command.dataset.state = "off";
    command.textContent = "On"; // show "On" because webcam is now OFF
    durationInput.disabled = false;
  }
}

function killProcess() {

}

function startApp() {

}

function closeApp() {
  
}
