/**
 * Initialize a color autocomplete component.
 * @param {string} containerId - The ID of the autocomplete container element
 * @param {function} onSelect - Callback when a color is selected: (number, hex, name) => void
 * @returns {function} Cleanup function to remove event listeners
 */
function initColorAutocomplete(containerId, onSelect) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.warn(`Autocomplete container not found: ${containerId}`);
        return () => {};
    }

    const input = container.querySelector('.autocomplete-input');
    const dropdown = container.querySelector('.autocomplete-dropdown');
    const items = container.querySelectorAll('.autocomplete-item');
    const sections = container.querySelectorAll('.autocomplete-section');
    let activeIndex = -1;

    function showDropdown() {
        dropdown.classList.add('active');
        filterColors(input.value);
    }

    function hideDropdown() {
        dropdown.classList.remove('active');
        activeIndex = -1;
        updateActiveItem([]);
    }

    function filterColors(query) {
        const searchTerm = query.toLowerCase().trim();

        // Filter individual items
        items.forEach(item => {
            const number = (item.dataset.number || '').toLowerCase();
            const name = item.dataset.searchName || '';
            const nameDe = item.dataset.searchNameDe || '';

            const matches = searchTerm === '' ||
                number.includes(searchTerm) ||
                name.includes(searchTerm) ||
                nameDe.includes(searchTerm);

            item.style.display = matches ? '' : 'none';
        });

        // Show/hide section headers based on whether any items in that section are visible
        sections.forEach(section => {
            const sectionItems = section.querySelectorAll('.autocomplete-item');
            const hasVisibleItems = Array.from(sectionItems).some(item => item.style.display !== 'none');
            section.style.display = hasVisibleItems ? '' : 'none';
        });

        activeIndex = -1;
        updateActiveItem([]);
    }

    function selectColor(item) {
        const number = item.dataset.number;
        const hex = item.dataset.hex;
        const name = item.dataset.name || '';

        input.value = '';
        hideDropdown();

        if (onSelect) {
            onSelect(number, hex, name);
        }
    }

    function getVisibleItems() {
        return Array.from(items).filter(item => item.style.display !== 'none');
    }

    function updateActiveItem(visibleItems) {
        items.forEach(item => item.classList.remove('active'));
        if (visibleItems && activeIndex >= 0 && visibleItems[activeIndex]) {
            visibleItems[activeIndex].classList.add('active');
            visibleItems[activeIndex].scrollIntoView({ block: 'nearest' });
        }
    }

    function handleKeydown(event) {
        const visibleItems = getVisibleItems();

        if (event.key === 'ArrowDown') {
            event.preventDefault();
            if (!dropdown.classList.contains('active')) {
                showDropdown();
            }
            activeIndex = Math.min(activeIndex + 1, visibleItems.length - 1);
            updateActiveItem(visibleItems);
        } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            activeIndex = Math.max(activeIndex - 1, -1);
            updateActiveItem(visibleItems);
        } else if (event.key === 'Enter') {
            event.preventDefault();
            if (activeIndex >= 0 && visibleItems[activeIndex]) {
                selectColor(visibleItems[activeIndex]);
            }
        } else if (event.key === 'Escape') {
            hideDropdown();
            input.blur();
        }
    }

    function handleInput() {
        if (!dropdown.classList.contains('active')) {
            showDropdown();
        }
        filterColors(input.value);
    }

    function handleItemClick(event) {
        const item = event.target.closest('.autocomplete-item');
        if (item) {
            selectColor(item);
        }
    }

    function handleDocumentClick(event) {
        if (!event.target.closest('.autocomplete-container') ||
            !container.contains(event.target)) {
            hideDropdown();
        }
    }

    // Attach event listeners
    input.addEventListener('focus', showDropdown);
    input.addEventListener('input', handleInput);
    input.addEventListener('keydown', handleKeydown);
    dropdown.addEventListener('click', handleItemClick);
    document.addEventListener('click', handleDocumentClick);

    // Return cleanup function
    return function cleanup() {
        input.removeEventListener('focus', showDropdown);
        input.removeEventListener('input', handleInput);
        input.removeEventListener('keydown', handleKeydown);
        dropdown.removeEventListener('click', handleItemClick);
        document.removeEventListener('click', handleDocumentClick);
    };
}
