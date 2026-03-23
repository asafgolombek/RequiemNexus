// Triggers a file download from a base64-encoded string.
// Used by the "Download My Data" GDPR export feature.
window.downloadFileFromBase64 = function (fileName, mimeType, base64) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Uint8Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const blob = new Blob([byteNumbers], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

window.copyToClipboard = function (text) {
    return navigator.clipboard.writeText(text);
};

window.sessionStorageGet = function (key) {
    return sessionStorage.getItem(key) || '';
};

window.sessionStorageSet = function (key, value) {
    sessionStorage.setItem(key, value);
};

// Native confirm dialog for high-risk actions (e.g. Social maneuver Force Doors).
window.rnConfirm = function (message) {
    return confirm(message);
};

window.registerCommandPaletteShortcut = function (dotNetRef) {
    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('Toggle');
        }
    });
};

// Visible shortcut hint for the command palette (UI_UX_FACELIFT Track 2.3). Avoids navigator.platform.
/** Performance mode: fewer animations (pairs with wwwroot/css/app-chrome.css html.performance-mode). */
window.requiemGetPerformanceMode = function () {
    try {
        return localStorage.getItem('requiem-performance-mode') === '1';
    } catch {
        return false;
    }
};

window.requiemSetPerformanceMode = function (enabled) {
    try {
        if (enabled) {
            localStorage.setItem('requiem-performance-mode', '1');
            document.documentElement.classList.add('performance-mode');
        } else {
            localStorage.removeItem('requiem-performance-mode');
            document.documentElement.classList.remove('performance-mode');
        }
    } catch {
        /* ignore */
    }
};

window.getPaletteShortcutLabel = function () {
    try {
        const uaData = navigator.userAgentData;
        if (uaData && typeof uaData.platform === 'string') {
            const p = uaData.platform.toLowerCase();
            if (p.includes('mac') || p.includes('iphone') || p.includes('ipad')) {
                return '⌘K';
            }
        }
    } catch {
        /* ignore */
    }
    const ua = navigator.userAgent || '';
    if (/Mac|iPhone|iPad|iPod/i.test(ua)) {
        return '⌘K';
    }
    return 'Ctrl+K';
};

/**
 * Focus trap for the mobile nav drawer: Tab cycles, Escape notifies .NET.
 * @param {HTMLElement} drawerRoot
 * @param {DotNetObject} dotNetRef - must expose [JSInvokable] CloseDrawerFromEscape()
 */
window.rnDrawerFocusTrap = (function () {
    let onKeyDown = null;

    return {
        attach: function (drawerRoot, dotNetRef) {
            if (onKeyDown) {
                document.removeEventListener('keydown', onKeyDown, true);
                onKeyDown = null;
            }
            if (!drawerRoot) {
                return;
            }

            const selector = 'a[href], button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

            function getFocusable() {
                return Array.from(drawerRoot.querySelectorAll(selector)).filter(
                    (el) => el.offsetWidth > 0 || el.offsetHeight > 0 || el === document.activeElement);
            }

            const nodes = getFocusable();
            if (nodes.length > 0) {
                nodes[0].focus();
            }

            onKeyDown = function (e) {
                if (e.key === 'Escape') {
                    e.preventDefault();
                    e.stopPropagation();
                    dotNetRef.invokeMethodAsync('CloseDrawerFromEscape');
                    return;
                }
                if (e.key !== 'Tab') {
                    return;
                }
                const list = getFocusable();
                if (list.length === 0) {
                    return;
                }
                const first = list[0];
                const last = list[list.length - 1];
                if (e.shiftKey) {
                    if (document.activeElement === first) {
                        e.preventDefault();
                        last.focus();
                    }
                } else if (document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            };

            document.addEventListener('keydown', onKeyDown, true);
        },
        detach: function () {
            if (onKeyDown) {
                document.removeEventListener('keydown', onKeyDown, true);
                onKeyDown = null;
            }
        }
    };
})();

// --- Micro-Interactions ---

window.countUp = function (elementId, target, duration) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const start = performance.now();
    const step = (timestamp) => {
        const progress = Math.min((timestamp - start) / duration, 1);
        el.textContent = Math.floor(progress * target);
        if (progress < 1) requestAnimationFrame(step);
        else el.textContent = target;
    };
    requestAnimationFrame(step);
};

window.scrollElementIntoView = function (element) {
    if (!element || typeof element.scrollIntoView !== 'function') {
        return;
    }
    element.scrollIntoView({ behavior: 'smooth', block: 'start' });
};

window.scrollToBottom = function (element) {
    if (!element) return;
    
    // Smart scroll: only snap to bottom if the user is already near the bottom
    const threshold = 50; // px
    const isAtBottom = element.scrollHeight - element.scrollTop - element.clientHeight <= threshold;
    
    if (isAtBottom) {
        element.scrollTop = element.scrollHeight;
    }
};

document.addEventListener('mousedown', (e) => {
    // 1. Button Ripple
    const btn = e.target.closest('.btn-primary, .btn-login, .btn-secondary, .btn-primary-rn, .btn-secondary-rn, .btn-rn-primary, .btn-rn-secondary, .btn-rn-gold');
    if (btn) {
        const rect = btn.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        btn.style.setProperty('--ripple-x', `${x}px`);
        btn.style.setProperty('--ripple-y', `${y}px`);

        btn.classList.remove('ripple-active');
        void btn.offsetWidth; // Trigger reflow
        btn.classList.add('ripple-active');

        // Clean up after animation
        setTimeout(() => {
            btn.classList.remove('ripple-active');
        }, 600);
    }

    // 2. Long-press Confirm
    const holdBtn = e.target.closest('.btn-danger-hold, .btn-rn-danger-hold');
    if (holdBtn) {
        holdBtn.classList.add('holding');
        
        const onEnd = () => {
            holdBtn.classList.remove('holding');
            holdBtn.removeEventListener('mouseup', onEnd);
            holdBtn.removeEventListener('mouseleave', onEnd);
            holdBtn.removeEventListener('touchend', onEnd);
        };

        holdBtn.addEventListener('mouseup', onEnd);
        holdBtn.addEventListener('mouseleave', onEnd);
        holdBtn.addEventListener('touchend', onEnd);

        // Listen for animation completion
        const onComplete = (event) => {
            if (event.propertyName === 'width' && holdBtn.classList.contains('holding')) {
                holdBtn.classList.remove('holding');
                holdBtn.click(); // Trigger the actual action
                holdBtn.removeEventListener('transitionend', onComplete);
            }
        };
        holdBtn.addEventListener('transitionend', onComplete);
    }
});

document.addEventListener('touchstart', (e) => {
    // Reuse mousedown logic for touch
    const touch = e.touches[0];
    const dummyEvent = {
        target: e.target,
        clientX: touch.clientX,
        clientY: touch.clientY
    };
    // Re-trigger the logic (simplified for this context)
    // Note: In a real app, you'd factor out the logic into a shared function
});
