const terminal = document.getElementById('terminal');

// Format a timestamp from milliseconds or date object
function formatTime(timestamp) {
    // If it's a Date object, use it directly
    const date = timestamp instanceof Date ? timestamp : new Date(timestamp);
    return date.toTimeString().split(' ')[0] + '.' + 
           date.getMilliseconds().toString().padStart(3, '0');
}

function addLine(data) {
    const line = document.createElement('div');
    line.className = 'line';
    
    let typeClass = '';
    
    if (typeof data === 'string') {
        // For string messages, use current time as fallback
        const now = new Date();
        line.innerHTML = `<span class="timestamp">${formatTime(now)}</span><span class="message">${data}</span>`;
    } else {
        const type = data.Type || 'Unknown';
        const clientId = data.ClientId || '-';
        const message = data.Message || '';
        
        // Use the timestamp from the data if available, otherwise use current time
        const timestamp = data.Timestamp ? new Date(data.Timestamp) : new Date();
        
        let displayType = type;
        typeClass = type.toLowerCase();
        
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
            <span class="timestamp">${formatTime(timestamp)}</span>
            <span class="client-id">${clientId}</span>
            <span class="type ${typeClass}">${displayType}</span>
            <span class="message">${message}</span>
        `;
    }
    
    terminal.insertBefore(line, terminal.firstChild);

    // Apply type filters
    if (window.shouldShowLine && typeClass && !window.shouldShowLine(typeClass)) {
        line.classList.add('hidden');
    }
    
    // Apply client ID filter if active
    if (window.checkClientMatch && !window.checkClientMatch(line)) {
        line.style.display = 'none';
    }
    
    // If there's an active search, apply it to the new line
    const searchInput = document.getElementById('search-input');
    if (searchInput && searchInput.value.trim()) {
        const searchText = searchInput.value.trim();
        const matches = window.highlightMatches ? window.highlightMatches(line, searchText) : 0;
        if (matches === 0) {
            line.style.display = 'none';
        }
    }
}

// Export for use in other modules
window.addLine = addLine;