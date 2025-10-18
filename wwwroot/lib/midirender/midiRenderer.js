// midiRenderer.js

const logEvent = (msg) => {
    try {
        const stepReadEl = document.getElementById("stepRead");
        if (stepReadEl) {
            stepReadEl.innerHTML += msg;
        }
    } catch (e) {
        console.warn('logEvent failed:', e);
    }
};


/**
 * renderMidiFromUrl
 * -----------------
 * Convenience helper that loads a MIDI file by URL (fetch), wraps it into a File
 * object (so existing drawScore logic & FileReader path can be reused), then calls drawScore.
 * All layout parameters mirror drawScore and have the same defaults.
 *
 * @param {string} midiUrl - URL (relative or absolute) to .mid file
 * @param {number} [maxBarsToRender=1000] - Maximum number of measures to render (1000 means full rendering).
 * @param {string} [ELEMENT_FOR_RENDERING='notation'] - target element id for notation
 * @param {string} [ELEMENT_FOR_COMMENTS='comments'] - target element id for messages
 * @param {number} [GENERALWIDTH=1200]
 * @param {number} [HEIGHT=200]
 * @param {number} [TOPPADDING=20]
 * @param {number} [BARWIDTH=250]
 * @param {number} [CLEFZONE=60]
 * @param {number} [Xmargin=10]
 * @returns {Promise<void>}
 *
 * Usage example:
 *   await renderMidiFromUrl('/Uploads/example.mid', 8);
 */
async function renderMidiFromUrl(
    midiUrl,
    maxBarsToRender = 1000,
    ELEMENT_FOR_RENDERING = 'notation',
    ELEMENT_FOR_COMMENTS = 'comments',
    GENERALWIDTH = 1200,
    HEIGHT = 200,
    TOPPADDING = 20,
    BARWIDTH = 250,
    CLEFZONE = 60,
    Xmargin = 10
) {
    try {
        if (!midiUrl) throw new Error('midiUrl is required');
        const resp = await fetch(midiUrl);
        if (!resp.ok) throw new Error(`Failed to fetch MIDI (${resp.status})`);
        const blob = await resp.blob();
        const filename = midiUrl.split('/').pop() || 'remote.mid';
        const file = new File([blob], filename, { type: blob.type || 'audio/midi' });
        drawScore(file, ELEMENT_FOR_RENDERING, ELEMENT_FOR_COMMENTS, GENERALWIDTH, HEIGHT, TOPPADDING, BARWIDTH, CLEFZONE, Xmargin, maxBarsToRender);
    } catch (err) {
        console.error('renderMidiFromUrl error:', err);
        const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);  
        if (commentsDiv) {
            commentsDiv.innerHTML = `Error loading MIDI: ${err.message}`;
        }
    }
}

/**
 * renderMidiSegmentFromUrl
 * ------------------------
 * Loads MIDI by URL and renders a segment starting from the measure nearest to (and <=) the
 * measure that contains the startNoteIndex-th NoteOn event. Start measure is snapped to multiples of 4 (0,4,8,...).
 * All layout parameters mirror drawScore and have the same defaults.
 * @param {string} midiUrl - URL (relative or absolute) to .mid file
 * @param {number} [startNoteIndex=0] - zero-based index of NoteOn event to start from
 * @param {string} [ELEMENT_FOR_RENDERING='notation'] - target element id for notation
 * @param {string} [ELEMENT_FOR_COMMENTS='comments'] - target element id for messages
 * @param {number} [GENERALWIDTH=1200]
 * @param {number} [HEIGHT=200]
 * @param {number} [TOPPADDING=20]
 * @param {number} [BARWIDTH=250]
 * @param {number} [CLEFZONE=60]
 * @param {number} [Xmargin=10]
 * @param {number} [barsToRender=12] - number of measures to render in the segment
 * @returns {Promise<void>}
 * Usage example:
 *  await renderMidiSegmentFromUrl('/Uploads/example.mid', 10, 'notation', 'comments', 1200, 200, 20, 250, 60, 10, 12);
 */
async function renderMidiSegmentFromUrl(
    midiUrl,
    startNoteIndex = 0,
    ELEMENT_FOR_RENDERING = 'notation',
    ELEMENT_FOR_COMMENTS = 'comments',
    GENERALWIDTH = 1200,
    HEIGHT = 200,
    TOPPADDING = 20,
    BARWIDTH = 250,
    CLEFZONE = 60,
    Xmargin = 10,
    barsToRender = 12
) {
    try {
        if (!midiUrl) throw new Error('midiUrl is required');
        const resp = await fetch(midiUrl);
        if (!resp.ok) throw new Error(`Failed to fetch MIDI (${resp.status})`);
        const arrayBuf = await resp.arrayBuffer();
        const uint8 = new Uint8Array(arrayBuf);

        // Pre-parse to find measure index for the matched note
        const midiData = MidiParser.Uint8(uint8);
        const ticksPerBeat = Array.isArray(midiData.timeDivision) ? 480 : midiData.timeDivision;
        let allEvents = SetEventsAbsoluteTime(midiData);
        ensureEndEvent(allEvents);
        const measureMap = createMeasureMap(allEvents, ticksPerBeat);
        const measures = groupEventsByMeasure(allEvents, measureMap);

        // Find absTime of the startNoteIndex-th NoteOn (velocity > 0)
        let targetAbsTime = 0;
        let count = 0;
        for (const ev of allEvents) {
            if (ev && ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0) {
                if (count === (parseInt(startNoteIndex, 10) || 0)) {
                    targetAbsTime = ev.absTime || 0;
                    break;
                }
                count++;
            }
        }
        // Determine measure index containing this absTime
        const idxKeys = Object.keys(measureMap).map(k => parseInt(k, 10)).sort((a,b)=>a-b);
        let matchedMeasureIdx = 0;
        for (let i = 0; i < idxKeys.length - 1; i++) {
            const start = measureMap[idxKeys[i]];
            const next = measureMap[idxKeys[i + 1]];
            if (targetAbsTime >= start && targetAbsTime < next) { matchedMeasureIdx = idxKeys[i]; break; }
        }
        // Snap to nearest lower multiple of 4
        let startAtMeasureIndex = matchedMeasureIdx - (matchedMeasureIdx % 4);
        if (startAtMeasureIndex < 0) startAtMeasureIndex = 0;

        // Render the segment
        const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
        renderMidiFileToNotation(
            uint8,
            ELEMENT_FOR_RENDERING,
            GENERALWIDTH,
            HEIGHT,
            TOPPADDING,
            BARWIDTH,
            CLEFZONE,
            Xmargin,
            commentsDiv,
            barsToRender,
            startAtMeasureIndex
        );
    } catch (err) {
        console.error('renderMidiSegmentFromUrl error:', err);
        const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
        if (commentsDiv) commentsDiv.innerHTML = `Error loading MIDI: ${err.message}`;
    }
}


/**
 * drawScore
 * ----------
 * Renders a musical score from a MIDI file and displays it in the specified HTML element.
 *
 * @param {File} file - The MIDI file to render.
 * @param {string} ELEMENT_FOR_RENDERING - The ID of the HTML element where the score SVG will be rendered.
 * @param {string} ELEMENT_FOR_COMMENTS - The ID of the HTML element where status or error messages will be displayed.
 * @param {number} [GENERALWIDTH=1200] - The width of the rendered score in pixels.
 * @param {number} [HEIGHT=200] - The height of each stave row in pixels.
 * @param {number} [TOPPADDING=20] - The top padding for the score in pixels.
 * @param {number} [BARWIDTH=250] - The width of each measure/bar in pixels.
 * @param {number} [CLEFZONE=60] - The width reserved for the clef and time signature zone in pixels.
 * @param {number} [Xmargin=10] - The left margin for the score in pixels.
 * @param {number} [maxBarsToRender=1000] - Maximum number of measures to render (1000 means full rendering).
 *
 * Reads the provided MIDI file, parses its contents, and renders the musical notation using VexFlow.
 * Stores the resulting SVG and comments in sessionStorage for later retrieval.
 * Displays success or error messages in the comments element.
 */


function drawScore(file, ELEMENT_FOR_RENDERING, ELEMENT_FOR_COMMENTS, GENERALWIDTH = 1200, HEIGHT = 200, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10, maxBarsToRender = 1000) {
    console.log("FOO: midiRenderer.js - drawScore");
    const notationDiv = document.getElementById(ELEMENT_FOR_RENDERING);
    const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);

    if (file) {
        console.log("drawScore: File selected:", file);
        const reader = new FileReader();
        notationDiv.innerHTML = "";
        commentsDiv.innerHTML = "";
        reader.onload = function (e) {
            console.log("drawScore: File read successfully");
            const uint8 = new Uint8Array(e.target.result);

            try {
                renderMidiFileToNotation(
                    uint8,
                    ELEMENT_FOR_RENDERING,
                    GENERALWIDTH,
                    HEIGHT,
                    TOPPADDING,
                    BARWIDTH,
                    CLEFZONE,
                    Xmargin,
                    commentsDiv,
                    maxBarsToRender,
                    0 // startAtMeasureIndex (default from beginning)
                );

                const svg = notationDiv.querySelector("svg");
                if (svg) {
                    sessionStorage.setItem("notationSVG", svg.outerHTML);
                }

                // Message depends on maxBarsToRender value
                const msg = (maxBarsToRender === 1000)
                    ? "нотне зображення виведено повністю"
                    : `нотне зображення виведено з обмеженням у ${maxBarsToRender} тактів`;
                commentsDiv.innerHTML += msg;
                sessionStorage.setItem("comment", commentsDiv.innerHTML);
            }
            catch (error) {
                console.error("drawScore: Error rendering MIDI file:", error);
                commentsDiv.innerHTML = `Error rendering MIDI file ${error}`;
                sessionStorage.setItem("comment", commentsDiv.innerHTML);
            }
        };
        reader.readAsArrayBuffer(file);
    }
}


