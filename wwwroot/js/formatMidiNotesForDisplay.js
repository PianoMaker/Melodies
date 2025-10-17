// Відображення списком
export function formatMidiNotesForDisplay(midiFile) {
    let output = "MIDI Notes:\n";
    const ticksPerBeat = midiFile.header.getTicksPerBeat();
    const MIN_REST_DURATION = 10; // Не відображати паузи менше 10 ticks

    // Отримати перший знайдений музичний розмір (або 4/4 за замовчуванням)
    const firstTs = getFirstTimeSignature(midiFile);
    const tsNum = firstTs?.numerator ?? 4;
    const tsDen = firstTs?.denominator ?? 4;
    const ticksPerMeasure = Math.round(ticksPerBeat * (tsNum * 4 / tsDen));

    midiFile.tracks.forEach((track, index) => {
        const events = midiFile.getTrackEvents(index);

        let currentTick = 0;
        let activeNotes = {};
        let lastEventWasNoteOff = false;
        let lastNoteEndTick = 0;

        events.forEach(event => {
            currentTick += event.delta || 0;

            // NoteOn (velocity > 0)
            if (event.type === 8 && event.subtype === 9 && event.param2 > 0) {
                if (lastEventWasNoteOff && currentTick > lastNoteEndTick) {
                    const restTicks = currentTick - lastNoteEndTick;
                    if (restTicks >= MIN_REST_DURATION) {
                        output += `${ticksToRestDuration(restTicks / 2, ticksPerBeat)}   [${Math.round(restTicks)}]\n`;
                    } else {
                        activeNotes[event.param1] = lastNoteEndTick;
                    }
                }
                activeNotes[event.param1] = currentTick;
                lastEventWasNoteOff = false;
            }
            // NoteOff (або NoteOn з velocity === 0)
            else if ((event.type === 8 && event.subtype === 8) ||
                     (event.type === 8 && event.subtype === 9 && event.param2 === 0)) {
                if (activeNotes[event.param1] !== undefined) {
                    const startTick = activeNotes[event.param1];
                    let durationTicks = currentTick - startTick;
                    durationTicks = roundDuration(durationTicks, ticksPerBeat);

                    const barIndex = ticksPerMeasure > 0 ? (Math.floor(startTick / ticksPerMeasure) + 1) : 1;
                    const tickInBar = ticksPerMeasure > 0 ? Math.round(startTick % ticksPerMeasure) : startTick;

                    // Додаємо лог тільки для Experimental/Notation
                    if (typeof window !== 'undefined' && window.location.pathname.includes("Experimental/Notation")) {
                        console.log(`NoteAnalizer: track=${index}, startTick=${startTick}, barIndex=${barIndex}, tickInBar=${tickInBar}, note=${event.param1}`);
                    }

                    output += `${ticksToDuration(durationTicks / 2, ticksPerBeat)}   ${getVexFlowNoteName(event.param1)}  [${event.param1} : ${Math.round(durationTicks)}]   ${barIndex}:${tickInBar}\n`;

                    lastNoteEndTick = currentTick;
                    lastEventWasNoteOff = true;
                    delete activeNotes[event.param1];
                }
            }
        });
    });

    return output;
}
;

// Пошук першого музичного розміру у файлі (перший трак з подією 0x58). Якщо не знайдено — 4/4
function getFirstTimeSignature(midiFile) {
    try {
        for (let ti = 0; ti < midiFile.tracks.length; ti++) {
            const events = midiFile.getTrackEvents(ti);
            for (const ev of events) {
                if (ev.subtype === 0x58) {
                    const numerator = ev.data?.[0] ?? 4;
                    const denominator = ev.data?.[1] ? Math.pow(2, ev.data[1]) : 4;
                    return { numerator, denominator };
                }
                // Деякі збірки MIDIFile можуть кодувати як meta
                if (ev.type === 0xFF && ev.metaType === 0x58 && Array.isArray(ev.data)) {
                    const numerator = ev.data?.[0] ?? 4;
                    const denominator = ev.data?.[1] ? Math.pow(2, ev.data[1]) : 4;
                    return { numerator, denominator };
                }
            }
        }
    } catch { /* ignore, повернемо 4/4 */ }
    return { numerator: 4, denominator: 4 };
}

