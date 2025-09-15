// Відображення списком
export function formatMidiNotesForDisplay(midiFile) {
    let output = "MIDI Notes:\n";
    let activeNotes = {};
    let currentTick = 0;
    let lastEventWasNoteOff = false;
    let lastNoteEndTick = 0;
    const ticksPerBeat = midiFile.header.getTicksPerBeat();
    const MIN_REST_DURATION = 10; // Не відображати паузи менше 10 ticks

    midiFile.tracks.forEach((track, index) => {
        const events = midiFile.getTrackEvents(index);
        events.forEach(event => {
            currentTick += event.delta || 0;

            if (event.type === 8 && event.subtype === 9 && event.param2 > 0) {
                if (lastEventWasNoteOff && currentTick > lastNoteEndTick) {
                    const restTicks = currentTick - lastNoteEndTick;
                    if (restTicks >= MIN_REST_DURATION) {
                        output += `${ticksToRestDuration(restTicks / 2, ticksPerBeat)}   [${Math.round(restTicks)}]\n`;
                    } else {
                        // Корегуємо початок наступної ноти, ігноруючи коротку паузу
                        activeNotes[event.param1] = lastNoteEndTick;
                    }
                }
                activeNotes[event.param1] = currentTick;
                lastEventWasNoteOff = false;
            }
            else if ((event.type === 8 && event.subtype === 8) ||
                (event.type === 8 && event.subtype === 9 && event.param2 === 0)) {
                if (activeNotes[event.param1] !== undefined) {
                    let durationTicks = currentTick - activeNotes[event.param1];
                    // Округлюємо тривалість, якщо вона на 10 ticks менша від наступного рівня
                    durationTicks = roundDuration(durationTicks, ticksPerBeat);
                    output += `${ticksToDuration(durationTicks / 2, ticksPerBeat)}   ${getVexFlowNoteName(event.param1)}  [${event.param1} : ${Math.round(durationTicks)}]\n`;
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
