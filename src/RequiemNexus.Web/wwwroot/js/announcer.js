// Screen reader live-region announcements (Phase 13). Loaded as an ES module from Blazor.
export function announce(message, priority = 'polite') {
    const el = document.getElementById(
        priority === 'assertive' ? 'rn-assertive-announcer' : 'rn-polite-announcer'
    );
    if (!el) {
        return;
    }
    el.textContent = '';
    requestAnimationFrame(() => {
        el.textContent = message;
    });
}
