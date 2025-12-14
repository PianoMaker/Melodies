//// працює для сторінки розширеного пошуку (search)
//// відображає детальний аналіз мелодії, яку обираємо по кліку миші

//document.addEventListener("DOMContentLoaded", function () {
//    // Responsive: shorten song-title on narrow screens
//    function adjustSongTitles() {
//        const breakpoint = 720; // px
//        const useShort = window.innerWidth <= breakpoint;
//        document.querySelectorAll('.song-title').forEach(el => {
//            const full = el.dataset.full ?? el.textContent ?? '';
//            const short = el.dataset.short ?? full;
//            el.textContent = useShort ? short : full;
//        });
//    }

//    // debounce helper
//    function debounce(fn, wait) {
//        let t = null;
//        return function () {
//            clearTimeout(t);
//            t = setTimeout(() => fn.apply(this, arguments), wait);
//        };
//    }

//    adjustSongTitles();
//    window.addEventListener('resize', debounce(adjustSongTitles, 120));

//    // Отримуємо всі рядки таблиці та всі контейнери аналізу мелодій
//    let rows = document.querySelectorAll("table tbody tr");
//    let melodies = document.querySelectorAll("div[id^='melody']");

//    // Спочатку ховаємо всі мелодії
//    melodies.forEach(melody => melody.style.display = "none");

//    // Допоміжна: рендер нотації для конкретного контейнера melodyN
//    function renderNotationForMelodyContainer(melodyContainer) {
//        if (!melodyContainer) return;

//        // шукаємо notation-контейнер усередині блоку мелодії
//        const notationEl = melodyContainer.querySelector("div[id^='notation_match_']");
//        if (!notationEl) return;

//        // уникаємо повторного рендеру
//        if (notationEl.dataset.rendered === "1") return;

//        const midiUrl = notationEl.dataset.midiUrl;
//        const startPos = parseInt(notationEl.dataset.startPosition ?? "0", 10) || 0;
//        const commentsId = notationEl.dataset.commentsId;
//        const commentsElId = commentsId || (function () {
//            const id = notationEl.id || "";
//            return id.replace("notation_match_", "comments_match_");
//        })();

//        if (!midiUrl || typeof window.renderMidiSegmentFromUrl !== "function") {
//            console.warn("Notation render skipped: midiUrl or renderer is missing");
//            return;
//        }

//        try {
//			console.log(`search.js Rendering notation for ${notationEl.id} from ${midiUrl} starting at ${startPos}`);
//            // Use the same HEIGHT/TOPPADDING used by Details so scale matches
//            window.renderMidiSegmentFromUrl(
//                midiUrl,
//                startPos,
//                notationEl.id,
//                commentsElId,
//                1200,  // GENERALWIDTH (keep as before)
//                150,  // HEIGHT 
//                20,   // TOPPADDING -> match Details (was 10)
//                250,  // BARWIDTH
//                60,   // CLEFZONE
//                10,   // Xmargin
//                12    // barsToRender
//            );
//            notationEl.dataset.rendered = "1";
//        } catch (e) {
//            console.warn("renderMidiSegmentFromUrl failed", e);
//        }
//    }

//    // helper: select melody by index (shared by row click and nav buttons)
//    function selectMelodyByIndex(index) {
//        if (!melodies || melodies.length === 0) return;
//        index = Math.max(0, Math.min(index, melodies.length - 1));

//        // hide all
//        melodies.forEach(m => m.style.display = 'none');
//        // show selected
//        const selected = document.getElementById(`melody${index}`);
//        if (selected) {
//            selected.style.display = 'block';
//            renderNotationForMelodyContainer(selected);
//        }

//        // update row highlight using CSS class to override td backgrounds
//        rows.forEach(r => r.classList.remove('selected-row'));
//        if (rows[index]) rows[index].classList.add('selected-row');
//    }

//    // Відображаємо першу знайдену мелодію (якщо є) і рендеримо її нотацію
//    if (melodies.length > 0) {
//        selectMelodyByIndex(0);
//    }

//    // Додаємо обробник подій для кожного рядка
//    rows.forEach((row, index) => {
//        row.addEventListener("click", function () {
//            console.log(`displaying melody ${index}`);
//            selectMelodyByIndex(index);
//        });
//    });

//	// Рендер знайдених мелодій по кліку на них
//    const container = document.getElementById('matchedMelodiesContainer');
//    if (container && typeof window.renderMidiFromUrl === 'function') {
//        function clearAllNotations(){
//            document.querySelectorAll('[id^="notation_match_"]').forEach(div => div.innerHTML = '');
//            document.querySelectorAll('[id^="comments_match_"]').forEach(div => div.innerHTML = '');
//        }
//        container.addEventListener('click', function(e){
//            const melodyBlock = e.target.closest('.melody-block');
//            // navigation buttons handling: prev / next
//            const navBtn = e.target.closest('.song-nav');
//            if (navBtn && container.contains(navBtn)) {
//                e.preventDefault();
//                // find current visible index
//                const currentIndex = Array.from(melodies).findIndex(m => m.style.display && m.style.display !== 'none');
//                if (navBtn.classList.contains('prev')) {
//                    selectMelodyByIndex((currentIndex === -1 ? 0 : currentIndex) - 1);
//                } else if (navBtn.classList.contains('next')) {
//                    selectMelodyByIndex((currentIndex === -1 ? 0 : currentIndex) + 1);
//                }
//                return;
//            }

//            if (!melodyBlock || !container.contains(melodyBlock)) return;
//            const notationDiv = melodyBlock.querySelector('[id^="notation_match_"]');
//            if (!notationDiv) return;
//            const midiUrl = notationDiv.getAttribute('data-midi-url');
//            const commentsId = notationDiv.getAttribute('data-comments-id');
//            if (!midiUrl) return;
//            try {
//                clearAllNotations();
//                window.renderMidiFromUrl(midiUrl, 12, notationDiv.id, commentsId);
//                notationDiv.scrollIntoView({ behavior: 'smooth', block: 'center' });
//            } catch (e) {
//                console.warn('renderMidiFromUrl failed', e);
//            }
//        });
//    }

//	// UI adjustments
//    console.log("adjusting NoteSearch UI");
//    const saveButton = document.getElementById("createMIDI");
//    if (saveButton) {
//        saveButton.textContent = "попередній перегляд";
//    }

//    // Bind LCS gap display and enable/disable based on algorithm
//    const gap = document.getElementById('lcsGap');
//    const gapVal = document.getElementById('lcsGapVal');
//    const algSub = document.getElementById('algSubstring');
//    const algSeq = document.getElementById('algSubsequence');

//    function updateGapEnabled() {
//        const enabled = algSeq && algSeq.checked;
//        if (gap) gap.disabled = !enabled;
//        const labelWrap = gap ? gap.nextElementSibling : null; // span with value
//        if (labelWrap) {
//            if (enabled) labelWrap.classList.remove('text-muted');
//            else labelWrap.classList.add('text-muted');
//        }
//    }

//    if (gap && gapVal) {
//        gap.addEventListener('input', () => gapVal.textContent = gap.value);
//    }
//    if (algSub) algSub.addEventListener('change', updateGapEnabled);
//    if (algSeq) algSeq.addEventListener('change', updateGapEnabled);
//    updateGapEnabled();

//    // Hide loading and show results after DOM is ready
//    const loadingEl = document.getElementById('searchLoading');
//    const resultsEls = document.querySelectorAll('.notescontainer, .table');
//    if (loadingEl) loadingEl.style.display = 'none';
//    resultsEls.forEach(el => el.classList.remove('hide'));
//});
