// Search bar pop-ups (2026-11-22): a two-month date-range calendar and a guests / rooms stepper.
// Progressive enhancement: without this script the native date inputs and guest select still work.
// The real #checkin / #checkout inputs stay in the form (hidden) and receive the chosen dates, so stay.js keeps validating them.
(() => {
    const form = document.querySelector('#hotel-search');
    const checkin = document.querySelector('#checkin');
    const checkout = document.querySelector('#checkout');
    const guestSelect = document.querySelector('#guests');
    if (!form || !checkin || !checkout) return;
    form.classList.add('has-pickers');

    const pad = n => String(n).padStart(2, '0');
    const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
    const parse = s => { if (!s) return null; const [y, m, d] = s.split('-').map(Number); return new Date(y, m - 1, d); };
    const addDays = (d, n) => new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);
    const locale = 'th-TH-u-ca-gregory';
    const monthName = d => d.toLocaleDateString(locale, { month: 'long', year: 'numeric' });
    const shortDate = d => d.toLocaleDateString(locale, { day: 'numeric', month: 'short' });
    const longDate = d => d.toLocaleDateString(locale, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    const weekdays = ['จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.', 'อา.'];
    const today = (() => { const n = new Date(); return new Date(n.getFullYear(), n.getMonth(), n.getDate()); })();
    const icon = path => `<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">${path}</svg>`;
    let openPop = null;

    function close(pop, focusTrigger) {
        if (!pop || pop.hidden) return;
        pop.hidden = true;
        pop._trigger.setAttribute('aria-expanded', 'false');
        if (focusTrigger) pop._trigger.focus();
        if (openPop === pop) openPop = null;
    }
    // Pop-ups are placed with fixed positioning under their field, so containers with overflow:hidden cannot clip them
    const wide = window.matchMedia('(min-width: 701px)');
    function place(pop) {
        if (!wide.matches) { pop.style.top = pop.style.left = ''; return; }
        const r = pop.parentElement.getBoundingClientRect(), w = pop.offsetWidth;
        const left = pop.classList.contains('guest-pop') ? r.right - w : r.left;
        pop.style.left = Math.max(16, Math.min(left, window.innerWidth - w - 16)) + 'px';
        pop.style.top = (r.bottom + 8) + 'px';
    }
    window.addEventListener('scroll', () => openPop && place(openPop), { passive: true });
    window.addEventListener('resize', () => openPop && place(openPop));
    function open(pop) {
        if (openPop && openPop !== pop) close(openPop, false);
        pop.hidden = false;
        pop._trigger.setAttribute('aria-expanded', 'true');
        openPop = pop;
        pop._onOpen?.();
        place(pop);
        // Not enough room below the field: scroll the page just enough to show the whole pop-up
        const over = pop.getBoundingClientRect().bottom - window.innerHeight + 16;
        if (wide.matches && over > 0) window.scrollBy({ top: over, behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
    }
    // The event path is fixed when the click starts, so a day button that re-rendering removed still counts as inside
    document.addEventListener('click', e => { if (!openPop) return; const path = e.composedPath(); if (!path.includes(openPop) && !path.includes(openPop._trigger)) close(openPop, false); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape' && openPop) close(openPop, true); });
    // Keyboard users tabbing past the pop-up close it
    document.addEventListener('focusin', e => { if (openPop && !openPop.contains(e.target) && !openPop._trigger.contains(e.target)) close(openPop, false); });

    function field(cls, label, wrapperOf) {
        const wrap = document.createElement('div');
        wrap.className = 'search-field picker-field ' + cls;
        wrap.innerHTML = `<span class="picker-label" id="${cls}-label">${label}</span><button type="button" class="picker-trigger" aria-haspopup="dialog" aria-expanded="false" aria-labelledby="${cls}-label ${cls}-value"><span class="picker-value" id="${cls}-value"></span></button>`;
        wrapperOf.before(wrap);
        return wrap;
    }

    /* ---------- dates ---------- */
    const inField = checkin.closest('.search-field'), outField = checkout.closest('.search-field');
    const dates = field('dates-field', 'เช็กอิน – เช็กเอาต์', inField);
    inField.style.display = outField.style.display = 'none';
    const datesValue = dates.querySelector('.picker-value');
    const cal = document.createElement('div');
    cal.className = 'picker-pop date-pop'; cal.id = 'date-pop'; cal.hidden = true;
    cal.setAttribute('role', 'dialog'); cal.setAttribute('aria-label', 'เลือกวันเช็กอินและเช็กเอาต์');
    cal._trigger = dates.querySelector('.picker-trigger');
    cal._trigger.setAttribute('aria-controls', cal.id);
    dates.append(cal);

    let start = parse(checkin.value), end = parse(checkout.value), hover = null;
    let view = new Date((start || today).getFullYear(), (start || today).getMonth(), 1);
    let focusDay = start || today;

    function writeInputs() {
        checkin.value = start ? iso(start) : '';
        checkout.value = end ? iso(end) : '';
        checkin.dispatchEvent(new Event('change', { bubbles: true }));
        checkout.dispatchEvent(new Event('change', { bubbles: true }));
        const nights = start && end ? Math.round((end - start) / 86400000) : 0;
        datesValue.textContent = start && end ? `${shortDate(start)} – ${shortDate(end)} · ${nights} คืน` : start ? `${shortDate(start)} – เลือกวันเช็กเอาต์` : 'เลือกวันที่';
        dates.classList.toggle('has-value', Boolean(start));
    }
    function monthGrid(first) {
        const lead = (first.getDay() + 6) % 7; // Monday first
        const days = new Date(first.getFullYear(), first.getMonth() + 1, 0).getDate();
        let cells = '';
        for (let i = 0; i < lead; i++) cells += '<span class="cal-blank" aria-hidden="true"></span>';
        for (let d = 1; d <= days; d++) {
            const day = new Date(first.getFullYear(), first.getMonth(), d), key = iso(day);
            const past = day < today;
            const rangeEnd = end || (start && hover && hover > start ? hover : null);
            const cls = ['cal-day'];
            if (start && key === iso(start)) cls.push('is-start');
            if (rangeEnd && key === iso(rangeEnd)) cls.push('is-end');
            if (start && rangeEnd && day > start && day < rangeEnd) cls.push('in-range');
            if (key === iso(today)) cls.push('is-today');
            const selected = (start && key === iso(start)) || (end && key === iso(end));
            cells += `<button type="button" class="${cls.join(' ')}" data-date="${key}" tabindex="${key === iso(focusDay) ? 0 : -1}" aria-label="${longDate(day)}${key === iso(today) ? ' (วันนี้)' : ''}"${selected ? ' aria-pressed="true"' : ''}${past ? ' disabled' : ''}>${d}</button>`;
        }
        return `<div class="cal-month"><h3 class="cal-title">${monthName(first)}</h3><div class="cal-week" aria-hidden="true">${weekdays.map(w => `<span>${w}</span>`).join('')}</div><div class="cal-grid">${cells}</div></div>`;
    }
    function renderCal(keepFocus) {
        const second = new Date(view.getFullYear(), view.getMonth() + 1, 1);
        const atStart = view.getFullYear() === today.getFullYear() && view.getMonth() === today.getMonth();
        const nights = start && end ? Math.round((end - start) / 86400000) : 0;
        const hint = !start ? 'เลือกวันเช็กอิน' : !end ? 'เลือกวันเช็กเอาต์' : `${nights} คืน · ${shortDate(start)} – ${shortDate(end)}`;
        cal.innerHTML = `
            <div class="cal-nav">
                <button type="button" class="cal-arrow" data-move="-1" aria-label="เดือนก่อน"${atStart ? ' disabled' : ''}>${icon('<path d="M15 6l-6 6 6 6"/>')}</button>
                <p class="cal-hint" aria-live="polite">${hint}</p>
                <button type="button" class="cal-arrow" data-move="1" aria-label="เดือนถัดไป">${icon('<path d="M9 6l6 6-6 6"/>')}</button>
            </div>
            <div class="cal-months">${monthGrid(view)}${monthGrid(second)}</div>
            <div class="picker-foot"><button type="button" class="picker-link" data-clear>ล้างวันที่</button><button type="button" class="stay-button picker-done" data-done>เสร็จ</button></div>`;
        if (keepFocus) cal.querySelector(`[data-date="${iso(focusDay)}"]`)?.focus();
    }
    function pick(day) {
        if (!start || end || day <= start) { start = day; end = null; }
        else { end = day; }
        focusDay = day; hover = null;
        writeInputs(); renderCal(true);
    }
    function moveFocus(day) {
        if (day < today) return;
        focusDay = day;
        const last = new Date(view.getFullYear(), view.getMonth() + 2, 0);
        if (day < view) view = new Date(day.getFullYear(), day.getMonth(), 1);
        else if (day > last) view = new Date(day.getFullYear(), day.getMonth() - 1, 1);
        renderCal(true);
    }
    cal.addEventListener('click', e => {
        const b = e.target.closest('button'); if (!b || b.disabled) return;
        if (b.dataset.date) pick(parse(b.dataset.date));
        else if (b.dataset.move) { view = new Date(view.getFullYear(), view.getMonth() + Number(b.dataset.move), 1); renderCal(false); }
        else if ('clear' in b.dataset) { start = end = null; writeInputs(); renderCal(false); }
        else if ('done' in b.dataset) close(cal, true);
    });
    cal.addEventListener('mouseover', e => {
        const b = e.target.closest('.cal-day'); if (!b || !start || end) return;
        const d = parse(b.dataset.date); if (hover && iso(hover) === iso(d)) return;
        hover = d;
        cal.querySelectorAll('.cal-day').forEach(x => {
            const xd = parse(x.dataset.date);
            x.classList.toggle('in-range', d > start && xd > start && xd < d);
            x.classList.toggle('is-end', d > start && iso(xd) === iso(d));
        });
    });
    cal.addEventListener('keydown', e => {
        const b = e.target.closest('.cal-day'); if (!b) return;
        const d = parse(b.dataset.date), steps = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 };
        if (e.key in steps) { e.preventDefault(); moveFocus(addDays(d, steps[e.key])); }
    });
    cal._onOpen = () => { renderCal(false); (cal.querySelector('.cal-day[tabindex="0"]:not([disabled])') || cal.querySelector('.cal-day:not([disabled])'))?.focus(); };
    cal._trigger.addEventListener('click', () => cal.hidden ? open(cal) : close(cal, false));
    writeInputs();

    /* ---------- guests and rooms ---------- */
    if (!guestSelect) return;
    const guestField = guestSelect.closest('.search-field');
    const guests = field('guests-field', 'ผู้เข้าพัก', guestField);
    guestField.style.display = 'none';
    const hidden = document.createElement('input');
    hidden.type = 'hidden'; hidden.name = 'guests'; guestSelect.removeAttribute('name');
    const roomsInput = document.createElement('input');
    roomsInput.type = 'hidden'; roomsInput.name = 'rooms';
    guests.append(hidden, roomsInput);
    const counts = { adults: Number(guestSelect.value) || 2, children: 0, rooms: 1 };
    const limits = { adults: [1, 10], children: [0, 6], rooms: [1, 5] };
    const rows = [['adults', 'ผู้ใหญ่', 'อายุ 13 ปีขึ้นไป'], ['children', 'เด็ก', 'อายุ 0–12 ปี'], ['rooms', 'ห้อง', 'จำนวนห้องที่จอง']];
    const pop = document.createElement('div');
    pop.className = 'picker-pop guest-pop'; pop.id = 'guest-pop'; pop.hidden = true;
    pop.setAttribute('role', 'dialog'); pop.setAttribute('aria-label', 'เลือกจำนวนผู้เข้าพักและห้อง');
    pop._trigger = guests.querySelector('.picker-trigger');
    pop._trigger.setAttribute('aria-controls', pop.id);
    guests.append(pop);
    const guestValue = guests.querySelector('.picker-value');
    function renderGuests() {
        const total = counts.adults + counts.children;
        hidden.value = total; roomsInput.value = counts.rooms;
        guestValue.textContent = `ผู้ใหญ่ ${counts.adults}${counts.children ? ` · เด็ก ${counts.children}` : ''} · ${counts.rooms} ห้อง`;
        pop.innerHTML = `<p class="guest-total">ผู้เข้าพัก ${total} คน</p>` + rows.map(([k, label, note]) => {
            const [min, max] = limits[k];
            return `<div class="guest-row"><div><span class="guest-name" id="g-${k}">${label}</span><small>${note}</small></div>
                <div class="stepper" role="group" aria-labelledby="g-${k}">
                    <button type="button" class="step" data-k="${k}" data-d="-1" aria-label="ลด${label}"${counts[k] <= min ? ' disabled' : ''}>${icon('<path d="M6 12h12"/>')}</button>
                    <output aria-live="polite">${counts[k]}</output>
                    <button type="button" class="step" data-k="${k}" data-d="1" aria-label="เพิ่ม${label}"${counts[k] >= max || (k === 'rooms' && counts.rooms >= counts.adults) ? ' disabled' : ''}>${icon('<path d="M12 6v12M6 12h12"/>')}</button>
                </div></div>`;
        }).join('') + '<p class="guest-note">1 ห้องต้องมีผู้ใหญ่อย่างน้อย 1 คน</p><div class="picker-foot"><span></span><button type="button" class="stay-button picker-done" data-done>เสร็จ</button></div>';
    }
    pop.addEventListener('click', e => {
        const b = e.target.closest('button'); if (!b || b.disabled) return;
        if ('done' in b.dataset) { close(pop, true); return; }
        const k = b.dataset.k; const [min, max] = limits[k];
        counts[k] = Math.min(max, Math.max(min, counts[k] + Number(b.dataset.d)));
        if (counts.rooms > counts.adults) counts.rooms = counts.adults;
        renderGuests();
        (pop.querySelector(`.step[data-k="${k}"][data-d="${b.dataset.d}"]:not([disabled])`) || pop.querySelector(`.step[data-k="${k}"]:not([disabled])`) || pop.querySelector('[data-done]')).focus();
    });
    pop._onOpen = () => pop.querySelector('.step:not([disabled])')?.focus();
    pop._trigger.addEventListener('click', () => pop.hidden ? open(pop) : close(pop, false));
    renderGuests();
})();
