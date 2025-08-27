// Глобальне визначення durationMapping
const durationMapping = {
    1: 'w',
    w: 'w',
    2: 'h',
    h: 'h',
    4: 'q',
    q: 'q',
    8: '8',
    16: '16',
    32: '32',
    64: '64'
};

const reverseDurationMapping = {
    w: 1,
    "1": 1,
    h: 2,
    "2": 2,
    q: 4,
    "4": 4,
    "8": 8,
    "16": 16,
    "32": 32,
    "64": 64
};

// --- Enharmonic support (minimal) ---
let currentKeySignature = 0; // -7..+7 (sf)
let enharmonicPreference = 'auto'; // 'auto' | 'sharps' | 'flats'
function setEnharmonicPreference(pref) {
    console.log("FOO: midiparser_ext.js - setEnharmonicPreference");
    if (['auto','sharps','flats'].includes(pref)) {
        enharmonicPreference = pref;
        console.log('Enharmonic preference =', pref);
    } else console.warn('Unknown enharmonic preference', pref);
}
if (typeof window !== 'undefined') window.setEnharmonicPreference = setEnharmonicPreference;

// Аналізує події MIDI і оновлює currentKeySignature, що познчає тональність 
// Викликати після отримання подій MIDI
// шукає останню подію Key Signature (0xFF 0x59) і оновлює currentKeySignature
// sf: -7..+7, mode: 0=major, 1=minor
// currentKeySignature - глобальна змінна
// enharmonicPreference - 'auto' | 'sharps' | 'flats'

function updateKeySignatureFromEvents(events) {
    console.log("FOO: midiparser_ext.js - updateKeySignatureFromEvents");
    for (const ev of events) {
        if (ev.type === 0xFF && ev.metaType === 0x59 && ev.data && ev.data.length >= 2) {
            // sf byte is signed (-7..+7). Data[0] already 0..255, convert
            const raw = ev.data[0];
            currentKeySignature = raw > 127 ? raw - 256 : raw;
            console.log('Tonality: Key signature meta: sf =', currentKeySignature, 'mode', ev.data[1] === 0 ? 'major':'minor');
        }
    }
}
if (typeof window !== 'undefined') window.updateKeySignatureFromEvents = updateKeySignatureFromEvents;

function midiNoteToVexFlow(midiNote) {
    console.log("FOO: midiparser_ext.js - midiNoteToVexFlow");
    // Decide spelling based on preference / key signature
    const sharpNames = ["C","C#","D","D#","E","F","F#","G","G#","A","A#","B"];
    const flatNames  = ["C","Db","D","Eb","E","F","Gb","G","Ab","A","Bb","B"];
    const pc = midiNote % 12;
    const octave = Math.floor(midiNote / 12) - 1; // keep existing shift
    let useFlats;
    if (enharmonicPreference === 'sharps') useFlats = false; else if (enharmonicPreference === 'flats') useFlats = true; else useFlats = currentKeySignature < 0;
    const chosen = useFlats ? flatNames[pc] : sharpNames[pc];
    const accidental = chosen.includes('#') ? '#' : (chosen.includes('b') ? 'b' : null);
    return { key: `${chosen.replace(/[#b]/,'')}/${octave}`, accidental };
}

function splitEventsIntoMeasures(midiEvents, ticksPerBeat) {
    console.log("FOO: midiparser_ext.js - splitEventsIntoMeasures");
    let measures = [];
    let currentMeasure = [];
    // Підраховуємо абсолютний час початку кожного такту
    const barStartAbsTimes = calculateBarStartAbsTimes(midiEvents, ticksPerBeat);
    let barIndex = 1; // 0 - це початок першого такту
    let nextBarAbsTime = barStartAbsTimes[barIndex] || Infinity;

    midiEvents.forEach((event) => {
        // Якщо подія перейшла межу нового такту
        while (event.absTime >= nextBarAbsTime) {
            measures.push(currentMeasure);
            currentMeasure = [];
            barIndex++;
            nextBarAbsTime = barStartAbsTimes[barIndex] || Infinity;
        }
        currentMeasure.push(event);
    });
    if (currentMeasure.length > 0) {
        measures.push(currentMeasure);
    }
    return measures;
}

