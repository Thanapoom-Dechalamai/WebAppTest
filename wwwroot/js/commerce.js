(() => {
    document.querySelectorAll('[data-social]').forEach(button => button.addEventListener('click', () => {
        document.querySelector('#social-status').textContent = `${button.dataset.social} เป็นปุ่มตัวอย่าง ยังไม่เชื่อมต่อบัญชี กรุณาใช้อีเมลและรหัสผ่านเพื่อเข้าสู่ระบบ`;
    }));
    document.querySelectorAll('[data-amount]').forEach(button => button.addEventListener('click', () => {
        document.querySelector('[name="Form.Amount"]').value = button.dataset.amount;
    }));
    document.querySelectorAll('.cancel-order, .staff-cancel').forEach(form => form.addEventListener('submit', event => {
        if (!window.confirm('ยืนยันยกเลิกรายการนี้และรับเครดิตคืนเต็มจำนวน?')) event.preventDefault();
    }));
    document.querySelectorAll('.hotel-detail-link').forEach(link => link.addEventListener('click', () => {
        const url = new URL(link.href);
        for (const field of ['checkin', 'checkout', 'guests']) {
            const value = document.getElementById(field)?.value;
            if (value) url.searchParams.set(field, value);
        }
        link.href = url.toString();
    }));
    const parameters = new URLSearchParams(location.search);
    document.querySelectorAll('.room-booking').forEach(form => {
        const checkIn = form.querySelector('[name="CheckIn"]');
        const checkOut = form.querySelector('[name="CheckOut"]');
        for (const [key, field] of [['checkin',checkIn],['checkout',checkOut]]) {
            const value = parameters.get(key);
            if (value && /^\d{4}-\d{2}-\d{2}$/.test(value) && value >= field.min) field.value = value;
        }
        const guests = Number(parameters.get('guests'));
        if (guests >= 1 && guests <= 10) {
            form.querySelector('[name="Guests"]').value = guests;
            form.querySelector('[name="Rooms"]').value = Math.ceil(guests / 2);
        }
        checkIn.addEventListener('change', () => {
            const date = new Date(checkIn.value + 'T12:00:00');
            if (Number.isNaN(date.getTime())) return;
            date.setDate(date.getDate()+1);
            const next = `${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;
            checkOut.min = next;
            if (checkOut.value <= checkIn.value) checkOut.value = next;
        });
    });
})();

// A hidden field marked data-copy-from="<id>" takes that field's current value when its form is sent.
// Checkout uses it so applying a promo code (a separate form) keeps the special request just typed.
document.addEventListener('submit', (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) return;
    form.querySelectorAll('input[data-copy-from]').forEach((hidden) => {
        const source = document.getElementById(hidden.dataset.copyFrom);
        if (source) hidden.value = source.value;
    });
}, true);
