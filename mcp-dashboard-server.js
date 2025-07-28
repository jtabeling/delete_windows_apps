const express = require('express');
const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

const app = express();
const port = 3001;

// Middleware
app.use(express.json());
app.use(express.static('public'));

// MCP Server Registry
const mcpServers = [
    { name: 'filesystem', file: 'filesystem-mcp-server.js', status: 'unknown', description: 'File system operations' },
    { name: 'git', file: 'git-mcp-server.js', status: 'unknown', description: 'Git repository management' },
    { name: 'docker', file: 'docker-mcp-server.js', status: 'unknown', description: 'Docker container management' },
    { name: 'system', file: 'system-mcp-server.js', status: 'unknown', description: 'System information and operations' },
    { name: 'memory', file: 'memory-mcp-server.js', status: 'unknown', description: 'Memory and cache management' },
    { name: 'docker-v2', file: 'docker-mcp-server-v2.js', status: 'unknown', description: 'Enhanced Docker operations' },
    { name: 'docker-cli', file: 'docker-cli-server.js', status: 'unknown', description: 'Docker CLI interface' },
    { name: 'registry', file: 'mcp-server-registry.js', status: 'unknown', description: 'MCP server registry' }
];

// Store running processes
const runningProcesses = new Map();

// Check if a server file exists
function checkServerExists(serverFile) {
    return fs.existsSync(path.join(__dirname, serverFile));
}

// Start an MCP server
function startServer(serverName) {
    const server = mcpServers.find(s => s.name === serverName);
    if (!server) {
        return { success: false, error: 'Server not found' };
    }

    if (!checkServerExists(server.file)) {
        return { success: false, error: `Server file ${server.file} not found` };
    }

    if (runningProcesses.has(serverName)) {
        return { success: false, error: 'Server already running' };
    }

    try {
        const process = spawn('node', [server.file], {
            stdio: ['pipe', 'pipe', 'pipe'],
            cwd: __dirname
        });

        runningProcesses.set(serverName, process);
        server.status = 'running';

        process.on('close', (code) => {
            console.log(`Server ${serverName} stopped with code ${code}`);
            runningProcesses.delete(serverName);
            server.status = 'stopped';
        });

        process.on('error', (error) => {
            console.error(`Error starting server ${serverName}:`, error);
            runningProcesses.delete(serverName);
            server.status = 'error';
        });

        return { success: true, message: `Server ${serverName} started successfully` };
    } catch (error) {
        return { success: false, error: error.message };
    }
}

// Stop an MCP server
function stopServer(serverName) {
    const process = runningProcesses.get(serverName);
    if (!process) {
        return { success: false, error: 'Server not running' };
    }

    try {
        process.kill('SIGTERM');
        runningProcesses.delete(serverName);
        const server = mcpServers.find(s => s.name === serverName);
        if (server) server.status = 'stopped';
        return { success: true, message: `Server ${serverName} stopped successfully` };
    } catch (error) {
        return { success: false, error: error.message };
    }
}

// API Routes
app.get('/api/servers', (req, res) => {
    // Update status for all servers
    mcpServers.forEach(server => {
        if (runningProcesses.has(server.name)) {
            server.status = 'running';
        } else if (server.status === 'running') {
            server.status = 'stopped';
        }
    });

    res.json({
        servers: mcpServers,
        runningCount: runningProcesses.size,
        totalCount: mcpServers.length
    });
});

app.post('/api/servers/:serverName/start', (req, res) => {
    const { serverName } = req.params;
    const result = startServer(serverName);
    res.json(result);
});

app.post('/api/servers/:serverName/stop', (req, res) => {
    const { serverName } = req.params;
    const result = stopServer(serverName);
    res.json(result);
});

app.get('/api/status', (req, res) => {
    res.json({
        dashboard: 'running',
        port: port,
        uptime: process.uptime(),
        memory: process.memoryUsage(),
        runningServers: Array.from(runningProcesses.keys())
    });
});

