// midiRenderer.js



/**
 * (AUTO-STEM HELPER)
 * -------------------------------------------------
 * Added helper to optionally apply VexFlow auto stem logic to every created note
 * without modifying or removing existing functions or comments. Rest durations
 * (codes ending with 'r') are skipped. Safe to call multiple times.
 */
function applyAutoStem(note, durationCode) {
    try {
        if (!note) return;
        if (typeof durationCode === 'string' && /r$/.test(durationCode)) return; // skip rests
        if (typeof note.autoStem === 'function') {
            note.autoStem();
        }
    } catch (e) {
        console.warn('applyAutoStem failed:', e);
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




// ����ֲ� ²���˲��ֲ� MIDI �����
// ----------------------
// ������ Uint8Array � MIDI ������, ID �������� ��� ����������, ������ � ������ ������� ����� �� ���� ���������.
// ����������� �������� MidiParser � VexFlow ��� �������� MIDI �� ���������� ���.
// ��������� ������ ���� � �������� HTML �������.
// ���������:
// - uint8: Uint8Array � MIDI ������.
// - ELEMENT_FOR_RENDERING: ID HTML �������� ��� ���������� ���.
// - GENERALWIDTH: ������ ������� ����� � �������.
// - HEIGHT: ������ ������� ����� ������� ����� � �������.
// - TOPPADDING: ������ ������ ������� ����� � �������.
// - BARWIDTH: ������ ������� ����� � �������.
// - CLEFZONE: ������ ���� ��� ����� �� ������ ����� � �������.
// - Xmargin: ˳��� ������ ������� ����� � �������.
// �������: void
// ----------------------
// ����������� SetEventsAbsoluteTime, createmeasureMap, groupEventsByMeasure �� ���� ������� �������.
// ��������� ���������� ���������� � ������������� setTimeout ��� ��������� ���������� UI.
// ---------------------


function renderMidiFileToNotation(uint8, ELEMENT_FOR_RENDERING, GENERALWIDTH, HEIGHT = 200, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10, commentsDiv) {
    console.log("FOO: midiRenderer.js - renderMidiFileToNotation");
    if (!ELEMENT_FOR_RENDERING) {
        throw new Error(`Element with id ${ELEMENT_FOR_RENDERING} not found.`);
    }    
    let midiData = MidiParser.Uint8(uint8);
    let ticksPerBeat = Array.isArray(midiData.timeDivision) ? 480 : midiData.timeDivision; 
    let allEvents = SetEventsAbsoluteTime(midiData);

    // �������� �������� EndTrack
    ensureEndEvent(allEvents);

    const measureMap = createMeasureMap(allEvents, ticksPerBeat);
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

// ����ֲ� ���������� ���Ҳ�
// ----------------------
// ������ ���� �����, ����� �����, ������� ���� �� ��, ��'��� score � context � VexFlow �� ���� ���������.
// ��������� ����� ���� � ������ ���� � ����������� �������� ���, ����, ������ �� ����� �������� ��������.
// ���������:
// - measureMap: ��'���, �� �������� ������� ����� � �������� ���.
// - measures: ����� �����, �� ����� ���� � �� ����� MIDI-����.
// - ticksPerBeat: ʳ������ ��� �� ������� ����.
// - score: ��'��� score � VexFlow ��� ��������� ���.
// - context: �������� ���������� � VexFlow.
// - Xmargin: ˳��� ������ ������� ����� � �������.
// - TOPPADDING: ������ ������ ������� ����� � �������.
// - BARWIDTH: ������ ������� ����� � �������.
// - CLEFZONE: ������ ���� ��� ����� �� ������ ����� � �������.
// - HEIGHT: ������ ������� ����� ������� ����� � �������.
// - GENERALWIDTH: �������� ������ ������� ����� � �������.
// �������: void
// ----------------------


function renderMeasures(measureMap, measures, ticksPerBeat, score, context, Xmargin, TOPPADDING, BARWIDTH, CLEFZONE, HEIGHT, GENERALWIDTH, commentsDiv) {
    console.log("FOO: midiRenderer.js - renderMeasures ");
    let Xposition = Xmargin;
    let Yposition = TOPPADDING;
    let thresholdGap = ticksPerBeat / 8;


    // ���������� ����� ����� (�� ������������� 4/4)
    let currentNumerator = 4;
    let currentDenominator = 4;
    const activeNotes = {};

    // ���� ��� ���������� ������� ����� �� ������� �����
    let isFirstMeasureInRow = true;
    // �������� key signature (����������� ��� meta ����� 0x59)
    let currentKeySig = null;

    // Prune trailing measures that contain no Note On events (only offs / meta etc.)
    while (measures.length > 0 && !hasNoteOn(measures[measures.length - 1])) {
        console.log("Pruning trailing empty measure without Note On events");
        measures.pop();
    }

    // �������� �� �����
    // ----------------------
    // ��� ������� �����:
    // - ������� ����� �����, ���� � �������� ����.
    // - ������� ���������� ��� ����� � measureMap.
    // - ���������� ���������� ��� ��� �������� ��� � ������������ �����.
    // - ��������� ������� X �� Y ��� ������� �����.
    // - ������� ������ ���� (stave) � ������ �� ������� �����.
    // - �������� ����� ���� � ����:
    //   - ��� Note On: ���� ����, �������� ����� �� ������, �������� �������� �������� ���.
    //   - ��� Note Off: ������� ����, ���� �������, ������� ���� � ��������.
    // - ���� � ������ ���� � ���� �����, ��������� �� �� ���� �����.
    // - ���� ���� �������� ���, �������� �������� ���������� ���� � ���� ��.
    // - ��������� ���� � ������ ����.
    // - ������� ������� X ��� ���������� �����.
    // ----------------------
    measures.forEach((measure, index) => {
        measure = normalizeMetaEvents(measure);
                       
        let keySignatureChanged;
        let keySigName;
        ({ keySignatureChanged, keySigName, currentKeySig } = getKeySignatureChanges(measure, currentKeySig));

        
        const stepRead = document.getElementById("stepRead");
        logEvent(`<br/>LE: Processing measure ${index + 1}`);
        logMeasureEvents(measure);

        // �������� ��������� ����� ����� ��� ���������
        const previousNumerator = currentNumerator;
        const previousDenominator = currentDenominator;

        ({ currentNumerator, currentDenominator } = adjustTimeSignature(measure, currentNumerator, currentDenominator));
        let ticksPerMeasure = CalculateTicksPerMeasure(currentNumerator, ticksPerBeat, currentDenominator);

        // ����������, �� ������� ����� �����
        const timeSignatureChanged = (currentNumerator !== previousNumerator || currentDenominator !== previousDenominator);

        // Use precise start from measureMap if present, else fallback
        const barStartAbsTime = (measureMap[index] !== undefined) ? measureMap[index] : index * ticksPerMeasure;

        // --- ������������ startTime ��� �������� ��� �� ������� ����� ---
        processActiveNotesFromPreviousBar(activeNotes, index, barStartAbsTime);

        // ����������, �� ������� ������� �� ����� �����
        const oldYposition = Yposition;
        ({ Xposition, Yposition } = adjustXYposition(Xposition, GENERALWIDTH, BARWIDTH, Yposition, HEIGHT, Xmargin, index, barStartAbsTime));
        
        // ���� Y ������� ��������, ������� ������� ����� �����
        if (Yposition !== oldYposition) {
            isFirstMeasureInRow = true;
            console.log(`New row started at measure ${index + 1}, Y position: ${Yposition}`);
        }

        let STAVE_WIDTH = adjustStaveWidth(BARWIDTH, index, CLEFZONE, isFirstMeasureInRow, timeSignatureChanged);

        // ��������� ������ ���� (stave)
        // ������ ���� �� ����� �����, ���� ������� (����� ����� � key signature)
        const stave = setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow, timeSignatureChanged, keySignatureChanged, keySigName);

        // ������� ������ ����
        stave.setContext(context).draw();

        const notes = [];
        const ties = [];
        let lastNoteOffTime = 0;
        let lastNoteOnTime = -1;

        // NEW: �������� ���� ��������� ���������� � ����� ����� (letter+octave -> '#','b','n')
        const measureAccState = {};
       
        // ��������� ���������� �� ��䳿 � ����
        console.log(`Processing events in measure ${index + 1}`);

        // ����� ����� notes[] �� ties[] ��� ��������� �����
        renderMeasure();


        // ���� � "������" ���� (activeNotes), ��������� �� �� ���� �����
        if (Object.keys(activeNotes).length > 0) {
            const nextBoundary = measureMap[index + 1];
            const measureEndTick = (nextBoundary !== undefined) ? nextBoundary : barStartAbsTime + ticksPerMeasure; // fallback
            console.log(`AN: bar ${index + 1}, measureEndTick = ${measureEndTick}`)
            console.log(`${Object.keys(activeNotes).length} active note(s) still exists in measure ${index + 1}`);
            if (stepRead) stepRead.innerHTML += `<i> act.note</i>`;
            // UPDATED: �������� measureAccState ��� ������������� ����� � ����� �����
            drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties, currentKeySig, measureAccState);
        }

        
        // ���� ������� ���� �� �������� �� ���� �����, ������ �����        
        if (Object.keys(activeNotes).length === 0) {
            console.log(`check for missing rests in measure ${index + 1}`);
            addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat);
        }

        // ����������, �� ������� ��������� ������� ����/�����, ���� ���� �������� �� ��� �����
        correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat);

        console.log(`start to draw measure ${index + 1}`);

        // �������� ticksPerBeat � drawMeasure
        drawMeasure(notes, BARWIDTH, context, stave, ties, index, commentsDiv, currentNumerator, currentDenominator, ticksPerBeat);

        Xposition += STAVE_WIDTH;
        
        // ������� ���� ���� ������� ������� ����� � �����
        isFirstMeasureInRow = false;


        // ���������� MIDI-��䳿 � ������ �����
        // ----------------------
        // // ��� ����� ��䳿:
        // - ���� �� Note On � velocity > 0:
        //   - ���� ����� �� ������� �����, ���� �� ����� ���� � ���� �������� ���.
        //   - ���� ����� �� ��������� Note Off � �������� Note On.
        //   - ��������, �� �������� ��� � ��������� Note On.
        //   - ���� ���� �� �������� ���.
        // - ���� �� Note Off ��� Note On � velocity = 0:
        //   - ������� ����, �������� �� ���������.
        //   - ���� ��� �� ������.
        //   - ������� ���� � �������� ���.
        // - ���� �������� ���� � ����� �� ������ notes[]        
        // ----------------------
        function renderMeasure() {
            console.log("FOO: midiRenderer.js - renderMeasure"); 
            let isFirstNoteInMeasure = true; 

            const isNoteOn = (ev) => ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0;
            const isNoteOff = (ev) => ev.type === 0x8 || (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0);

            // �������: �� �����; � ����� ������ ���� ������ OFF, ���� ON
            const sorted = [...measure].sort((a, b) => {
                if (a.absTime !== b.absTime) return a.absTime - b.absTime;
                const rank = (ev) => isNoteOff(ev) ? 0 : (isNoteOn(ev) ? 1 : 2);
                return rank(a) - rank(b);
            });

            // ������ �� ����������� OFF ��� ���� ���� �� ������ ���
            const closedAtTick = new Set();

            sorted.forEach((event) => {
                console.log("MIDI Event:", event);

                if (isNoteOn(event)) {
                    const pitch = event.data[0];
                    if (stepRead) stepRead.innerHTML += ` on ${pitch} <span class="tick">[${event.absTime}]</span>`;

                    if (isFirstNoteInMeasure && Object.keys(activeNotes).length === 0) {
                        AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime);
                        isFirstNoteInMeasure = false;
                    }

                    addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes);
                    checkConcide(event, lastNoteOnTime);

                    activeNotes[pitch] = event.absTime;
                    lastNoteOnTime = event.absTime;
                }
                else if (isNoteOff(event)) {
                    const pitch = event.data[0];
                    const key = `${pitch}@${event.absTime}`;
                    if (closedAtTick.has(key)) return; // �������
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
                                const note = processNoteElement(durationCode, vexKey, accToDraw);
                                // APPLY AUTO STEM for each note piece
                                applyAutoStem(note, durationCode);

                                const allocatedTicks = Math.round(durationTicks * (nominalTicksArr[pieceIdx] / nominalSum));
                                note.__srcTicks = allocatedTicks;

                                notes.push(note);

                                // ���� � ��������� ����, ������ ���
                                AddTie(previousNote, ties, note);

                                previousNote = note;
                            });


                        }
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
// ������� ������� ��� ������� �������� ����� (key signatures)
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
// ������� ������� ��� ���������� ������� ����� � MIDI �����
// ���������� ���, ���� �������� �� ��� �����
// ----------------------

function correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat) {
    console.log("FOO: midiRenderer.js - correctExtraNotes");
    if (notes.length === 0) return;
    
    // ���������� �������� ��������� ��� ��� � ����
    let totalTicks = 0;
    notes.forEach(note => {
        // �������� ��������� ����/�����
        const duration = note.getDuration();
        const isRest = duration.endsWith('r');
        const baseDuration = isRest ? duration.slice(0, -1) : duration;
        const dotted = /\./.test(duration);
        const noteTicks = calculateTicksFromDuration(baseDuration, ticksPerBeat);
        totalTicks += noteTicks;
    });
    
    console.log(`AN: Total measure ticks: ${totalTicks}, allowed: ${ticksPerMeasure}`);
    
    // ���� �������� ��������� �������� ����, ��������� ������� ����
    if (totalTicks > ticksPerMeasure && notes.length > 0) {
        const lastNote = notes[notes.length - 1];
        const lastNoteDuration = lastNote.getDuration();
        const isRest = lastNoteDuration.endsWith('r');
        const baseDuration = isRest ? lastNoteDuration.slice(0, -1) : lastNoteDuration;
        const dotted = /\./.test(lastNoteDuration);
        const lastNoteTicks = calculateTicksFromDuration(baseDuration, ticksPerBeat);
        
        // ���������� ������ ��� ���������� ��� �������� ����
        const excessTicks = totalTicks - ticksPerMeasure;
        const remainingTicks = lastNoteTicks - excessTicks;
        
        console.log(`Last note excess: ${excessTicks}, remaining: ${remainingTicks}`);
        
        if (remainingTicks > 0) {
            // ��������� ��������� �������� ��������� ��� ���������� ���
            const newDurations = getDurationFromTicks(remainingTicks, ticksPerBeat);
            if (newDurations.length > 0) {
                const newDuration = newDurations[0]; // ������ ����� (��������) ���������
                const finalDuration = isRest ? newDuration + 'r' : newDuration;
                
                // ��������� ���� ���� � ���������� ���������
                if (isRest) {
                    const newRest = createRest(newDuration);
                    if (newRest) {
                        notes[notes.length - 1] = newRest;
                        console.log(`Shortened last rest to: ${finalDuration}`);
                    }
                } else {
                    // ��� ��� ������� �������� ������ �� ����� ����������
                    const keys = lastNote.getKeys();
                    const newNote = processNoteElement(newDuration, keys[0], null);
                    if (newNote) {
                        // ������� ����� ���������� � ���������� ����
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
            // ���� ���������� ��� �����������, ��������� ������� ����
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

function drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties, currentKeySig, measureAccState) {
    console.log("FOO: midiRenderer.js - drawActiveNotes");
        // ���������� �� ����, �� ���������� ��������� �� ���� �����
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
                // UPDATED: measure-aware accidental logic for split pieces of a sustained note
                const accToDraw = decideAccidentalForNote(key, accidental, currentKeySig, measureAccState, pieceIdx);
                const note = processNoteElement(durationCode, key, accToDraw);
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

// ����� sf/mi � ����� ��� VexFlow addKeySignature
function mapKeySignatureName(sf, mi) {
    // sf: -7..+7 (������� ������ (��'���) ��� 䳺�� (������)), mi: 0=major,1=minor
    const majors = ['Cb','Gb','Db','Ab','Eb','Bb','F','C','G','D','A','E','B','F#','C#'];
    const minors = ['Abm','Ebm','Bbm','Fm','Cm','Gm','Dm','Am','Em','Bm','F#m','C#m','G#m','D#m','A#m'];
    const idx = sf + 7;
    if (idx < 0 || idx >= majors.length) return null;
    return mi === 0 ? majors[idx] : minors[idx];
}

// �������� ����� ����� ��� ����� ��� sf (-7..+7)
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

// ������� accidental ��� ����������� � ����������� key signature
// ���� ���� �������� � ������ � ������� null (�� ����������)
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

// NEW: ���������� ����� ���������� � ����� ����� � ����������� KS � ��������� ��������� �����
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
            // spelled is natural � cancel any prior accidental (#/b) or explicit 'n'
            if (prev !== 'n') toPrint = 'n';
        }
    }

    // Update measure state only when we explicitly print something
    if (toPrint) {
        measureAccState[id] = toPrint;
    }

    return toPrint; // '#', 'b', 'n' or null
}

//  ----------------------
// ����ֲ� ������������ MIDI ���� � ������ VEXFLOW
// ���������:
// - midiNote: ֳ�� ����� MIDI ���� (0-127).
// - currentKeySig: �������� ��'��� ���������� {sf: -7..+7, mi: 0|1} ��� null.
// �������: ��'��� {key: 'C/4', accidental: '#', 'b', 'n' ��� null}.
// ----------------------

function midiNoteToVexFlowWithKey(midiNote, currentKeySig) {
    console.log("FOO: midiRenderer.js - midiNoteToVexFlowWithKey");
    const sharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    const flatNames  = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
    const pc = midiNote % 12;
    const octaveRaw = Math.floor(midiNote / 12) - 1;
    const sf = currentKeySig && typeof currentKeySig.sf === 'number' ? currentKeySig.sf : 0;
    const useFlats = sf < 0; // 䳺�� (sf>0) -> sharps, ������� (sf<0) -> flats, C (0) -> sharps

    // ������� ���� �� sf
    let chosen = useFlats ? flatNames[pc] : sharpNames[pc];
    let outOctave = octaveRaw;

    // ����������� ���������� (�������� ������ ��������):
    // - ����� 5 ������ (sf <= -6): ���� B -> Cb (�������� ������), ���� E -> Fb (�� � ������)
    // - ����� 5 䳺�� (sf >= +6): ���� F -> E# (�� � ������)
    if (sf <= -6 && pc === 11) {
        chosen = 'Cb';
        outOctave = octaveRaw + 1;
    } else if (sf <= -6 && pc === 4) {
        chosen = 'Fb';
    } else if (sf >= 6 && pc === 5) {
        chosen = 'E#';
    }

    console.log(`Note spell: midi=${midiNote}, pc=${pc}, in=${useFlats?'flats':'sharps'}, sf=${sf} => ${chosen}${outOctave}`);

    const accidental = chosen.includes('#') ? '#' : (chosen.includes('b') ? 'b' : null);
    return { key: `${chosen.replace(/[#b]/, '')}/${outOctave}`, accidental };
}

function setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow = false, timeSignatureChanged = false, keySignatureChanged = false, keySigName = null) {
    console.log("FOO: midiRenderer.js - setStave");
    const stave = new Vex.Flow.Stave(Xposition, Yposition, STAVE_WIDTH);
    
    // ������ ���� ��� ������� ����� ������ ��� ��� ������� ����� � ������� �����
    if (index === 0 || isFirstMeasureInRow) {
        stave.addClef("treble");
        console.log(`Adding clef to measure ${index + 1}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
    }
    
    // ������ ����� ��� ����� (key signature) ���� ������� � �� ������ ����, ��� ������ � �����, ��� ���� ��������
    if (keySigName && (index === 0 || keySignatureChanged || isFirstMeasureInRow)) {
        try {
            console.log(`About to add key signature: ${keySigName} (index=${index}, changed=${keySignatureChanged}, firstRow=${isFirstMeasureInRow})`);
            stave.addKeySignature(keySigName);
            console.log(`Adding key signature ${keySigName} to measure ${index + 1}`);
        } catch (e) {
            console.warn(`Failed to add key signature ${keySigName} to measure ${index + 1}`, e);
        }
    }
    
    // ������ ����� ����� ��� ������� ����� ��� ���� ����� ���������
    if (index === 0 || timeSignatureChanged) {
        stave.addTimeSignature(`${currentNumerator}/${currentDenominator}`);
        console.log(`Adding time signature ${currentNumerator}/${currentDenominator} to measure ${index + 1}, timeSignatureChanged: ${timeSignatureChanged}`);
    }
    
    return stave;
}

function adjustStaveWidth(BARWIDTH, index, CLEFZONE, isFirstMeasureInRow = false, timeSignatureChanged = false) {
    console.log("FOO: midiRenderer.js - adjustStaveWidth");
    let STAVE_WIDTH = BARWIDTH;
    
    // ������ CLEFZONE ��� ������� ����� ������ ��� ��� ������� ����� � ������� �����
    if (index === 0 || isFirstMeasureInRow) {
        STAVE_WIDTH += CLEFZONE;
        console.log(`Adding CLEFZONE to measure ${index + 1}, total width: ${STAVE_WIDTH}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
    }
    
    // ������ ���������� ������ ��� ������ �����, ���� �� ������� (��� �� ��� ������� �����, �� ��� ��� ���������� CLEFZONE)
    if (timeSignatureChanged && index > 0 && !isFirstMeasureInRow) {
        STAVE_WIDTH += 30; // ���������� ������ ��� ������ �����
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

// �������� ������� drawMeasure � ���������� �������� ������� �� ����������
// ----------------------
// ���������:
// - notes: ����� ��� ��� ����������.
// - BARWIDTH: ������ ����� � �������.
// - context: �������� ���������� � VexFlow.
// - stave: ��'��� stave � VexFlow ��� ��������� �����.
// - ties: ����� �� ��� ���.
// - index: ������ ��������� �����.
// - commentsDiv: HTML ������� ��� ��������� ���������.
// - currentNumerator: �������� ��������� ������ �����.
// - currentDenominator: �������� ��������� ������ �����.
// - ticksPerBeat: ʳ������ ��� �� ������� ����.
// �������: void
// ----------------------
// ����������� Vex.Flow.Voice, Vex.Flow.Formatter �� makeBeams ��� ������������ �� ���������� ���.
    
function drawMeasure(notes, BARWIDTH, context, stave, ties, index, commentsDiv, currentNumerator, currentDenominator, ticksPerBeat) {
    console.log("FOO: midiRenderer.js - drawMeasure");
    try {
        if (notes.length > 0) {
            // Գ������� ��������� ����
            const validNotes = notes.filter(note => note !== null && note !== undefined);
            
            if (validNotes.length === 0) {
                console.warn(`Measure ${index + 1} has no valid notes to render.`);
                return;
            }

            // ���������� ����� ��� (beams) � �������� �������
            let beams = calculateBeams(validNotes, ticksPerBeat, index, currentNumerator, currentDenominator);
            
            // ��������� voice � ����������� ��������� ������ �����
            const voice = new Vex.Flow.Voice({
                num_beats: currentNumerator || 4,
                beat_value: currentDenominator || 4
            });

            // ������������ ������� ����� � false, ��� �������� ������� ����� ������������ �����������
            voice.setStrict(false);

            // ������ ���� �������� �� ������
            voice.addTickables(validNotes);
            
            // ��������� �����
            const formatter = new Vex.Flow.Formatter();
            formatter.joinVoices([voice]).format([voice], BARWIDTH - 50);
            
            // ������� �����
            voice.draw(context, stave);

            // ��������� � ������� ����
            drawTuplets(currentNumerator, currentDenominator, validNotes, ticksPerBeat, index, context);

            // ����������� ����� ��� (beams) 
            drawBeams(beams, context, index);
            
            
            // ����������� ��� (ties) 
            drawTies(ties, context, index);

        } else {
            console.warn(`Measure ${index + 1} has no notes to render.`);
        }
    } catch (error) {
        console.error(`Error rendering measure ${index + 1}: ${error.message}`);
        commentsDiv.innerHTML += `<br>Error rendering measure ${index + 1}: ${error.message}<br>`;
    }
}

// ----------------------
// ����ֲ� ��� ������� � ���������� ������ ����� (BEAMS) � �������� �������
// ����������� makeBeams.js 
// ----------------------
function calculateBeams(validNotes, ticksPerBeat, index, currentNumerator, currentDenominator) {
    let beams = [];
    if (typeof makeBeams === 'function' && validNotes.length > 0 && ticksPerBeat) {
        try {
            // ��������� ��������� ��������� ��� makeBeams
            const measureForBeams = {
                notes: validNotes.map(note => ({
                    vexNote: note // ��������� StaveNote � ��������� ���������
                }))
            };
            console.log(`Calling makeBeams for measure ${index + 1} with ${validNotes.length} notes and ticksPerBeat ${ticksPerBeat}`);



            const timeSignature = { num: currentNumerator, den: currentDenominator };

            const beamResult = makeBeams(measureForBeams, ticksPerBeat, timeSignature);

            beams = beamResult.beams || [];
            console.log(`makeBeams found ${beams.length} beam groups for measure ${index + 1}`);
        } catch (beamError) {
            console.warn(`Error in makeBeams for measure ${index + 1}:`, beamError);
            beams = []; // Fallback �� ���������� ������
        }
    } else {
        console.log(`makeBeams skipped for measure ${index + 1}: function=${typeof makeBeams}, notes=${validNotes.length}, ticksPerBeat=${ticksPerBeat}`);
    }
    return beams;
}

// ----------------------
// ����ֲ� ��� ������� � ��������� ˲� � �������� �������
// ����������� Vex.Flow.StaveTie
// ���������:
// - ties: ����� ��'���� StaveTie � VexFlow.
// - context: �������� ���������� � VexFlow.
// - index: ������ ��������� �����.
// �������: void
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
// ����ֲ� ��� ������� � ��������� �в���� � �������� �������
// ����������� Vex.Flow.Tuplet
// ���������:
// - currentNumerator: �������� ��������� ������ �����.
// - currentDenominator: �������� ��������� ������ �����.
// - validNotes: ����� ��'���� StaveNote � VexFlow.
// - ticksPerBeat: ʳ������ ��� �� ������� ����.
// - index: ������ ��������� �����.
// - context: �������� ���������� � VexFlow.
// �������: void
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
// ����ֲ� ��� ������� � ��������� BEAMS � �������� �������
// ����������� Vex.Flow.Beam
// ���������:
// - beams: ����� ��'���� Beam � VexFlow.
// - context: �������� ���������� � VexFlow.
// - index: ������ ��������� �����.
// �������: void
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
// ����ֲ� ��� ��������� ����� (REST) � ������������� VEXFLOW
// ���������:
// - durationCode: ����� ��������� ����� (���������, "q", "h", "8", "16.", ����).
// �������: ��'��� Vex.Flow.StaveNote, �� ����� ����� �����, ��� null � ��� �������.
// ----------------------
    
function addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat) {
    console.log("FOO: midiRenderer.js - addMissingRests");
    while (lastNoteOffTime < ticksPerMeasure - thresholdGap) {
        const remainingTicks = ticksPerMeasure - lastNoteOffTime;
        console.log(`MR:add Missing Rest is running: remaining: ${remainingTicks}`);
        const restDurations = getDurationFromTicks(remainingTicks, ticksPerBeat);
        let timeadded = 0;
        restDurations.forEach((restDuration) => {
            notes.push(createRest(restDuration));
            timeadded += calculateTicksFromDuration(restDuration, ticksPerBeat);
        });
        lastNoteOffTime += timeadded;
        console.log(`MR:adding a rest done, time added = ${timeadded}, update lastNoteOffTime: ${lastNoteOffTime}`);
    }
}
// ----------------------
// ����ֲ� ��� ��������� ���̲�� ����� �� ��Ĳ��� TIME SIGNATURE
// ���� ���� TimeSignature � ����
// ������� ������� currentNumerator � currentDenominator
// ���������:
// - measure: ����� MIDI ���� ��������� �����.
// - currentNumerator: �������� ��������� ������ �����.
// - currentDenominator: �������� ��������� ������ �����.
// �������: ��'��� {currentNumerator, currentDenominator}.
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
// ����ֲ� ��� ��������� ��� �� ������������ �����
// ���� � ������ ���� � ������������ �����
// ���������:
// - activeNotes: ��'��� � ��������� ������ {pitch: startTime}.
// - measure: ����� MIDI ���� ��������� �����.
// �������: void
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
// ����ֲ� ��� ��������� ��������� ���̲�� ����� � ��Ĳ� TIME SIGNATURE
// ���� ���� TimeSignature ����� � ������� ����
// ������� {numerator, denominator}
// ���� ���� ��䳿, ������� 4/4 �� �������������
// ���������:
// - measures: ����� �����, ����� ���� � ����� MIDI ����.
// �������: ��'��� {numerator, denominator}.
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
// ����ֲ� ��� ��������� ˲�� (TIE) ̲� ������
// ���� ��������� ���� �� null
// ���������:
// - previousNote: ��������� ���� (Vex.Flow.StaveNote) ��� null.
// - ties: ����� �� (Vex.Flow.StaveTie) ��� ����������.
// - note: ������� ���� (Vex.Flow.StaveNote).
// �������: void
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
// ����ֲ� ��� ��������� ���� ̲� ������
// ���� � ������ �� ������� ���������� ���� � ��������� �������� ����
// ���������:
// - lastNoteOffTime: ���������� ��� ��������� �������� ����.
// - event: ������� ���� ����.
// - ticksPerBeat: ʳ������ ��� �� ������� ����.
// - thresholdGap: ̳������� ������� ��� ��� ��������� �����.
// - notes: ����� ��� ��� ����������.
// �������: void
// ----------------------

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

// ----------------------
// ����ֲ� ��� ��������� ����� �� ������� �����
// ���� ����� ���� � ���� ���������� �� � 0
// ���������:
// - event: ����� ���� � ����.
// - ticksPerBeat: ʳ������ ��� �� ������� ����.
// - thresholdGap: ̳������� ������� ��� ��� ��������� �����.
// - notes: ����� ��� ��� ����������.
// - barStartAbsTime: ���������� ��� ������� �����.
// �������: void
// ----------------------
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

// ----------------------
// ����ֲ� ��� ��������� ���� � ������������� VEXFLOW
// ���������:
// - durationCode: ��� ��������� ���� (���������, 'q', 'h', '8', '16r' ��� �����).
// - key: ������ ���� � ������ VexFlow (���������, 'C/4', 'D#/5').
// - accidental: ���� ���������� ('#', 'b', 'n') ��� null.
// �������: ��'��� Vex.Flow.StaveNote ��� null � ��� �������.
// ----------------------
function processNoteElement(durationCode, key, accidental) {
    console.log("FOO: midiRenderer.js - processNoteElement");
    key = key.replace('/', ''); // �������� ����� /
    const note = createNote(key, durationCode);
    if (note && accidental) {
        note.addAccidental(0, new Vex.Flow.Accidental(accidental));
    }
    // APPLY AUTO STEM for every newly processed note
    applyAutoStem(note, durationCode);
    return note;
}


// ----------------------
// ����ֲ� ��� ���������� ���в��ί ������ ������� ����� � �����Ѳ
// ���������:
// - measuresCount: ʳ������ �����.
// - GENERALWIDTH: �������� ������ �������.
// - BARWIDTH: ������ ������ �����.
// - HEIGHT: ������ ������ ����� ������� �����.
// - TOPPADDING: ������ ������ (�� ������������� 20).
// - CLEFZONE: ��������� ������ ��� ������� ����� � ����� (�� ������������� 60).
// - Xmargin: ˳��� ������ (�� ������������� 10).
// �������: �������� ������ �������, ��������� ��� ��������� ��� �����.
// ----------------------
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

// ----------------------
// ����ֲ� ��� ��������� ����ֲ� X,Y ��� ������� ������ �����
// ----------------------
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
// ----------------------
// ����ֲ� ��� ��������� ������ �����
// ----------------------
function getStaveWidth(BARWIDTH, index, CLEFZONE) {
    console.log("FOO: midiRenderer.js - getStaveWidth");
    let STAVE_WIDTH = BARWIDTH;
    if (index === 0) {
        STAVE_WIDTH += CLEFZONE;
        console.log("First stave width:", STAVE_WIDTH);
    }
    return STAVE_WIDTH;
}


const logEvent = (msg) => {
    if (stepRead) {
        stepRead.innerHTML += msg;
    }
};


window.drawScore = drawScore;
window.renderMidiFromUrl = renderMidiFromUrl;
