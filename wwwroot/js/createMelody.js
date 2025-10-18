//створює мелодію за натисканням клавіш піаніно
//читання нотного рядку відбувається по одній ноті у конструкторі Note(string input) 

document.addEventListener("DOMContentLoaded", function () {

    console.log("createMelody.js starts.");
    

    const buttons = document.querySelectorAll('#pianoroll button');
    const audioPlayer = document.getElementById('audioPlayer'); // аудіоплеєр
    const audioSource = document.getElementById('audioSource'); // джерело для аудіофайлу     
    let pianodisplay = document.getElementById("pianodisplay");
    const keysInput_save = document.getElementById("keysInput-save")
    const keysInput_search = document.getElementById("keysInput-search")
    const searchAlgorithmInput = document.getElementById("searchAlgorithmInput");
    const createMIDIButton = document.getElementById('createMIDI');//кнопка "зберегти"
    const searchButton = document.getElementById('searchBtn');//кнопка "зберегти"
    const playButton = document.getElementById('melodyPlayBtn');
    const saver = document.getElementById("saver");//тимчасове збереження мелодії
    const authorSaver = document.getElementById("authorSaver");//тимчасове збереження автора    
    const titleInput = document.getElementById("titleInput");
    const selectAuthor = document.getElementById("selectAuthor");
    const copyBtn = document.getElementById("copyBtn");
    const submitMelodyBtn = document.getElementById("submitMelodyBtn")
    const melodyFileInput = document.getElementById('melodyFileInput');

    if (!pianodisplay) {
        console.warn('pianodisplay element not found – aborting createMelody.js initialization');
        return; // без нотного поля немає сенсу виконувати далі
    }

    // ===== LIVE NOTATION (Create page) using patternRenderer.js =====
    // Load renderer deps only once and setup live render like on Search page
    (function setupLiveNotationOnCreate(){
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

        // Dynamic loader for js files
        function loadScriptOnce(src){
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

        // Ensure VexFlow is available (try the same path as on Search page first)
        function ensureVexFlow(){
            if (window.Vex && window.Vex.Flow) return Promise.resolve();
            // Try correct path used by Search page
            return loadScriptOnce('/lib/midirender/vexflow.js')
                .catch(() => Promise.resolve())
                .then(() => {
                    if (window.Vex && window.Vex.Flow) return;
                    // Fallback to layout path, in case it's present there
                    return loadScriptOnce('/lib/vexflow.js').catch(() => {/* ignore */});
                })
                .then(() => {
                    if (!(window.Vex && window.Vex.Flow)) {
                        console.warn('VexFlow could not be loaded');
                    }
                });
        }

        // Deps in correct order (rendererUtils must be before midiRenderer)
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
        function scheduleRender(){
            if (renderTimer) clearTimeout(renderTimer);
            renderTimer = setTimeout(renderFromTextarea, 50);
        }

        const { num, den } = getSearchTimeSignature();
        function safeRender(pattern){
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
            } catch(e){ console.warn('safeRender failed', e); }
        }
        function getPattern() { return pianodisplay ? pianodisplay.value : ''; }




        function renderFromTextarea(){
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
        function hookPianoButtons(){
            const piano = document.getElementById('pianoroll');
            if (!piano) return;
            piano.addEventListener('click', () => setTimeout(scheduleRender, 0));
        }
        function hookRestButton(){
            const restBtn = document.getElementById('pausebutton');
            if (!restBtn) return; 
            restBtn.addEventListener('click', () => setTimeout(scheduleRender, 0));
        }
        function hookTextareaInput(){
            if (!pianodisplay) return;
            pianodisplay.addEventListener('input', scheduleRender);
            pianodisplay.addEventListener('keyup', scheduleRender);
            pianodisplay.addEventListener('change', scheduleRender);
        }
        function startPolling(){ setInterval(renderFromTextarea, 200); }
        function renderOnLoad(){ renderFromTextarea(); }

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

    // Відновлення значення з saver тільки якщо воно не порожнє
    if (saver) {
        const saved = (saver.innerText || '').trim();
        if (saved.length > 0) {
            pianodisplay.value = saved;
        }
    }
    const savedTitle = sessionStorage.getItem("savedTitle");
    const savedAuthorId = sessionStorage.getItem("selectedAuthorId");
    

    if (titleInput && savedTitle) {
        console.log(`restoring saved title: ${savedTitle}`)
        const savedTitleDiv = document.getElementById("savedTitle");
        if (savedTitleDiv) savedTitleDiv.textContent = `savedtitle = ${savedTitle}`;
        titleInput.value = savedTitle;
    }
    else if (titleInput) {
        console.log(`saved title is null`);
    }
    if (selectAuthor && savedAuthorId) {
        console.log(`restoring saved authorId: ${savedAuthorId}`)
        selectAuthor.value = savedAuthorId;
    }
    else if (selectAuthor) {
        console.log(`saved authorId is null`);  
    }
        
    if (saver) saver.style.display = 'none';    

    if (titleInput && selectAuthor && submitMelodyBtn && savedTitle && savedAuthorId)
        submitMelodyBtn.style.display = 'block';

    //обробник завантажувача файлів (присутній лише на сторінці Create)
    if (melodyFileInput) {
        melodyFileInput.addEventListener('change', function () {
            const fileInput = this;
            if (fileInput.files.length > 0) {
                const copyBtnLocal = document.getElementById('copyBtn');
                if (copyBtnLocal) copyBtnLocal.style.display = 'inline-block';
            }
        });
    }
    

    //обробник кнопок з тривалістю
    let duration = '4';
    const durationbuttons = document.querySelectorAll('.durationbutton');
    const restBtn = document.getElementById('pausebutton');
    const dotBtn = document.getElementById('dotbutton'); // NEW

    durationbuttons.forEach((button, index) => {
        button.addEventListener('click', () => {
            duration = String(2 ** index); // 2^index дає потрібне значення
            console.log('duration:', duration);
        });
    });

    // NEW: toggle крапки + підсвітка
    if (dotBtn) {
        dotBtn.addEventListener('click', () => {
            dotBtn.classList.toggle('highlight');
            // no immediate change to textarea, but schedule render to keep UI in sync
            if (window.__scheduleLiveNotationRender) window.__scheduleLiveNotationRender();
        });
    }

    // Допоміжна: чи активна крапка зараз
    function isDottedActive() {
        return document.getElementById('dotbutton')?.classList.contains('highlight') || false;
    }

    // обробник клавіш фортепіано
    buttons.forEach(button => {
        button.addEventListener('click', function () {
            const key = this.getAttribute('data-key');
            // ... програвання звуку ...

            // Додаємо крапку до тривалості, якщо активна
            const dotSuffix = isDottedActive() ? '.' : '';
            pianodisplay.value += `${key}${duration}${dotSuffix}_`;
            if (createMIDIButton) createMIDIButton.style.background = "lightgreen";
            if (playButton) {
                playButton.style.background = "lightgray";
                const playIcon = document.querySelector('.fas.fa-play');
                if (playIcon) playIcon.style.color = "gray";
            }
            if (window.__scheduleLiveNotationRender) window.__scheduleLiveNotationRender();
        });

    });

    // обробник паузи
    if (restBtn) {
        restBtn.addEventListener('click', function () {
            const dotSuffix = isDottedActive() ? '.' : '';
            pianodisplay.value += `r${duration}${dotSuffix}_`;
            if (createMIDIButton) createMIDIButton.style.background = "lightgreen";
            if (playButton) {
                playButton.style.background = "lightgray";
                const playIcon = document.querySelector('.fas.fa-play');
                if (playIcon) playIcon.style.color = "gray";
            }
            if (window.__scheduleLiveNotationRender) window.__scheduleLiveNotationRender();
        })
    }

    //кнопка "Відтворення"
    if (playButton) {
        playButton.addEventListener('click', function (e) {

            const previewMp3path = document.getElementById('previewMp3path')
            if (!previewMp3path) return;
            var filepath = previewMp3path.textContent.trim();
            var audioPlayer = document.getElementById('audioPlayer');
            const audioSource = document.getElementById('audioSource');
            if (!audioPlayer || !audioSource) return;
            audioSource.src = filepath;
            console.log(`Play.js play preview from ${audioSource.src}`);
            audioPlayer.load();
            audioPlayer.play().catch(err => {
                console.error("Помилка при програванні:", err);
            });
        });
    }

    //кнопка "Зберегти" (Create) або "попередній перегляд" (Search)
    if (createMIDIButton && titleInput && selectAuthor && submitMelodyBtn) {
        // Create page behavior
        createMIDIButton.addEventListener('click', function (event) {
            event.preventDefault();
            var unique = checkIfunique();
            if (unique) {
                if (keysInput_save) keysInput_save.value = pianodisplay.value
                sessionStorage.setItem("savedTitle", titleInput.value);
                sessionStorage.setItem("selectedAuthorId", selectAuthor.value);
                console.log("Відправка форми з Keys:", keysInput_save ? keysInput_save.value : '(no element)');
                console.log(`Збереження Title: ${titleInput.value}, selectedAuthorId: ${selectAuthor.value}`);
                // Викликає OnPostMelody()
                const melodyForm = document.getElementById('melodyForm');
                if (melodyForm) melodyForm.submit();
            }
            else alert("Мелодія даного автора з такою назвою вже існує");

        });
    } else if (createMIDIButton) {
        // Fallback для сторінок без title/author (наприклад, Search)
        createMIDIButton.addEventListener('click', function (event) {
            // Не відміняємо submit, просто встановлюємо Keys перед відправкою
            if (keysInput_save) keysInput_save.value = pianodisplay.value;
            console.log("Preview submit with Keys:", keysInput_save ? keysInput_save.value : '(no element)');
        });
    }

    //кнопка "Пошук"
    if (searchButton) {
        searchButton.addEventListener('click', function (event) {
            event.preventDefault();
            if (keysInput_search) keysInput_search.value = pianodisplay.value
            console.log("Відправка форми з Keys:", keysInput_search ? keysInput_search.value : '(no element)');

            // синхронізувати вибраний алгоритм пошуку
            const selectedAlg = document.querySelector('input[name="SearchAlgorithm"]:checked');
            if (selectedAlg && searchAlgorithmInput) {
                searchAlgorithmInput.value = selectedAlg.value;
                console.log("Selected algorithm:", selectedAlg.value);
            }

            // Відправка форми
            const searchForm = document.getElementById('notesearchForm');
            if (searchForm) searchForm.submit();

        });
    }

    //перевіряє унікальність назви+композитора
    async function checkIfunique() {
        if (!titleInput || !selectAuthor) return false;
        var title = titleInput.value;
        var authorId = selectAuthor.value;

        try {
            const response = await fetch(`/Melodies/Create?handler=CheckFileExists&title=${encodeURIComponent(title)}&authorId=${authorId}`);
            if (!response.ok) {
                console.error("Помилка сервера:", response.statusText);
                return false;
            }
            const exist = await response.json();
            return !exist;
        } catch (error) {
            console.error("Помилка при виконанні запиту:", error);
            return false;
        }
    }

    // відображення кнопки ДОДАТИ МЕЛОДІЮ
    if (titleInput && selectAuthor && submitMelodyBtn) {
        titleInput.addEventListener("input", function () {
            
            if (titleInput.value.length > 2 && selectAuthor.value) {
                submitMelodyBtn.style.display = 'block';
                console.log("displaying submit btn");
            }
            else submitMelodyBtn.style.display = 'none';
        });
    }

});

//копіювальник назви
function copytitle(event) {
    event.preventDefault();
    console.log("copytitle function is running");
    const fileInput = document.getElementById("melodyFileInput");
    const titleInput = document.getElementById("titleInput"); // стандартний id для asp-for="Melody.Title"

    if (fileInput && fileInput.files.length > 0 && titleInput) {
        const filename = fileInput.files[0].name;        
        let nameWithoutExtension = filename.replace(/\.[^/.]+$/, "");        
        let nameCapitalized = nameWithoutExtension.charAt(0).toUpperCase() + nameWithoutExtension.slice(1);
        titleInput.value = nameCapitalized;
        titleInput.dispatchEvent(new Event("input", { bubbles: true }));
    } else {
        alert("Файл не вибрано");
    }
}

function getSearchTimeSignature() {
    const num = parseInt(document.getElementById('TimeSignatureNumerator')?.value) || 4;
    const den = parseInt(document.getElementById('TimeSignatureDenominator')?.value) || 4;
    return { num, den };
}