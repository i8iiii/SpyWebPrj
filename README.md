# SpyWebPrj

**SpyWebPrj** is a C# Windows Forms application designed for remote monitoring, screen recording, and real-time communication. It leverages powerful libraries like **FlashCap** and **SharpAvi** for high-performance video capture and encoding, along with WebSocket integration for efficient network communication.

## 🚀 Features

* **Screen Recording**:
    * Captures high-quality screen footage using `FlashCap`.
    * Encodes and saves video output in AVI format using `SharpAvi`.
* **Real-time Communication**:
    * Implements `System.Net.WebSockets` and `System.Net.Sockets` for low-latency client-server communication.
* **System Diagnostics**:
    * Monitors system processes and diagnostics using `System.Diagnostics`.
    * Interacts with the Windows Registry via `Microsoft.Win32`.
* **Low-Level System Interaction**:
    * Utilizes `System.Runtime.InteropServices` for accessing native Windows APIs.
* **Background Operation**:
    * Designed to run efficiently as a Windows Forms application.

## 🛠️ Technologies Used

* **Language**: C# (.NET)
* **Framework**: Windows Forms
* **Video Capture**: [FlashCap](https://github.com/joshua-ferrell/FlashCap)
* **Video Encoding**: [SharpAvi](https://github.com/baSSiLL/SharpAvi)
* **Networking**: Standard .NET WebSockets & Sockets

## 📋 Prerequisites

Before running the project, ensure you have the following installed:

* [Visual Studio 2022](https://visualstudio.microsoft.com/) (or compatible IDE)
* .NET Framework (compatible version, e.g., 4.7.2 or .NET 6/7/8 depending on your configuration): https://dotnet.microsoft.com/en-us/
* node.js lastest version: https://nodejs.org/en

## ⚙️ Installation

1.  **Clone the repository**
    ```bash
    git clone [https://github.com/i8iiii/SpyWebPrj.git](https://github.com/i8iiii/SpyWebPrj.git)
    ```

2.  **Open the Project**
    * IDE could be VS2022 or VSCode.
3.  **Requirements**
* *Ensure `FlashCap` and `SharpAvi` are correctly installed.*
```
dotnet add package SharpAvi

dotnet add package FlashCap
```
* *Ensure `ws` is installed.
```
npm install ws
```
## 🖥️ Running program
* You have to run 2 terminals

```with socket
cd socket
node socket.js
```

```with server
cd server
dotnet run
```

## ⚠️ Disclaimer

This software is for **educational purposes only**. The developer is not responsible for any misuse of this software. Ensure you have permission from the network owner and device owner before running monitoring software.
