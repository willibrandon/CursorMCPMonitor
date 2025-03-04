const themes = ['dark', 'light', 'hc-dark', 'hc-light'];
let currentThemeIndex = 0;
const themeIndicator = document.getElementById('theme-indicator');

// Check for saved theme preference
const savedTheme = localStorage.getItem('theme');
if (savedTheme) {
    document.documentElement.setAttribute('data-theme', savedTheme);
    currentThemeIndex = themes.indexOf(savedTheme);
    themeIndicator.textContent = `theme: ${savedTheme}`;
}

// Click to cycle themes
themeIndicator.addEventListener('click', () => {
    currentThemeIndex = (currentThemeIndex + 1) % themes.length;
    const theme = themes[currentThemeIndex];
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
    themeIndicator.textContent = `theme: ${theme}`;
}); 