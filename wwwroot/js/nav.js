(function() {
    'use strict';

    const hamburger = document.querySelector('.nav-hamburger');
    const navDropdownMenu = document.querySelector('.nav-dropdown-menu');
    const dropdownToggle = document.querySelector('.nav-dropdown-toggle');
    const logo = document.querySelector('.nav-logo');

    let menuOverlay = null;

    function createMenuOverlay() {
        if (document.getElementById('menu-overlay')) {
            menuOverlay = document.getElementById('menu-overlay');
            return;
        }

        menuOverlay = document.createElement('div');
        menuOverlay.id = 'menu-overlay';
        menuOverlay.className = 'overlay menu-overlay';

        // Clone and transform the dropdown menu content
        const menuContent = navDropdownMenu ? transformMenuContent(navDropdownMenu.innerHTML) : '';

        menuOverlay.innerHTML = `
            <div class="overlay-header">
                <a class="nav-logo" href="${logo?.href || '/'}">
                    <img src="${logo?.querySelector('img')?.src || '/logo.svg'}" alt="Protabula" height="28" />
                </a>
                <button class="overlay-close" aria-label="Close">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M18 6 6 18"/>
                        <path d="m6 6 12 12"/>
                    </svg>
                </button>
            </div>
            <div class="overlay-content">
                ${menuContent}
            </div>
        `;
        document.body.appendChild(menuOverlay);

        const closeBtn = menuOverlay.querySelector('.overlay-close');
        closeBtn.addEventListener('click', closeMenuOverlay);

        // Close when clicking a link
        menuOverlay.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', closeMenuOverlay);
        });

        // Close when clicking outside content
        menuOverlay.addEventListener('click', (e) => {
            if (e.target === menuOverlay) {
                closeMenuOverlay();
            }
        });
    }

    // Transform old class names to new ones
    function transformMenuContent(html) {
        return html
            .replace(/dropdown-section/g, 'menu-section')
            .replace(/dropdown-section-title/g, 'menu-section-title');
    }

    function openMenuOverlay() {
        createMenuOverlay();
        menuOverlay.classList.add('active');
        document.body.style.overflow = 'hidden';
        hamburger?.setAttribute('aria-expanded', 'true');
        dropdownToggle?.setAttribute('aria-expanded', 'true');
    }

    function closeMenuOverlay() {
        if (menuOverlay) {
            menuOverlay.classList.remove('active');
            document.body.style.overflow = '';
            hamburger?.setAttribute('aria-expanded', 'false');
            dropdownToggle?.setAttribute('aria-expanded', 'false');
        }
    }

    // Hamburger menu toggle (mobile)
    hamburger?.addEventListener('click', function() {
        const expanded = this.getAttribute('aria-expanded') === 'true';
        if (expanded) {
            closeMenuOverlay();
        } else {
            openMenuOverlay();
        }
    });

    // Desktop dropdown toggle - opens overlay
    dropdownToggle?.addEventListener('click', function(e) {
        e.stopPropagation();
        const expanded = this.getAttribute('aria-expanded') === 'true';
        if (expanded) {
            closeMenuOverlay();
        } else {
            openMenuOverlay();
        }
    });

    // Close on Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            closeMenuOverlay();
        }
    });
})();
