// працює для сторінки розширеного пошуку (search)
// відображає детальний аналіз мелодії, яку обираємо по кліку миші

document.addEventListener("DOMContentLoaded", function () {
    // Отримуємо всі рядки таблиці та всі контейнери аналізу мелодій
    let rows = document.querySelectorAll("table tbody tr");
    let melodies = document.querySelectorAll("div[id^='melody']");

    // Спочатку ховаємо всі мелодії
    melodies.forEach(melody => melody.style.display = "none");

    // Допоміжна: рендер нотації для конкретного контейнера melodyN
    function renderNotationForMelodyContainer(melodyContainer) {
        if (!melodyContainer) return;

        // шукаємо notation-контейнер усередині блоку мелодії
        const notationEl = melodyContainer.querySelector("div[id^='notation_match_']");
        if (!notationEl) return;

        // уникаємо повторного рендеру
        if (notationEl.dataset.rendered === "1") return;

        const midiUrl = notationEl.dataset.midiUrl;
        const startPos = parseInt(notationEl.dataset.startPosition ?? "0", 10) || 0;
        const commentsId = notationEl.dataset.commentsId;
        const commentsElId = commentsId || (function () {
            const id = notationEl.id || "";
            return id.replace("notation_match_", "comments_match_");
        })();

        if (!midiUrl || typeof window.renderMidiSegmentFromUrl !== "function") {
            console.warn("Notation render skipped: midiUrl or renderer is missing");
            return;
        }

        try {
            window.renderMidiSegmentFromUrl(
                midiUrl,
                startPos,
                notationEl.id,
                commentsElId,
                900,  // GENERALWIDTH
                200,  // HEIGHT per row
                20,   // TOPPADDING
                250,  // BARWIDTH
                60,   // CLEFZONE
                10,   // Xmargin
                12    // barsToRender
            );
            notationEl.dataset.rendered = "1";
        } catch (e) {
            console.warn("renderMidiSegmentFromUrl failed", e);
        }
    }

    // Відображаємо першу знайдену мелодію (якщо є) і рендеримо її нотацію
    if (melodies.length > 0) {
        melodies[0].style.display = "block";
        rows[0]?.style && (rows[0].style.backgroundColor = "lightyellow");
        renderNotationForMelodyContainer(melodies[0]);
    }

    // Додаємо обробник подій для кожного рядка
    rows.forEach((row, index) => {
        row.addEventListener("click", function () {
            console.log(`displaying melody ${index}`);
            // Ховаємо всі мелодії
            melodies.forEach(melody => melody.style.display = "none");

            // Відображаємо обрану мелодію
            let selectedMelody = document.getElementById(`melody${index}`);
            if (selectedMelody) {
                selectedMelody.style.display = "block";
                renderNotationForMelodyContainer(selectedMelody);
            }

            rows.forEach(r => r.style.backgroundColor = "");
            row.style.backgroundColor = "lightyellow";
        });
    });
});
