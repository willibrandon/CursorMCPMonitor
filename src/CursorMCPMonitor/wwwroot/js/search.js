const searchContainer = document.getElementById('search-container');
const searchInput = document.getElementById('search-input');
const searchCount = document.getElementById('search-count');
let searchTimeout = null;

// Clear search highlights
function clearSearch() {
    const matches = terminal.getElementsByClassName('search-match');
    while (matches.length > 0) {
        const match = matches[0];
        match.outerHTML = match.innerHTML;
    }
    searchCount.textContent = '';

    // Show all lines when clearing search (if not hidden by other filters)
    const lines = terminal.getElementsByClassName('line');
    for (const line of lines) {
        line.classList.remove('hidden-by-search');
        
        // Show line unless hidden by other filters
        if (!line.classList.contains('hidden-by-type') && 
            !line.classList.contains('hidden-by-client') &&
            !line.classList.contains('hidden-by-time')) {
            line.style.display = '';
        }
    }
}

// Highlight search matches in a line
function highlightMatches(line, searchText) {
    if (!searchText) return 0;
    
    const regex = new RegExp(escapeRegExp(searchText), 'gi');
    const messageEl = line.querySelector('.message');
    if (!messageEl) return 0;

    const text = messageEl.textContent;
    const matches = text.match(regex);
    if (!matches) return 0;

    messageEl.innerHTML = text.replace(regex, '<span class="search-match">$&</span>');
    return matches.length;
}

// Escape special characters in search text
function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// Perform search
function performSearch() {
    console.log('[SEARCH] Performing search');
    const searchText = searchInput.value.trim();
    clearSearch();
    
    if (!searchText) return;
    
    const lines = terminal.getElementsByClassName('line');
    let totalMatches = 0;
    let matchedLines = 0;
    
    for (const line of lines) {
        // Skip checking lines already hidden by other filters
        if (line.classList.contains('hidden-by-type') || 
            line.classList.contains('hidden-by-client') ||
            line.classList.contains('hidden-by-time')) {
            continue;
        }
        
        const matches = highlightMatches(line, searchText);
        if (matches > 0) {
            totalMatches += matches;
            matchedLines++;
            line.classList.remove('hidden-by-search');
            line.style.display = '';
        } else {
            line.classList.add('hidden-by-search');
            line.style.display = 'none';
        }
    }
    
    if (totalMatches > 0) {
        searchCount.textContent = `${matchedLines} lines, ${totalMatches} matches`;
    } else {
        searchCount.textContent = 'No matches';
    }
}

// Debounced search
function debouncedSearch() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(performSearch, 100);
}

// Event listeners
searchInput.addEventListener('input', debouncedSearch);

searchInput.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        searchInput.value = '';
        clearSearch();
        e.preventDefault();
    }
});

// Global keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Use forward slash to focus search
    if (e.key === '/' && document.activeElement !== searchInput && 
        document.activeElement.tagName !== 'INPUT') {
        searchInput.focus();
        searchInput.select();
        e.preventDefault(); // Prevent browser's quick find
    }
});

// Export for use in other modules
window.applySearchFilter = performSearch; 