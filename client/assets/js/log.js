
const ipInput = document.getElementById('ipInput');
const statusText = document.getElementById("statusText");
const logBox = document.getElementById("logBox");
const adminLogBox = document.getElementById("adminLogBox");

function log(msg, isAdmin = false) {
    const now = new Date();
    const time = now.getHours() + ":" + now.getMinutes() + ":" + now.getSeconds();
    const line = `<div><span style="color: #888;">[${time}]</span> ${msg}</div>`;
    
    // 1. Luôn ghi vào log trang chủ
    logBox.innerHTML += line;
    logBox.scrollTop = logBox.scrollHeight;

    // 2. Nếu là tin nhắn quan trọng hoặc từ Agent, ghi thêm vào log Admin
    if (isAdmin && adminLogBox) {
        adminLogBox.innerHTML += line;
        adminLogBox.scrollTop = adminLogBox.scrollHeight;
    }
}