// Функція для округлення тривалості
export function roundDuration(durationTicks, ticksPerBeat) {
    const standardDurations = [
        { ticks: ticksPerBeat / 16, threshold: 10 },
        { ticks: ticksPerBeat / 8, threshold: 10 },
        { ticks: ticksPerBeat / 4, threshold: 10 },
        { ticks: ticksPerBeat / 2, threshold: 10 },
        { ticks: ticksPerBeat, threshold: 10 },
        { ticks: ticksPerBeat * 2, threshold: 10 } // Double whole
    ];

    for (const duration of standardDurations) {
        if (Math.abs(durationTicks - duration.ticks) <= duration.threshold) {
            return duration.ticks;
        }
    }
    return durationTicks; // Повертаємо оригінальне значення, якщо не знайшли близького
}
export function ticksToDuration(ticks, ticksPerBeat) {
    // Визначаємо стандартні тривалості в ticks
    const thirtySecond = ticksPerBeat / 8; // 32nd
    const sixteenth = ticksPerBeat / 4; // 16th
    const eighth = ticksPerBeat / 2; // 8th
    const quarter = ticksPerBeat; // Quarter
    const half = ticksPerBeat * 2; // Half
    const whole = ticksPerBeat * 4; // Whole


    // Додаємо порогове значення для округлення (10 ticks)
    const threshold = 10;

    // Визначаємо найближчу стандартну тривалість
    if (Math.abs(ticks - thirtySecond) <= threshold) return "𝅘𝅥𝅯";
    if (Math.abs(ticks - sixteenth) <= threshold) return "𝅘𝅥𝅮";
    if (Math.abs(ticks - eighth) <= threshold) return "𝅘𝅥𝅭";
    if (Math.abs(ticks - quarter) <= threshold) return "𝅘𝅥";
    if (Math.abs(ticks - half) <= threshold) return "𝅗𝅥";
    if (Math.abs(ticks - whole) <= threshold) return "𝅝";

    // Для точкових нот
    if (Math.abs(ticks - sixteenth * 1.5) <= threshold) return "𝅘𝅥𝅮.";
    if (Math.abs(ticks - eighth * 1.5) <= threshold) return "𝅘𝅥𝅭.";
    if (Math.abs(ticks - quarter * 1.5) <= threshold) return "𝅘𝅥.";
    if (Math.abs(ticks - half * 1.5) <= threshold) return "𝅗𝅥.";

    // Якщо тривалість не відповідає жодному стандарту, повертаємо найближчий варіант
    if (ticks < sixteenth) return "𝅘𝅥𝅯";
    if (ticks < eighth) return "𝅘𝅥𝅮";
    if (ticks < quarter) return "𝅘𝅥𝅭";
    if (ticks < half) return "𝅘𝅥";
    if (ticks < whole) return "𝅗𝅥";
    return "𝅝";
}
export function ticksToRestDuration(ticks, ticksPerBeat) {
    // Визначаємо стандартні тривалості в ticks
    const thirtySecond = ticksPerBeat / 8; // 32nd
    const sixteenth = ticksPerBeat / 4; // 16th
    const eighth = ticksPerBeat / 2; // 8th
    const quarter = ticksPerBeat; // Quarter
    const half = ticksPerBeat * 2; // Half
    const whole = ticksPerBeat * 4; // Whole


    // Додаємо порогове значення для округлення (10 ticks)
    const threshold = 10;

    // Визначаємо найближчу стандартну тривалість
    if (Math.abs(ticks - thirtySecond) <= threshold) return "𝄿"; // 32nd rest
    if (Math.abs(ticks - sixteenth) <= threshold) return "𝄾"; // 16th rest
    if (Math.abs(ticks - eighth) <= threshold) return "𝄽"; // 8th rest
    if (Math.abs(ticks - quarter) <= threshold) return "𝄼"; // Quarter rest
    if (Math.abs(ticks - half) <= threshold) return "𝄻"; // Half rest
    if (Math.abs(ticks - whole) <= threshold) return "𝄺"; // Whole rest


    // Для точкових пауз
    if (Math.abs(ticks - sixteenth * 1.5) <= threshold) return "𝄾."; // Dotted 16th
    if (Math.abs(ticks - eighth * 1.5) <= threshold) return "𝄽."; // Dotted 8th
    if (Math.abs(ticks - quarter * 1.5) <= threshold) return "𝄼."; // Dotted quarter
    if (Math.abs(ticks - half * 1.5) <= threshold) return "𝄻."; // Dotted half


    // Якщо тривалість не відповідає жодному стандарту, повертаємо найближчий варіант
    if (ticks < sixteenth) return "𝄿";
    if (ticks < eighth) return "𝄾";
    if (ticks < quarter) return "𝄽";
    if (ticks < half) return "𝄼";
    if (ticks < whole) return "𝄻";
    return "𝄺";
}
// Допоміжна функція для отримання імені ноти
export function getNoteName2(noteNumber) {
    const noteNames = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];
    const octave = Math.floor(noteNumber / 12) - 1;
    return noteNames[noteNumber % 12] + octave;
}

export function getVexFlowNoteName(noteNumber) {
    const noteNames = ['c', 'c#', 'd', 'd#', 'e', 'f', 'f#', 'g', 'g#', 'a', 'a#', 'b'];
    const octave = Math.floor(noteNumber / 12) - 1;
    const noteIndex = noteNumber % 12;

    // Визначаємо правильну октаву для VexFlow
    let vexFlowOctave = octave;
    if (noteIndex >= 0) { // Для всіх нот, починаючи з C
        vexFlowOctave += 1;
    }

    return `${noteNames[noteIndex]}/${vexFlowOctave}`;
}
