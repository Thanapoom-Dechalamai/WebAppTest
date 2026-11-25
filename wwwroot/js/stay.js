(() => {
    const toggle = document.querySelector('.menu-toggle');
    const navigation = document.querySelector('.stay-navigation');
    toggle?.addEventListener('click', () => {
        const open = navigation.classList.toggle('is-open');
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'ปิดเมนู' : 'เปิดเมนู');
    });
    navigation?.querySelectorAll('a').forEach(a => a.addEventListener('click', () => {
        navigation.classList.remove('is-open');
        toggle?.setAttribute('aria-expanded', 'false');
        toggle?.setAttribute('aria-label', 'เปิดเมนู');
    }));

    // Home rows: the arrows scroll a row by about one screen of cards and hide at either end.
    const smooth = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth';
    document.querySelectorAll('.row-scroller').forEach(box => {
        const track = box.querySelector('.row-track'), prev = box.querySelector('.row-prev'), next = box.querySelector('.row-next');
        if (!track || !prev || !next) return;
        const update = () => {
            prev.hidden = track.scrollLeft <= 4;
            next.hidden = track.scrollLeft + track.clientWidth >= track.scrollWidth - 4;
        };
        prev.addEventListener('click', () => track.scrollBy({ left: -track.clientWidth * 0.9, behavior: smooth() }));
        next.addEventListener('click', () => track.scrollBy({ left: track.clientWidth * 0.9, behavior: smooth() }));
        track.addEventListener('scroll', update, { passive: true });
        window.addEventListener('resize', update);
        update();
    });

    const search = document.querySelector('#hotel-search');
    if (!search) return;
    if (!document.querySelector('.hotel-grid')) {
        // No catalogue on this page (home rows, hotel list): check the dates, then the form goes to /Stays as a normal GET.
        const ci = document.querySelector('#checkin'), co = document.querySelector('#checkout'), err = document.querySelector('#search-error');
        const now = new Date();
        const todayIso = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
        search.addEventListener('submit', event => {
            let message = '';
            if (Boolean(ci?.value) !== Boolean(co?.value)) message = 'กรุณาเลือกวันที่เช็กอินและเช็กเอาต์ให้ครบ';
            else if (ci?.value && co.value <= ci.value) message = 'วันเช็กเอาต์ต้องอยู่หลังวันเช็กอิน';
            else if (ci?.value && ci.value < todayIso) message = 'กรุณาเลือกวันเช็กอินตั้งแต่วันนี้เป็นต้นไป';
            if (err) { err.textContent = message; err.hidden = !message; }
            if (message) event.preventDefault();
        });
        return;
    }
    const destination = document.querySelector('#destination');
    const checkin = document.querySelector('#checkin');
    const checkout = document.querySelector('#checkout');
    const guests = document.querySelector('#guests');
    const error = document.querySelector('#search-error');
    const result = document.querySelector('#hotel-result');
    const cards = [...document.querySelectorAll('.hotel-card')];
    const today = new Date();
    const localDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
    checkin.min = checkout.min = localDate;
    checkin.addEventListener('change', () => { checkout.min = checkin.value || localDate; });
    const keyword = document.querySelector('#hotel-keyword');
    const pagination = document.querySelector('#catalog-pagination');
    const previous = document.querySelector('#previous-page');
    const next = document.querySelector('#next-page');
    const pageStatus = document.querySelector('#page-status');
    let category = '';
    const categoryButtons = [...document.querySelectorAll('[data-category].environment-category')];
    const viewFilter = document.querySelector('#filter-view');
    const poolFilter = document.querySelector('#filter-pool');
    const seaFilter = document.querySelector('#filter-sea');
    const forestFilter = document.querySelector('#filter-forest');
    const within = (value, limit) => limit === '' || (value !== '' && Number(value) <= Number(limit));
    const pageSize = 12;
    const sortSelect = document.querySelector('#hotel-sort');
    const grid = document.querySelector('.hotel-grid');
    cards.forEach((card, index) => { card.dataset.order = index; });
    // Same choices as the sort menu: recommended (catalogue order), price low/high, best reviews.
    const sorters = {
        'price-asc': (a, b) => Number(a.dataset.price) - Number(b.dataset.price),
        'price-desc': (a, b) => Number(b.dataset.price) - Number(a.dataset.price),
        'rating': (a, b) => Number(b.dataset.rating) - Number(a.dataset.rating) || Number(b.dataset.reviews) - Number(a.dataset.reviews)
    };
    const sortCards = list => {
        const compare = sorters[sortSelect?.value];
        return [...list].sort((a, b) => (compare ? compare(a, b) : 0) || Number(a.dataset.order) - Number(b.dataset.order));
    };
    let page = 1;
    const normalize = value => value.normalize('NFKC').toLocaleLowerCase().trim();
    const matchesCategory = (card, selected) => !selected ||
        card.dataset.category === selected ||
        (card.dataset.themes || '').split(' ').includes(selected) ||
        (selected === 'pool' && card.dataset.pool === 'true') ||
        (selected === 'mountain' && card.dataset.view === 'mountain');
    function renderCatalog(resetPage = false) {
        if (resetPage) page = 1;
        const query = normalize(keyword.value);
        const matches = cards.filter(card =>
            matchesCategory(card, category) &&
            (!viewFilter?.value || card.dataset.view === viewFilter.value) &&
            (!poolFilter.value || card.dataset.pool === poolFilter.value) &&
            within(card.dataset.sea, seaFilter.value) &&
            within(card.dataset.forest, forestFilter.value) &&
            (!destination.value || card.dataset.city === destination.value) &&
            (!query || normalize(card.dataset.name + ' ' + card.dataset.city).includes(query)));
        const sorted = sortCards(matches);
        sortCards(cards).forEach(card => grid.appendChild(card));
        sorted.forEach(card => grid.appendChild(card));
        matches.splice(0, matches.length, ...sorted);
        const pageCount = Math.max(1, Math.ceil(matches.length / pageSize));
        page = Math.min(Math.max(page, 1), pageCount);
        const start = (page - 1) * pageSize;
        const visible = new Set(matches.slice(start, start + pageSize));
        cards.forEach(card => { card.hidden = !visible.has(card); });
        const end = Math.min(start + pageSize, matches.length);
        result.textContent = (destination.value || 'ทั่วไทย 77 จังหวัด') + ' · พบ ' + matches.length +
            ' ที่พัก' + (matches.length ? ' · แสดง ' + (start + 1) + '–' + end : '');
        document.querySelector('#empty-results').hidden = matches.length > 0;
        pagination.hidden = matches.length <= pageSize;
        pageStatus.textContent = 'หน้า ' + page + ' / ' + pageCount;
        previous.disabled = page === 1;
        next.disabled = page === pageCount;
    }
    function goToPage(change) {
        page += change;
        renderCatalog();
        document.querySelector('#hotels').scrollIntoView({ block: 'start' });
    }
    previous.addEventListener('click', () => goToPage(-1));
    next.addEventListener('click', () => goToPage(1));
    categoryButtons.forEach(button => button.addEventListener('click', () => {
        category = button.dataset.category;
        categoryButtons.forEach(b => { b.classList.toggle('active', b === button); b.setAttribute('aria-pressed', String(b === button)); });
        renderCatalog(true);
    }));
    [viewFilter, poolFilter, seaFilter, forestFilter].filter(Boolean).forEach(input => input.addEventListener('change', () => renderCatalog(true)));
    keyword.addEventListener('input', () => renderCatalog(true));
    sortSelect?.addEventListener('change', () => renderCatalog(true));
    destination.addEventListener('change', () => renderCatalog(true));
    search.addEventListener('submit', event => {
        event.preventDefault();
        let message = '';
        if (Boolean(checkin.value) !== Boolean(checkout.value)) message = 'กรุณาเลือกวันที่เช็กอินและเช็กเอาต์ให้ครบ';
        else if (checkin.value && checkout.value <= checkin.value) message = 'วันเช็กเอาต์ต้องอยู่หลังวันเช็กอิน';
        else if (checkin.value && checkin.value < localDate) message = 'กรุณาเลือกวันเช็กอินตั้งแต่วันนี้เป็นต้นไป';
        error.textContent = message;
        error.hidden = !message;
        if (message) return;
        renderCatalog(true);
        document.querySelector('#hotels').scrollIntoView({ block: 'start' });
    });
    document.querySelector('#show-all').addEventListener('click', () => {
        category = '';
        categoryButtons.forEach(b => { b.classList.toggle('active', b.dataset.category === ''); b.setAttribute('aria-pressed', String(b.dataset.category === '')); });
        [viewFilter, poolFilter, seaFilter, forestFilter].filter(Boolean).forEach(input => input.value = '');
        destination.value = '';
        keyword.value = '';
        error.hidden = true;
        renderCatalog(true);
    });
    renderCatalog();

    document.querySelector('#reset-filters')?.addEventListener('click',()=>document.querySelector('#show-all').click());
    categoryButtons.forEach(b=>{
        const count=cards.filter(c=>matchesCategory(c,b.dataset.category)).length;
        const label=b.querySelector('.category-count'); if(label) label.textContent=count+' ที่พัก';
    });
    const promo=new URLSearchParams(location.search).get('promo');
    if(promo){
        document.querySelectorAll('.hotel-detail-link').forEach(a=>{const u=new URL(a.href);u.searchParams.set('promo',promo);a.href=u;});
        const note=document.createElement('p');note.className='selected-promo';note.textContent='โค้ดที่เลือก: '+promo+' · จะตรวจสิทธิ์และเงื่อนไขอีกครั้งก่อนชำระ';
        (document.querySelector('.catalog-tools') || document.querySelector('#environment-filters'))?.before(note);
    }
})();
