// ===== LIVE NOTATION (Create page) using patternRenderer.js =====
// Load renderer deps only once and setup live render like on Search page
(function setupLiveNotationOnCreate() {
    // Inject live notation containers if missing (same ids as on Search)
    const container = document.getElementById('innerNotesContainer');
    if (container) {
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

    // завантажує скрипт лише один раз
    function loadScriptOnce(src) {
        if (!src) return Promise.resolve();
        const key = `__loaded_${src}`;
        if (window[key]) return window[key];
        window[key] = new Promise((resolve, reject) => {
            const s = document.createElement('script');
            s.src = src;
            s.onload = () => resolve();
            s.onerror = (e) => reject(e);
            document.body.appendChild(s);
        });
        return window[key];
    }

    // функція для завантаження VexFlow з можливими шляхами
    function ensureVexFlow() {
        if (window.Vex && window.Vex.Flow) return Promise.resolve();
        // Try correct path used by Search page
        return loadScriptOnce('/lib/midirender/vexflow.js')
            .catch(() => Promise.resolve())
            .then(() => {
                if (window.Vex && window.Vex.Flow) return;
                // Fallback to layout path, in case it's present there
                return loadScriptOnce('/lib/vexflow.js').catch(() => {/* ignore */ });
            })
            .then(() => {
                if (!(window.Vex && window.Vex.Flow)) {
                    console.warn('VexFlow could not be loaded');
                }
            });
    }

    // Залежності midirender
    const deps = [
        '/lib/midirender/midiparser_ext.js',
        '/lib/midirender/makeBeams.js',
        '/lib/midirender/rendererUtils.js',
        '/lib/midirender/midiRenderer.js',
        '/lib/midirender/patternRenderer.js'
    ];

    // Render helpers (mirroring livePatternRenderer.js)
    let lastPattern = null;
    let renderTimer = null;
    function scheduleRender() {
        if (renderTimer) clearTimeout(renderTimer);
        renderTimer = setTimeout(renderFromTextarea, 50);
    }

    const { num, den } = getSearchTimeSignature();

    // Рендер введеної користувачем нотної послідовності
    function safeRender(pattern) {
        try {
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

    function renderFromTextarea() {
        const current = getPattern();
        if (current === lastPattern) return;
        lastPattern = current;
        const noMsg = document.getElementById('noNotesMsg');
        if (noMsg) {
            if (current && current.trim().length > 0) noMsg.style.display = 'none';
            else noMsg.style.display = '';
        }
        safeRender(current);
    }
    function hookPianoButtons() {
        const piano = document.getElementById('pianoroll');
        if (!piano) return;
        piano.addEventListener('click', () => setTimeout(scheduleRender, 0));
    }
    function hookRestButton() {
        const restBtn = document.getElementById('pausebutton');
        if (!restBtn) return;
        restBtn.addEventListener('click', () => setTimeout(scheduleRender, 0));
    }
    function hookTextareaInput() {
        if (!pianodisplay) return;
        pianodisplay.addEventListener('input', scheduleRender);
        pianodisplay.addEventListener('keyup', scheduleRender);
        pianodisplay.addEventListener('change', scheduleRender);
    }
    function startPolling() { setInterval(renderFromTextarea, 200); }
    function renderOnLoad() { renderFromTextarea(); }

    // If libs already present (e.g., Search has static includes), skip dynamic loading
    const depsAlreadyPresent =
        (window.Vex && window.Vex.Flow) &&
        (typeof window.renderPatternString === 'function') &&
        (typeof window.createRest === 'function') &&      // from midiparser_ext
        (typeof window.getTotalTicksForNote === 'function'); // from rendererUtils

    if (depsAlreadyPresent) {
        hookPianoButtons();
        hookRestButton();
        hookTextareaInput();
        startPolling();
        renderOnLoad();
        window.__scheduleLiveNotationRender = scheduleRender;
        return;
    }

    // Load Vex first, then deps, then bind and render
    Promise.resolve()
        .then(() => ensureVexFlow())
        .then(() => deps.reduce((p, src) => p.then(() => loadScriptOnce(src)), Promise.resolve()))
        .then(() => {
            hookPianoButtons();
            hookRestButton();
            hookTextareaInput();
            startPolling();
            renderOnLoad();
        })
        .catch(err => console.warn('Failed to init live notation on Create:', err));

    // Expose scheduleRender to other handlers below
    window.__scheduleLiveNotationRender = scheduleRender;
})();
// ===== END LIVE NOTATION =====