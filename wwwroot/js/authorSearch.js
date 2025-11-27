(function () {
    const input = document.getElementById('authorSearch');
    const results = document.getElementById('authorResults');
    const hiddenId = document.getElementById('authorIdHidden');

    if (!input || !results || !hiddenId) return;

    let lastQuery = '';
    let debounceTimer = null;

    function renderResults(items) {
        results.innerHTML = '';
        if (!Array.isArray(items) || items.length === 0) {
            const empty = document.createElement('div');
            empty.className = 'list-group-item small text-muted';
            empty.textContent = 'Нічого не знайдено';
            results.appendChild(empty);
            return;
        }
        items.forEach(a => {
            const div = document.createElement('button');
            div.type = 'button';
            div.className = 'list-group-item list-group-item-action';
            const full = [a.surname, a.name].filter(Boolean).join(' ') || a.displayName || '';
            const en = [a.surnameEn, a.nameEn].filter(Boolean).join(' ');
            div.innerHTML = `
                <div class="d-flex justify-content-between align-items-center">
                    <span>${full}</span>
                    <small class="text-muted">${en}</small>
                </div>`;
            div.addEventListener('click', () => {
                hiddenId.value = a.id;
                input.value = full;
                results.innerHTML = '';
            });
            results.appendChild(div);
        });
    }

    async function searchAuthors(q) {
        try {
            const url = `/Melodies/Create?handler=AuthorSearch&q=${encodeURIComponent(q)}`;
            const resp = await fetch(url, { headers: { 'Accept': 'application/json' } });
            if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
            const data = await resp.json();
            renderResults(data);
        } catch (e) {
            console.warn('author search failed', e);
            renderResults([]);
        }
    }

    input.addEventListener('input', () => {
        const q = (input.value || '').trim();
        if (q === lastQuery) return;
        lastQuery = q;

        clearTimeout(debounceTimer);
        if (q.length < 3) {
            results.innerHTML = '';
            return;
        }
        debounceTimer = setTimeout(() => searchAuthors(q), 200);
    });

    // Clear selection when the user edits the query
    input.addEventListener('keydown', () => { hiddenId.value = ''; });
})();