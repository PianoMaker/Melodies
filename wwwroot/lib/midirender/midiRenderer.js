// midiRenderer.js



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
 *
 * Reads the provided MIDI file, parses its contents, and renders the musical notation using VexFlow.
 * Stores the resulting SVG and comments in sessionStorage for later retrieval.
 * Displays success or error messages in the comments element.
 */


function drawScore(file, ELEMENT_FOR_RENDERING, ELEMENT_FOR_COMMENTS, GENERALWIDTH = 1200, HEIGHT = 200, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10) {
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
                renderMidiFileToNotation(uint8, ELEMENT_FOR_RENDERING, GENERALWIDTH, HEIGHT, TOPPADDING, BARWIDTH, CLEFZONE, Xmargin, commentsDiv);
                const svg = notationDiv.querySelector("svg");
                if (svg) {
                    sessionStorage.setItem("notationSVG", svg.outerHTML);
                }
                commentsDiv.innerHTML += `File rendered successfully`;
                sessionStorage.setItem("comment", commentsDiv.innerHTML)
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
 * renderMidiFromUrl
 * -----------------
 * Convenience helper that loads a MIDI file by URL (fetch), wraps it into a File
 * object (so existing drawScore logic & FileReader path can be reused), then calls drawScore.
 * All layout parameters mirror drawScore and have the same defaults.
 *
 * @param {string} midiUrl - URL (relative or absolute) to .mid file
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
 *   await renderMidiFromUrl('/Uploads/example.mid');
 */
async function renderMidiFromUrl(
    midiUrl,
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
        drawScore(file, ELEMENT_FOR_RENDERING, ELEMENT_FOR_COMMENTS, GENERALWIDTH, HEIGHT, TOPPADDING, BARWIDTH, CLEFZONE, Xmargin);
    } catch (err) {
        console.error('renderMidiFromUrl error:', err);
        const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
        if (commentsDiv) {
            commentsDiv.innerHTML = `Error loading MIDI: ${err.message}`;
        }
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
function createmeasureMap(midiEvents, ticksPerBeat) {
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

// Group events into measures using measureMap (absTime based)
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




// ФУНЦІЯ ВІЗУАЛІЗАЦІЇ MIDI ФАЙЛУ
// ----------------------
// Приймає Uint8Array з MIDI файлом, ID елемента для рендерингу, ширину і висоту нотного стану та інші параметри.
// Використовує бібліотеки MidiParser і VexFlow для парсингу MIDI та рендерингу нот.
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
// Повертає: void
// ----------------------
// Використовує SetEventsAbsoluteTime, createmeasureMap, groupEventsByMeasure та інші допоміжні функції.
// Рендеринг відбувається асинхронно з використанням setTimeout для уникнення блокування UI.
// ----------------------


function renderMidiFileToNotation(uint8, ELEMENT_FOR_RENDERING, GENERALWIDTH, HEIGHT = 200, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10, commentsDiv) {
    console.log("FOO: midiRenderer.js - renderMidiFileToNotation");
    if (!ELEMENT_FOR_RENDERING) {
        throw new Error(`Element with id ${ELEMENT_FOR_RENDERING} not found.`);
    }    
    let midiData = MidiParser.Uint8(uint8);
    let ticksPerBeat = Array.isArray(midiData.timeDivision) ? 480 : midiData.timeDivision; // 480 - дефолт
    let allEvents = SetEventsAbsoluteTime(midiData);

    // Перевірка наявності EndTrack
    ensureEndEvent(allEvents);

    const measureMap = createmeasureMap(allEvents, ticksPerBeat);
    const measures = groupEventsByMeasure(allEvents, measureMap);

    GENERALHEIGHT = calculateRequiredHeight(measures.length, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin);

    setTimeout(() => {
        const factory = new Vex.Flow.Factory({
            renderer: {
                elementId: ELEMENT_FOR_RENDERING,
                width: GENERALWIDTH,
                height: GENERALHEIGHT
            }
        });

        const context = factory.getContext();
        const score = factory.EasyScore();

        renderMeasures(measureMap, measures, ticksPerBeat, score, context, Xmargin, TOPPADDING, BARWIDTH, CLEFZONE, HEIGHT, GENERALWIDTH, commentsDiv);
    }, 0);
}

// ФУНКЦІЯ РЕНДЕРИНГУ ТАКТІВ
// ----------------------
// Приймає мапу тактів, масив тактів, кількість тіксів на біт, об'єкт score і context з VexFlow та інші параметри.
// Рендерить кожен такт у нотний стан з урахуванням активних нот, пауз, лігатур та інших музичних елементів.
// Параметри:
// - measureMap: Об'єкт, що відображає індекси тактів у початкові тікі.
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

    // Prune trailing measures that contain no Note On events (only offs / meta etc.)
    while (measures.length > 0 && !hasNoteOn(measures[measures.length - 1])) {
        console.log("Pruning trailing empty measure without Note On events");
        measures.pop();
    }

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
    // - Якщо є активні ноти в кінці такту, домальовує їх до кінця такту.
    // - Якщо немає активних нот, перевіряє наявність пропущених пауз і додає їх.
    // - Рендерить такт у нотний стан.
    // - Оновлює позицію X для наступного такту.
    // ----------------------
    measures.forEach((measure, index) => {
        if (typeof updateKeySignatureFromEvents === 'function') {
            updateKeySignatureFromEvents(measure);
            console.log(`Tonality: ${currentKeySignature}, Mode: ${measure[1] === 0 ? 'Major' : 'Minor'}`);
        }
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

        // Use precise start from measureMap if present, else fallback
        const barStartAbsTime = (measureMap[index] !== undefined) ? measureMap[index] : index * ticksPerMeasure;

        // --- Встановлюємо startTime для активних нот на початок такту ---
        processActiveNotesFromPreviousBar(activeNotes, index, barStartAbsTime);

        // Перевіряємо, чи потрібно перейти на новий рядок
        const oldYposition = Yposition;
        ({ Xposition, Yposition } = adjustXYposition(Xposition, GENERALWIDTH, BARWIDTH, Yposition, HEIGHT, Xmargin, index, barStartAbsTime));
        
        // Якщо Y позиція змінилась, значить почався новий рядок
        if (Yposition !== oldYposition) {
            isFirstMeasureInRow = true;
            console.log(`New row started at measure ${index + 1}, Y position: ${Yposition}`);
        }

        let STAVE_WIDTH = adjustStaveWidth(BARWIDTH, index, CLEFZONE, isFirstMeasureInRow, timeSignatureChanged);

        const stave = setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow, timeSignatureChanged);

        stave.setContext(context).draw();

        const notes = [];
        const ties = [];
        let lastNoteOffTime = 0;
        let lastNoteOnTime = -1;

        //Обробка кожної події в такті
        console.log(`Processing events in measure ${index + 1}`);
        measure.forEach((event, idx) => {

            console.log("MIDI Event:", event);
            // Note On
            if (event.type === 0x9 && !(event.data && event.data[1] === 0)) {

                const pitch = event.data[0];
                console.log(`note On event for pitch ${pitch}`)
                if (stepRead) stepRead.innerHTML += ` on ${pitch} <span class="tick">[${event.absTime}]</span>`
                // Додаємо паузу тільки якщо немає активних нот
                if (idx == 0 && Object.keys(activeNotes).length === 0) {
                    AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime);
                }

                addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes);

                checkConcide(event, lastNoteOnTime);

                activeNotes[pitch] = event.absTime;
                console.log(`adding active note with pitch: ${pitch}, abs.time ${event.absTime}`)
                lastNoteOnTime = event.absTime;
            }
            // Note Off
            else if (event.type === 0x8 || (event.type === 0x9 && event.data && event.data[1] === 0)) { // Note Off
                const pitch = event.data[0];
                const startTime = activeNotes[pitch];
                console.log(`note Off event for pitch ${pitch}`)
                if (stepRead) stepRead.innerHTML += ` off ${pitch} <span class="tick">[${event.absTime}]</span>`;
                if (startTime !== undefined) {
                    const durationTicks = event.absTime - startTime;

                    if (durationTicks > 0) {
                        const durationsCode = getDurationFromTicks(durationTicks, ticksPerBeat);
                        const { key, accidental } = midiNoteToVexFlow(pitch);

                        let previousNote = null;

                        durationsCode.forEach((durationCode) => {
                            console.log(`Processing note with pitch: ${pitch}, key: ${key}, accidental: ${accidental}, durationCode: ${durationCode}`);
                            const note = processNoteElement(durationCode, key, accidental);
                            notes.push(note);

                            // Якщо є попередня нота, додаємо лігу
                            AddTie(previousNote, ties, note);

                            previousNote = note;
                        });


                    }
                    lastNoteOffTime = event.absTime;

                }
                else { console.log(`starttime for ${pitch} is ${startTime}`) }
                if (activeNotes[pitch] !== undefined) {
                    console.log(`deleting active note with pitch ${pitch}`)
                    delete activeNotes[pitch];
                }
            }
        });

                        
        // Якщо є активні ноти, домалюємо їх до кінця такту
        if (Object.keys(activeNotes).length > 0) {
            const nextBoundary = measureMap[index + 1];
            const measureEndTick = (nextBoundary !== undefined) ? nextBoundary : barStartAbsTime + ticksPerMeasure; // fallback
            console.log(`AN: bar ${index + 1}, measureEndTick = ${measureEndTick}`)
            console.log(`${Object.keys(activeNotes).length} active note(s) still exists in measure ${index + 1}`);
            if (stepRead) stepRead.innerHTML += `<i> act.note</i>`;
            drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties);
        }

        
        // Якщо остання нота не доходить до кінця такту, додаємо паузу        
        if (Object.keys(activeNotes).length === 0) {
            console.log(`check for missing rests in measure ${index + 1}`);
            addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat);
        }

        correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat);

        console.log(`start to draw measure ${index + 1}`);

        // Виклик makeBeams для групування нот
        let beams = [];
        if (typeof makeBeams === 'function' && notes.length > 0) {
            try {
                const measureForBeams = { notes: notes };
                const beamResult = makeBeams(measureForBeams, ticksPerBeat, {
                    beamableDurations: new Set(['8', '16', '32', '64', '128']),
                    minGroupSize: 2,
                    splitOnBeat: false
                });
                beams = beamResult.beams || [];
                console.log(`makeBeams found ${beams.length} beam groups for measure ${index + 1}`);
            } catch (beamError) {
                console.warn(`Error in makeBeams for measure ${index + 1}:`, beamError);
            }
        }

        drawMeasure(notes, score, BARWIDTH, context, stave, ties, index, commentsDiv, currentNumerator, currentDenominator, beams);

        Xposition += STAVE_WIDTH;
        
        // Скидаємо флаг після обробки першого такту в рядку
        isFirstMeasureInRow = false;
    });

};

