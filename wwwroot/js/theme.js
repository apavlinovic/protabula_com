(function() {
    'use strict';

    const STORAGE_KEY = 'theme';

    function getSystemTheme() {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function getStoredTheme() {
        return localStorage.getItem(STORAGE_KEY);
    }

    function getEffectiveTheme() {
        return getStoredTheme() || getSystemTheme();
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(STORAGE_KEY, theme);
        updateToggleButton(theme);
    }

    function updateToggleButton(theme) {
        const button = document.querySelector('.theme-toggle');
        if (!button) return;

        const sunIcon = button.querySelector('.icon-sun');
        const moonIcon = button.querySelector('.icon-moon');

        if (sunIcon && moonIcon) {
            // Show sun in dark mode (click to switch to light)
            // Show moon in light mode (click to switch to dark)
            sunIcon.style.display = theme === 'dark' ? 'block' : 'none';
            moonIcon.style.display = theme === 'light' ? 'block' : 'none';
        }

        button.setAttribute('aria-label', theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode');
    }

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') || getEffectiveTheme();
        setTheme(current === 'dark' ? 'light' : 'dark');
    }

    // Listen for system preference changes (only if user hasn't manually set a preference)
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function(e) {
        if (!getStoredTheme()) {
            setTheme(e.matches ? 'dark' : 'light');
        }
    });

    // Initialize button state when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            updateToggleButton(getEffectiveTheme());
        });
    } else {
        updateToggleButton(getEffectiveTheme());
    }

    // Expose toggle function globally
    window.toggleTheme = toggleTheme;
})();