// Serve the dashboard HTML
app.get('/', (req, res) => {
    const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MCP Server Dashboard</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 15px;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
            overflow: hidden;
        }
        .header {
            background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }
        .header h1 {
            font-size: 2.5em;
            margin-bottom: 10px;
        }
        .header p {
            font-size: 1.2em;
            opacity: 0.9;
        }
        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            padding: 30px;
            background: #f8f9fa;
        }
        .stat-card {
            background: white;
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }
        .stat-number {
            font-size: 2em;
            font-weight: bold;
            color: #4facfe;
        }
        .stat-label {
            color: #666;
            margin-top: 5px;
        }
        .servers {
            padding: 30px;
        }
        .server-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
            gap: 20px;
        }
        .server-card {
            background: white;
            border: 2px solid #e9ecef;
            border-radius: 10px;
            padding: 20px;
            transition: all 0.3s ease;
        }
        .server-card:hover {
            border-color: #4facfe;
            box-shadow: 0 10px 25px rgba(0,0,0,0.1);
        }
        .server-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 15px;
        }
        .server-name {
            font-size: 1.2em;
            font-weight: bold;
            color: #333;
        }
        .status-badge {
            padding: 5px 12px;
            border-radius: 20px;
            font-size: 0.8em;
            font-weight: bold;
            text-transform: uppercase;
        }
        .status-running { background: #d4edda; color: #155724; }
        .status-stopped { background: #f8d7da; color: #721c24; }
        .status-error { background: #fff3cd; color: #856404; }
        .status-unknown { background: #e2e3e5; color: #383d41; }
        .server-description {
            color: #666;
            margin-bottom: 15px;
            line-height: 1.4;
        }
        .server-actions {
            display: flex;
            gap: 10px;
        }
        .btn {
            padding: 8px 16px;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-weight: bold;
            transition: all 0.3s ease;
            flex: 1;
        }
        .btn-start {
            background: #28a745;
            color: white;
        }
        .btn-start:hover { background: #218838; }
        .btn-stop {
            background: #dc3545;
            color: white;
        }
        .btn-stop:hover { background: #c82333; }
        .btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }
        .refresh-btn {
            background: #17a2b8;
            color: white;
            padding: 10px 20px;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-weight: bold;
            margin-bottom: 20px;
        }
        .refresh-btn:hover { background: #138496; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🚀 MCP Server Dashboard</h1>
            <p>Manage and monitor your MCP servers</p>
        </div>
        
        <div class="stats">
            <div class="stat-card">
                <div class="stat-number" id="runningCount">0</div>
                <div class="stat-label">Running Servers</div>
            </div>
            <div class="stat-card">
                <div class="stat-number" id="totalCount">0</div>
                <div class="stat-label">Total Servers</div>
            </div>
            <div class="stat-card">
                <div class="stat-number" id="uptime">0s</div>
                <div class="stat-label">Dashboard Uptime</div>
            </div>
        </div>
        
        <div class="servers">
            <button class="refresh-btn" onclick="refreshServers()">🔄 Refresh Status</button>
            <div class="server-grid" id="serverGrid">
                <!-- Servers will be populated here -->
            </div>
        </div>
    </div>

    <script>
        let servers = [];
        
        async function loadServers() {
            try {
                const response = await fetch('/api/servers');
                const data = await response.json();
                servers = data.servers;
                updateStats(data);
                renderServers();
            } catch (error) {
                console.error('Error loading servers:', error);
            }
        }
        
        function updateStats(data) {
            document.getElementById('runningCount').textContent = data.runningCount;
            document.getElementById('totalCount').textContent = data.totalCount;
        }
        
        function renderServers() {
            const grid = document.getElementById('serverGrid');
            grid.innerHTML = servers.map(server => \`
                <div class="server-card">
                    <div class="server-header">
                        <div class="server-name">\${server.name}</div>
                        <div class="status-badge status-\${server.status}">\${server.status}</div>
                    </div>
                    <div class="server-description">\${server.description}</div>
                    <div class="server-actions">
                        <button class="btn btn-start" 
                                onclick="startServer('\${server.name}')"
                                \${server.status === 'running' ? 'disabled' : ''}>
                            ▶️ Start
                        </button>
                        <button class="btn btn-stop" 
                                onclick="stopServer('\${server.name}')"
                                \${server.status !== 'running' ? 'disabled' : ''}>
                            ⏹️ Stop
                        </button>
                    </div>
                </div>
            \`).join('');
        }
        
        async function startServer(serverName) {
            try {
                const response = await fetch(\`/api/servers/\${serverName}/start\`, {
                    method: 'POST'
                });
                const result = await response.json();
                if (result.success) {
                    loadServers();
                } else {
                    alert('Error: ' + result.error);
                }
            } catch (error) {
                console.error('Error starting server:', error);
                alert('Error starting server');
            }
        }
        
        async function stopServer(serverName) {
            try {
                const response = await fetch(\`/api/servers/\${serverName}/stop\`, {
                    method: 'POST'
                });
                const result = await response.json();
                if (result.success) {
                    loadServers();
                } else {
                    alert('Error: ' + result.error);
                }
            } catch (error) {
                console.error('Error stopping server:', error);
                alert('Error stopping server');
            }
        }
        
        function refreshServers() {
            loadServers();
        }
        
        // Load servers on page load
        loadServers();
        
        // Auto-refresh every 5 seconds
        setInterval(loadServers, 5000);
    </script>
</body>
</html>
    `;
    res.send(html);
});

// Start the server
app.listen(port, () => {
    console.log(`🚀 MCP Dashboard running at http://localhost:${port}`);
    console.log(`📊 Monitor and manage all your MCP servers`);
    console.log(`🔗 Web Interface: http://localhost:3000`);
    console.log(`📈 Dashboard: http://localhost:${port}`);
});

// Graceful shutdown
process.on('SIGINT', () => {
    console.log('\n🛑 Shutting down MCP Dashboard...');
    runningProcesses.forEach((process, name) => {
        console.log(`Stopping ${name}...`);
        process.kill('SIGTERM');
    });
    process.exit(0);
}); 