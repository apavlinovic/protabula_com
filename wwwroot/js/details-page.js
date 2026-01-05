/**
 * Details Page JavaScript
 * Handles modal preview, 3D preview controls, and clipboard functionality
 */
(function() {
    'use strict';

    // Cache busting version - update when JS files change
    const JS_VERSION = '2.4.0';

    let lastFocusedElement = null;
    let preview = null;
    let threeJsLoaded = false;

    // ===========================================
    // Modal Functions
    // ===========================================

    function openColorPreview() {
        lastFocusedElement = document.activeElement;
        const modal = document.getElementById('colorPreview');
        if (!modal) return;

        modal.classList.add('active', 'fullscreen');
        document.body.style.overflow = 'hidden';
        modal.querySelector('.color-preview-close')?.focus();
    }

    function openColorPreviewWithBg(bgColor) {
        const modal = document.getElementById('colorPreview');
        if (!modal) return;

        modal.style.backgroundColor = bgColor;
        modal.querySelectorAll('.bg-option').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.bg === bgColor);
        });

        lastFocusedElement = document.activeElement;
        modal.classList.add('active');
        modal.classList.remove('fullscreen');
        document.body.style.overflow = 'hidden';
        modal.querySelector('.color-preview-close')?.focus();
    }

    function closeColorPreview(event) {
        if (event.target.classList.contains('color-preview-overlay') ||
            event.target.classList.contains('color-preview-fullscreen') ||
            event.target.classList.contains('color-preview-close')) {
            closeModal();
        }
    }

    function closeModal() {
        const modal = document.getElementById('colorPreview');
        if (!modal) return;

        modal.classList.remove('active', 'fullscreen');
        document.body.style.overflow = '';
        if (lastFocusedElement) {
            lastFocusedElement.focus();
        }
    }

    function changePreviewBg(color, button) {
        const modal = document.getElementById('colorPreview');
        if (!modal) return;

        modal.style.backgroundColor = color;
        modal.querySelectorAll('.bg-option').forEach(btn => btn.classList.remove('active'));
        button.classList.add('active');
        event.stopPropagation();
    }

    // ===========================================
    // Clipboard Functions
    // ===========================================

    async function copyImageUrl(button) {
        const url = button.dataset.url;
        const originalText = button.textContent;
        const copiedText = button.dataset.copiedText;

        try {
            await navigator.clipboard.writeText(url);
            button.textContent = copiedText;
            button.disabled = true;
            setTimeout(() => {
                button.textContent = originalText;
                button.disabled = false;
            }, 2000);
        } catch (err) {
            console.error('Failed to copy URL:', err);
            // Fallback for older browsers
            const textarea = document.createElement('textarea');
            textarea.value = url;
            textarea.style.position = 'fixed';
            textarea.style.opacity = '0';
            document.body.appendChild(textarea);
            textarea.select();
            try {
                document.execCommand('copy');
                button.textContent = copiedText;
                button.disabled = true;
                setTimeout(() => {
                    button.textContent = originalText;
                    button.disabled = false;
                }, 2000);
            } catch (e) {
                console.error('Fallback copy failed:', e);
            }
            document.body.removeChild(textarea);
        }
    }

    // ===========================================
    // 3D Preview Functions
    // ===========================================

    function getPreviewConfig() {
        const container = document.getElementById('ral-preview-canvas');
        if (!container) return null;

        return {
            container: container,
            colorHex: container.dataset.colorHex || '#888888',
            colorLrv: parseFloat(container.dataset.colorLrv) || 50,
            undertoneHex: container.dataset.undertoneHex || null,
            undertoneStrength: parseFloat(container.dataset.undertoneStrength) || 0
        };
    }

    function initPreview() {
        const config = getPreviewConfig();
        if (!config || typeof RalPreview === 'undefined' || typeof THREE === 'undefined') return;

        preview = RalPreview.create(config.container, {
            model: 'sphere',
            finish: 'satin',
            color: {
                hex: config.colorHex,
                undertoneHex: config.undertoneHex,
                undertoneStrength: config.undertoneStrength
            },
            lrv: config.colorLrv,
            controls: { orbit: true, autoRotate: true, zoom: true, pan: false },
            background: 'transparent',
            powder: { intensity: 0.25, scale: 1.0 }
        });

        setupPreviewControls(config);
    }

    function setupPreviewControls(config) {
        const controlsContainer = document.querySelector('.specular-controls');
        if (!controlsContainer) return;

        // Event delegation for finish, model, and lighting buttons
        controlsContainer.addEventListener('click', (e) => {
            const finishBtn = e.target.closest('[data-finish]');
            const modelBtn = e.target.closest('[data-model]');
            const lightingBtn = e.target.closest('[data-lighting]');

            if (finishBtn) {
                handleFinishChange(finishBtn);
            } else if (modelBtn) {
                handleModelChange(modelBtn, config);
            } else if (lightingBtn) {
                handleLightingChange(lightingBtn);
            }
        });
    }

    function handleFinishChange(btn) {
        if (!preview) return;

        document.querySelector('[data-finish].active')?.classList.remove('active');
        btn.classList.add('active');
        preview.setFinish(btn.dataset.finish);
    }

    function handleLightingChange(btn) {
        if (!preview) return;

        document.querySelector('[data-lighting].active')?.classList.remove('active');
        btn.classList.add('active');
        preview.setLighting(btn.dataset.lighting);
    }

    function handleModelChange(btn, config) {
        document.querySelector('[data-model].active')?.classList.remove('active');
        btn.classList.add('active');

        // Read current UI state before disposing
        const currentFinish = document.querySelector('[data-finish].active')?.dataset.finish || 'satin';
        const currentLighting = document.querySelector('[data-lighting].active')?.dataset.lighting || 'studio';

        // Dispose and recreate with new model
        if (preview) {
            preview.dispose();
        }

        preview = RalPreview.create(config.container, {
            model: btn.dataset.model,
            finish: currentFinish,
            color: {
                hex: config.colorHex,
                undertoneHex: config.undertoneHex,
                undertoneStrength: config.undertoneStrength
            },
            lrv: config.colorLrv,
            controls: { orbit: true, autoRotate: true, zoom: true, pan: false },
            background: 'transparent',
            powder: { intensity: 0.25, scale: 1.0 }
        });

        // Apply the lighting preset that was selected before the model change
        if (currentLighting !== 'studio') {
            preview.setLighting(currentLighting);
        }
    }

    function loadThreeJs() {
        if (threeJsLoaded) return;
        threeJsLoaded = true;

        // Three.js is loaded via module, wait for ready event
        window.addEventListener('three-ready', function() {
            const script = document.createElement('script');
            script.src = '/js/ral-preview.js?v=' + JS_VERSION;
            script.onload = initPreview;
            document.head.appendChild(script);
        });

        // Load the Three.js loader module
        const loader = document.createElement('script');
        loader.type = 'module';
        loader.src = '/js/three-loader.js?v=' + JS_VERSION;
        document.head.appendChild(loader);
    }

    // ===========================================
    // Event Listeners Setup
    // ===========================================

    function setupModalEvents() {
        const modal = document.getElementById('colorPreview');
        if (!modal) return;

        // Focus trap for modal
        modal.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                const focusable = modal.querySelectorAll('button:not([style*="display: none"]):not([disabled])');
                if (focusable.length === 0) return;

                const first = focusable[0];
                const last = focusable[focusable.length - 1];

                if (e.shiftKey && document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            }
        });

        // Close on Escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                closeModal();
            }
        });
    }

    function setupContrastSegments() {
        const contrastStrip = document.querySelector('.contrast-strip');
        if (!contrastStrip) return;

        // Event delegation for contrast segments
        contrastStrip.addEventListener('click', (e) => {
            const segment = e.target.closest('.contrast-segment');
            if (segment && segment.dataset.bg) {
                openColorPreviewWithBg(segment.dataset.bg);
            }
        });

        contrastStrip.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                const segment = e.target.closest('.contrast-segment');
                if (segment && segment.dataset.bg) {
                    e.preventDefault();
                    openColorPreviewWithBg(segment.dataset.bg);
                }
            }
        });
    }

    function setupColorPreviewTriggers() {
        // Event delegation for color preview triggers
        document.addEventListener('click', (e) => {
            const trigger = e.target.closest('[data-action="open-preview"]');
            if (trigger) {
                openColorPreview();
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                const trigger = e.target.closest('[data-action="open-preview"]');
                if (trigger) {
                    e.preventDefault();
                    openColorPreview();
                }
            }
        });
    }

    function setupLazyThreeJs() {
        const previewCanvas = document.getElementById('ral-preview-canvas');
        if (!previewCanvas) return;

        // Use IntersectionObserver to load Three.js only when visible
        if ('IntersectionObserver' in window) {
            const observer = new IntersectionObserver((entries) => {
                if (entries[0].isIntersecting) {
                    loadThreeJs();
                    observer.disconnect();
                }
            }, { rootMargin: '200px' });

            observer.observe(previewCanvas);
        } else {
            // Fallback: load immediately
            loadThreeJs();
        }
    }

    // ===========================================
    // Initialize
    // ===========================================

    function init() {
        setupModalEvents();
        setupContrastSegments();
        setupColorPreviewTriggers();
        setupLazyThreeJs();
    }

    // Run on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose minimal API for inline event handlers (temporary, for backwards compatibility)
    window.DetailsPage = {
        openColorPreview,
        openColorPreviewWithBg,
        closeColorPreview,
        closeModal,
        changePreviewBg,
        copyImageUrl
    };

})();
