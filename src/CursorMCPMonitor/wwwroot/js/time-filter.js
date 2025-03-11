// Ultra-simplified time-based filtering
document.addEventListener('DOMContentLoaded', () => {
    const startTimeFilter = document.getElementById('start-time-filter');
    const endTimeFilter = document.getElementById('end-time-filter');
    const applyTimeFilter = document.getElementById('apply-time-filter');
    const clearTimeFilter = document.getElementById('clear-time-filter');
    
    // Format time to match the log format: HH:MM:SS.SSS
    function formatTimeForFilter(date) {
        const hours = date.getHours().toString().padStart(2, '0');
        const minutes = date.getMinutes().toString().padStart(2, '0');
        const seconds = date.getSeconds().toString().padStart(2, '0');
        const ms = date.getMilliseconds().toString().padStart(3, '0');
        return `${hours}:${minutes}:${seconds}.${ms}`;
    }
    
    // Set up initial values
    const now = new Date();
    endTimeFilter.value = formatTimeForFilter(now);
    
    const startTime = new Date(now.getTime() - 60 * 60 * 1000);
    startTimeFilter.value = formatTimeForFilter(startTime);
    
    // Simple time filter application
    applyTimeFilter.addEventListener('click', () => {
        console.log('Apply time filter clicked');
        
        const startValue = startTimeFilter.value;
        const endValue = endTimeFilter.value;
        
        if (!startValue || !endValue) {
            alert('Please enter both start and end times');
            return;
        }
        
        // Direct manipulation of all lines
        const lines = document.querySelectorAll('#terminal .line');
        console.log(`Filtering ${lines.length} lines`);
        
        lines.forEach(line => {
            const timestamp = line.querySelector('.timestamp');
            if (!timestamp) return;
            
            const timeText = timestamp.textContent.trim();
            
            // Simple string comparison - if the timestamp is alphabetically between start and end
            const isInRange = (startValue <= timeText && timeText <= endValue);
            
            // Directly set display property
            if (isInRange) {
                line.style.display = '';
                line.classList.add('time-matched');
            } else {
                line.style.display = 'none';
                line.classList.remove('time-matched');
            }
        });
        
        // Show feedback
        document.getElementById('time-filter').classList.add('filter-active');
    });
    
    // Clear filter
    clearTimeFilter.addEventListener('click', () => {
        console.log('Clear time filter clicked');
        
        // Simply show all lines
        const lines = document.querySelectorAll('#terminal .line');
        lines.forEach(line => {
            line.style.display = '';
            line.classList.remove('time-matched');
        });
        
        document.getElementById('time-filter').classList.remove('filter-active');
        
        // Reset the type and client filters if needed
        if (typeof window.updateVisibility === 'function') {
            window.updateVisibility();
        }
    });
}); 