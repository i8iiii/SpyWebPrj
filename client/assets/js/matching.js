// --- Phần 1: Logic cho LAN MATCH (matchingPage) ---

const statusText = document.getElementById("statusText");
const logBox = document.getElementById("logBox");

function log(msg) {
    if (logBox) {
        logBox.innerHTML += `<div>${msg}</div>`;
        logBox.scrollTop = logBox.scrollHeight;
    }
}

// Giả lập kết nối (bạn có thể thay bằng logic thật)
document.getElementById("hostBtn").addEventListener("click", () => {
    statusText.textContent = "Hosting...";
    log("Waiting for a player to join...");
    
    setTimeout(() => {
        log("Player joined successfully!");
        onConnectionSuccessful(); 
    }, 1000); // Giảm thời gian chờ
});

document.getElementById("joinBtn").addEventListener("click", () => {
    statusText.textContent = "Searching...";
    log("Looking for host...");
    
    setTimeout(() => {
        log("Connected to host!");
        onConnectionSuccessful(); 
    }, 1000); // Giảm thời gian chờ
});

function onConnectionSuccessful() {
    statusText.textContent = 'Connected! Ready to proceed.';
    const nextBtn = document.getElementById('nextBtn'); // Lấy nút Next
    if(nextBtn) {
        nextBtn.disabled = false;
        nextBtn.style.opacity = '1.0';
        nextBtn.style.pointerEvents = 'auto';
    }
}


// --- Phần 2: Logic Điều hướng (Navigation) ---

const matchingPage = document.getElementById('matchingPage');
const menuPage = document.getElementById('menuPage');
const nextBtn = document.getElementById('nextBtn');
const backBtn = document.getElementById('backBtn');

function showMenuPage() {
    if (matchingPage) matchingPage.style.display = 'none';
    if (menuPage) menuPage.style.display = 'flex'; // Dùng 'flex' để căn giữa
}

function showMatchingPage() {
    if (matchingPage) matchingPage.style.display = 'flex'; // Dùng 'flex' để căn giữa
    if (menuPage) menuPage.style.display = 'none';
}

if (nextBtn) nextBtn.addEventListener('click', showMenuPage);
if (backBtn) backBtn.addEventListener('click', showMatchingPage);


// --- Phần 3: Logic ADMIN DASHBOARD (menuPage) ---
// Đây là phần mới, liên kết với app.py

const adminLogBox = document.getElementById('adminLogBox');

function adminLog(message, type = 'normal') {
    if (!adminLogBox) return; // Kiểm tra nếu log box tồn tại
    const pre = document.createElement('pre');
    pre.textContent = `> ${message}`;
    if (type === 'error') {
        pre.style.color = '#f87171'; // Red
    } else if (type === 'success') {
        pre.style.color = '#4ade80'; // Green
    } else if (type === 'info') {
        pre.style.color = '#38bdf8'; // Blue
    }
    adminLogBox.appendChild(pre);
    adminLogBox.scrollTop = adminLogBox.scrollHeight;
}

// Gắn sự kiện cho các nút admin
const btnListProcess = document.getElementById('btnListProcess');
if (btnListProcess) {
    btnListProcess.addEventListener('click', async () => {
        adminLog("Dang lay danh sach tien trinh...", 'info');
        try {
            const response = await fetch('/api/list_processes');
            const data = await response.json();

            if (data.success) {
                adminLog("Lay danh sach tien trinh thanh cong!", 'success');
                // Xóa log cũ, chỉ giữ dòng đầu
                adminLogBox.innerHTML = '<pre>> Dang cho lenh...</pre>'; 
                
                // Tạo bảng
                const table = document.createElement('table');
                table.style.width = '100%';
                table.innerHTML = `
                    <thead>
                        <tr>
                            <th style="text-align: left; padding: 2px;">PID</th>
                            <th style="text-align: left; padding: 2px;">Name</th>
                        </tr>
                    </thead>
                `;
                const tbody = document.createElement('tbody');
                if (data.processes) {
                    for (const proc of data.processes) {
                        const row = document.createElement('tr');
                        row.innerHTML = `
                            <td style="padding: 2px;">${proc.pid}</td>
                            <td style="padding: 2px;">${proc.name}</td>
                        `;
                        tbody.appendChild(row);
                    }
                }
                table.appendChild(tbody);
                adminLogBox.appendChild(table);

            } else {
                adminLog(`LOI: ${data.error}`, 'error');
            }
        } catch (err) {
            adminLog(`LOI KET NOI: ${err.message}`, 'error');
        }
    });
}

const btnRestart = document.getElementById('btnRestart');
if (btnRestart) {
    btnRestart.addEventListener('click', async () => {
        adminLog("Dang gui lenh RESTART...", 'info');
        try {
            const response = await fetch('/api/restart');
            const data = await response.json();
            if (data.success) {
                adminLog("Server phan hoi: " + data.message, 'success');
            } else {
                adminLog(`LOI: ${data.error}`, 'error');
            }
        } catch (err) {
            adminLog(`LOI KET NOI: ${err.message}`, 'error');
        }
    });
}

const btnShutdown = document.getElementById('btnShutdown');
if (btnShutdown) {
    btnShutdown.addEventListener('click', async () => {
        adminLog("Dang gui lenh SHUTDOWN...", 'info');
        try {
            const response = await fetch('/api/shutdown');
            const data = await response.json();
            if (data.success) {
                adminLog("Server phan hoi: " + data.message, 'success');
            } else {
                adminLog(`LOI: ${data.error}`, 'error');
            }
        } catch (err) {
            adminLog(`LOI KET NOI: ${err.message}`, 'error');
        }
    });
}

// Thêm sự kiện cho các nút chưa có chức năng
const btnScreenshot = document.getElementById('btnScreenshot');
if (btnScreenshot) {
    btnScreenshot.addEventListener('click', () => {
        adminLog("Chuc nang 'Take Screenshot' chua duoc cai dat.", 'info');
    });
}

const btnManageApp = document.getElementById('btnManageApp');
if (btnManageApp) {
    btnManageApp.addEventListener('click', () => {
        adminLog("Chuc nang 'Quan ly Ung dung' chua duoc cai dat.", 'info');
    });
}

const btnSendCommand = document.getElementById('btnSendCommand');
if (btnSendCommand) {
    btnSendCommand.addEventListener('click', () => {
        adminLog("Chuc nang 'Gui Lenh' chua duoc cai dat.", 'info');
    });
}