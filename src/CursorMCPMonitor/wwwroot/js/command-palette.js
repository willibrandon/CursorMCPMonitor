const commandPalette = document.getElementById('command-palette');
const commandInput = document.getElementById('command-input');
const commandList = document.getElementById('command-list');
let selectedIndex = 0;
let filteredCommands = [];
let autoScroll = true;

const commands = [
    { id: 'clear', title: 'Clear Logs', shortcut: 'Ctrl+K', action: clearLogs },
    { id: 'copy', title: 'Copy Visible Entries', shortcut: 'Ctrl+C', action: copyVisibleEntries },
    { id: 'autoscroll', title: 'Toggle Auto-scroll', shortcut: 'Ctrl+S', action: toggleAutoScroll },
    { id: 'search', title: 'Focus Search', shortcut: '/', action: focusSearch }
];

function toggleCommandPalette(show) {
    commandPalette.classList.toggle('visible', show);
    if (show) {
        commandInput.value = '';
        commandInput.focus();
        filterCommands('');
    }
}

function filterCommands(query) {
    const items = commandList.getElementsByClassName('command-item');
    query = query.toLowerCase();
    
    for (const item of items) {
        const title = item.querySelector('.command-title').textContent.toLowerCase();
        item.style.display = title.includes(query) ? '' : 'none';
        item.classList.remove('selected');
    }
    
    // Select first visible item
    const visibleItems = [...items].filter(item => item.style.display !== 'none');
    if (visibleItems.length > 0) {
        selectedIndex = 0;
        visibleItems[0].classList.add('selected');
    }
}

function executeCommand(commandId) {
    const command = commands.find(cmd => cmd.id === commandId);
    if (command) {
        toggleCommandPalette(false);
        command.action();
    }
}

// Command implementations
function clearLogs() {
    const terminal = document.getElementById('terminal');
    terminal.innerHTML = '';
}

function copyVisibleEntries() {
    const terminal = document.getElementById('terminal');
    const visibleLines = [...terminal.getElementsByClassName('line')]
        .filter(line => line.style.display !== 'none')
        .map(line => line.textContent)
        .join('\n');
    
    navigator.clipboard.writeText(visibleLines).then(() => {
        // Could add a toast notification here
    });
}

function toggleAutoScroll() {
    autoScroll = !autoScroll;
    // Could add a visual indicator here
}

function focusSearch() {
    const searchInput = document.getElementById('search-input');
    searchInput.focus();
    searchInput.select();
}

// Event listeners
commandInput.addEventListener('input', (e) => {
    filterCommands(e.target.value);
});

commandInput.addEventListener('keydown', (e) => {
    const items = [...commandList.getElementsByClassName('command-item')]
        .filter(item => item.style.display !== 'none');
    
    switch (e.key) {
        case 'ArrowDown':
            e.preventDefault();
            items[selectedIndex]?.classList.remove('selected');
            selectedIndex = (selectedIndex + 1) % items.length;
            items[selectedIndex]?.classList.add('selected');
            break;
            
        case 'ArrowUp':
            e.preventDefault();
            items[selectedIndex]?.classList.remove('selected');
            selectedIndex = (selectedIndex - 1 + items.length) % items.length;
            items[selectedIndex]?.classList.add('selected');
            break;
            
        case 'Enter':
            const selectedCommand = items[selectedIndex]?.getAttribute('data-command');
            if (selectedCommand) {
                executeCommand(selectedCommand);
            }
            break;
            
        case 'Escape':
            toggleCommandPalette(false);
            break;
    }
});

// Global keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Ctrl/Cmd + P to show command palette
    if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
        e.preventDefault();
        toggleCommandPalette(true);
    }
    
    // Ctrl/Cmd + K to clear logs
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        clearLogs();
    }
    
    // Ctrl/Cmd + S to toggle auto-scroll
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        toggleAutoScroll();
    }
});

// Click handlers for command items
commandList.addEventListener('click', (e) => {
    const commandItem = e.target.closest('.command-item');
    if (commandItem) {
        const commandId = commandItem.getAttribute('data-command');
        executeCommand(commandId);
    }
});

// Export for use in other modules
window.toggleCommandPalette = toggleCommandPalette;
window.autoScroll = () => autoScroll; 