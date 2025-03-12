// Time-based filtering for full timestamps (YYYY-MM-DD HH:MM:SS.SSS)
document.addEventListener('DOMContentLoaded', () => {
    const startTimeFilter = document.getElementById('start-time-filter');
    const endTimeFilter = document.getElementById('end-time-filter');
    const applyTimeFilter = document.getElementById('apply-time-filter');
    const clearTimeFilter = document.getElementById('clear-time-filter');
    const timeFilterContainer = document.getElementById('time-filter');
    
    // Smart date-time editor implementation
    function setupSmartTimeEditor(input) {
        // Define the timestamp format template
        const FORMAT_TEMPLATE = 'YYYY-MM-DD HH:MM:SS.SSS';
        const FORMAT_SEPARATORS = ['-', '-', ' ', ':', ':', '.'];
        const SECTION_LENGTHS = [4, 2, 2, 2, 2, 2, 3]; // YYYY, MM, DD, HH, MM, SS, SSS
        
        // Set initial value with the current timestamp
        if (!input.value) {
            const now = new Date();
            input.value = formatTimestamp(now);
        }
        
        // Highlight section on click
        input.addEventListener('click', (e) => {
            const cursorPos = getCursorPosition(input);
            if (cursorPos >= 0) {
                const section = getSectionFromPosition(cursorPos);
                selectSection(input, section);
            }
        });
        
        // Handle key navigation and editing
        input.addEventListener('keydown', (e) => {
            // Allow backspace, delete to work normally
            if (e.key === 'Backspace' || e.key === 'Delete') {
                return;
            }
            
            // Handle Enter key for filter application
            if (e.key === 'Enter') {
                applyTimeFilter.click();
                e.preventDefault();
                return;
            }
            
            const cursorPos = getCursorPosition(input);
            const currentSection = getSectionFromPosition(cursorPos);
            
            // Handle keyboard navigation between sections
            if (e.key === 'ArrowLeft') {
                e.preventDefault();
                
                // Move to previous section
                if (currentSection > 0) {
                    selectSection(input, currentSection - 1);
                }
                return;
            }
            
            if (e.key === 'ArrowRight' || e.key === 'Tab') {
                if (e.key === 'Tab' && e.shiftKey) {
                    // Let Tab+Shift work normally to go to previous field
                    return;
                }
                
                e.preventDefault();
                
                // Move to next section
                if (currentSection < SECTION_LENGTHS.length - 1) {
                    selectSection(input, currentSection + 1);
                } else if (e.key === 'Tab') {
                    // Move to next input when Tab pressed in last section
                    if (input === startTimeFilter) {
                        endTimeFilter.focus();
                        selectSection(endTimeFilter, 0);
                    } else if (input === endTimeFilter) {
                        applyTimeFilter.focus();
                    }
                }
                return;
            }
            
            // Handle arrow up/down to increment/decrement values
            if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
                e.preventDefault();
                
                const selectedText = getSelectedText(input);
                if (!selectedText) {
                    // If nothing selected, select current section
                    selectSection(input, currentSection);
                    return;
                }
                
                if (currentSection >= 0) {
                    const sectionValue = parseInt(selectedText, 10);
                    
                    if (!isNaN(sectionValue)) {
                        let newValue;
                        const increment = e.key === 'ArrowUp' ? 1 : -1;
                        
                        // Apply proper limits based on section
                        switch(currentSection) {
                            case 0: // Year
                                newValue = Math.max(1970, sectionValue + increment);
                                break;
                            case 1: // Month
                                newValue = (sectionValue + increment - 1 + 12) % 12 + 1; // 1-12
                                break;
                            case 2: // Day
                                newValue = (sectionValue + increment - 1 + 31) % 31 + 1; // 1-31
                                break;
                            case 3: // Hour
                                newValue = (sectionValue + increment + 24) % 24; // 0-23
                                break;
                            case 4: // Minute
                            case 5: // Second
                                newValue = (sectionValue + increment + 60) % 60; // 0-59
                                break;
                            case 6: // Millisecond
                                newValue = (sectionValue + increment + 1000) % 1000; // 0-999
                                break;
                            default:
                                return;
                        }
                        
                        // Format the new value with proper padding
                        const paddedValue = String(newValue).padStart(SECTION_LENGTHS[currentSection], '0');
                        
                        // Replace the selected text with the new value
                        replaceSelectedText(input, paddedValue);
                        
                        // Re-select the same section
                        selectSection(input, currentSection);
                    }
                }
                
                return;
            }
            
            // Quick date shortcuts (with Alt key)
            if (e.altKey) {
                e.preventDefault();
                
                // Get current timestamp
                const now = new Date();
                
                // Alt+1-9 for quick time jumps
                switch(e.key) {
                    case '1': // Last hour
                        if (input === startTimeFilter) {
                            input.value = formatTimestamp(new Date(now.getTime() - 60 * 60 * 1000));
                        } else if (input === endTimeFilter) {
                            input.value = formatTimestamp(now);
                        }
                        break;
                    case '2': // Last 3 hours
                        if (input === startTimeFilter) {
                            input.value = formatTimestamp(new Date(now.getTime() - 3 * 60 * 60 * 1000));
                        } else if (input === endTimeFilter) {
                            input.value = formatTimestamp(now);
                        }
                        break;
                    case '3': // Last 6 hours
                        if (input === startTimeFilter) {
                            input.value = formatTimestamp(new Date(now.getTime() - 6 * 60 * 60 * 1000));
                        } else if (input === endTimeFilter) {
                            input.value = formatTimestamp(now);
                        }
                        break;
                    case '4': // Last 12 hours
                        if (input === startTimeFilter) {
                            input.value = formatTimestamp(new Date(now.getTime() - 12 * 60 * 60 * 1000));
                        } else if (input === endTimeFilter) {
                            input.value = formatTimestamp(now);
                        }
                        break;
                    case '5': // Last 24 hours (1 day)
                        if (input === startTimeFilter) {
                            input.value = formatTimestamp(new Date(now.getTime() - 24 * 60 * 60 * 1000));
                        } else if (input === endTimeFilter) {
                            input.value = formatTimestamp(now);
                        }
                        break;
                    case '6': // Last 48 hours (2 days)
                        if (input === startTimeFilter) {
                            input.value = formatTimestamp(new Date(now.getTime() - 48 * 60 * 60 * 1000));
                        } else if (input === endTimeFilter) {
                            input.value = formatTimestamp(now);
                        }
                        break;
                    case '7': // Last 7 days
                        if (input === startTimeFilter) {
                            input.value = formatTimestamp(new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000));
                        } else if (input === endTimeFilter) {
                            input.value = formatTimestamp(now);
                        }
                        break;
                    case 'n': // Now
                        input.value = formatTimestamp(now);
                        break;
                    case 't': // Today start
                        const todayStart = new Date(now);
                        todayStart.setHours(0, 0, 0, 0);
                        input.value = formatTimestamp(todayStart);
                        break;
                    case 'e': // Today end
                        const todayEnd = new Date(now);
                        todayEnd.setHours(23, 59, 59, 999);
                        input.value = formatTimestamp(todayEnd);
                        break;
                    case 'a': // Apply filter
                        applyTimeFilter.click();
                        break;
                }
                
                return;
            }
            
            // Allow only numbers for input
            if (!/^\d$/.test(e.key)) {
                e.preventDefault();
                return;
            }
            
            // Get the currently selected text (if any)
            const selectedText = getSelectedText(input);
            
            if (currentSection >= 0) {
                // If the entire section is selected or section is empty
                if (selectedText.length === SECTION_LENGTHS[currentSection]) {
                    e.preventDefault();
                    
                    // For year (4 digits), start with the typed number
                    if (currentSection === 0) {
                        // For first digit of year, pad with 3 zeros
                        const newValue = e.key.padStart(SECTION_LENGTHS[currentSection], '0');
                        replaceSelectedText(input, newValue);
                        selectSection(input, currentSection);
                    } else {
                        // For other fields, replace and potentially auto-advance
                        
                        // If it's a 2-digit field, handle differently for first vs second digit
                        if (SECTION_LENGTHS[currentSection] === 2) {
                            // Create a new range that only selects the first digit
                            input.setSelectionRange(input.selectionStart, input.selectionStart + 1);
                            
                            // Type the first digit
                            document.execCommand('insertText', false, e.key);
                            
                            // Check if this is valid for the field
                            const newSectionStart = input.selectionStart - 1;
                            let newFirstDigit = parseInt(input.value.charAt(newSectionStart), 10);
                            
                            // Validate first digit based on field type
                            let maxFirstDigit;
                            switch(currentSection) {
                                case 1: // Month (1-12)
                                    maxFirstDigit = 1;
                                    break;
                                case 2: // Day (1-31)
                                    maxFirstDigit = 3;
                                    break;
                                case 3: // Hour (0-23)
                                    maxFirstDigit = 2;
                                    break;
                                case 4: // Minute (0-59)
                                case 5: // Second (0-59)
                                    maxFirstDigit = 5;
                                    break;
                                default:
                                    maxFirstDigit = 9;
                            }
                            
                            // If digit is too large, reset to max allowed
                            if (newFirstDigit > maxFirstDigit) {
                                // Special case handling
                                if (currentSection === 1 && newFirstDigit > 1) { // Month
                                    input.value = input.value.substring(0, newSectionStart) + 
                                                 '0' + newFirstDigit + 
                                                 input.value.substring(newSectionStart + 2);
                                    selectSection(input, currentSection + 1); // Move to next section
                                    return;
                                } else if (currentSection === 2 && newFirstDigit > 3) { // Day
                                    input.value = input.value.substring(0, newSectionStart) + 
                                                 '0' + newFirstDigit + 
                                                 input.value.substring(newSectionStart + 2);
                                    selectSection(input, currentSection + 1); // Move to next section
                                    return;
                                }
                            }
                            
                            // Select the second digit position
                            input.setSelectionRange(input.selectionStart, input.selectionStart + 1);
                        } else if (SECTION_LENGTHS[currentSection] === 3) {
                            // For milliseconds (3 digits)
                            // Replace the selected text with the typed digit followed by zeros
                            const newValue = e.key + '00';
                            replaceSelectedText(input, newValue);
                            
                            // Select the second position
                            const newPos = input.selectionStart - 2;
                            input.setSelectionRange(newPos, newPos + 1);
                        }
                    }
                } else {
                    // If partial selection or cursor is positioned within a section
                    
                    // Get the starting position of the current section
                    let sectionStart = 0;
                    for (let i = 0; i < currentSection; i++) {
                        sectionStart += SECTION_LENGTHS[i] + 1; // +1 for separator
                    }
                    
                    // Get the full current section value including the new digit
                    const currentSectionEnd = sectionStart + SECTION_LENGTHS[currentSection];
                    const currentValue = input.value;
                    const currentPos = getCursorPosition(input);
                    
                    // Type the digit
                    document.execCommand('insertText', false, e.key);
                    
                    // Check if we filled the section and should auto-advance
                    const newCursorPos = getCursorPosition(input);
                    if (newCursorPos >= currentSectionEnd) {
                        // We're at the end of this section, move to next
                        if (currentSection < SECTION_LENGTHS.length - 1) {
                            selectSection(input, currentSection + 1);
                        }
                    }
                }
                
                e.preventDefault();
                return;
            }
        });
        
        // Add an input handler to manage digit-by-digit typing
        input.addEventListener('input', (e) => {
            // This handles cases where the browser inserts text despite our preventDefault
            const cursorPos = getCursorPosition(input);
            const currentSection = getSectionFromPosition(cursorPos);
            
            // If we're at the end of a section, auto-advance to next section
            if (currentSection >= 0) {
                let sectionStart = 0;
                for (let i = 0; i < currentSection; i++) {
                    sectionStart += SECTION_LENGTHS[i] + 1; // +1 for separator
                }
                
                const sectionEnd = sectionStart + SECTION_LENGTHS[currentSection];
                
                // If cursor is at the end of a section, move to next section
                if (cursorPos === sectionEnd && currentSection < SECTION_LENGTHS.length - 1) {
                    selectSection(input, currentSection + 1);
                }
            }
        });
        
        // Get the cursor position in the input
        function getCursorPosition(input) {
            return input.selectionStart;
        }
        
        // Get selected text in the input
        function getSelectedText(input) {
            return input.value.substring(input.selectionStart, input.selectionEnd);
        }
        
        // Replace the selected text with new text
        function replaceSelectedText(input, newText) {
            const start = input.selectionStart;
            const end = input.selectionEnd;
            const currentValue = input.value;
            
            input.value = currentValue.substring(0, start) + 
                          newText + 
                          currentValue.substring(end);
        }
        
        // Get the timestamp section based on cursor position
        function getSectionFromPosition(position) {
            let charCount = 0;
            
            for (let i = 0; i < SECTION_LENGTHS.length; i++) {
                const sectionStart = charCount;
                charCount += SECTION_LENGTHS[i];
                
                // Add separator length except for the last section
                if (i < FORMAT_SEPARATORS.length) {
                    charCount += 1; // separator length
                }
                
                if (position >= sectionStart && position < charCount) {
                    return i;
                }
            }
            
            return -1;
        }
        
        // Select a specific section of the timestamp
        function selectSection(input, section) {
            if (section < 0 || section >= SECTION_LENGTHS.length) return;
            
            let start = 0;
            
            // Calculate the start position of the desired section
            for (let i = 0; i < section; i++) {
                start += SECTION_LENGTHS[i];
                start += 1; // separator
            }
            
            const end = start + SECTION_LENGTHS[section];
            
            // Select the section
            input.setSelectionRange(start, end);
        }
        
        // Initialize with the template format if empty
        input.addEventListener('focus', () => {
            if (!input.value.trim()) {
                const now = new Date();
                input.value = formatTimestamp(now);
                selectSection(input, 0); // Select the year section initially
            }
        });
    }
    
    // Parse timestamp string to Date object
    function parseTimestamp(timestampStr) {
        // Expected format: YYYY-MM-DD HH:MM:SS.SSS
        if (!timestampStr || typeof timestampStr !== 'string') return null;
        
        try {
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
            console.error('Error parsing timestamp:', e);
            return null;
        }
    }
    
    // Format a Date object to match the log timestamp format
    function formatTimestamp(date) {
        if (!date || !(date instanceof Date)) {
            return '';
        }
        
        const year = date.getFullYear();
        const month = (date.getMonth() + 1).toString().padStart(2, '0');
        const day = date.getDate().toString().padStart(2, '0');
        const hours = date.getHours().toString().padStart(2, '0');
        const minutes = date.getMinutes().toString().padStart(2, '0');
        const seconds = date.getSeconds().toString().padStart(2, '0');
        const ms = date.getMilliseconds().toString().padStart(3, '0');
        
        return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}.${ms}`;
    }
    
    // Set initial values - end time to now, start time to 24 hours ago
    const now = new Date();
    const oneDayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);
    
    // Initialize the smart date-time editors
    startTimeFilter.value = formatTimestamp(oneDayAgo);
    endTimeFilter.value = formatTimestamp(now);
    
    // Set up smart editors for both inputs
    setupSmartTimeEditor(startTimeFilter);
    setupSmartTimeEditor(endTimeFilter);
    
    // Apply the time filter
    applyTimeFilter.addEventListener('click', () => {
        console.log('[TIME FILTER] Applying time filter');
        
        // Get the user input values or use placeholders if empty
        let startValue = startTimeFilter.value.trim();
        let endValue = endTimeFilter.value.trim();
        
        if (!startValue) startValue = startTimeFilter.placeholder;
        if (!endValue) endValue = endTimeFilter.placeholder;
        
        // Validate inputs
        if (!startValue || !endValue) {
            alert('Please enter valid start and end timestamps');
            return;
        }
        
        // Try to parse timestamps
        const startDate = parseTimestamp(startValue);
        const endDate = parseTimestamp(endValue);
        
        if (!startDate || !endDate) {
            alert('Invalid timestamp format. Use YYYY-MM-DD HH:MM:SS.SSS');
            return;
        }
        
        // Filter the log entries
        const lines = document.querySelectorAll('#terminal .line');
        console.log(`[TIME FILTER] Filtering ${lines.length} log entries between ${startValue} and ${endValue}`);
        
        let visibleCount = 0;
        lines.forEach(line => {
            const timestampEl = line.querySelector('.timestamp');
            if (!timestampEl) return;
            
            const timeText = timestampEl.textContent.trim();
            const entryDate = parseTimestamp(timeText);
            
            if (!entryDate) return;
            
            const isInRange = (entryDate >= startDate && entryDate <= endDate);
            
            if (isInRange) {
                line.classList.remove('time-filtered');
                visibleCount++;
            } else {
                line.classList.add('time-filtered');
                // Note: We don't update display here anymore to allow combined filtering
            }
        });
        
        console.log(`[TIME FILTER] Time filter applied: ${visibleCount} entries in range`);
        
        // Apply visual indicator for active filter
        timeFilterContainer.classList.add('filter-active');
        
        // Apply combined filtering if the combined filter function exists
        if (typeof applyAllFilters === 'function') {
            setTimeout(applyAllFilters, 50);
        }
    });
    
    // Clear the time filter
    clearTimeFilter.addEventListener('click', () => {
        console.log('[TIME FILTER] Clearing time filter');
        
        // Reset to default time range (last 24 hours)
        const now = new Date();
        const oneDayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);
        
        startTimeFilter.value = formatTimestamp(oneDayAgo);
        endTimeFilter.value = formatTimestamp(now);
        
        // Remove filtered class from all lines
        const lines = document.querySelectorAll('#terminal .line');
        lines.forEach(line => {
            line.classList.remove('time-filtered');
        });
        
        // Remove active filter indicator
        timeFilterContainer.classList.remove('filter-active');
        
        // Apply combined filtering if the combined filter function exists
        if (typeof applyAllFilters === 'function') {
            setTimeout(applyAllFilters, 50);
        }
    });
    
    // Export function for checking time matches when new lines are added
    window.checkTimeMatch = (line) => {
        // If filter is not active, all lines match
        if (!timeFilterContainer.classList.contains('filter-active')) {
            return true;
        }
        
        // Get the timestamp from the line
        const timestampEl = line.querySelector('.timestamp');
        if (!timestampEl) return true;
        
        const timeText = timestampEl.textContent.trim();
        const entryDate = parseTimestamp(timeText);
        
        if (!entryDate) return true;
        
        // Get current filter values
        let startValue = startTimeFilter.value.trim();
        let endValue = endTimeFilter.value.trim();
        
        if (!startValue) startValue = startTimeFilter.placeholder;
        if (!endValue) endValue = endTimeFilter.placeholder;
        
        const startDate = parseTimestamp(startValue);
        const endDate = parseTimestamp(endValue);
        
        if (!startDate || !endDate) return true;
        
        const isInRange = (entryDate >= startDate && entryDate <= endDate);
        if (!isInRange) {
            line.classList.add('time-filtered');
        }
        
        return isInRange;
    };
});