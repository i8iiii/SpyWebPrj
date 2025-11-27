// ================================================
// WebSocket Gateway (Node.js) - High Performance
// ================================================

const WebSocket = require("ws");

// Cấu hình giới hạn Max Payload (1GB) để tránh lỗi RangeError
const MAX_PAYLOAD = 1024 * 1024 * 1024 * 1024; 

// Tạo websocket server với cấu hình nâng cao
const wss = new WebSocket.Server({ 
    port: 8080,
    maxPayload: MAX_PAYLOAD 
});

console.log(`🔌 WebSocket Gateway running at ws://localhost:8080`);
console.log(`🚀 Max Payload Size: ${MAX_PAYLOAD / 1024 / 1024} MB`);

let clients = {
    web: null,
    agent: null
};

wss.on("connection", (ws, req) => {
    const ip = req.socket.remoteAddress;
    console.log(`🔗 New connection from: ${ip}`);

    ws.on("message", (raw) => {
        // Chuyển raw buffer sang string (Cẩn thận với file quá lớn có thể gây chậm ở bước này)
        // Với file video, ta không nên log toàn bộ nội dung ra console vì sẽ treo terminal
        const msg = raw.toString();
        
        // Log thông minh: Nếu tin nhắn quá dài (video/ảnh), chỉ log kích thước
        if (msg.length > 200) {
            const preview = msg.substring(0, 50) + "...";
            console.log(`📩 MSG (${(msg.length / 1024).toFixed(2)} KB): ${preview}`);
        } else {
            console.log("📩 MSG:", msg);
        }

        // ===== REGISTER WEB CLIENT =====
        if (msg === "REGISTER_WEB") {
            clients.web = ws;
            console.log("🌐 Web client registered.");
            ws.send("SERVER: Web connected");
            return;
        }

        // ===== REGISTER C++ AGENT =====
        if (msg === "REGISTER_AGENT") {
            clients.agent = ws;
            console.log("🖥️ Agent registered.");
            ws.send("SERVER: Agent connected");
            return;
        }

        // ===== WEB → AGENT =====
        if (ws === clients.web) {
            if (clients.agent) {
                // console.log("➡️ Forwarding WEB → AGENT"); // Uncomment nếu muốn debug kỹ
                clients.agent.send(msg);
            } else {
                ws.send("SERVER: Agent not connected");
            }
            return;
        }

        // ===== AGENT → WEB =====
        if (ws === clients.agent) {
            if (clients.web) {
                console.log("⬅️ Forwarding AGENT → WEB"); // Uncomment nếu muốn debug kỹ
                clients.web.send(msg);
            }
            return;
        }
    });

    ws.on("error", (err) => {
        console.error(`⚠️ Error on connection ${ip}:`, err.message);
    });

    ws.on("close", () => {
        if (ws === clients.web) {
            clients.web = null;
            console.log("❌ Web client disconnected");
        }
        if (ws === clients.agent) {
            clients.agent = null;
            console.log("❌ Agent disconnected");
        }
    });
});