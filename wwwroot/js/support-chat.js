(() => {
    const feed = document.querySelector("[data-chat-feed]");
    if (!feed) return;
    const label = document.querySelector("[data-chat-label]");
    const statuses = { Open: "รอ support ตอบ", Answered: "support ตอบแล้ว", Closed: "ปิดเรื่องแล้ว" };
    feed.scrollTop = feed.scrollHeight;
    let previous = feed.querySelector("[data-chat-status]")?.outerHTML;
    let busy = false;
    async function refresh() {
        if (busy || document.hidden) return;
        busy = true;
        try {
            const response = await fetch(feed.dataset.chatFeed, { cache: "no-store" });
            if (!response.ok || response.redirected) return;
            const fragment = new DOMParser().parseFromString(await response.text(), "text/html").querySelector("[data-chat-status]");
            if (!fragment || fragment.outerHTML === previous) return;
            const nearBottom = feed.scrollHeight - feed.scrollTop - feed.clientHeight < 48;
            previous = fragment.outerHTML;
            feed.replaceChildren(fragment);
            if (label) label.textContent = statuses[fragment.dataset.chatStatus] ?? "";
            if (fragment.dataset.chatStatus === "Closed") {
                const composer = document.querySelector(".chat-composer");
                if (composer) {
                    for (const control of composer.querySelectorAll("textarea, input, button")) control.disabled = true;
                }
            }
            if (nearBottom) feed.scrollTop = feed.scrollHeight;
        } catch {
            // Preserve the current thread while the network is unavailable.
        } finally {
            busy = false;
        }
    }
    window.setInterval(refresh, 4000);
    document.addEventListener("visibilitychange", refresh);
})();