/**
 * createmeasureMap
 * ----------------
 * Generates a mapping of measure indexes to the starting ticks for each measure.
 *
 * @param {Array} midiEvents - Array of MIDI event objects (each should have an "absTime" property).
 * @param {number} ticksPerBeat - The number of ticks per quarter note.
 * @returns {Object} An object where each key is a measure index (starting at 0) and the value is the start tick.
 *
 * The function handles time signature changes by processing all time signature events.
 * If no time signature event is found at tick 0, a default 4/4 is inserted.
 */
function createMeasureMap(midiEvents, ticksPerBeat) {
    console.log("FOO: midiRenderer.js - createmeasureMap");
    const stepRead = document.getElementById("stepRead");
    if (stepRead) stepRead.innerHTML = `<p>Step Read</p><br/>`;
    const measure_map = document.getElementById("measureMap");
    if (measure_map) measure_map.innerHTML = `<p>Measure Map</p><br/>`;
    const logMeasure = (msg) => {
        if (measure_map) measure_map.innerHTML += msg + "<br>";
        console.log(msg); 
    };
    

    if (midiEvents.length === 0) return {};

    // Ensure time signature at tick 0
    if (!(midiEvents[0].type === 0xFF && midiEvents[0].metaType === 0x58)) {
        midiEvents.unshift({
            type: 0xFF,
            metaType: 0x58,
            deltaTime: 0,
            absTime: 0,
            data: [4, 2] // 4/4
        });
    }

    const timeSignatureEvents = midiEvents.filter(e => e.type === 0xFF && e.metaType === 0x58);
    const maxAbsTime = midiEvents.reduce((m, e) => e.absTime > m ? e.absTime : m, 0);

    const measureMap = {};
    let barIndex = 0;

    for (let i = 0; i < timeSignatureEvents.length; i++) {
        const ts = timeSignatureEvents[i];
        const numerator = ts.data?.[0] ?? 4;
        const denominator = ts.data?.[1] ? Math.pow(2, ts.data[1]) : 4;
        const ticksPerMeasure = ticksPerBeat * numerator * 4 / denominator;

        const segmentStart = ts.absTime;
        const segmentEnd = (i + 1 < timeSignatureEvents.length)
            ? timeSignatureEvents[i + 1].absTime
            : maxAbsTime;

        for (let tick = segmentStart; tick < segmentEnd; tick += ticksPerMeasure) {
            measureMap[barIndex] = tick;
            barIndex++;            
            logMeasure(`measureMap: Measure ${barIndex} starts at tick ${tick}`);
        }

        // Keep reference to last segment properties for trailing sentinel
        if (i === timeSignatureEvents.length - 1) {
            const lastStart = measureMap[barIndex - 1];
            const sentinelStart = lastStart + ticksPerMeasure; // always add sentinel
            measureMap[barIndex] = sentinelStart;
            console.log(`Sentinel (end) start added at tick ${sentinelStart} (index ${barIndex})`);
            logMeasure(`Sentinel measure start at tick ${sentinelStart}`);
        }
    }

    return measureMap;
}

// ----------------------
// Helper: ensure EndTrack event exists
// ----------------------
function groupEventsByMeasure(allEvents, measureMap) {
    console.log("FOO: midiRenderer.js - groupEventsByMeasure");
    const measures = [];
    const indices = Object.keys(measureMap)
        .map(k => parseInt(k))
        .sort((a, b) => a - b);
    // Last index is sentinel (start of measure after last real), so iterate to length-1
    for (let i = 0; i < indices.length - 1; i++) {
        const startTick = measureMap[indices[i]];
        const nextTick = measureMap[indices[i + 1]];
        const measureEvents = allEvents.filter(ev => ev.absTime >= startTick && ev.absTime < nextTick);
        measures.push(measureEvents);
    }
    return measures;
}

// Helper: check a measure for any Note On with velocity > 0
function hasNoteOn(measure) {
    console.log("FOO: midiRenderer.js - hasNoteOn");
    return measure.some(ev => ev.type === 0x9 && ev.data && ev.data.length > 1 && ev.data[1] !== 0);
}




// ФУНКЦІЯ ВІЗУАЛІЗАЦІЇ MIDI ФАЙЛУ
// ----------------------
// Приймає Uint8Array з MIDI файлом, ID елемента для рендерингу, ширину і висоту нотного стану та інші параметри.
// Використовує бібліотеки MidiParser и VexFlow для парсингу MIDI та рендерингу нот.
// Рендерить нотний стан у вказаний HTML елемент.
// Параметри:
// - uint8: Uint8Array з MIDI файлом.
// - ELEMENT_FOR_RENDERING: ID HTML елемента для рендерингу нот.
// - GENERALWIDTH: Ширина нотного стану в пікселях.
// - HEIGHT: Висота кожного рядка нотного стану в пікселях.
// - TOPPADDING: Верхній відступ нотного стану в пікселях.
// - BARWIDTH: Ширина кожного такту в пікселях.
// - CLEFZONE: Ширина зони для ключа та розміру такту в пікселях.
// - Xmargin: Лівий відступ нотного стану в пікселях.
// - commentsDiv: HTML елемент для виведення статусу або помилок.
// - maxBarsToRender: Максимальна кількість тактів для рендерингу (1000 - повний рендеринг).
// - startAtMeasureIndex: Індекс такту, з якого починати рендеринг (вирівняний до кратного 4).
// Повертає: void
// ----------------------
// Використовує SetEventsAbsoluteTime, createmeasureMap, groupEventsByMeasure та інші допоміжні функції.
// Рендеринг відбувається асинхронно з використанням setTimeout для уникнення блокування UI.
// ---------------------


function renderMidiFileToNotation(uint8, ELEMENT_FOR_RENDERING, GENERALWIDTH, HEIGHT = 200, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10, commentsDiv, maxBarsToRender = 1000, startAtMeasureIndex = 0) {
    console.log("FOO: midiRenderer.js - renderMidiFileToNotation");
    if (!ELEMENT_FOR_RENDERING) {
        throw new Error(`Element with id ${ELEMENT_FOR_RENDERING} not found.`);
    }
    let midiData = MidiParser.Uint8(uint8);
    let ticksPerBeat = Array.isArray(midiData.timeDivision) ? 480 : midiData.timeDivision;
    let allEvents = SetEventsAbsoluteTime(midiData);

    // Перевірка наявності EndTrack
    ensureEndEvent(allEvents);

    const measureMap = createMeasureMap(allEvents, ticksPerBeat);
    const measures = groupEventsByMeasure(allEvents, measureMap);

	// обчислюємо індекс початкового такту, вирівняного до кратного 4
    let startIdx = parseInt(startAtMeasureIndex, 10);
    if (!isFinite(startIdx) || startIdx < 0) startIdx = 0;
    startIdx = startIdx - (startIdx % 4);
    if (startIdx > measures.length - 1) startIdx = Math.max(0, measures.length - 1);

	// обчислюємо скільки тактів залишилось від startIdx до кінця
    const remaining = Math.max(0, measures.length - startIdx);
    const renderCount = (typeof maxBarsToRender === 'number' && isFinite(maxBarsToRender) && maxBarsToRender !== 1000)
        ? Math.min(remaining, maxBarsToRender)
        : remaining;

    // Витягуємо вікно тактів для рендерингу
	// починаючи з startIdx, довжиною renderCount
    const measuresWindow = measures.slice(startIdx, startIdx + renderCount);

    // Тримаємо лише ті такти, які містять Note On події
    const measuresToRender = [...measuresWindow];
    while (measuresToRender.length > 0 && !hasNoteOn(measuresToRender[measuresToRender.length - 1])) {
        console.log("Pruning trailing empty measure without Note On events (pre-height)");
        measuresToRender.pop();
    }
    const effectiveCount = measuresToRender.length;

	// створюємо новий measureMap для заданого діапазону
    const slicedMap = {};
    for (let i = 0; i <= effectiveCount; i++) {
        slicedMap[i] = measureMap[startIdx + i];
    }

    // ВИКОРИСТАТИ ФАКТИЧНУ ШИРИНУ КОНТЕЙНЕРА
    const MIN_SCORE_WIDTH = Math.max(320, CLEFZONE + BARWIDTH + Xmargin * 2);

    const target = document.getElementById(ELEMENT_FOR_RENDERING);
    const containerWidth = (target && target.clientWidth) ? target.clientWidth : 0;

    const effectiveWidth = Math.max(
        MIN_SCORE_WIDTH,
        containerWidth || GENERALWIDTH || 1200
    );

    // rendererUtils.js
    const rowsHeight = calculateRequiredHeight(measuresToRender, effectiveWidth, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin);

    setTimeout(() => {
        const factory = new Vex.Flow.Factory({
            renderer: {
                elementId: ELEMENT_FOR_RENDERING,
                width: effectiveWidth,
                height: rowsHeight
            }
        });

        const context = factory.getContext();
        const score = factory.EasyScore();

        // РЕНДЕРИНГ ТАКТІВ
        // ----------------------
        renderMeasures(slicedMap, measuresToRender, ticksPerBeat, score, context, Xmargin, TOPPADDING, BARWIDTH, CLEFZONE, HEIGHT, effectiveWidth, commentsDiv);

        // ----------------------
        // ПІСЛЯ-ОБРОБКА SVG
        // ----------------------
		// Динамічно підганяє висоту SVG під фактичний вміст, щоб уникнути зайвих відступів.
        try {
            const container = document.getElementById(ELEMENT_FOR_RENDERING);
            const svg = container && container.querySelector('svg');
            if (svg && typeof svg.getBBox === 'function') {
                const adjust = () => {
                    const bbox = svg.getBBox();
                    const contentH = Math.max(rowsHeight, Math.ceil((bbox ? bbox.height : 0) + 10));
                    if (contentH > 0) {
                        const vb = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
                        const vbW = vb && vb.width ? vb.width : (parseFloat(svg.getAttribute('width')) || effectiveWidth);
                        if (vbW > 0) svg.setAttribute('viewBox', `0 0 ${vbW} ${contentH}`);
                        svg.setAttribute('height', String(contentH));
                        if (svg.style) {
                            svg.style.height = contentH + 'px';
                            svg.style.maxHeight = 'none';
                        }
                        if (container && container.style) {
                            container.style.minHeight = contentH + 'px';
                            container.style.height = contentH + 'px';
                            container.style.maxHeight = 'none';
                        }
                    }
                };
                // run multiple times to ensure everything settles
                adjust();
                if (typeof requestAnimationFrame === 'function') requestAnimationFrame(adjust);
                setTimeout(adjust, 50);  // one more time after a short delay
            }
        } catch (e) { console.warn('post-fix svg size failed', e); }
    }, 0);

    return {
        totalMeasures: measures.length,
        renderedMeasures: effectiveCount,
        limited: effectiveCount < remaining,
        maxBarsToRender,
        startAtMeasureIndex: startIdx
    };
}

