(function() {
    const hamburger = document.querySelector('.nav-hamburger');
    const navMenu = document.querySelector('.nav-menu');
    const dropdownToggles = document.querySelectorAll('.nav-dropdown-toggle');

    // Hamburger menu toggle
    hamburger?.addEventListener('click', function() {
        const expanded = this.getAttribute('aria-expanded') === 'true';
        this.setAttribute('aria-expanded', !expanded);
        navMenu.classList.toggle('open', !expanded);
    });

    // Dropdown toggles
    dropdownToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            e.stopPropagation();
            const expanded = this.getAttribute('aria-expanded') === 'true';

            // Close other dropdowns
            dropdownToggles.forEach(other => {
                if (other !== this) {
                    other.setAttribute('aria-expanded', 'false');
                }
            });

            this.setAttribute('aria-expanded', !expanded);
        });
    });

    // Close dropdowns when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.nav-dropdown')) {
            dropdownToggles.forEach(toggle => {
                toggle.setAttribute('aria-expanded', 'false');
            });
        }
    });

    // Close on Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            dropdownToggles.forEach(toggle => {
                toggle.setAttribute('aria-expanded', 'false');
            });
            hamburger?.setAttribute('aria-expanded', 'false');
            navMenu?.classList.remove('open');
        }
    });

    // Close mobile menu when clicking a link
    navMenu?.querySelectorAll('a').forEach(link => {
        link.addEventListener('click', function() {
            hamburger?.setAttribute('aria-expanded', 'false');
            navMenu.classList.remove('open');
        });
    });
})();
