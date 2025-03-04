const terminal = document.getElementById('terminal');

function formatTime(date) {
    return date.toTimeString().split(' ')[0] + '.' + 
           date.getMilliseconds().toString().padStart(3, '0');
}

function addLine(data) {
    const now = new Date();
    const line = document.createElement('div');
    line.className = 'line';
    
    if (typeof data === 'string') {
        line.innerHTML = `<span class="timestamp">${formatTime(now)}</span><span class="message">${data}</span>`;
    } else {
        const type = data.Type || 'Unknown';
        const clientId = data.ClientId || '-';
        const message = data.Message || '';
        
        let displayType = type;
        let typeClass = type.toLowerCase();
        
        switch(type) {
            case 'CreateClient':
                displayType = 'CREATE';
                typeClass = 'create';
                break;
            case 'ListOfferings':
                displayType = 'LIST';
                typeClass = 'list';
                break;
            case 'MCPError':
                displayType = 'ERROR';
                typeClass = 'error';
                break;
            case 'ClientClosed':
                displayType = 'CLOSE';
                typeClass = 'close';
                break;
            case 'Connected':
                displayType = 'CONNECT';
                typeClass = 'connect';
                break;
            case 'GenericError':
                displayType = 'ERROR';
                typeClass = 'error';
                break;
            case 'NoServerInfo':
                displayType = 'WARNING';
                typeClass = 'warning';
                break;
            case 'UnrecognizedKeys':
                displayType = 'WARNING';
                typeClass = 'warning';
                break;
        }

        line.innerHTML = `
            <span class="timestamp">${formatTime(now)}</span>
            <span class="client-id">${clientId}</span>
            <span class="type ${typeClass}">${displayType}</span>
            <span class="message">${message}</span>
        `;
    }
    
    terminal.insertBefore(line, terminal.firstChild);
}

// Export for use in other modules
window.addLine = addLine; 