function calculateTotalDuration(notesString = "") {
    console.log("FOO: midiparser_ext.js - calculateTotalDuration");
    const notes = notesString.split(/[,|]/);
    let totalDuration = 0;

    notes.forEach(note => {
        const duration = getDurationValueFromNote(note.trim());
        totalDuration += duration;
    });

    return totalDuration;
}

// приймає код тривалості і повертає тривалість в тіках
function calculateTicksFromDuration(duration, ticksPerBeat) {
    console.log("FOO: midiparser_ext.js - calculateTicksFromDuration");

    let result = getDurationFromCode(duration) * ticksPerBeat;
    console.log(`Calculating ticks for duration: ${duration}, result: ${result}`);
    return result;
}

// приймає кількість тіків, повертає код тривалості
function getDurationFromTicks(ticks, ticksPerBeat) {
    console.log("FOO: midiparser_ext.js - getDurationFromTicks");
    if (typeof ticks !== "number" || typeof ticksPerBeat !== "number" || ticks <= 0 || ticksPerBeat <= 0) {
        console.warn("Invalid input to getDurationFromTicks:", { ticks, ticksPerBeat });
        return []; // Повертаємо порожній масив у разі некоректних даних
    }
    console.log('Calculating duration for ticks: ' + ticks + ', ticksPerBeat: ' + ticksPerBeat);
    var quarterTicks = ticksPerBeat; // Тривалість чверті дорівнює TPQN
    let mingap = ticksPerBeat / 16; // Мінімальний проміжок між нотами

    const durations = [];

    // Розбиваємо ticks на частини
    while (ticks > 0) {
        if (ticks >= quarterTicks * 6 - mingap) {
            durations.push("w."); // Ціла нота з крапкою
            ticks -= quarterTicks * 6;
        } else if (ticks >= quarterTicks * 4 - mingap) {
            durations.push("w"); // Ціла нота
            ticks -= quarterTicks * 4;
        } else if (ticks >= quarterTicks * 3 - mingap) {
            durations.push("h."); // Половинна нота з крапкою
            ticks -= quarterTicks * 3;
        } else if (ticks >= quarterTicks * 2 - mingap) {
            durations.push("h"); // Половинна нота
            ticks -= quarterTicks * 2;
        } else if (ticks >= quarterTicks * 1.5 - mingap) {
            durations.push("q."); // Чверть нота з крапкою
            ticks -= quarterTicks * 1.5;
        } else if (ticks >= quarterTicks * 1 - mingap) {
            durations.push("q"); // Чверть нота
            ticks -= quarterTicks * 1;
        } else if (ticks >= quarterTicks * 0.75 - mingap) {
            durations.push("8."); // Восьма нота з крапкою
            ticks -= quarterTicks * 0.75;
        } else if (ticks >= quarterTicks * 0.5 - mingap) {
            durations.push("8"); // Восьма нота
            ticks -= quarterTicks * 0.5;
        } else if (ticks >= quarterTicks * 0.25 - mingap) {
            durations.push("16"); // 0.25 = 64 тіків при ticksPerBeat=256
            ticks -= quarterTicks * 0.25;
        } else if (ticks >= quarterTicks * 0.125 - mingap) {
            durations.push("32"); // 0.125 = 32 тіки при ticksPerBeat=256
            ticks -= quarterTicks * 0.125;
        } else {
            console.warn(`Calculated durations: unknown / ${ticks} ticks`);
            break;
        }
    }
    console.log('Calculated durations: ', durations);


    return durations;
}


