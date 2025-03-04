const searchContainer = document.getElementById('search-container');
const searchInput = document.getElementById('search-input');
const searchCount = document.getElementById('search-count');
let searchTimeout = null;

// Show/hide search bar
function toggleSearch(show) {
    searchContainer.classList.toggle('visible', show);
    if (show) {
        searchInput.focus();
        searchInput.select();
    } else {
        searchInput.value = '';
        clearSearch();
    }
}

// Clear search highlights
function clearSearch() {
    const matches = terminal.getElementsByClassName('search-match');
    while (matches.length > 0) {
        const match = matches[0];
        match.outerHTML = match.innerHTML;
    }
    searchCount.textContent = '';
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
    const searchText = searchInput.value.trim();
    clearSearch();
    
    if (!searchText) return;
    
    const lines = terminal.getElementsByClassName('line');
    let totalMatches = 0;
    let matchedLines = 0;
    
    for (const line of lines) {
        const matches = highlightMatches(line, searchText);
        if (matches > 0) {
            totalMatches += matches;
            matchedLines++;
            line.style.display = '';
        } else {
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
        toggleSearch(false);
        e.preventDefault();
    }
});

// Global keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Ctrl/Cmd + F to show search
    if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
        toggleSearch(true);
        e.preventDefault();
    }
});

// Export for use in other modules
window.toggleSearch = toggleSearch; 