// ----------------------
// Допоміжні функції для рендерингу нотного стану з MIDI файлу
// Скорочення нот, якщо виходять за межі такту
// ----------------------

function correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat) {
    console.log("FOO: midiRenderer.js - correctExtraNotes");
    if (notes.length === 0) return;
    
    // Обчислюємо загальну тривалість всіх нот у тіках
    let totalTicks = 0;
    notes.forEach(note => {
        // Отримуємо тривалість ноти/Паузи
        const duration = note.getDuration();
        const isRest = duration.endsWith('r');
        const baseDuration = isRest ? duration.slice(0, -1) : duration;
        const noteTicks = calculateTicksFromDuration(baseDuration, ticksPerBeat);
        totalTicks += noteTicks;
    });
    
    console.log(`AN: Total measure ticks: ${totalTicks}, allowed: ${ticksPerMeasure}`);
    
    // Якщо загальна тривалість перевищує такт, скорочуємо останню ноту
    if (totalTicks > ticksPerMeasure && notes.length > 0) {
        const lastNote = notes[notes.length - 1];
        const lastNoteDuration = lastNote.getDuration();
        const isRest = lastNoteDuration.endsWith('r');
        const baseDuration = isRest ? lastNoteDuration.slice(0, -1) : lastNoteDuration;
        const lastNoteTicks = calculateTicksFromDuration(baseDuration, ticksPerBeat);
        
        // Обчислюємо скільки тіків залишається для останньої ноти
        const excessTicks = totalTicks - ticksPerMeasure;
        const remainingTicks = lastNoteTicks - excessTicks;
        
        console.log(`Last note excess: ${excessTicks}, remaining: ${remainingTicks}`);
        
        if (remainingTicks > 0) {
            // Знаходимо найближчу підходящу тривалість для залишкових тіків
            const newDurations = getDurationFromTicks(remainingTicks, ticksPerBeat);
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
                    const newNote = processNoteElement(newDuration, keys[0], null);
                    if (newNote) {
                        // Копіюємо знаки альтерації з оригінальної ноти
                        const modifiers = lastNote.getModifiers();
                        modifiers.forEach(modifier => {
                            if (modifier.constructor.name === 'Accidental') {
                                newNote.addModifier(modifier);
                            }
                        });
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

function drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties) {
    console.log("FOO: midiRenderer.js - drawActiveNotes");
        // Домалювати всі ноти, які залишилися активними до кінця такту
    Object.keys(activeNotes).forEach(pitch => {
        const stepRead = document.getElementById("stepRead");
        console.log(`AN: trying to process ${pitch}, measureEndTick = ${measureEndTick}`)
        const startTime = activeNotes[pitch];
        const durationTicks = measureEndTick - startTime;
        if (durationTicks >= 0) {
            const durationsCode = getDurationFromTicks(durationTicks, ticksPerBeat);
            const { key, accidental } = midiNoteToVexFlow(Number(pitch));
            console.log(`AN: found activeNote ${pitch} (${key}${accidental})`);
            if (stepRead) stepRead.innerHTML+=`=${pitch}`
            let previousNote = null;
            durationsCode.forEach(durationCode => {
                const note = processNoteElement(durationCode, key, accidental);
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

function setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow = false, timeSignatureChanged = false) {
    console.log("FOO: midiRenderer.js - setStave");
    const stave = new Vex.Flow.Stave(Xposition, Yposition, STAVE_WIDTH);
    
    // Додаємо ключ для першого такту взагалі або для першого такту в кожному рядку
    if (index === 0 || isFirstMeasureInRow) {
        stave.addClef("treble");
        console.log(`Adding clef to measure ${index + 1}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
    }
    
    // Додаємо розмір такту для першого такту або коли розмір змінюється
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

function adjustXYposition(Xposition, GENERALWIDTH, BARWIDTH, Yposition, HEIGHT, Xmargin, index, barStartAbsTime) {
    console.log("FOO: midiRenderer.js - adjustXYposition");
    if (Xposition > GENERALWIDTH - BARWIDTH) {
        Yposition += HEIGHT; Xposition = Xmargin;
        console.log("Yposition updated:", Yposition);
    } else {
        console.log(`General width ${GENERALWIDTH} vs ${BARWIDTH} + ${Xposition}`);
    }
    console.log(`Processing measure ${index + 1} starting from tick: ${barStartAbsTime} X=${Xposition}  Y=${Yposition}`);
    return { Xposition, Yposition };
}

function drawMeasure(notes, score, BARWIDTH, context, stave, ties, index, commentsDiv, currentNumerator, currentDenominator, beams = []) {
    console.log("FOO: midiRenderer.js - drawMeasure");
    try {
        if (notes.length > 0) {
            // Фільтруємо некоректні ноти
            const validNotes = notes.filter(note => note !== null && note !== undefined);
            
            if (validNotes.length === 0) {
                console.warn(`Measure ${index + 1} has no valid notes to render.`);
                return;
            }
            
            // Створюємо voice з урахуванням поточного розміру такту
            const voice = new Vex.Flow.Voice({
                num_beats: currentNumerator || 4,
                beat_value: currentDenominator || 4
            });
            
            voice.setStrict(false);
            voice.addTickables(validNotes);
            
            // Форматуємо голос
            const formatter = new Vex.Flow.Formatter();
            formatter.joinVoices([voice]).format([voice], BARWIDTH - 50);
            
            // Малюємо голос
            voice.draw(context, stave);
            
            // Малюємо beam об'єкти (групування нот)
            if (beams && beams.length > 0) {
                beams.forEach((beam) => {
                    try {
                        beam.setContext(context).draw();
                        console.log(`Beam drawn for measure ${index + 1}`);
                    } catch (beamError) {
                        console.warn(`Error drawing beam in measure ${index + 1}:`, beamError);
                    }
                });
            }
            
            // Малюємо лігатури з обробкою помилок
            ties.forEach((tie) => {
                try {
                    tie.setContext(context).draw();
                } catch (tieError) {
                    console.warn(`Error drawing tie in measure ${index + 1}:`, tieError);
                }
            });
        } else {
            console.warn(`Measure ${index + 1} has no notes to render.`);
        }
    } catch (error) {
        console.error(`Error rendering measure ${index + 1}: ${error.message}<br>`);
        commentsDiv.innerHTML += `<br>Error rendering measure ${index + 1}: ${error.message}<br>`;
    }
}

// Оновлена функція для додавання пауз з урахуванням ticksPerMeasure
function addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat) {
    console.log("FOO: midiRenderer.js - addMissingRests");
    while (lastNoteOffTime < ticksPerMeasure - thresholdGap) {
        const remainingTicks = ticksPerMeasure - lastNoteOffTime;
        console.log(`add Missing Rest is running: remaining: ${remainingTicks}`);
        const restDurations = getDurationFromTicks(remainingTicks, ticksPerBeat);
        let timeadded = 0;
        restDurations.forEach((restDuration) => {
            notes.push(createRest(restDuration));
            timeadded += calculateTicksFromDuration(restDuration, ticksPerBeat);
        });
        lastNoteOffTime += timeadded;
        console.log(`adding a rest done, time added = ${timeadded}, update lastNoteOffTime: ${lastNoteOffTime}`);
    }
}

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
        console.log(`add note ${pitch} from previous bar`);
    });
}

/**
 * getTimeSignature
 * ----------------
 * Визначає розмір такту (чисельник і знаменник) для нотного стану на основі подій першого такту.
 *
 * @param {Array} measures - Масив тактів, де кожен такт — це масив MIDI-подій.
 * @returns {Object} Об'єкт з полями numerator (чисельник) і denominator (знаменник) розміру такту.
 *
 * Логіка:
 * - Перебирає події першого такту (measures[0]).
 * - Якщо знаходить meta-подію типу 0x58 (Time Signature), повертає відповідний розмір такту.
 * - Якщо не знайдено, повертає стандартний розмір 4/4.
 */
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

function AddTie(previousNote, ties, note) {
    console.log("FOO: midiRenderer.js - AddTie");
    if (previousNote) {
        ties.push(new Vex.Flow.StaveTie({
            first_note: previousNote,
            last_note: note
        }));
    }
}

function getXYposition(Xmargin, TOPPADDING, GENERALWIDTH, BARWIDTH, HEIGHT, index, barStartAbsTime) {
    console.log("FOO: midiRenderer.js - getXYposition");
    let Xposition = Xmargin;
    let Yposition = TOPPADDING;
    if (Xposition > GENERALWIDTH - BARWIDTH) {
        Yposition += HEIGHT; Xposition = Xmargin;
        console.log("Yposition updated:", Yposition);
    }
    else {
        console.log(`General ${GENERALWIDTH} - ${BARWIDTH} vs ${Xposition}`);
    }
    console.log(`Processing measure ${index + 1} starting from tick: ${barStartAbsTime} X=${Xposition}  Y=${Yposition}`);
    return { Xposition, Yposition };
}

function getStaveWidth(BARWIDTH, index, CLEFZONE) {
    console.log("FOO: midiRenderer.js - getStaveWidth");
    let STAVE_WIDTH = BARWIDTH;
    if (index === 0) {
        STAVE_WIDTH += CLEFZONE;
        console.log("First stave width:", STAVE_WIDTH);
    }
    return STAVE_WIDTH;
}

function addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes) {
    console.log("FOO: midiRenderer.js - addRestsBetween");
    if (lastNoteOffTime > 0 && event.absTime > lastNoteOffTime) {
        const gapTicks = event.absTime - lastNoteOffTime;
        const restDurations = getDurationFromTicks(gapTicks, ticksPerBeat);
        if (gapTicks > thresholdGap) {
            restDurations.forEach((restDuration) => {
                console.log(`current event abs time ${event.absTime} vs lastNoteOffTime: ${lastNoteOffTime}: rest is needed: ${restDuration}`);
                notes.push(createRest(restDuration));
            });
        }
    }
}

function AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime) {
    console.log("FOO: midiRenderer.js - AddStartRest");
    const relTime = event.absTime - barStartAbsTime;
    if (relTime > 0) {
        const restDurations = getDurationFromTicks(relTime, ticksPerBeat);
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

function processNoteElement(durationCode, key, accidental) {
    console.log("FOO: midiRenderer.js - processNoteElement");
    key = key.replace('/', ''); // випиляти зайве /
    const note = createNote(key, durationCode);
    if (note && accidental) {
        note.addAccidental(0, new Vex.Flow.Accidental(accidental));
    }
    return note;
}


function calculateRequiredHeight(measuresCount, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING = 20, CLEFZONE = 60, Xmargin = 10) {
    console.log("FOO: midiRenderer.js - calculateRequiredHeight");
    let Xposition = Xmargin;
    let rows = 1;
    for (let i = 0; i < measuresCount; i++) {
        let STAVE_WIDTH = BARWIDTH;
        if (i === 0) STAVE_WIDTH += CLEFZONE;
        if (Xposition > GENERALWIDTH - BARWIDTH) {
            rows++;
            Xposition = Xmargin;
        }
        Xposition += STAVE_WIDTH;
    }
    return TOPPADDING + HEIGHT * rows;
}

function prepareMidiEvents(arrayBuffer) {
    console.log("FOO: midiRenderer.js - prepareMidiEvents");
    console.log("Preparing MIDI events from arrayBuffer...");
    let midiEvents = [];
    let uint8 = (arrayBuffer instanceof Uint8Array) ? arrayBuffer : new Uint8Array(arrayBuffer);
    if (typeof MIDIParser !== 'undefined') {
        MIDIParser.parse(uint8, function(obj) {
            if (obj && obj.track && Array.isArray(obj.track.events)) {
                midiEvents = midiEvents.concat(obj.track.events);
            }
        });
    } else if (typeof MidiParser !== 'undefined') {
        MidiParser.parse(uint8, function(obj) {
            if (obj && obj.track && Array.isArray(obj.track.events)) {
                midiEvents = midiEvents.concat(obj.track.events);
            }
        });
    } else {
        console.error('Neither MIDIParser nor MidiParser is defined.');
    }
    return midiEvents;
}


const logEvent = (msg) => {
    if (stepRead) {
        stepRead.innerHTML += msg;
    }
};


window.drawScore = drawScore;
window.renderMidiFromUrl = renderMidiFromUrl;
