(function() {
    'use strict';

    const searchTrigger = document.getElementById('search-trigger');
    if (!searchTrigger) return;

    const culture = searchTrigger.dataset.culture || 'en';
    const placeholder = searchTrigger.dataset.placeholder || 'Search...';
    const noResultsText = searchTrigger.dataset.noResults || 'No results found';

    let debounceTimer = null;
    let activeIndex = -1;
    let currentResults = [];
    let overlay = null;
    let overlayInput = null;
    let overlayResults = null;

    function debounce(func, wait) {
        return function(...args) {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => func.apply(this, args), wait);
        };
    }

    function createOverlay() {
        if (document.getElementById('search-overlay')) {
            overlay = document.getElementById('search-overlay');
            overlayInput = document.getElementById('overlay-search-input');
            overlayResults = document.getElementById('overlay-search-results');
            return;
        }

        overlay = document.createElement('div');
        overlay.id = 'search-overlay';
        overlay.className = 'overlay search-overlay';
        overlay.innerHTML = `
            <div class="overlay-header">
                <div class="search-overlay-input-wrapper">
                    <svg class="search-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="11" cy="11" r="8"/>
                        <path d="m21 21-4.3-4.3"/>
                    </svg>
                    <input type="text"
                           id="overlay-search-input"
                           class="search-overlay-input"
                           placeholder="${placeholder}"
                           autocomplete="off" />
                </div>
                <button class="overlay-close" aria-label="${culture === 'de' ? 'Schließen' : 'Close'}">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M18 6 6 18"/>
                        <path d="m6 6 12 12"/>
                    </svg>
                </button>
            </div>
            <div id="overlay-search-results" class="overlay-content"></div>
        `;
        document.body.appendChild(overlay);

        overlayInput = document.getElementById('overlay-search-input');
        overlayResults = document.getElementById('overlay-search-results');
        const closeBtn = overlay.querySelector('.overlay-close');

        overlayInput.addEventListener('input', (e) => {
            const query = e.target.value.trim();
            debouncedSearch(query);
        });

        overlayInput.addEventListener('keydown', handleKeydown);

        closeBtn.addEventListener('click', closeOverlay);

        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                closeOverlay();
            }
        });

        // Handle clicks on results
        overlayResults.addEventListener('click', (e) => {
            const item = e.target.closest('.search-result-item');
            if (item) {
                return;
            }
        });

        // Handle hover on results
        overlayResults.addEventListener('mousemove', (e) => {
            const item = e.target.closest('.search-result-item');
            if (item) {
                activeIndex = parseInt(item.dataset.index, 10);
                updateActiveDescendant();
            }
        });
    }

    function openOverlay() {
        createOverlay();
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden';

        overlayInput.value = '';
        overlayResults.innerHTML = '';
        currentResults = [];
        activeIndex = -1;

        requestAnimationFrame(() => {
            overlayInput.focus();
        });
    }

    function closeOverlay() {
        if (overlay) {
            overlay.classList.remove('active');
            document.body.style.overflow = '';
            activeIndex = -1;
        }
    }

    async function fetchResults(query) {
        if (query.length < 2) {
            overlayResults.innerHTML = '';
            currentResults = [];
            return;
        }

        try {
            const response = await fetch(`/api/colors/search?q=${encodeURIComponent(query)}&culture=${culture}`);
            if (!response.ok) throw new Error('Search failed');

            const data = await response.json();

            currentResults = [];
            if (data.categories) {
                data.categories.forEach(cat => {
                    cat.colors.forEach(color => {
                        currentResults.push({ ...color, category: cat.key });
                    });
                });
            }

            renderResults(data);
        } catch (error) {
            console.error('Search error:', error);
            overlayResults.innerHTML = '';
            currentResults = [];
        }
    }

    function renderResults(data) {
        if (!data.categories || data.categories.length === 0) {
            if (overlayInput.value.trim().length >= 2) {
                overlayResults.innerHTML = `<div class="search-no-results">${noResultsText}</div>`;
            } else {
                overlayResults.innerHTML = '';
            }
            return;
        }

        let html = '';
        let globalIndex = 0;

        data.categories.forEach(category => {
            html += `<div class="search-category-section">`;
            html += `<div class="search-category-header">${category.name}</div>`;
            category.colors.forEach(color => {
                html += `
                    <a href="/${culture}/ral-colors/${color.slug}"
                       class="search-result-item"
                       role="option"
                       id="search-result-${globalIndex}"
                       data-index="${globalIndex}">
                        <span class="search-result-swatch ${color.needsDarkText ? 'dark' : ''}" style="background-color: ${color.hex};"></span>
                        <span class="search-result-text">
                            <span class="search-result-number">RAL ${color.number}</span>
                            <span class="search-result-name">${color.name || ''}</span>
                        </span>
                    </a>
                `;
                globalIndex++;
            });
            html += `</div>`;
        });

        overlayResults.innerHTML = html;
        activeIndex = -1;
    }

    function updateActiveDescendant() {
        const items = overlayResults.querySelectorAll('.search-result-item');
        items.forEach((item, index) => {
            item.classList.toggle('active', index === activeIndex);
        });
    }

    function navigateResults(direction) {
        const items = overlayResults.querySelectorAll('.search-result-item');
        if (items.length === 0) return;

        if (direction === 'down') {
            activeIndex = activeIndex < items.length - 1 ? activeIndex + 1 : 0;
        } else {
            activeIndex = activeIndex > 0 ? activeIndex - 1 : items.length - 1;
        }

        updateActiveDescendant();

        const activeItem = items[activeIndex];
        if (activeItem) {
            activeItem.scrollIntoView({ block: 'nearest' });
        }
    }

    function selectResult() {
        if (activeIndex >= 0 && currentResults[activeIndex]) {
            window.location.href = `/${culture}/ral-colors/${currentResults[activeIndex].slug}`;
        }
    }

    function handleKeydown(e) {
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                navigateResults('down');
                break;
            case 'ArrowUp':
                e.preventDefault();
                navigateResults('up');
                break;
            case 'Enter':
                e.preventDefault();
                if (activeIndex >= 0) {
                    selectResult();
                } else if (currentResults.length > 0) {
                    activeIndex = 0;
                    selectResult();
                }
                break;
            case 'Escape':
                closeOverlay();
                break;
        }
    }

    const debouncedSearch = debounce(fetchResults, 200);

    searchTrigger.addEventListener('click', openOverlay);

    // Keyboard shortcut: Cmd/Ctrl + K to open search
    document.addEventListener('keydown', (e) => {
        if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
            e.preventDefault();
            openOverlay();
        }
    });
})();