// ФУНКЦІЯ РЕНДЕРИНГУ ТАКТІВ
// ----------------------
// Приймає мапу тактів, масив тактів, кількість тіксів на біт, об'єкт score і context з VexFlow та інші параметри.
// Рендерить кожен такт у нотний стан з урахуванням активних нот, пауз, лігатур та інших музичних елементів.
// Параметри:
// - measureMap: Об'єкт, що відображає індекси тактів у початкові тіки.
// - measures: Масив тактів, де кожен такт — це масив MIDI-подій.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - score: Об'єкт score з VexFlow для створення нот.
// - context: Контекст рендерингу з VexFlow.
// - Xmargin: Лівий відступ нотного стану в пікселях.
// - TOPPADDING: Верхній відступ нотного стану в пікселях.
// - BARWIDTH: Ширина кожного такту в пікселях.
// - CLEFZONE: Ширина зони для ключа та розміру такту в пікселях.
// - HEIGHT: Висота кожного рядка нотного стану в пікселях.
// - GENERALWIDTH: Загальна ширина нотного стану в пікселях.
// - commentsDiv: HTML елемент для виведення статусу або помилок.
// Повертає: void
// ----------------------


function renderMeasures(measureMap, measures, ticksPerBeat, score, context, Xmargin, TOPPADDING, BARWIDTH, CLEFZONE, HEIGHT, GENERALWIDTH, commentsDiv) {
    console.log("FOO: midiRenderer.js - renderMeasures ");
    let Xposition = Xmargin;
    let Yposition = TOPPADDING;
    let thresholdGap = ticksPerBeat / 8;


    // Початковий розмір такту (за замовчуванням 4/4)
    let currentNumerator = 4;
    let currentDenominator = 4;
    const activeNotes = {};

    // Флаг для відстеження першого такту на кожному рядку
    let isFirstMeasureInRow = true;
    // Поточний key signature (оновлюється при meta подіях 0x59)
    let currentKeySig = null;
    let meanBarWidth = null;

    meanBarWidth = GetMeanBarWidth(BARWIDTH, measures);


    // Перебирає усі такти
    // ----------------------
    // Для кожного такту:
    // - Оновлює розмір такту, якщо є відповідна подія.
    // - Визначає початковий час такту з measureMap.
    // - Встановлює початковий час для активних нот з попереднього такту.
    // - Налаштовує позицію X та Y для нотного стану.
    // - Створює нотний стан (stave) з ключем та розміром такту.
    // - Обробляє кожну подію в такті:
    //   - Для Note On: додає ноту, обробляє паузи між нотами, перевіряє наявність активних нот.
    //   - Для Note Off: завершує ноту, додає лігатури, видаляє ноту з активних.
    // - Якщо є активні ноти в кінці такту, домалюємо їх до кінця такту.
    // - Якщо немає активних нот, перевіряє наявність пропущених пауз і додає їх.
    // - Рендерить такт у нотний стан.
    // - Оновлює позицію X для наступного такту.
    // ----------------------
    measures.forEach((measure, index) => {
        measure = normalizeMetaEvents(measure);
                       
        let keySignatureChanged;
        let keySigName;
        ({ keySignatureChanged, keySigName, currentKeySig } = getKeySignatureChanges(measure, currentKeySig));

        
        const stepRead = document.getElementById("stepRead");
        logEvent(`<br/>LE: Processing measure ${index + 1}`);
        logMeasureEvents(measure);

        // Зберігаємо попередній розмір такту для порівняння
        const previousNumerator = currentNumerator;
        const previousDenominator = currentDenominator;

        ({ currentNumerator, currentDenominator } = adjustTimeSignature(measure, currentNumerator, currentDenominator));
        let ticksPerMeasure = CalculateTicksPerMeasure(currentNumerator, ticksPerBeat, currentDenominator);

        // Перевіряємо, чи змінився розмір такту
        const timeSignatureChanged = (currentNumerator !== previousNumerator || currentDenominator !== previousDenominator);

		// Отримуємо абсолютний час початку такту з measureMap
        const barStartAbsTime = (measureMap[index] !== undefined) ? measureMap[index] : index * ticksPerMeasure;

        // --- Встановлюємо startTime для активних нот на початку такту ---
        processActiveNotesFromPreviousBar(activeNotes, index, barStartAbsTime);

        // NEW: флаг, чи починався такт зі звучання (ноти, перенесені з попер. такту)
        const measureStartedWithActive = Object.keys(activeNotes).length > 0;

        // Перевіряємо, чи потрібно перейти на новий рядок
        const oldYposition = Yposition;
        ({ Xposition, Yposition } = adjustXYposition(Xposition, GENERALWIDTH, meanBarWidth, Yposition, HEIGHT, Xmargin, index, barStartAbsTime));   
        
        // Якщо Y позиція змінилась, значить почався новий рядок
        if (Yposition !== oldYposition) {
            isFirstMeasureInRow = true;
            console.log(`New row started at measure ${index + 1}, Y position: ${Yposition}`);
        }

        let STAVE_WIDTH = adjustStaveWidth(meanBarWidth, index, CLEFZONE, isFirstMeasureInRow, timeSignatureChanged);

        // Визначити потрібний ключ (clef) для цього такту на основі висот нот
        const clefChoice = decideClefForMeasure(measure);

        // Створюємо нотний стан (stave)
        // Додаємо ключ та розмір такту, якщо потрібно (тепер також і key signature)
        const stave = setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow, timeSignatureChanged, keySignatureChanged, keySigName, clefChoice);

        // Малюємо нотний стан
        stave.setContext(context).draw();

        const notes = [];
        const ties = [];
        let lastNoteOffTime = 0;
        let lastNoteOnTime = -1;

        // NEW: поточний стан показаних альтерацій у межах такту (letter+octave -> '#','b','n')
        const measureAccState = {};
       
		// Обробляємо кожну подію в такті
        renderMeasure();


        // Якщо є "активні" ноти (activeNotes), домалюємо їх до кінця такту
        if (Object.keys(activeNotes).length > 0) {
            const nextBoundary = measureMap[index + 1];
            const measureEndTick = (nextBoundary !== undefined) ? nextBoundary : barStartAbsTime + ticksPerMeasure; // fallback
            console.log(`AN: bar ${index + 1}, measureEndTick = ${measureEndTick}`)
            if (stepRead) stepRead.innerHTML += `<i> act.note</i>`;
            drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties, currentKeySig, measureAccState, clefChoice);
        }

        
        // Якщо остання нота не доходить до кінця такту, додаємо паузу        
        if (Object.keys(activeNotes).length === 0) {
            addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat);
        }

        // Перевіряємо, чи потрібно скоротити останню ноту/паузу, якщо вона виходить за межі такту
        correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat, clefChoice);

        console.log(`start to draw measure ${index + 1}`);

        // Передаємо ticksPerBeat в drawMeasure
        drawMeasure(notes, meanBarWidth, context, stave, ties, index, commentsDiv, currentNumerator, currentDenominator, ticksPerBeat);

        Xposition += STAVE_WIDTH;
        
        // Скидаємо флаг після обробки першого такту в рядку
        isFirstMeasureInRow = false;


        // Трансформує MIDI-події у нотний текст
        // ----------------------
        // // Для кожної події:
        // - Якщо це Note On з velocity > 0:
        //   - Додає паузу на початку такту, якщо це перша нота і немає активних нот.
        //   - Додає паузи між останньою Note Off і поточною Note On.
        //   - Перевіряє, чи збігається час з останньою Note On.
        //   - Додає ноту до активних нот.
        // - Якщо це Note Off або Note On з velocity = 0:
        //   - Завершує ноту, обчислює її тривалість.
        //   - Додає ліги між нотами.
        //   - Видаляє ноту з активних нот.
        // - Додає обчислені ноти і паузи до масиву notes[]        
        // ----------------------


        function renderMeasure() {
            console.log("FOO: midiRenderer.js - renderMeasure"); 
            let isFirstNoteInMeasure = true; 

            const isNoteOn = (ev) => ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0;
            const isNoteOff = (ev) => ev.type === 0x8 || (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0);

            // Сортуємо: за часом; у межах одного часу спершу OFF, потім ON
            const sorted = [...measure].sort((a, b) => {
                if (a.absTime !== b.absTime) return a.absTime - b.absTime;
                const rank = (ev) => isNoteOff(ev) ? 0 : (isNoteOn(ev) ? 1 : 2);
                return rank(a) - rank(b);
            });

            // Захист від дубльованих OFF для однієї ноти на одному тіці
            const closedAtTick = new Set();

            sorted.forEach((event) => {
                console.log("MIDI Event:", event);

                if (isNoteOn(event)) {
                    const pitch = event.data[0];
                    if (stepRead) stepRead.innerHTML += ` on ${pitch} <span class="tick">[${event.absTime}]</span>`;

                    // Add starting rest only if the bar did NOT start with sustained notes
                    if (isFirstNoteInMeasure && !measureStartedWithActive) {
                        const relTime = event.absTime - barStartAbsTime;
                        if (relTime > thresholdGap) {
                            AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime);
                            // mark timeline filled up to this first note (absolute time)
                            lastNoteOffTime = event.absTime;
                        }
                        isFirstNoteInMeasure = false;
                    } else if (isFirstNoteInMeasure) {
                        // bar began with sustained notes -> just flip the flag
                        isFirstNoteInMeasure = false;
                    }

                    // Add rests between previous end and current event (if any)
                    addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes);
                    checkConcide(event, lastNoteOnTime);

                    activeNotes[pitch] = event.absTime;
                    lastNoteOnTime = event.absTime;
                }
                else if (isNoteOff(event)) {
                    const pitch = event.data[0];
                    const key = `${pitch}@${event.absTime}`;
                    if (closedAtTick.has(key)) return; // дублікат
                    closedAtTick.add(key);

                    const startTime = activeNotes[pitch];
                    if (stepRead) stepRead.innerHTML += ` off ${pitch} <span class="tick">[${event.absTime}]</span>`;

                    if (startTime !== undefined) {
                        const durationTicks = event.absTime - startTime;
                        if (durationTicks > 0) {
                            const durationsCode = getDurationFromTicks(durationTicks, ticksPerBeat);
                            const { key: vexKey, accidental } = midiNoteToVexFlowWithKey(pitch, currentKeySig);

                            const nominalTicksArr = durationsCode.map(dc => calculateTicksFromDuration(dc, ticksPerBeat));
                            const nominalSum = nominalTicksArr.reduce((a, b) => a + b, 0) || 1;

                            let previousNote = null;
                            durationsCode.forEach((durationCode, pieceIdx) => {
                                const accToDraw = decideAccidentalForNote(vexKey, accidental, currentKeySig, measureAccState, pieceIdx);
                                const note = processNoteElement(durationCode, vexKey, accToDraw, clefChoice);
                                applyAutoStem(note, durationCode);

                                const allocatedTicks = Math.round(durationTicks * (nominalTicksArr[pieceIdx] / nominalSum));
                                note.__srcTicks = allocatedTicks;

                                notes.push(note);

                                // Якщо є попередня нота, додаємо лігу
                                AddTie(previousNote, ties, note);

                                previousNote = note;
                            });


                        }
                        // lastNoteOffTime is absolute absTime (end of the note we just processed)
                        lastNoteOffTime = event.absTime;

                    }
                    else { console.log(`starttime for ${pitch} is ${startTime}`); }
                    if (activeNotes[pitch] !== undefined) {
                        console.log(`deleting active note with pitch ${pitch}`);
                        delete activeNotes[pitch];
                    }
                }
            });
        }
    });

};

