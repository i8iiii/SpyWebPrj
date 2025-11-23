// ================================================
// WebSocket Gateway (Node.js)
// ================================================
// Nhiệm vụ của Gateway:
// - Nhận kết nối từ Web Client (index.html)
// - Nhận kết nối từ C++ Agent (server.exe)
// - Chuyển tiếp lệnh qua lại
// - Không xử lý logic bên trong
// ================================================

const WebSocket = require("ws");

// Tạo websocket server
const wss = new WebSocket.Server({ port: 8080 });
console.log("🔌 WebSocket Gateway running at ws://localhost:8080");

let clients = {
    web: null,
    agent: null
};

wss.on("connection", (ws, req) => {
    console.log("🔗 New connection:", req.socket.remoteAddress);

    ws.on("message", (raw) => {
        const msg = raw.toString();
        console.log("📩 MSG:", msg);

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
                console.log("➡️ Forwarding WEB → AGENT:", msg);
                clients.agent.send(msg);
            } else {
                ws.send("SERVER: Agent not connected");
            }
            return;
        }

        // ===== AGENT → WEB =====
        if (ws === clients.agent) {
            if (clients.web) {
                console.log("⬅️ Forwarding AGENT → WEB:", msg);
                clients.web.send(msg);
            }
            return;
        }
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
