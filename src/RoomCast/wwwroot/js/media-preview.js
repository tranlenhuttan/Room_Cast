(() => {
    const overlayRoot = document.querySelector('[data-preview-root]');
    if (!overlayRoot) {
        return;
    }

    const cache = new Map();
    let activeCleanup = null;
    let activeTrigger = null;

    const focusableSelectors = [
        'a[href]',
        'area[href]',
        'button:not([disabled])',
        'input:not([disabled]):not([type="hidden"])',
        'select:not([disabled])',
        'textarea:not([disabled])',
        '[tabindex]:not([tabindex="-1"])'
    ].join(',');

    const unlockScroll = () => {
        document.body.classList.remove('overflow-hidden');
    };

    const closeOverlay = () => {
        if (activeCleanup) {
            activeCleanup();
            activeCleanup = null;
        }

        overlayRoot.innerHTML = '';
        overlayRoot.classList.add('hidden');
        unlockScroll();

        if (activeTrigger && typeof activeTrigger.focus === 'function') {
            activeTrigger.focus();
        }
        activeTrigger = null;
    };

    const setupFocusTrap = (dialogElement) => {
        const focusable = Array.from(dialogElement.querySelectorAll(focusableSelectors))
            .filter(el => !el.hasAttribute('disabled') && el.tabIndex !== -1);

        const first = focusable[0] ?? dialogElement;
        const last = focusable[focusable.length - 1] ?? dialogElement;

        const handleKeyDown = (event) => {
            if (event.key === 'Tab') {
                if (focusable.length === 0) {
                    event.preventDefault();
                    dialogElement.focus();
                    return;
                }

                if (event.shiftKey) {
                    if (document.activeElement === first) {
                        event.preventDefault();
                        last.focus();
                    }
                } else if (document.activeElement === last) {
                    event.preventDefault();
                    first.focus();
                }
            }

            if (event.key === 'Escape') {
                event.preventDefault();
                closeOverlay();
            }
        };

        dialogElement.addEventListener('keydown', handleKeyDown);
        first.focus();

        return () => {
            dialogElement.removeEventListener('keydown', handleKeyDown);
        };
    };

    const openOverlay = (html, triggerElement) => {
        overlayRoot.innerHTML = html;
        overlayRoot.classList.remove('hidden');
        document.body.classList.add('overflow-hidden');

        const backdrop = overlayRoot.querySelector('[data-preview-overlay]');
        const dialog = backdrop?.querySelector('.media-preview-dialog');
        const closeButton = overlayRoot.querySelector('[data-preview-close]');

        if (!backdrop || !dialog) {
            console.error('Media preview overlay is missing required elements.');
            return;
        }

        const cleanupCallbacks = [];

        const backdropClickHandler = (event) => {
            if (event.target === backdrop) {
                closeOverlay();
            }
        };
        backdrop.addEventListener('mousedown', backdropClickHandler);
        cleanupCallbacks.push(() => backdrop.removeEventListener('mousedown', backdropClickHandler));

        if (closeButton) {
            const closeClickHandler = () => closeOverlay();
            closeButton.addEventListener('click', closeClickHandler);
            cleanupCallbacks.push(() => closeButton.removeEventListener('click', closeClickHandler));
        }

        cleanupCallbacks.push(setupFocusTrap(dialog));

        activeCleanup = () => {
            cleanupCallbacks.forEach(detach => detach());
        };
        activeTrigger = triggerElement;
    };

    const fetchPreview = async (id, triggerElement) => {
        if (!id) {
            return;
        }

        if (cache.has(id)) {
            openOverlay(cache.get(id), triggerElement);
            return;
        }

        try {
            const response = await fetch(`/MediaFiles/Preview/${encodeURIComponent(id)}`, {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error(`Preview failed with status ${response.status}`);
            }

            const html = await response.text();
            cache.set(id, html);
            openOverlay(html, triggerElement);
        }
        catch (error) {
            console.error('Failed to load preview', error);
            alert('Sorry, we could not open the preview. Please try again later.');
        }
    };

    document.addEventListener('click', (event) => {
        const trigger = event.target.closest('[data-preview-trigger]');
        if (!trigger) {
            return;
        }

        const fileId = trigger.getAttribute('data-preview-id');
        if (!fileId) {
            return;
        }

        event.preventDefault();
        fetchPreview(fileId, trigger);
    });

    document.addEventListener('mediaPreview:close', closeOverlay);
})();
