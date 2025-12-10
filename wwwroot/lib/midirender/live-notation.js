// ===== LIVE NOTATION (Create page) using patternRenderer.js =====
// Load renderer deps only once and setup live render like on Search page
// Expects container elements to be passed in
// container - main container to prepend live notation div
// pianodisplay - textarea input for pattern
// numeratorInput - input for time signature numerator
// denominatorInput - input for time signature denominator
// pianoKeysContainer - container with piano keys buttons
// restBtn - button to insert rest
// noNotesMsg - element to show/hide when there are no notes
// options (optional):
//   - autoHookTimeSignature: true|false  (attach change listeners to numerator/denominator that call scheduleRender)
//   - observeIds: ['lettersdisplay','saver']  (array of element ids to MutationObserver -> scheduleRender)
// Usage: call setupLiveNotationOnCreate with appropriate elements after the DOM is ready
// Example:
//   setupLiveNotationOnCreate({
//	   container: document.getElementById('patternContainer'),
//	   pianodisplay: document.getElementById('pianodisplay'),
//	   numeratorInput: document.getElementById('timeSigNum'),
//	   denominatorInput: document.getElementById('timeSigDen'),
//	   pianoKeysContainer: document.getElementById('pianoKeys'),
//	   restBtn: document.getElementById('insertRestBtn'),
//	   noNotesMsg: document.getElementById('noNotesMessage')
//   }, { autoHookTimeSignature: true, observeIds: ['lettersdisplay','saver'] });
// ==========================


window.`setupLiveNotationOnCreate = function ({
    container,
    pianodisplay,
    numeratorInput,
    denominatorInput,
    pianoKeysContainer,
    restBtn,
    noNotesMsg
}, options = {}) {
    // Inject live notation containers if missing (same ids as on Search)
    console.log('[LNS] setupLiveNotationCreate.js...');
    if (container) {
        // Respect server-side suppression flag if present
        if (window.__suppressInitialLiveNotation) {
            console.debug('setupLiveNotationOnCreate: suppressed initial liveNotation by server');
        } else {
            console.debug('setupLiveNotationOnCreate: populate liveNotation');
            if (!document.getElementById('liveNotation')) {
                const live = document.createElement('div');
                live.id = 'liveNotation';
                live.className = 'mb-3';
                container.prepend(live);
            }
            if (!document.getElementById('patternComments')) {
                const comments = document.createElement('div');
                comments.id = 'patternComments';
                comments.style.display = 'none';
                container.insertBefore(comments, container.children[1] || null);
            }
        }
    }

	// Обюробники подій для тригера рендеру
    restBtn.addEventListener('click', () => setTimeout(scheduleRender, 0));
    pianoKeysContainer.addEventListener('click', () => setTimeout(scheduleRender, 0));
    pianodisplay.addEventListener('input', scheduleRender);
    pianodisplay.addEventListener('keyup', scheduleRender);
    pianodisplay.addEventListener('change', scheduleRender);


    // Render helpers (mirroring livePatternRenderer.js)
    let lastRenderKey = null;
    let renderTimer = null; 
    function scheduleRender() {
        if (renderTimer) clearTimeout(renderTimer);
        renderTimer = setTimeout(renderFromTextarea, 50);
    }

	//==========================
    // Рендер введеної користувачем нотної послідовності
	//==========================
    function safeRender(pattern) {
        try {
            const { num, den } = getSearchTimeSignature();
            console.log('live notation render:', { pattern, num, den });
            if (typeof window.renderPatternString !== 'function') return;
            const renderEl = document.getElementById('liveNotation');
            const commentsEl = document.getElementById('patternComments');
            if (!renderEl || !commentsEl) return;
            window.renderPatternString(
                pattern || '',
                renderEl.id,
                commentsEl.id,
                num, den,
                undefined,
                120,
                20,
                250,
                60,
                10
            );
        } catch (e) { console.warn('safeRender failed', e); }
    }

    // Отримує поточний нотний рядок з textarea
    function getPattern() { return pianodisplay ? pianodisplay.value : ''; }

    // Перевіряє зміни в textarea і викликає рендер при зміні
    // Використовує lastRenderKey для уникнення зайвих рендерів
    function renderFromTextarea() {
        const current = getPattern();
        const { num, den } = getSearchTimeSignature();
        const key = `${current}|${num}/${den}`; // include timesig so changing it forces render

        if (lastRenderKey === key) return; // skip if nothing changed
        lastRenderKey = key;

        if (noNotesMsg) {
            if (current && current.trim().length > 0) noNotesMsg.style.display = 'none';
            else noNotesMsg.style.display = '';
        }
        safeRender(current);
    }

    // helper to attach MutationObserver to an element id -> scheduleRender
    function addMutationObserverById(id) {
        if (!id || typeof MutationObserver === 'undefined') return;
        const el = document.getElementById(id);
        if (!el) return;
        try {
            const obs = new MutationObserver(() => {
                scheduleRender();
            });
            obs.observe(el, { childList: true, characterData: true, subtree: true });
        } catch (e) {
            console.warn('addMutationObserverById failed for', id, e);
        }
    }






    // optional guarded polling (not started by default, kept for debug/fallback)
    let _pollIntervalId = null;
    function startPolling() {
        if (_pollIntervalId) return;
        _pollIntervalId = setInterval(renderFromTextarea, 200);
    }
    function stopPolling() {
        if (!_pollIntervalId) return;
        clearInterval(_pollIntervalId);
        _pollIntervalId = null;
    }
    // expose stopPolling if needed later
    window.__stopLiveNotationPolling = stopPolling;

    function renderOnLoad() { renderFromTextarea(); }

    //==========================
    // Парсинг музичного розміру
    //==========================
    function getSearchTimeSignature() {
        const num = parseInt(numeratorInput?.value) || 4;
        const den = parseInt(denominatorInput?.value) || 4;
        return { num, den };
    }

    //==========================
    // Ініціалізація live notation
    //==========================
    // Перевіряє чи вже завантажені залежності
    // Якщо так, одразу налаштовує live notation
	// Якщо ні, завантажує їх і потім налаштовує
    const depsAlreadyPresent =
        (window.Vex && window.Vex.Flow) &&
        (typeof window.renderPatternString === 'function') &&
        (typeof window.createRest === 'function') &&
        (typeof window.getTotalTicksForNote === 'function');

    // If caller requested automatic time-signature hooks, add them
    if (options && options.autoHookTimeSignature) {
        try {
            if (numeratorInput) numeratorInput.addEventListener('change', scheduleRender);
            if (denominatorInput) denominatorInput.addEventListener('change', scheduleRender);
        } catch (e) { /* ignore */ }
    }

    // If caller supplied observeIds, attach MutationObservers
    if (options && Array.isArray(options.observeIds)) {
        options.observeIds.forEach(id => addMutationObserverById(id));
    }


    

    // Expose scheduleRender to other handlers below
    window.__scheduleLiveNotationRender = scheduleRender;
};
// ===== END LIVE NOTATION =====
// Відновлення значення з saver тільки якщо воно не порожнє