// ----------------------
// Допоміжні функції для обробки ключових знаків (key signatures)
// ----------------------
function getKeySignatureChanges(measure, currentKeySig) {
    let ks = updateKeySignatureFromEvents(measure);
    if (ks) { console.log(`ks: Tonality: ${ks.sf}, Mode: ${ks.mi}`); }
    let keySignatureChanged = false;
    if (ks) {
        if (!currentKeySig || currentKeySig.sf !== ks.sf || currentKeySig.mi !== ks.mi) {
            currentKeySig = ks;
            keySignatureChanged = true;
            console.log(`Key signature changed -> sf:${ks.sf} mi:${ks.mi}`);
        }
    }
    const keySigName = currentKeySig ? mapKeySignatureName(currentKeySig.sf, currentKeySig.mi) : null;
    return { keySignatureChanged, keySigName, currentKeySig };
}

// ----------------------
// Допоміжні функції для рендеринга нотного стану з MIDI файлу
// Скорочення нот, якщо виходять за межі такту
// ----------------------

function correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat, clef) {
    console.log("FOO: midiRenderer.js - correctExtraNotes");
    if (notes.length === 0) return;
    
    // Обчислюємо загальну тривалість всіх нот у тіках з урахуванням крапок
    let totalTicks = notes.reduce((sum, note) => sum + getTotalTicksForNote(note, ticksPerBeat), 0);
    
    console.log(`AN: Total measure ticks: ${totalTicks}, allowed: ${ticksPerMeasure}`);
    
    // Якщо загальна тривалість перевищує такт, скорочуємо останню ноту
    if (totalTicks > ticksPerMeasure && notes.length > 0) {
        const lastNote = notes[notes.length - 1];
        const lastNoteDuration = lastNote.getDuration();
        const isRest = lastNoteDuration.endsWith('r');
        
        // Обчислюємо фактичну тривалість останньої ноти з урахуванням крапок
        const lastNoteTicks = getTotalTicksForNote(lastNote, ticksPerBeat);
        
        // Обчислюємо скільки тіків залишається для останньої ноти
        const excessTicks = totalTicks - ticksPerMeasure;
        const remainingTicks = lastNoteTicks - excessTicks;
        
        console.log(`Last note excess: ${excessTicks}, remaining: ${remainingTicks}`);
        
        if (remainingTicks > 0) {
            // Знаходимо найближчу підходящу тривалість для залишкових тіків
            const newDurations = (typeof ticksToCleanDurations === 'function')
                ? ticksToCleanDurations(remainingTicks, ticksPerBeat)
                : getDurationFromTicks(remainingTicks, ticksPerBeat);
            if (newDurations.length > 0) {
                const newDuration = newDurations[0]; // Беремо першу (найбільшу) тривалість
                const finalDuration = isRest ? newDuration + 'r' : newDuration;
                
                // Створюємо нову ноту з скороченою тривалістю
                if (isRest) {
                    const newRest = createRest(newDuration);
                    if (newRest) {
                        notes[notes.length - 1] = newRest;
                        console.log(`Shortened last rest to: ${finalDuration}`);
                    }
                } else {
                    // Для нот потрібно зберегти висоту та знаки альтерації
                    const keys = lastNote.getKeys();
                    // При створенні укоротженої ноти передаємо clef
                    const newNote = processNoteElement(newDuration, keys[0], null, clef);
                    if (newNote) {
                        // Копіюємо знаки альтерації з оригінальної ноти
                        const modifiers = lastNote.getModifiers();
                        modifiers.forEach(modifier => {
                            if (modifier.constructor.name === 'Accidental') {
                                newNote.addModifier(modifier);
                            }
                        });
                        // APPLY AUTO STEM for shortened note
                        applyAutoStem(newNote, newDuration);
                        notes[notes.length - 1] = newNote;
                        console.log(`AN: Shortened last note to: ${newDuration}`);
                    }
                }
            }
        } else {
            // Якщо залишкових тіків недостатньо, видаляємо останню ноту
            console.log("AN: Removing last note - no remaining ticks");
            notes.pop();
        }
    }
}

