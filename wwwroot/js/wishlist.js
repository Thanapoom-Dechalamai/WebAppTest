// Heart buttons: send the same form with fetch so the page does not reload.
// If fetch fails for any reason the form is submitted normally (it works without JavaScript).
document.addEventListener('submit', async (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.classList.contains('wishlist-form')) return;
    event.preventDefault();
    const button = form.querySelector('button');
    if (button.disabled) return;
    button.disabled = true;
    try {
        const response = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'fetch' },
            credentials: 'same-origin'
        });
        if (!response.ok) throw new Error(String(response.status));
        const { liked } = await response.json();
        const hotelId = form.querySelector('input[name="hotelId"]').value;
        // The same hotel can appear more than once on a page; keep every heart in step.
        document.querySelectorAll('form.wishlist-form').forEach((other) => {
            if (other.querySelector('input[name="hotelId"]').value !== hotelId) return;
            const b = other.querySelector('button');
            const label = liked ? b.dataset.labelOn : b.dataset.labelOff;
            b.setAttribute('aria-pressed', liked ? 'true' : 'false');
            b.setAttribute('aria-label', label);
            b.title = label;
        });
    } catch {
        form.submit();
    } finally {
        button.disabled = false;
    }
});
