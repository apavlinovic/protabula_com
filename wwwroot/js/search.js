(function() {
    'use strict';

    const searchInput = document.getElementById('color-search');
    const searchResults = document.getElementById('search-results');
    const searchContainer = searchInput?.closest('.nav-search');

    if (!searchInput || !searchResults || !searchContainer) return;

    const culture = searchInput.dataset.culture || 'en';
    let debounceTimer = null;
    let activeIndex = -1;
    let currentResults = [];

    // Debounce function
    function debounce(func, wait) {
        return function(...args) {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => func.apply(this, args), wait);
        };
    }

    // Fetch search results
    async function fetchResults(query) {
        if (query.length < 2) {
            hideResults();
            return;
        }

        try {
            const response = await fetch(`/api/colors/search?q=${encodeURIComponent(query)}&culture=${culture}`);
            if (!response.ok) throw new Error('Search failed');

            currentResults = await response.json();
            renderResults(currentResults);
        } catch (error) {
            console.error('Search error:', error);
            hideResults();
        }
    }

    // Render results dropdown
    function renderResults(results) {
        if (results.length === 0) {
            searchResults.innerHTML = `<div class="search-no-results">${getNoResultsText()}</div>`;
            showResults();
            return;
        }

        searchResults.innerHTML = results.map((color, index) => `
            <a href="/${culture}/ral-colors/${color.slug}"
               class="search-result-item"
               role="option"
               id="search-result-${index}"
               data-index="${index}">
                <span class="search-result-swatch ${color.needsDarkText ? 'dark' : ''}" style="background-color: ${color.hex};"></span>
                <span class="search-result-text">
                    <span class="search-result-number">RAL ${color.number}</span>
                    <span class="search-result-name">${color.name || ''}</span>
                </span>
            </a>
        `).join('');

        activeIndex = -1;
        showResults();
    }

    // Get localized "no results" text
    function getNoResultsText() {
        return culture === 'de' ? 'Keine Ergebnisse gefunden' : 'No results found';
    }

    // Show results dropdown
    function showResults() {
        searchResults.classList.add('active');
        searchContainer.setAttribute('aria-expanded', 'true');
    }

    // Hide results dropdown
    function hideResults() {
        searchResults.classList.remove('active');
        searchContainer.setAttribute('aria-expanded', 'false');
        activeIndex = -1;
        updateActiveDescendant();
    }

    // Update active descendant for accessibility
    function updateActiveDescendant() {
        const items = searchResults.querySelectorAll('.search-result-item');
        items.forEach((item, index) => {
            item.classList.toggle('active', index === activeIndex);
        });

        searchInput.setAttribute('aria-activedescendant',
            activeIndex >= 0 ? `search-result-${activeIndex}` : '');
    }

    // Navigate results with keyboard
    function navigateResults(direction) {
        const items = searchResults.querySelectorAll('.search-result-item');
        if (items.length === 0) return;

        if (direction === 'down') {
            activeIndex = activeIndex < items.length - 1 ? activeIndex + 1 : 0;
        } else {
            activeIndex = activeIndex > 0 ? activeIndex - 1 : items.length - 1;
        }

        updateActiveDescendant();

        // Scroll into view if needed
        const activeItem = items[activeIndex];
        if (activeItem) {
            activeItem.scrollIntoView({ block: 'nearest' });
        }
    }

    // Select current result
    function selectResult() {
        if (activeIndex >= 0 && currentResults[activeIndex]) {
            window.location.href = `/${culture}/ral-colors/${currentResults[activeIndex].slug}`;
        }
    }

    // Debounced search handler
    const debouncedSearch = debounce(fetchResults, 300);

    // Event listeners
    searchInput.addEventListener('input', (e) => {
        const query = e.target.value.trim();
        debouncedSearch(query);
    });

    searchInput.addEventListener('keydown', (e) => {
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                if (searchResults.classList.contains('active')) {
                    navigateResults('down');
                }
                break;
            case 'ArrowUp':
                e.preventDefault();
                if (searchResults.classList.contains('active')) {
                    navigateResults('up');
                }
                break;
            case 'Enter':
                e.preventDefault();
                if (activeIndex >= 0) {
                    selectResult();
                } else if (currentResults.length > 0) {
                    // Select first result if none is highlighted
                    activeIndex = 0;
                    selectResult();
                }
                break;
            case 'Escape':
                hideResults();
                searchInput.blur();
                break;
        }
    });

    searchInput.addEventListener('focus', () => {
        if (searchInput.value.trim().length >= 2 && currentResults.length > 0) {
            showResults();
        }
    });

    // Click outside to close
    document.addEventListener('click', (e) => {
        if (!searchContainer.contains(e.target)) {
            hideResults();
        }
    });

    // Handle result item clicks (for touch devices)
    searchResults.addEventListener('click', (e) => {
        const item = e.target.closest('.search-result-item');
        if (item) {
            // Let the link handle navigation naturally
            return;
        }
    });

    // Handle hover on results
    searchResults.addEventListener('mousemove', (e) => {
        const item = e.target.closest('.search-result-item');
        if (item) {
            activeIndex = parseInt(item.dataset.index, 10);
            updateActiveDescendant();
        }
    });
})();