function calculateEndless(notesString) {
    console.log("FOO: midiparser_ext.js - calculateEndless");

    let totalDuration = calculateTotalDuration(notesString);
    if (totalDuration % 1 === 0)
        return `${totalDuration}/4`;
    else if
        (totalDuration * 2 % 1 === 0)
        return `${totalDuration * 2}/8`;
    else if
        (totalDuration * 4 % 1 === 0)
        return `${totalDuration * 4}/16`;
    else if
        (totalDuration * 8 % 1 === 0)
        return `${totalDuration * 8}/32`;
    else return "unknown";
}

//приймає розмір такту (чисельник/знаменник) і повертає тривалість такту в одиницях чвертної ноти
function getBarDuration(timeSignature) {
    console.log("FOO: midiparser_ext.js - getBarDuration");
    const [numerator, denominator] = timeSignature.split('/').map(Number);
    return numerator * (4 / denominator);
}

function getDurationFromCode(durationCode) {
    console.log("FOO: midiparser_ext.js - getDurationFromCode");
    console.log(`Getting duration value for code: ${durationCode}`);
    try {
        const isDotted = durationCode.endsWith('.');
        const baseDuration = isDotted ? durationCode.slice(0, -1) : durationCode;

        const baseValue = reverseDurationMapping[baseDuration];
        console.log(`Base value for duration code ${baseDuration}: ${baseValue}`);
        if (!baseValue) {
            console.warn(`Unknown duration code: ${durationCode}`);
            return 1; // Значення за замовчуванням
        }
        const resultDuration = 4 / baseValue;
        return !isDotted ? resultDuration : resultDuration * 1.5;

    }
    catch {
        console.warn(`Unknown duration code: ${durationCode}`);
        return 1;
    }
}

//приймає код тривалості з отриманого коду ноти
const getDurationValueFromNote = (note) => {
    console.log("FOO: midiparser_ext.js - getDurationValueFromNote");
    const parts = note.split('/');
    if (parts.length > 1) {
        let result = getDurationFromCode(parts[1]);
        console.log("Note parts: ", result);
        return result;
    }
    else {
        console.warn(`No duration code found in note: ${note}`);
        return 1
    }
};


function createRest(duration) {
    console.log("FOO: midiparser_ext.js - createRest");
    console.log("Creating rest with duration: ", duration);
    const isDotted = duration.endsWith('.');
    const baseDuration = isDotted ? duration.slice(0, -1) : duration;

    try {
        const staveNote = new Vex.Flow.StaveNote({
            keys: ['b/4'], // ключ для паузи неважливий
            duration: `${baseDuration}r` // Форматуємо тривалість як паузу
        });

        if (isDotted) {
            console.log("Is dotted:", isDotted);
            staveNote.addDot(0); // Додаємо крапку до паузи
        }

        return staveNote;
    } catch (error) {
        console.error(`Failed to create rest with duration: ${duration}`, error);
        return null; // Return null if creation fails
    }
}


