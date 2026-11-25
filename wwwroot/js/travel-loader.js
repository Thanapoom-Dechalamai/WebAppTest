(() => {
    const loader = document.querySelector('#travel-loader');
    if (!loader) return;
    const dismiss = document.querySelector('#travel-dismiss');
    let fallback;
    function reset() {
        clearTimeout(fallback);
        loader.hidden = true;
        dismiss.hidden = true;
    }
    dismiss.addEventListener('click', reset);
    document.addEventListener('keydown', event => { if (event.key === 'Escape') reset(); });
    window.addEventListener('pageshow', reset);
    window.addEventListener('pagehide', reset);
    window.addEventListener('hashchange', reset);
    // Clear arrival state left by earlier versions; loading belongs to the outgoing page.
    try { sessionStorage.removeItem('lodgo-travel-arrival'); } catch {}
    document.addEventListener('click', event => {
        if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        const link = event.target.closest('a[href]');
        if (!link || link.hasAttribute('download') || (link.target && link.target !== '_self')) return;
        const url = new URL(link.href, location.href);
        if (url.origin !== location.origin || !/\/Stays\/Details(?:\/|$)/i.test(url.pathname)) return;
        reset();
        // A room/overview anchor only scrolls this document, so no new page will hide a loader.
        if (url.pathname === location.pathname && url.search === location.search) return;
        loader.hidden = false;
        dismiss.hidden = false;
        // Keep native navigation intact and recover if a request is cancelled or fails.
        fallback = setTimeout(reset, 8000);
    });
})();
