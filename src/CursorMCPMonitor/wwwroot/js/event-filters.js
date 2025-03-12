// Event filtering functionality
document.addEventListener('DOMContentLoaded', () => {
    const filterCheckboxes = document.querySelectorAll('#event-filters input[type="checkbox"]');
    const filterAll = document.getElementById('filter-all');
    const filterNone = document.getElementById('filter-none');
    
    // Object to track active filters
    const activeFilters = {
        create: true,
        list: true,
        connect: true,
        error: true,
        warning: true,
        close: true
    };

    // Apply filters to all existing lines
    function applyFilters() {
        console.log('[EVENT FILTER] Applying event type filters');
        const lines = document.querySelectorAll('#terminal .line');
        
        lines.forEach(line => {
            const typeElement = line.querySelector('.type');
            
            // If no type element, skip this line (probably a system message)
            if (!typeElement) return;
            
            const typeClass = Array.from(typeElement.classList)
                .find(cls => cls !== 'type');
            
            if (typeClass && !activeFilters[typeClass]) {
                line.classList.add('hidden-by-type');
                line.style.display = 'none';
            } else {
                line.classList.remove('hidden-by-type');
                // Only show the line if it's not hidden by other filters
                if (!line.classList.contains('hidden-by-search') && 
                    !line.classList.contains('hidden-by-client') &&
                    !line.classList.contains('hidden-by-time')) {
                    line.style.display = '';
                }
            }
        });
        
        // Re-apply client filter if active
        if (document.getElementById('client-id-filter').value.trim() !== '') {
            // Trigger the client filter if it exists
            if (typeof filterClientId === 'function') {
                filterClientId();
            }
        }
        
        // Re-apply search filter if active
        if (window.applySearchFilter) {
            window.applySearchFilter();
        }
    }
    
    // Set up event listeners for each checkbox
    filterCheckboxes.forEach(checkbox => {
        const filterType = checkbox.getAttribute('data-filter-type');
        
        checkbox.addEventListener('change', () => {
            activeFilters[filterType] = checkbox.checked;
            applyFilters();
        });
    });
    
    // "All" button handler
    filterAll.addEventListener('click', () => {
        filterCheckboxes.forEach(checkbox => {
            checkbox.checked = true;
            const filterType = checkbox.getAttribute('data-filter-type');
            activeFilters[filterType] = true;
        });
        applyFilters();
    });
    
    // "None" button handler
    filterNone.addEventListener('click', () => {
        filterCheckboxes.forEach(checkbox => {
            checkbox.checked = false;
            const filterType = checkbox.getAttribute('data-filter-type');
            activeFilters[filterType] = false;
        });
        applyFilters();
    });
    
    // Export filter function to be used when adding new lines
    window.shouldShowLine = (typeClass) => {
        return activeFilters[typeClass] || false;
    };
    
    // Expose the applyFilters function to be used by other modules
    window.applyEventFilters = applyFilters;
});