function createNote (noteKey, duration) {
    console.log("FOO: midiparser_ext.js - createNote");
    console.log(`Creating note with noteKey ${noteKey} duration: ${duration}`);
    const noteMatch = noteKey.match(/^([a-gA-G])(b|#)?(\d)?$/);
    if (!noteMatch) {
        console.error(`Invalid note key: ${noteKey}`);
        return null;
    }

    const [, letter, accidental, octaveRaw] = noteMatch;
    const octave = octaveRaw|| '4'; // Default to octave 4
    const key = `${letter.toLowerCase()}${accidental || ''}/${octave}`;
    const isDotted = duration.endsWith('.');
    const baseDuration = isDotted ? duration.slice(0, -1) : duration;

    try {
        const staveNote = new Vex.Flow.StaveNote({
            keys: [key],
            duration: baseDuration
        });

        if (accidental) {
            staveNote.addAccidental(0, new Vex.Flow.Accidental(accidental));
        }

        if (isDotted) {
            staveNote.addDot(0); // Add dot if dotted
        }
        console.log(`Created note: ${key}, duration: ${baseDuration}`);
        return staveNote;
    } catch (error) {
        console.error(`Failed to create note with key: ${noteKey} and duration: ${duration}`, error);
        return null; // Return null if creation fails
    }
};

// // Аналіз нот із введеного рядку (ноти через коми)
function processNote(element) {
    console.log("FOO: midiparser_ext.js - processNote");
    const parts = element.split('/');
    console.log("Processing note: ", parts[0]);
    const noteKey = parts[0].toLowerCase();
    const durationKey = parts[1] || 'q';
    const isRest = parts[0].toLowerCase() === 'r' || durationKey.endsWith('r');

    if (isRest) {
        console.log("Creating rest with duration: ", durationKey);
        return createRest(durationKey);
    } else {
        return createNote(noteKey, durationKey);
    }
}

// аналізує MIDIFile і визначає розмір та тікі на такт
function getTimeSignatureAndTicksPerMeasure(midiFile) {
    console.log("FOO: midiparser_ext.js - getTimeSignatureAndTicksPerMeasure");
    console.log("Getting time signature and ticks per measure");
    const ticksPerBeat = midiFile.header.getTicksPerBeat(); // TPQN
    let lastNumerator = 4; // Початкове значення за замовчуванням
    let lastDenominator = 4;
    let lastTicksPerMeasure = ticksPerBeat * 4;

    const timeSignatureEvent = midiFile.getMidiEvents().find(event => event.type === 0xFF && event.metaType === 0x58);

    if (timeSignatureEvent && timeSignatureEvent.data) {
        const numerator = timeSignatureEvent.data[0]; // Чисельник
        const denominator = Math.pow(2, timeSignatureEvent.data[1]); // Знаменник
        const ticksPerMeasure = ticksPerBeat * numerator * (4 / denominator); // Обчислення тиках на такт

        // Оновлюємо останній відомий розмір такту
        lastNumerator = numerator;
        lastDenominator = denominator;
        lastTicksPerMeasure = ticksPerMeasure;

        return { numerator, denominator, ticksPerMeasure, ticksPerBeat };
    }

    // Якщо подія Time Signature не знайдена, повертаємо останній відомий розмір такту
    return { numerator: lastNumerator, denominator: lastDenominator, ticksPerMeasure: lastTicksPerMeasure, ticksPerBeat };
}

function getKeySignature(midiEvents) {
    console.log("FOO: midiparser_ext.js - getKeySignature");
    const keySignatureEvents = midiEvents.filter(event =>
        event.type === 0xFF && event.metaType === 0x59
    );

    return keySignatureEvents.map(event => ({ param1: event.data && event.data[0], param2: event.data && event.data[1] }));
}

function SetEventsAbsoluteTime(midiData) {
    console.log("FOO: midiparser_ext.js - SetEventsAbsoluteTime");
    let allEvents = [];
    midiData.track.forEach((track, trackIndex) => {
        let absTime = 0;
        if (Array.isArray(track.event)) {
            track.event.forEach(event => {
                absTime += event.deltaTime || 0;
                allEvents.push({ ...event, absTime, track: trackIndex });                
            });

        }
    });
    allEvents.sort((a, b) => a.absTime - b.absTime);
    
    return allEvents;
}


// Функція для логування подій з детальною інформацією
// logMeasureEvents(allEvents); викликати для логування
// allEvents - масив подій з SetEventsAbsoluteTime
// track, type, data, absTime
// type: 0x9 - NoteOn, 0x8 - NoteOff, 0xFF - Meta, 0xF0 - SysEx, 0xB - Controller, 0xC - ProgramChange, 0xA - Aftertouch, 0xE - PitchBend
// data: [note, velocity] для NoteOn/NoteOff
// absTime - абсолютний час події
// track - номер треку
// Використання: logMeasureEvents(allEvents);

function logMeasureEvents(allEvents) {
    console.log("FOO: midiparser_ext.js - logEvents");
    allEvents.forEach((event) => {
        let eventType = null;
        let noteInfo = '';
        if (event.type === 0x9) {
            if (event.data && event.data[1] === 0) {
                eventType = 'NoteOff';
            } else {
                eventType = 'NoteOn';
            }
            if (event.data && event.data.length > 0) {
                noteInfo = `, Note: ${event.data[0]}`;
            }
        } else if (event.type === 0x8) {
            eventType = 'NoteOff';
            if (event.data && event.data.length > 0) {
                noteInfo = `, Note: ${event.data[0]}`;
            }
        } else if (event.type === 0xFF) eventType = 'Meta';
        else if (event.type === 0xF0) eventType = 'SysEx';
        else if (event.type === 0xB) eventType = 'Controller';
        else if (event.type === 0xC) eventType = 'ProgramChange';
        else if (event.type === 0xA) eventType = 'Aftertouch';
        else if (event.type === 0xE) eventType = 'PitchBend';
        else eventType = 'Other';
        console.log(`LE: Track ${event.track}, Type: ${eventType}${noteInfo}, AbsTime: ${event.absTime}`);
    });
}

function findEndEvent(midiEvents, element) {
    console.log("FOO: midiparser_ext.js - findEndEvent");
    // Шукаємо подію End of Track
    const endEvent = midiEvents.find(event =>
        event.type === 0xFF && event.metaType === 0x2F
    );

    if (endEvent) {
        // Якщо подія знайдена, виводимо її параметри
        element.innerHTML += `<br>End Event found:<br>`;
        element.innerHTML += `Delta Time: ${endEvent.deltaTime || endEvent.delta}<br>`;
        element.innerHTML += `Absolute Time: ${endEvent.absTime || "N/A"}<br>`;
        return true;
    } else {
        // Якщо подія не знайдена, виводимо повідомлення
        element.innerHTML += `<br>No End Event found in the MIDI file.<br>`;
        return false;
    }
}


async function getFileFromPath(filepath) {
    console.log("FOO: midiparser_ext.js - getFileFromPath");
    if (typeof filepath === "string" && filepath) {
        console.log("getFileFromPath is running, filepath:", filepath);

        try {
            const response = await fetch(filepath);
            const blob = await response.blob();
            const filename = filepath.split('/').pop();
            const file = new File([blob], filename);
            console.log(`returning file ${filename}`);
            return file;
        } catch (error) {
            console.error("Не вдалося завантажити файл:", error);
            return null;
        }
    } else {
        console.error("filepath не є рядком або порожній:", filepath);
        return null;
    }
}




function playTimeToTicks(playTime, ticksPerBeat, tempo = 500000) {
    console.log("FOO: midiparser_ext.js - playTimeToTicks");
    // tempo — у мікросекундах на чверть ноти (default 500000)
    const tickDuration = (tempo / 1_000_000) / ticksPerBeat;
    return Math.round(playTime / tickDuration);
}

///
/// рахує кількість тактів
///
function getMeasureCount(midiEvents, ticksPerBeat) {
    console.log("FOO: midiparser_ext.js - getMeasureCount");
    ensureTimeSignature(midiEvents);

    const timeSignatureEvents = getTimeSignatureEvents(midiEvents);

    let maxAbsTime = 0;
    midiEvents.forEach(e => {
        if (typeof e.absTime === "number" && e.absTime > maxAbsTime) {
            maxAbsTime = e.absTime;
        }
    });

    let measureCount = 0;

    for (let i = 0; i < timeSignatureEvents.length; i++) {
        const current = timeSignatureEvents[i];
        const next = timeSignatureEvents[i + 1];
        const startTime = current.absTime;
        const endTime = next ? next.absTime : maxAbsTime;

        const numerator = current.param1;
        const denominator = Math.pow(2, current.param2);
        const ticksPerMeasure = ticksPerBeat * numerator * (4 / denominator);

        const intervalTicks = endTime - startTime;
        const measuresInInterval = Math.ceil(intervalTicks / ticksPerMeasure);

        measureCount += measuresInInterval;
    }

    return measureCount;
}


function calculateBarStartAbsTimes(midiEvents, ticksPerBeat) {
    console.log("FOO: midiparser_ext.js - calculateBarStartAbsTimes");
    let barStartAbsTimes = [0];
    let ticksPerMeasure = ticksPerBeat * 4;
    let currentAbsTime = 0;
    let currentTicks = 0;

    midiEvents.forEach(event => {
        // Оновлюємо ticksPerMeasure при зміні розміру
        if (event.type === 0xFF && event.metaType === 0x58) {
            const numerator = event.data && event.data[0] ? event.data[0] : 4;
            const denominator = event.data && event.data[1] ? Math.pow(2, event.data[1]) : 4;
            ticksPerMeasure = ticksPerBeat * numerator * (4 / denominator);
        }

        currentTicks += event.deltaTime || 0;
        currentAbsTime += event.deltaTime || 0;

        if (currentTicks >= ticksPerMeasure) {
            barStartAbsTimes.push(currentAbsTime);
            currentTicks = 0;
        }
    });

    return barStartAbsTimes;
}
function getTimeSignatureEvents(midiEvents) {
    console.log("FOO: midiparser_ext.js - getTimeSignatureEvents");
    return midiEvents.filter(event =>
        event.type === 0xFF && event.metaType === 0x58
    );
}

function ensureTimeSignature(midiEvents) {
    console.log("FOO: midiparser_ext.js - ensureTimeSignature");
        // Додаємо подію 4/4 на тік 0
        midiEvents.unshift({
            type: 0xFF,
            metaType: 0x58,
            deltaTime: 0,
            absTime: 0,
            data: [4, 2], // 4/4
            // інші потрібні поля за потреби
        });
        console.log("Time Signature 4/4 added at tick 0");
}



function getMidiEventFromArray(arrayBuffer) {
    console.log("FOO: midiparser_ext.js - getMidiEventFromArray");

    const midiFile = new MIDIFile(arrayBuffer);
    return midiFile.getMidiEvents();

}


function midiNoteFromVexKey(key) {
    console.log("FOO: midiparser_ext.js - midiNoteFromVexKey");
    // Простий приклад для C4 ("c/4") -> 60
    // Реалізуйте повну відповідність для всіх нот
    const noteNames = { 'c': 0, 'd': 2, 'e': 4, 'f': 5, 'g': 7, 'a': 9, 'b': 11 };
    const match = key.match(/^([a-g])(#|b)?\/(\d)$/i);
    if (!match) return null;
    let [, note, accidental, octave] = match;
    note = note.toLowerCase();
    let midi = 12 + (parseInt(octave) * 12) + noteNames[note];
    if (accidental === '#') midi += 1;
    if (accidental === 'b') midi -= 1;
    return midi;
}

function ensureEndEvent(midiEvents, element) {
    console.log("FOO: midiparser_ext.js - ensureEndEvent");
    const hasEndTrack = midiEvents.some(e => e.type === MIDIEvents.EVENT_META && e.subtype === MIDIEvents.EVENT_META_END_OF_TRACK
    );
    if (!hasEndTrack) {
        let lastEvent = midiEvents[midiEvents.length - 1];
        let endTime = lastEvent.absTime || lastEvent.playTime || 0;
        midiEvents.push({
            type: MIDIEvents.EVENT_META,
            subtype: MIDIEvents.EVENT_META_END_OF_TRACK,
            delta: 0,
            absTime: endTime,
            playTime: endTime,
            track: lastEvent.track || 0
        });
        if (element)
            element.innerHTML += `End of Track event added at ${endTime}.<br>`;
    }
}