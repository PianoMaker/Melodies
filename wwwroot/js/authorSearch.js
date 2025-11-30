//------------------------------------------------------
// Автодоповнення для вибору автора у формі створення мелодії
// Використовує Fetch API для запитів до сервера
//------------------------------------------------------
(function () {
    const input = document.getElementById('authorSearch');
    const results = document.getElementById('authorResults');
    const hiddenId = document.getElementById('authorIdHidden');

    if (!input || !results || !hiddenId) return;

    let lastQuery = '';
    let debounceTimer = null;

    //------------------------------------------------
    // Функції для пошуку авторів та відображення результатів
	//------------------------------------------------
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
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'list-group-item list-group-item-action';
            const full = [a.surname, a.name].filter(Boolean).join(' ') || a.displayName || '';
            const en = [a.surnameEn, a.nameEn].filter(Boolean).join(' ');
            btn.innerHTML = `
                <div class="d-flex justify-content-between align-items-center">
                    <span>${full}</span>
                    <small class="text-muted">${en}</small>
                </div>`;
            btn.addEventListener('click', () => {
                // Записуємо ЧИСЛОВИЙ id у приховане поле та текст у видиме поле
                hiddenId.value = a.id;
                input.value = full;
                results.innerHTML = '';

                // Зберігаємо обидва в sessionStorage (щоб пережити проміжний POST)
                try {
                    sessionStorage.setItem("selectedAuthorId", String(a.id));
                    sessionStorage.setItem("selectedAuthorName", full);
                    console.log('[authorSearch] saved selection', a.id, full);
                } catch (e) {
                    console.warn('[authorSearch] sessionStorage failed', e);
                }
            });
            results.appendChild(btn);
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
	//------------------------------------------------
    // Обробка вводу з затримкою (debounce)
	//------------------------------------------------

    input.addEventListener('input', () => {
        const q = (input.value || '').trim();
        if (q === lastQuery) return;
        lastQuery = q;

        clearTimeout(debounceTimer);
        if (q.length < 3) {
            results.innerHTML = '';
            // при редагуванні очищаємо раніше вибраний id
            if (hiddenId.value) {
                hiddenId.value = '';
                try {
                    sessionStorage.removeItem("selectedAuthorId");
                    sessionStorage.removeItem("selectedAuthorName");
                } catch (e) { /* ignore */ }
            }
            return;
        }
        debounceTimer = setTimeout(() => searchAuthors(q), 200);

        // захисне очищення
        if (hiddenId.value) {
            hiddenId.value = '';
            try {
                sessionStorage.removeItem("selectedAuthorId");
                sessionStorage.removeItem("selectedAuthorName");
            } catch (e) { /* ignore */ }
        }
    });

    // також очищуємо при keydown (сумісність)
    input.addEventListener('keydown', () => {
        if (hiddenId.value) {
            hiddenId.value = '';
            try {
                sessionStorage.removeItem("selectedAuthorId");
                sessionStorage.removeItem("selectedAuthorName");
            } catch (e) { /* ignore */ }
        }
    });

    //------------------------------------------------
    // Відновлення вибору при завантаженні, якщо він присутній
    //------------------------------------------------
    try {
        const savedId = sessionStorage.getItem("selectedAuthorId");
        const savedName = sessionStorage.getItem("selectedAuthorName");
        if (savedId && savedName) {
            hiddenId.value = savedId;
            input.value = savedName;
            console.log('[authorSearch] restored selection from sessionStorage', savedId, savedName);
        }
    } catch (e) {
        /* ignore storage errors */
    }
})();