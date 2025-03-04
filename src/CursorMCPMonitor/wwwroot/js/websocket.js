let ws;
let eventCount = 0;
let lastEventCount = 0;
let activeClients = new Set(); // Track unique client IDs
const wsStatus = document.getElementById('ws-status');
const clientCount = document.getElementById('client-count');
const eventRate = document.getElementById('event-rate');

// Update event rate every second
setInterval(() => {
    eventRate.textContent = eventCount - lastEventCount;
    lastEventCount = eventCount;
}, 1000);

function updateClientCount(data) {
    if (data.Type === 'CreateClient' && data.ClientId) {
        activeClients.add(data.ClientId);
        clientCount.textContent = activeClients.size;
    } else if (data.Type === 'ClientClosed' && data.ClientId) {
        activeClients.delete(data.ClientId);
        clientCount.textContent = activeClients.size;
    }
}

function connect() {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/ws`;
    
    ws = new WebSocket(wsUrl);

    ws.onopen = () => {
        wsStatus.classList.add('connected');
        addLine('Connected to MCP Monitor');
    };

    ws.onclose = () => {
        wsStatus.classList.remove('connected');
        addLine('Disconnected from MCP Monitor. Reconnecting...');
        setTimeout(connect, 5000);
    };

    ws.onerror = (error) => {
        wsStatus.classList.remove('connected');
        addLine('WebSocket error: ' + error);
    };

    ws.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            updateClientCount(data);
            addLine(data);
            eventCount++;
        } catch (e) {
            addLine(event.data);
            eventCount++;
        }
    };
}

// Export for use in other modules
window.wsConnect = connect; 