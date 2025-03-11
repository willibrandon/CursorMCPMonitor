// Client ID filtering functionality
document.addEventListener('DOMContentLoaded', () => {
    const clientIdFilter = document.getElementById('client-id-filter');
    const clearClientFilter = document.getElementById('clear-client-filter');
    
    let activeClientFilter = '';
    let filterTimeout = null;
    
    // Apply client ID filter to all lines
    function applyClientFilter() {
        const filterText = clientIdFilter.value.trim().toLowerCase();
        activeClientFilter = filterText;
        
        if (filterText === '') {
            // If no filter text, show all lines (respecting other filters)
            updateVisibility();
            return;
        }
        
        const lines = document.querySelectorAll('#terminal .line');
        
        lines.forEach(line => {
            const clientIdElement = line.querySelector('.client-id');
            
            // If no client ID element, skip this line
            if (!clientIdElement) return;
            
            const clientId = clientIdElement.textContent.trim().toLowerCase();
            
            // Set a data attribute to track client filter matches
            if (clientId.includes(filterText)) {
                line.setAttribute('data-client-match', 'true');
            } else {
                line.setAttribute('data-client-match', 'false');
            }
        });
        
        // Update visibility based on all active filters
        updateVisibility();
    }
    
    // Update visibility of lines based on all active filters
    function updateVisibility() {
        const lines = document.querySelectorAll('#terminal .line');
        
        lines.forEach(line => {
            // First check if it's hidden by type filters
            const isHiddenByTypeFilter = line.classList.contains('hidden');
            
            // Then check if it's hidden by client filter
            const isClientMatch = activeClientFilter === '' || 
                line.getAttribute('data-client-match') === 'true';
            
            // Apply combined visibility
            if (isHiddenByTypeFilter || !isClientMatch) {
                line.style.display = 'none';
            } else {
                line.style.display = '';
            }
        });
        
        // Re-apply search filter if active
        if (window.applySearchFilter) {
            window.applySearchFilter();
        }
    }
    
    // Set up event listeners
    clientIdFilter.addEventListener('input', () => {
        clearTimeout(filterTimeout);
        filterTimeout = setTimeout(applyClientFilter, 300);
    });
    
    clearClientFilter.addEventListener('click', () => {
        clientIdFilter.value = '';
        activeClientFilter = '';
        
        const lines = document.querySelectorAll('#terminal .line');
        lines.forEach(line => {
            line.removeAttribute('data-client-match');
        });
        
        updateVisibility();
    });
    
    // Export functions for use in other modules
    window.updateClientFilter = applyClientFilter;
    window.updateVisibility = updateVisibility;
    
    // Function to check if a new line matches the client filter
    window.checkClientMatch = (line) => {
        if (activeClientFilter === '') return true;
        
        const clientIdElement = line.querySelector('.client-id');
        if (!clientIdElement) return true;
        
        const clientId = clientIdElement.textContent.trim().toLowerCase();
        const isMatch = clientId.includes(activeClientFilter);
        
        line.setAttribute('data-client-match', isMatch ? 'true' : 'false');
        return isMatch;
    };
}); 