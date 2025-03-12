const terminal = document.getElementById('terminal');

// Format a timestamp from milliseconds or date object
function formatTime(timestamp) {
    // If it's a Date object, use it directly
    const date = timestamp instanceof Date ? timestamp : new Date(timestamp);
    
    // Format the date part (YYYY-MM-DD)
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    
    // Format the time part (HH:MM:SS.mmm)
    const time = date.toTimeString().split(' ')[0] + '.' + 
                 date.getMilliseconds().toString().padStart(3, '0');
    
    return `${year}-${month}-${day} ${time}`;
}

// Parse timestamp string into Date object
function parseTimestampString(timestampStr) {
    if (!timestampStr || typeof timestampStr !== 'string') return null;
    
    try {
        // Expected format: YYYY-MM-DD HH:MM:SS.SSS
        const [datePart, timePart] = timestampStr.split(' ');
        if (!datePart || !timePart) return null;
        
        const [year, month, day] = datePart.split('-').map(Number);
        let [hours, minutes, secondsWithMs] = timePart.split(':');
        
        // Handle milliseconds part
        let seconds = 0, milliseconds = 0;
        if (secondsWithMs && secondsWithMs.includes('.')) {
            const [sec, ms] = secondsWithMs.split('.');
            seconds = parseInt(sec, 10);
            milliseconds = parseInt(ms, 10);
        } else if (secondsWithMs) {
            seconds = parseInt(secondsWithMs, 10);
        }
        
        // JavaScript months are 0-indexed
        return new Date(year, month - 1, day, hours, minutes, seconds, milliseconds);
    } catch (e) {
        console.error('Error parsing timestamp string:', e);
        return null;
    }
}

// Central function to manage visibility based on all active filters
function updateVisibility() {
    const lines = document.querySelectorAll('#terminal .line');
    console.log('Running updateVisibility on', lines.length, 'lines');
    
    lines.forEach(line => {
        // Reset display to default first
        let shouldHide = false;
        
        // Check event type filter first
        if (line.classList.contains('hidden')) {
            shouldHide = true;
        }
        
        // Check time filter
        if (!shouldHide && line.classList.contains('time-filtered')) {
            shouldHide = true;
        }
        
        // Check client ID filter - either no filter or must match
        if (!shouldHide && line.getAttribute('data-client-match') === 'false') {
            shouldHide = true;
        }
        
        // Check search filter
        if (!shouldHide) {
            const searchInput = document.getElementById('search-input');
            if (searchInput && searchInput.value.trim()) {
                const hasMatches = line.querySelector('.search-match');
                if (!hasMatches) {
                    shouldHide = true;
                }
            }
        }
        
        // Apply final visibility
        line.style.display = shouldHide ? 'none' : '';
    });
}

// Insert a line into the terminal in descending chronological order (newest first)
function insertChronologically(newLine, eventTime) {
    // Get all existing lines
    const lines = terminal.querySelectorAll('.line');
    
    // If no lines, simply append
    if (lines.length === 0) {
        terminal.appendChild(newLine);
        return;
    }
    
    // Find the right position for this new entry (chronological - newest first)
    let inserted = false;
    for (const line of lines) {
        const timestampEl = line.querySelector('.timestamp');
        if (!timestampEl) continue;
        
        const existingTimestampStr = timestampEl.textContent;
        const existingTime = parseTimestampString(existingTimestampStr);
        
        if (!existingTime) continue;
        
        // If the new entry is newer (greater) than the current line
        // Insert it before this line
        if (eventTime > existingTime) {
            terminal.insertBefore(newLine, line);
            inserted = true;
            break;
        }
    }
    
    // If we haven't found a place to insert it, add it to the end
    if (!inserted) {
        terminal.appendChild(newLine);
    }
}

function addLine(data) {
    const line = document.createElement('div');
    line.className = 'line';
    
    let typeClass = '';
    let eventTime = new Date(); // Default time
    
    if (typeof data === 'string') {
        // For string messages, use current time as fallback
        line.innerHTML = `<span class="timestamp">${formatTime(eventTime)}</span><span class="message">${data}</span>`;
    } else {
        const type = data.Type || 'Unknown';
        const clientId = data.ClientId || '-';
        const message = data.Message || '';
        
        // Get the actual timestamp from the event
        // The server sends Timestamp as ISO string (or milliseconds since epoch)
        if (data.Timestamp) {
            // Try to parse the timestamp from the data
            try {
                // If it's a number (milliseconds), convert directly
                if (typeof data.Timestamp === 'number') {
                    eventTime = new Date(data.Timestamp);
                } 
                // If it's a string in ISO format, parse it
                else if (typeof data.Timestamp === 'string') {
                    eventTime = new Date(data.Timestamp);
                }
                
                // If we couldn't parse it or the date is invalid, use current time
                if (isNaN(eventTime.getTime())) {
                    console.warn('Invalid timestamp format:', data.Timestamp);
                    eventTime = new Date();
                }
            } catch (e) {
                console.error('Error parsing timestamp:', e);
                eventTime = new Date();
            }
        }
        
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
            <span class="timestamp">${formatTime(eventTime)}</span>
            <span class="client-id">${clientId}</span>
            <span class="type ${typeClass}">${displayType}</span>
            <span class="message">${message}</span>
        `;
        
        // Apply client ID filter immediately to the new line
        const activeFilter = document.getElementById('client-id-filter')?.value.trim() || '';
        if (activeFilter !== '') {
            const isMatch = clientId === activeFilter;
            line.setAttribute('data-client-match', isMatch ? 'true' : 'false');
        } else {
            line.setAttribute('data-client-match', 'true');
        }
    }
    
    // Insert the line into the terminal in chronological order
    insertChronologically(line, eventTime);

    // Apply type filters
    if (window.shouldShowLine && typeClass && !window.shouldShowLine(typeClass)) {
        line.classList.add('hidden');
    }
    
    // Apply time filter if active
    if (window.checkTimeMatch && !window.checkTimeMatch(line)) {
        line.classList.add('time-filtered');
    }
    
    // Apply all filters via the central visibility function
    updateVisibility();
}

// Export for use in other modules
window.addLine = addLine;
window.updateVisibility = updateVisibility;