function processActiveNotesFromPreviousBar(activeNotes, index, barStartAbsTime) {
    console.log("FOO: midiRenderer.js - processActiveNotesFromPreviousBar");
    if (Object.keys(activeNotes).length > 0 && index > 0) {
        console.log("AN: active notes from previous bar:", activeNotes);
        Object.keys(activeNotes).forEach(pitch => {
            activeNotes[pitch] = barStartAbsTime;
        });
    } else {
        console.log("AN: no active notes from previous bar");
    }
}

function drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties, currentKeySig, measureAccState, clef = 'treble') {
    console.log("FOO: midiRenderer.js - drawActiveNotes");
        // Домалювати всі ноти, які залишилися активними до кінця такту
    Object.keys(activeNotes).forEach(pitch => {
        const stepRead = document.getElementById("stepRead");
        console.log(`AN: trying to process ${pitch}, measureEndTick = ${measureEndTick}`)
        const startTime = activeNotes[pitch];
        const durationTicks = measureEndTick - startTime;
        if (durationTicks >= 0) {
            const durationsCode = getDurationFromTicks(durationTicks, ticksPerBeat);
            const { key, accidental } = midiNoteToVexFlowWithKey(Number(pitch), currentKeySig);
            console.log(`AN: found activeNote ${pitch} (${key}${accidental})`);

            const nominalTicksArr = durationsCode.map(dc => calculateTicksFromDuration(dc, ticksPerBeat));
            const nominalSum = nominalTicksArr.reduce((a, b) => a + b, 0) || 1;

            let previousNote = null;
            durationsCode.forEach((durationCode, pieceIdx) => {
                const accToDraw = decideAccidentalForNote(key, accidental, currentKeySig, measureAccState, pieceIdx);
                const note = processNoteElement(durationCode, key, accToDraw, clef);
                applyAutoStem(note, durationCode);

                const allocatedTicks = Math.round(durationTicks * (nominalTicksArr[pieceIdx] / nominalSum));
                note.__srcTicks = allocatedTicks;

                notes.push(note);
                AddTie(previousNote, ties, note);
                previousNote = note;
            });
        }
        else {
            console.log(`AN: could not process activeNote, duration:  ${durationTicks} ticks`);
            if (stepRead) stepRead.innerHTML += `=${durationTicks} ticks: ${measureEndTick} - ${startTime}`
        }
    });
}

function checkConcide(event, lastNoteOnTime) {
    console.log("FOO: midiRenderer.js - checkConcide");
    if (event.absTime == lastNoteOnTime) {
        console.log(`NoteOn coincides with previous NoteOn: ${event.absTime}`);
    }
}

function CalculateTicksPerMeasure(currentNumerator, ticksPerBeat, currentDenominator) {
    console.log("FOO: midiRenderer.js - CalculateTicksPerMeasure");
    return currentNumerator * ticksPerBeat * 4 / currentDenominator;
}

// Мапінг sf/mi у рядок для VexFlow addKeySignature
function mapKeySignatureName(sf, mi) {
    // sf: -7..+7 (кількість бемолів (від'ємні) або дієзів (додатні)), mi: 0=major,1=minor
    const majors = ['Cb','Gb','Db','Ab','Eb','Bb','F','C','G','D','A','E','B','F#','C#'];
    const minors = ['Abm','Ebm','Bbm','Fm','Cm','Gm','Dm','Am','Em','Bm','F#m','C#m','G#m','D#m','A#m'];
    const idx = sf + 7;
    if (idx < 0 || idx >= majors.length) return null;
    return mi === 0 ? majors[idx] : minors[idx];
}

// Побудова карти знаків при ключі для sf (-7..+7)
function buildKeySignatureMap(sf) {
    console.log("FOO: midiRenderer.js - buildKeySignatureMap");
    const sharpOrder = ['f', 'c', 'g', 'd', 'a', 'e', 'b'];
    const flatOrder  = ['b', 'e', 'a', 'd', 'g', 'c', 'f'];
    const map = {};
    if (sf > 0) {
        for (let i = 0; i < sf && i < sharpOrder.length; i++) {
            map[sharpOrder[i]] = '#';
        }
    } else if (sf < 0) {
        const count = Math.min(-sf, flatOrder.length);
        for (let i = 0; i < count; i++) {
            map[flatOrder[i]] = 'b';
        }
    }
    return map;
}

