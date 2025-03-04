let ws;

function connect() {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/ws`;
    
    ws = new WebSocket(wsUrl);

    ws.onopen = () => {
        addLine('Connected to MCP Monitor');
    };

    ws.onclose = () => {
        addLine('Disconnected from MCP Monitor. Reconnecting...');
        setTimeout(connect, 5000);
    };

    ws.onerror = (error) => {
        addLine('WebSocket error: ' + error);
    };

    ws.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            addLine(data);
        } catch (e) {
            addLine(event.data);
        }
    };
}

// Export for use in other modules
window.wsConnect = connect; 