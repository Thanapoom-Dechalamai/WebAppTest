(() => {
    const context = document.querySelector('meta[name="lodgo-account"]');
    if (!context) return;
    const renderedAccount = context.content;
    const key = 'lodgo-active-account';
    let checking = false;
    function reloadAccount() {
        // Hide a restored document before requesting the current account's page.
        document.documentElement.style.visibility = 'hidden';
        location.reload();
    }
    async function verifyAccount() {
        if (checking || document.visibilityState === 'hidden') return;
        checking = true;
        try {
            const response = await fetch(context.dataset.sessionUrl, {
                credentials: 'same-origin', cache: 'no-store',
                headers: { Accept: 'application/json' }
            });
            if (response.ok && (await response.json()).accountId !== renderedAccount) reloadAccount();
        } catch { /* A disconnected tab can retry when it next gains focus. */ }
        finally { checking = false; }
    }
    try { localStorage.setItem(key, renderedAccount); } catch {}
    window.addEventListener('storage', event => {
        if (event.key === key && event.newValue !== renderedAccount) verifyAccount();
    });
    window.addEventListener('focus', verifyAccount);
    document.addEventListener('visibilitychange', verifyAccount);
    window.addEventListener('pageshow', event => { if (event.persisted) reloadAccount(); });
})();