// Повертає accidental для відображення з урахуванням key signature
// Якщо знак збігається з ключем — повертає null (не показувати)
function filterAccidentalByKeySignature(vexKey, accidental, currentKeySig) {
    console.log("FOO: midiRenderer.js - filterAccidentalByKeySignature");
    // If no key signature context, keep whatever was decided upstream
    if (!currentKeySig) return accidental;

    const letterPart = (vexKey.split('/')[0] || '').replace(/[#b]/g, '').toLowerCase();
    const ksMap = buildKeySignatureMap(currentKeySig.sf);
    const ksAcc = ksMap[letterPart]; // '#', 'b', or undefined

    // No explicit accidental on note, but key signature would alter it -> show natural
    if (accidental == null) {
        if (ksAcc) return 'n';
        return null; // nothing to show if key sig does not alter this letter
    }

    // If explicit accidental matches key signature, don't display a redundant sign
    if (ksAcc && ksAcc === accidental) {
        return null;
    }

    // Otherwise, display the explicit accidental (# or b)
    return accidental;
}

// NEW: Визначення знаку альтерації у межах такту з урахуванням KS і попередніх показаних знаків
function decideAccidentalForNote(key, spelledAccidental, currentKeySig, measureAccState, pieceIdx = 0) {
    // Avoid repeating accidental on tied pieces (only on the first piece)
    if (pieceIdx > 0) return null;

    // key like 'C/4' -> letter 'c', octave '4'
    const [rawLetter, rawOct] = (key || 'c/4').split('/');
    const letter = (rawLetter || '').replace(/[#b]/g, '').toLowerCase();
    const octave = rawOct || '4';
    const id = `${letter}/${octave}`;

    // Key signature accidental for this letter
    const ksMap = currentKeySig ? buildKeySignatureMap(currentKeySig.sf) : {};
    const ksAcc = ksMap[letter]; // '#', 'b', or undefined

    // spelledAccidental is the enharmonic choice for this pitch: '#', 'b', or null (natural)
    const spelled = spelledAccidental ?? null;

    // Previous accidental shown in this measure for this pitch letter+octave
    // One of '#', 'b', 'n' or undefined (nothing shown yet)
    const prev = measureAccState[id];

    let toPrint = null;

    if (prev === undefined) {
        // First occurrence in the bar for this pitch letter+octave
        if (spelled === '#' || spelled === 'b') {
            // Print only if different from key signature
            if (spelled !== ksAcc) toPrint = spelled; // e.g. show '#' if KS doesn't already sharpen
        } else {
            // spelled is natural
            // If key signature alters this letter, we must show natural to cancel KS
            if (ksAcc) toPrint = 'n';
        }
    } else {
        // Subsequent occurrence in the same bar
        if (spelled === '#' || spelled === 'b') {
            // If we previously cancelled (n) or had a different accidental, we must show this accidental again
            if (prev !== spelled) toPrint = spelled;
        } else {
            // spelled is natural — cancel any prior accidental (#/b) or explicit 'n'
            if (prev !== 'n') toPrint = 'n';
        }
    }

    // Update measure state only when we explicitly print something
    if (toPrint) {
        measureAccState[id] = toPrint;
    }

    return toPrint; // '#', 'b', 'n' or null
}

// ----------------------
// УТИЛІТИ для визначення тоніки і PC з назви ключа (для мінорної енгармонізації)
// ----------------------
function ksNoteNameToPc(name) {
    const map = {
        'C': 0, 'B#': 0,
        'C#': 1, 'Db': 1,
        'D': 2,
        'D#': 3, 'Eb': 3,
        'E': 4, 'Fb': 4,
        'E#': 5, 'F': 5,
        'F#': 6, 'Gb': 6,
        'G': 7,
        'G#': 8, 'Ab': 8,
        'A': 9,
        'A#': 10, 'Bb': 10,
        'B': 11, 'Cb': 11
    };
    return map[name] ?? null;
}

function getMinorTonicPc(currentKeySig) {
    if (!currentKeySig) return null;
    const name = mapKeySignatureName(currentKeySig.sf, currentKeySig.mi); // напр. 'Am','Ebm'
    if (!name) return null;
    const root = name.replace(/m$/, ''); // прибрати 'm' у мінорі
    return ksNoteNameToPc(root);
}

//  ----------------------
// ФУНКЦІЯ ПЕРЕТВОРЕННЯ MIDI НОТИ У ФОРМАТ VEXFLOW
// Параметри:
// - midiNote: Ціле число MIDI ноти (0-127).
// - currentKeySig: Поточний об'єкт тональності {sf: -7..+7, mi: 0|1} або null.
// Повертає: Об'єкт {key: 'C/4', accidental: '#', 'b', 'n' або null}.
// ----------------------

function midiNoteToVexFlowWithKey(midiNote, currentKeySig) {
    console.log("FOO: midiRenderer.js - midiNoteToVexFlowWithKey");
    const sharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    const flatNames  = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
    const pc = midiNote % 12;
    const octaveRaw = Math.floor(midiNote / 12) - 1;

    const sf = currentKeySig && typeof currentKeySig.sf === 'number' ? currentKeySig.sf : 0;
    const useFlats = sf < 0; // бемольні ключі -> flats, дієзні -> sharps, C (0) -> sharps

    // Базовий вибір написання за ключем
    let chosen = useFlats ? flatNames[pc] : sharpNames[pc];
    let outOctave = octaveRaw;

    // Екстремальні ключі: зберегти правильну висоту октави та логіку написання
    if (sf <= -6 && pc === 11) {        // B -> Cb (октава +1)
        chosen = 'Cb';
        outOctave = octaveRaw + 1;
    } else if (sf <= -6 && pc === 4) {  // E -> Fb
        chosen = 'Fb';
    } else if (sf >= 6 && pc === 5) {   // F -> E#
        chosen = 'E#';
    }

    // МІНОР: IV, VI і VII підвищуються; I не понижується
    const isMinor = !!(currentKeySig && (currentKeySig.mi === 1 || (typeof currentKeySig.mi === 'number' && currentKeySig.mi !== 0)));
    if (isMinor) {
        const tonicPc = getMinorTonicPc(currentKeySig);
        if (typeof tonicPc === 'number') {
            const raisedVI  = (tonicPc + 9)  % 12; // VI#
            const raisedVII = (tonicPc + 11) % 12; // VII# (ввідний тон)
            const raisedIV  = (tonicPc + 6)  % 12; // IV#

            if ((pc === raisedVI || pc === raisedVII || pc === raisedIV) && pc !== 11) {
				console.log(`MR: Minor key alteration: pc=${pc}, tonicPc=${tonicPc}`);
                const naive = sharpNames[pc];
                if (!naive.includes('#')) {
                    const prevMap = { 'C':'B','D':'C','E':'D','F':'E','G':'F','A':'G','B':'A' };
                    const prev = prevMap[naive] || naive;
                    const special = prev + '#';
                    chosen = special;
                    // Adjust octave for wrapped case: B# corresponds to previous octave
                    outOctave = (prev === 'B') ? octaveRaw - 1 : octaveRaw;
                } else {
                    chosen = sharpNames[pc];
                    outOctave = octaveRaw;
                }
            }
        }
    }

    console.log(`Note spell: midi=${midiNote}, pc=${pc}, ks=${sf}, chosen=${chosen}${outOctave}`);

    const accidental = chosen.includes('#') ? '#' : (chosen.includes('b') ? 'b' : null);
    return { key: `${chosen.replace(/[#b]/, '')}/${outOctave}`, accidental };
}

// --- updated setStave to accept clef parameter ---
function setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow = false, timeSignatureChanged = false, keySignatureChanged = false, keySigName = null, clef = "treble") {
    console.log("FOO: midiRenderer.js - setStave");
    const stave = new Vex.Flow.Stave(Xposition, Yposition, STAVE_WIDTH);

    try {
        stave.clef = clef;
    } catch (e) {
		console.warn("MR: Could not set stave.clef directly:", e);
    }
    
    // Додаємо ключ для першого такту взагалі або для першого такту в кожному рядку
    if (index === 0 || isFirstMeasureInRow) {
        stave.addClef(clef);
        console.log(`Adding clef '${clef}' to measure ${index + 1}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
    }
    
    // Додаємо знаки при ключі (key signature) якщо доступні і це перший такт, або перший у рядку, або вона змінилась
    if (keySigName && (index === 0 || keySignatureChanged || isFirstMeasureInRow)) {
        try {
            console.log(`About to add key signature: ${keySigName} (index=${index}, changed=${keySignatureChanged}, firstRow=${isFirstMeasureInRow})`);
            stave.addKeySignature(keySigName);
            console.log(`Adding key signature ${keySigName} to measure ${index + 1}`);
        } catch (e) {
            console.warn(`Failed to add key signature ${keySigName} to measure ${index + 1}`, e);
        }
    }
    
    // Додаємо розмір такту для першого такту або коли розмір змінився
    if (index === 0 || timeSignatureChanged) {
        stave.addTimeSignature(`${currentNumerator}/${currentDenominator}`);
        console.log(`Adding time signature ${currentNumerator}/${currentDenominator} to measure ${index + 1}, timeSignatureChanged: ${timeSignatureChanged}`);
    }
    
    return stave;
}

function adjustStaveWidth(BARWIDTH, index, CLEFZONE, isFirstMeasureInRow = false, timeSignatureChanged = false) {
    console.log("FOO: midiRenderer.js - adjustStaveWidth");
    let STAVE_WIDTH = BARWIDTH;
    
    // Додаємо CLEFZONE для першого такту взагалі або для першого такту в кожному рядку
    if (index === 0 || isFirstMeasureInRow) {
        STAVE_WIDTH += CLEFZONE;
        console.log(`Adding CLEFZONE to measure ${index + 1}, total width: ${STAVE_WIDTH}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
    }
    
    // Додаємо додатковий простір для розміру такту, якщо він змінився (але не для першого такту, бо там вже врахований CLEFZONE)
    if (timeSignatureChanged && index > 0 && !isFirstMeasureInRow) {
        STAVE_WIDTH += 30; // Додатковий простір для розміру такту
        console.log(`Adding extra space for time signature change to measure ${index + 1}, total width: ${STAVE_WIDTH}`);
    }
    
    return STAVE_WIDTH;
}

// ----------------------
// ФУНКЦІЯ ДЛЯ КОРЕКТУВАННЯ ПОЗИЦІЇ X, Y ПРИ РЕНДЕРИНГУ
// ----------------------
// Параметри:
// - Xposition: Поточна позиція X для рендерингу.
// - GENERALWIDTH: Загальна ширина доступного простору для рендерингу.
// - BARWIDTH: Ширина одного такту.
// - Yposition: Поточна позиція Y для рендерингу.
// - HEIGHT: Висота одного рядка.
// - Xmargin: Лівий відступ.
// - index: Індекс поточного такту.
// - barStartAbsTime: Абсолютний час початку такту (для логів).
// Повертає: Об'єкт з оновленими Xposition і Yposition.
// ----------------------
function adjustXYposition(Xposition, GENERALWIDTH, BARWIDTH, Yposition, HEIGHT, Xmargin, index, barStartAbsTime) {
    console.log("FOO: midiRenderer.js - adjustXYposition");
    // Restore wrapping: when remaining width is not enough for another measure, move to next row
    if (Xposition > GENERALWIDTH - BARWIDTH) {
        Yposition += HEIGHT;
        Xposition = Xmargin;
        console.log("Yposition updated:", Yposition);
    } else {
        console.log(`General width ${GENERALWIDTH} vs ${BARWIDTH} + ${Xposition}`);
    }
    console.log(`Processing measure ${index + 1} starting from tick: ${barStartAbsTime} X=${Xposition}  Y=${Yposition}`);
    return { Xposition, Yposition };
}

// Оновлена функція drawMeasure з покращеною обробкою помилок та перевірками
// ----------------------
// Параметри:
// - notes: Масив нот для рендерингу.
// - BARWIDTH: Ширина такту в пікселях.
// - context: Контекст рендерингу з VexFlow.
// - stave: Об'єкт stave з VexFlow для поточного такту.
// - ties: Масив ліг для нот.
// - index: Індекс поточного такту.
// - commentsDiv: HTML елемент для виведення коментарів.
// - currentNumerator: Поточний чисельник розміру такту.
// - currentDenominator: Поточний знаменник розміру такту.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// Повертає: void
// ----------------------


function drawMeasure(notes, BARWIDTH, context, stave, ties, index, commentsDiv, currentNumerator, currentDenominator, ticksPerBeat) {
    console.log("FOO: midiRenderer.js - drawMeasure");
    try {
        if (notes.length > 0) {
            // Фільтруємо некоректні ноти
            const validNotes = notes.filter(note => note !== null && note !== undefined);

			//якщо жодної валідної ноти - додаємо паузу на весь такт
            if (validNotes.length === 0) {
                console.warn(`Measure ${index + 1} has no valid notes to render.`);
                const ticksPerMeasure = currentNumerator * ticksPerBeat * 4 / currentDenominator;
                const restDurations = getDurationFromTicks(ticksPerMeasure, ticksPerBeat);
                validNotes.push(createRest(restDurations[0]));
            }

            // Обчислення ребер нот (beams) з обробкою помилок
            let beams = calculateBeams(validNotes, ticksPerBeat, index, currentNumerator, currentDenominator);
            
            // Створюємо voice з урахуванням поточного розміру такту
            const voice = new Vex.Flow.Voice({
                num_beats: currentNumerator,
                beat_value: currentDenominator,
                resolution: Vex.Flow.RESOLUTION
            });

            // Встановлюємо строгий режим у false для більшої гнучкості при обробці тривалостей
            voice.setStrict(false);

            // Додаємо нотні елементи до голосу
            voice.addTickables(validNotes);

            // Ensure each note is associated with the current stave so VexFlow uses the stave clef/metrics
            validNotes.forEach((vn, idx) => {
                try {
                    if (vn && typeof vn.setStave === 'function') {
                        vn.setStave(stave);
                    }
                    // Log for quick diagnostics
                    const keys = (typeof vn.getKeys === 'function') ? vn.getKeys() : [];
                    console.log(`MR: note[${idx}] keys=${JSON.stringify(keys)}, stave.clef=${stave.clef || '(unknown)'}`);
                } catch (err) {
                    console.warn(`MR: Failed to setStave on note[${idx}]`, err);
                }
            });

            // --- restored formatter + availableWidth calculation (fixes "formatter is not defined") ---
            const formatter = new Vex.Flow.Formatter();
            const staveX = (typeof stave.getX === 'function') ? stave.getX() : stave.x || 0;
            const staveW = (typeof stave.getWidth === 'function') ? stave.getWidth() : stave.width || BARWIDTH;
            const noteStartX = (typeof stave.getNoteStartX === 'function') ? stave.getNoteStartX() : (staveX + 10);
            const rightX = staveX + staveW;
            const rightPadding = 10;
            const availableWidth = Math.max(10, rightX - noteStartX - rightPadding);

            formatter.joinVoices([voice]).format([voice], availableWidth);
            
            // Малюємо голос
            voice.draw(context, stave);

            // Розпізнаємо й малюємо тріолі
            drawTuplets(currentNumerator, currentDenominator, validNotes, ticksPerBeat, index, context);

            // Домальовуємо ребра нот (beams) 
            drawBeams(beams, context, index);

            // Домальовуємо ліги (ties) 
            drawTies(ties, context, index);

        } else {
            // Якщо такт порожній - додаємо паузу на весь такт
            const ticksPerMeasure = currentNumerator * ticksPerBeat * 4 / currentDenominator;
            const restDurations = (typeof ticksToCleanDurations === 'function')
                ? ticksToCleanDurations(ticksPerMeasure, ticksPerBeat)
                : getDurationFromTicks(ticksPerMeasure, ticksPerBeat);
            const wholeRest = createRest(restDurations[0]);
            if (wholeRest) {
                const voice = new Vex.Flow.Voice({
                    num_beats: currentNumerator,
                    beat_value: currentDenominator,
                    resolution: Vex.Flow.RESOLUTION
                });
                voice.setStrict(false);
                voice.addTickable(wholeRest);
                
                const formatter = new Vex.Flow.Formatter();
                formatter.joinVoices([voice]).format([voice], stave.getWidth() - 20);
                voice.draw(context, stave);
            }
        }
    } catch (error) {
        console.error(`Error rendering measure ${index + 1}: ${error.message}`);
        commentsDiv.innerHTML += `<br>Error rendering measure ${index + 1}: ${error.message}<br>`;
    }
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І ОБЧИСЛЕННЯ НОТНИХ РЕБЕР (BEAMS) З ОБРОБКОЮ ПОМИЛОК
// використовує makeBeams.js 
// ----------------------
function calculateBeams(validNotes, ticksPerBeat, index, currentNumerator, currentDenominator) {
    let beams = [];
    if (typeof makeBeams === 'function' && validNotes.length > 0 && ticksPerBeat) {
        try {
            // Створюємо правильну структуру для makeBeams
            const measureForBeams = {
                notes: validNotes.map(note => ({
                    vexNote: note // Обгортаємо StaveNote в очікувану структуру
                }))
            };
            console.log(`Calling makeBeams for measure ${index + 1} with ${validNotes.length} notes and ticksPerBeat ${ticksPerBeat}`);



            const timeSignature = { num: currentNumerator, den: currentDenominator };

            const beamResult = makeBeams(measureForBeams, ticksPerBeat, timeSignature);

            beams = beamResult.beams || [];
            console.log(`makeBeams found ${beams.length} beam groups for measure ${index + 1}`);
        } catch (beamError) {
            console.warn(`Error in makeBeams for measure ${index + 1}:`, beamError);
            beams = []; // Fallback до порожнього масиву
        }
    } else {
        console.log(`makeBeams skipped for measure ${index + 1}: function=${typeof makeBeams}, notes=${validNotes.length}, ticksPerBeat=${ticksPerBeat}`);
    }
    return beams;
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛюВАННЯ ЛІГ З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.StaveTie
// Параметри:
// - ties: Масив об'єктів StaveTie з VexFlow.
// - context: Контекст рендерингу з VexFlow.
// - index: Індекс поточного такту.
// Повертає: void
// ----------------------
function drawTies(ties, context, index) {
    ties.forEach((tie, tieIndex) => {
        try {
            tie.setContext(context).draw();
        } catch (tieError) {
            console.warn(`Error drawing tie ${tieIndex + 1} in measure ${index + 1}:`, tieError);
        }
    });
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛЮВАННЯ ТРІОЛЕЙ З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.Tuplet
// Параметри:
// - currentNumerator: Поточний чисельник розміру такту.
// - currentDenominator: Поточний знаменник розміру такту.
// - validNotes: Масив об'єктів StaveNote з VexFlow.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - index: Індекс поточного такту.
// - context: Контекст рендерингу з VexFlow.
// Повертає: void
// ----------------------
function drawTuplets(currentNumerator, currentDenominator, validNotes, ticksPerBeat, index, context) {
    let tuplets = [];
    try {
        if (typeof detectTuplets === 'function') {
            const timeSignature = { num: currentNumerator || 4, den: currentDenominator || 4 };
            const measureForTuplets = { notes: validNotes.map(vn => ({ vexNote: vn })) };
            tuplets = detectTuplets(measureForTuplets, ticksPerBeat, timeSignature);
            console.log(`MR:detectTuplets found ${tuplets.length} tuplets for measure ${index + 1}`);
        }
    } catch (te) {
        console.warn(`MR:detectTuplets error in measure ${index + 1}:`, te);
    }
    if (tuplets && tuplets.length) {
        tuplets.forEach((t, tIdx) => {
            try { t.setContext(context).draw(); }
            catch (e) { console.warn(`MR:Error drawing tuplet ${tIdx + 1} in measure ${index + 1}:`, e); }
        });
    }
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛЮВАННЯ BEAMS З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.Beam
// Параметри:
// - beams: Масив об'єктів Beam з VexFlow.
// - context: Контекст рендерингу з VexFlow.
// - index: Індекс поточного такту.
// Повертає: void
// ----------------------
function drawBeams(beams, context, index) {
    if (beams && beams.length > 0) {
        beams.forEach((beam, beamIndex) => {
            try {
                beam.setContext(context).draw();
                console.log(`MR:Beam ${beamIndex + 1} drawn for measure ${index + 1}`);
            } catch (beamError) {
                console.warn(`MR:Error drawing beam ${beamIndex + 1} in measure ${index + 1}:`, beamError);
            }
        });
    }
    else {
        console.log(`MR:No beams to draw for measure ${index + 1}`);
    }
}
    
// ----------------------
// ФУНКЦІЯ ДЛЯ СТВОРЕННЯ ПАУЗИ (REST) З ВИКОРИСТАННЯМ VEXFLOW
// Параметри:
// - durationCode: Рядок тривалості паузи (наприклад, "q", "h", "8", "16." тощо).
// Повертає: Об'єкт Vex.Flow.StaveNote, що представляє собою паузу, або null у разі помилки.
// ----------------------
    
function addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat) {
    // Захист від некоректних значень
    if (lastNoteOffTime >= ticksPerMeasure) {
        console.log('Util: lastNoteOffTime >= measure, no rests needed');
        return;
    }

    const remainingTicks = ticksPerMeasure - lastNoteOffTime;
    if (remainingTicks <= thresholdGap) {
        console.log('Utils: remaining ticks too small, skipping');
        return;
    }

    console.log(`Utils: need rest for ${remainingTicks} ticks`);
    const restDurations = (typeof ticksToCleanDurations === 'function')
        ? ticksToCleanDurations(remainingTicks, ticksPerBeat)
        : getDurationFromTicks(remainingTicks, ticksPerBeat);

    console.log(`Utils: got durations: ${restDurations.join(',')}`);

    let timeadded = 0;
    restDurations.forEach((restDuration) => {
        notes.push(createRest(restDuration));
        const ticks = calculateTicksFromDuration(restDuration, ticksPerBeat);
        timeadded += ticks;
        console.log(`Utils: added rest ${restDuration} (${ticks} ticks)`);
    });

    lastNoteOffTime += timeadded;
    console.log(`Utils: total added ${timeadded} ticks, lastNoteOffTime=${lastNoteOffTime}`);
}
// //----------------------
// ФУНКЦІЯ ДЛЯ ОНОВЛЕННЯ РОЗМІРУ ТАКТУ ЗА ПОДІЯМИ TIME SIGNATURE
// шукає подію TimeSignature в такті
// повертає оновлені currentNumerator і currentDenominator
// Параметри:
// - measure: Масив MIDI подій поточного такту.
// - currentNumerator: Поточний чисельник розміру такту.
// - currentDenominator: Поточний знаменник розміру такту.
// Повертає: Об'єкт {currentNumerator, currentDenominator}.
// ----------------------
function adjustTimeSignature(measure, currentNumerator, currentDenominator) {
    console.log("FOO: midiRenderer.js - adjustTimeSignature");
    for (const event of measure) {
        if (event.type === 0xFF && event.metaType === 0x58 && Array.isArray(event.data)) {
            currentNumerator = event.data[0];
            currentDenominator = Math.pow(2, event.data[1]);
            break;
        }
    }
    return { currentNumerator, currentDenominator };
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ НОТ ІЗ ПОПЕРЕДНЬОГО ТАКТУ
// якщо є активні ноти з попереднього такту
// Параметри:
// - previousNote: Попередня нота (Vex.Flow.StaveNote) або null.
// - ties: Масив ліг (Vex.Flow.StaveTie) для рендерингу.
// - note: Поточна нота (Vex.Flow.StaveNote).
// Повертає: void
// ----------------------
function AddNotesFromPreviousBar(activeNotes, measure) {
    console.log("FOO: midiRenderer.js - AddNotesFromPreviousBar");
    Object.keys(activeNotes).forEach(pitch => {
        measure.unshift({
            type: 0x9,
            channel: 0,
            deltaTime: 0,
            absTime: 0,
            data: [Number(pitch), 100],
        });
        console.log(`MR:add note ${pitch} from previous bar`);
    });
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ПОТОЧНОГО РОЗМІРУ ТАКТУ ЗАПОДІЯМИ TIME SIGNATURE
// шукає подію TimeSignature тільки в першому такті
// повертає {numerator, denominator}
// якщо немає події, повертає 4/4 за замовчуванням
// Параметри:
// - measures: Масив тактів, кожен такт — масив MIDI подій.
// Повертає: Об'єкт {numerator, denominator}.
// ----------------------
function getTimeSignature(measures) {
    console.log("FOO: midiRenderer.js - getTimeSignature");
    let numerator = 4, denominator = 4;
    if (measures.length > 0 && measures[0].length > 0) {
        for (const event of measures[0]) {
            if (event.type === 0xFF && event.metaType === 0x58 && Array.isArray(event.data)) {
                numerator = event.data[0];
                denominator = Math.pow(2, event.data[1]);
                break;
            }
        }
    }
    return { numerator, denominator };
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ЛІГИ (TIE) МІЖ НОТАМИ
// якщо попередня нота не null
// Параметри:
// - previousNote: Попередня нота (Vex.Flow.StaveNote) або null.
// - ties: Масив ліг (Vex.Flow.StaveTie) для рендерингу.
// - note: Поточна нота (Vex.Flow.StaveNote).
// Повертає: void
// ----------------------
function AddTie(previousNote, ties, note) {
    console.log("FOO: midiRenderer.js - AddTie");
    if (previousNote) {
        ties.push(new Vex.Flow.StaveTie({
            first_note: previousNote,
            last_note: note
        }));
    }
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПАУЗ МІЖ НОТАМИ
// якщо є розрив між останнім закінченням ноти і наступним початком ноти
// Параметри:
// - lastNoteOffTime: Абсолютний час закінчення останньої ноти.
// - event: Поточна подія ноти.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - thresholdGap: Мінімальна кількість тіків для додавання паузи.
// - notes: Масив нот для рендерингу.
// Повертає: void
// ----------------------

function addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes) {
    console.log("FOO: midiRenderer.js - addRestsBetween");
    if (lastNoteOffTime > 0 && event.absTime > lastNoteOffTime) {
        const gapTicks = event.absTime - lastNoteOffTime;
        const restDurations = (typeof ticksToCleanDurations === 'function')
            ? ticksToCleanDurations(gapTicks, ticksPerBeat)
            : getDurationFromTicks(gapTicks, ticksPerBeat);
        if (gapTicks > thresholdGap) {
            restDurations.forEach((restDuration) => {
                console.log(`current event abs time ${event.absTime} vs lastNoteOffTime: ${lastNoteOffTime}: rest is needed: ${restDuration}`);
                notes.push(createRest(restDuration));
            });
        }
    }
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПАУЗИ НА ПОЧАТКУ ТАКТУ
// якщо перша подія в такті починається не з 0
// Параметри:
// - event: Перша подія в такті.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - thresholdGap: Мінімальна кількість тіків для додавання паузи.
// - notes: Масив нот для рендерингу.
// - barStartAbsTime: Абсолютний час початку такту.
// Повертає: void
// ----------------------
function AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime) {
    console.log("FOO: midiRenderer.js - AddStartRest");
    const relTime = event.absTime - barStartAbsTime;
    if (relTime > 0) {
        const restDurations = (typeof ticksToCleanDurations === 'function')
            ? ticksToCleanDurations(relTime, ticksPerBeat)
            : getDurationFromTicks(relTime, ticksPerBeat);
        if (relTime > thresholdGap) {
            console.log(`adding a starting rest ${relTime}`);
            restDurations.forEach((restDuration) => {
                notes.push(createRest(restDuration));
            });
        }
    } else {
        console.log("no rest needed");
    }
}

// ----------------------
// ФУНКЦІЯ ДЛЯ СТВОРЕННЯ НОТИ З ВИКОРИСТАННЯМ VEXFLOW
// Параметри:
// - durationCode: Код тривалості ноти (наприклад, 'q', 'h', '8', '16r' для паузи).
//// - key: Висота ноти у форматі VexFlow (наприклад, 'C/4', 'D#/5').
//// - accidental: Знак альтерації ('#', 'b', 'n') або null.
// Повертає: Об'єкт Vex.Flow.StaveNote або null у разі помилки.
// ----------------------
function processNoteElement(durationCode, key, accidental, clef = 'treble') {
    console.log("FOO: midiRenderer.js - processNoteElement");
    // key може бути в форматі "C/4" або "C4" — normalize до формату, який очікує createNote (наприклад "c4")
    let normalized = (typeof key === 'string') ? key.replace('/', '') : key;
    normalized = (typeof normalized === 'string') ? normalized.toLowerCase() : normalized;

    // Передаємо clef у createNote, щоб StaveNote отримав правильний clef властивість
    const note = createNote(normalized, durationCode, clef);
    if (note && accidental) {
        note.addAccidental(0, new Vex.Flow.Accidental(accidental));
    }
    applyAutoStem(note, durationCode);
    return note;
}

// Decide clef for a measure based on pitch range.
// Returns "bass" or "treble".
function decideClefForMeasure(measure) {
    console.log("FOO: midiRenderer.js - decideClefForMeasure");
    const bassThreshold = 60; // MIDI pitch threshold under which we prefer bass clef
    let maxPitch = -1;
    let minPitch = 128;

    if (!Array.isArray(measure) || measure.length === 0) {
        return "treble";
    }

    for (const ev of measure) {
        if (ev && ev.type === 0x9 && Array.isArray(ev.data)) {
            const pitch = ev.data[0];
            if (typeof pitch === 'number') {
                if (pitch < minPitch) minPitch = pitch;
                if (pitch > maxPitch) maxPitch = pitch;
            }
        }
    }

    console.log(`Measure pitch range: min ${minPitch}, max ${maxPitch}`);

    // Choose bass if any note is below threshold (you can tweak logic)
    if (minPitch < bassThreshold) {
        console.log("Choosing bass clef");
        return "bass";
    }
    console.log("Choosing treble clef");
    return "treble";
}

function createNote(noteKey, duration, clef = 'treble') {
    console.log("FOO: midiparser_ext.js - createNote");
    const noteMatch = noteKey.match(/^([a-gA-G])(b|#)?(\d)?$/);
    if (!noteMatch) {
        console.error(`Invalid note key: ${noteKey}`);
        return null;
    }

    const [, letter, accidental, octaveRaw] = noteMatch;
    const octave = octaveRaw || '4'; // Default to octave 4
    const key = `${letter.toLowerCase()}${accidental || ''}/${octave}`;
    const isTriplet = duration.endsWith('t');
    const isDotted = duration.endsWith('.');
    const baseDuration = (isTriplet || isDotted) ? duration.slice(0, -1) : duration;

    try {
        // Передаємо clef у опціях StaveNote — це дозволяє VexFlow позиціонувати ноту відносно потрібного ключа
        const staveNote = new Vex.Flow.StaveNote({
            keys: [key],
            duration: baseDuration,
            clef: clef || 'treble'
        });

        if (accidental) {
            staveNote.addAccidental(0, new Vex.Flow.Accidental(accidental));
        }

        if (isDotted && !isTriplet) {
            staveNote.addDot(0); // Add dot if dotted
        }
        if (typeof staveNote.autoStem === 'function') {
            staveNote.autoStem(); 
        }

        if (isTriplet) {
            // Помітимо ноту як тріольну для швидкого складання Tuplet
            staveNote.__isTriplet = true;
            staveNote.__tripletBase = baseDuration; // 'q','8','16','32'
            staveNote.__durationCode = duration;    // 'qt','8t','16t','32t'
        }
        return staveNote;
    } catch (error) {
        console.error(`Failed to create note with key: ${noteKey} and duration: ${duration}`, error);
        return null; // Return null if creation fails
    }
};

