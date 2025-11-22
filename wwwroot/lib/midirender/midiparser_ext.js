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

// NOTE: getAllEvents moved to the bottom (after helper meta functions) to avoid referencing MidiMeta before it is defined.

// --- Enharmonic support (minimal) ---
let currentKeySignature = 0; // -7..+7 (sf)
let enharmonicPreference = 'auto'; // 'auto' | 'sharps' | 'flats'
function setEnharmonicPreference(pref) {
    console.log("FOO: midiparser_ext.js - setEnharmonicPreference");
    if (['auto', 'sharps', 'flats'].includes(pref)) {
        enharmonicPreference = pref;
        console.log('Enharmonic preference =', pref);
    } else console.warn('Unknown enharmonic preference', pref);
}
if (typeof window !== 'undefined') window.setEnharmonicPreference = setEnharmonicPreference;


// ---------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ОНОВЛЕННЯ ТОНАЛЬНОСТІ З ПОДІЙ MIDI
// Використання: updateKeySignatureFromEvents(midiEvents);
// Повертає об'єкт { sf, mi } або null, якщо подій Key Signature немає
// sf: -7..+7, mi: 0=major, 1=minor
// Потрібні глобальні функції: normalizeMetaEvent, decodeKeySignature, isKeySignatureEvent
// currentKeySignature - глобальна змінна
// enharmonicPreference - 'auto' | 'sharps' | 'flats'
// ---------------------------------------------------------------------

function updateKeySignatureFromEvents(events) {
    console.log("FOO: midiparser_ext.js - updateKeySignatureFromEvents");
    if (!Array.isArray(events)) return null;
    let ks = null;
    for (let i = 0; i < events.length; i++) {
        let ev = events[i];
        // Нормалізуємо meta події для стабільного поля data
        if (ev && ev.type === 0xFF && typeof normalizeMetaEvent === 'function') {
            ev = normalizeMetaEvent(ev);
        }
        // Перевіряємо різні форми key signature
        const isKS = (typeof isKeySignatureEvent === 'function') ? isKeySignatureEvent(ev) : (ev && ev.type === 0xFF && (ev.metaType === 0x59 || ev.subtype === 0x59));
        if (!isKS) continue;

        // Пробуємо розпарсити через decodeKeySignature
        if (typeof decodeKeySignature === 'function') {
            ks = decodeKeySignature(ev);
        }
        // Fallback 1: з data байтів
        if (!ks && ev && Array.isArray(ev.data) && ev.data.length >= 2) {
            let bytes = ev.data.slice();
            if (bytes.length >= 3 && bytes[0] === 2) bytes = bytes.slice(1);
            let sf = bytes[0]; if (sf > 127) sf -= 256;
            const miRaw = bytes[1];
            const mi = (miRaw === 0 || miRaw === 1) ? miRaw : (miRaw ? 1 : 0);
            ks = { sf, mi };
        }

        // Fallback 2: param1/param2
        if (!ks && ev && ev.param1 !== undefined && ev.param2 !== undefined) {
            let sf = ev.param1; if (sf > 127) sf -= 256;
            const miRaw = ev.param2;
            const mi = (miRaw === 0 || miRaw === 1) ? miRaw : (miRaw ? 1 : 0);
            ks = { sf, mi };
        }

        if (ks) break;
    }
    if (ks && typeof ks.sf === 'number') {
        currentKeySignature = ks.sf;
        // Extra safety
        ks.mi = (ks.mi === 0 || ks.mi === 1) ? ks.mi : (ks.mi ? 1 : 0);
        return { sf: ks.sf, mi: ks.mi };
    }
    return null;
}


function midiNoteToVexFlow(midiNote) {
    console.log("FOO: midiparser_ext.js - midiNoteToVexFlow");
    // Decide spelling based on preference / key signature
    const sharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    const flatNames = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
    const pc = midiNote % 12;// pitch class
    const octaveRaw = Math.floor(midiNote / 12) - 1; // keep existing shift

    // auto/sharps/flats preference
    let useFlats;
    if (enharmonicPreference === 'sharps') useFlats = false;
    else if (enharmonicPreference === 'flats') useFlats = true;
    else useFlats = currentKeySignature < 0;

    // Default choice
    let chosen = useFlats ? flatNames[pc] : sharpNames[pc];
    let outOctave = octaveRaw;

    // Extreme key signatures preference (only when auto):
    // - more than 5 flats (sf <= -6): B -> Cb (next octave), E -> Fb (same octave)
    // - more than 5 sharps (sf >= +6): F -> E# (same octave)
    if (enharmonicPreference === 'auto') {
        if (typeof currentKeySignature === 'number') {
            if (currentKeySignature <= -6 && pc === 11) {
                chosen = 'Cb';
                outOctave = octaveRaw + 1;
            } else if (currentKeySignature <= -6 && pc === 4) {
                chosen = 'Fb';
            } else if (currentKeySignature >= 6 && pc === 5) {
                chosen = 'E#';
            }
        }
    }

    const accidental = chosen.includes('#') ? '#' : (chosen.includes('b') ? 'b' : null);
    return { key: `${chosen.replace(/[#b]/, '')}/${outOctave}`, accidental };
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

/**
 * Повертає код тривалості ноти
 * @param {any} ticks        - тривалість у тіках
 * @param {any} ticksPerBeat - поточний TPQN
 * @returns
 */

function getDurationFromTicks(ticks, ticksPerBeat) {
    console.log("FOO: midiparser_ext.js - getDurationFromTicks");
    if (typeof ticks !== "number" || typeof ticksPerBeat !== "number" || ticks <= 0 || ticksPerBeat <= 0) {
        console.warn("Invalid input to getDurationFromTicks:", { ticks, ticksPerBeat });
        return [];                      // Повертаємо порожній масив у разі некоректних даних
    }
    console.log('Calculating duration for ticks: ' + ticks + ', ticksPerBeat: ' + ticksPerBeat);
    var quarterTicks = ticksPerBeat;    // Тривалість чверті дорівнює TPQN
    let mingap = ticksPerBeat / 16;     // Мінімальний проміжок між нотами

    const durations = [];

    // Значення для тріолей
    const triQ  = quarterTicks * (2 / 3);  // 'qt'
    const tri8  = quarterTicks / 3;        // '8t'
    const tri16 = quarterTicks / 6;        // '16t'
    const tri32 = quarterTicks / 12;       // '32t'

    // Розбиваємо ticks на частини
    while (ticks > 0) {
        // Спочатку обробляємо тріолі (вікно допуску ±mingap)
        if (ticks >= triQ - mingap && ticks <= triQ + mingap) {
            durations.push("qt");       //тріольна четвертна
            ticks -= triQ;
            continue;
        } else if (ticks >= tri8 - mingap && ticks <= tri8 + mingap) {
            durations.push("8t");       //тріольна вісімка
            ticks -= tri8;
            continue;
        } else if (ticks >= tri16 - mingap && ticks <= tri16 + mingap) {
            durations.push("16t");      //тріольна шіснадцятка
            ticks -= tri16;
            continue;
        } else if (ticks >= tri32 - mingap && ticks <= tri32 + mingap) {
            durations.push("32t");      //тріольна 32-га
            ticks -= tri32;
            continue;
        }
        // Регулярні тривалості
        if (ticks >= quarterTicks * 6 - mingap) {
            durations.push("w.");       // Ціла нота з крапкою
            ticks -= quarterTicks * 6;
        } else if (ticks >= quarterTicks * 4 - mingap) {
            durations.push("w");        // Ціла нота
            ticks -= quarterTicks * 4;
        } else if (ticks >= quarterTicks * 3 - mingap) {
            durations.push("h.");       // Половинка з крапкою
            ticks -= quarterTicks * 3;
        } else if (ticks >= quarterTicks * 2 - mingap) {
            durations.push("h");        // Половинка 
            ticks -= quarterTicks * 2;
        } else if (ticks >= quarterTicks * 1.5 - mingap) {
            durations.push("q.");       // Чвертна з крапкою
            ticks -= quarterTicks * 1.5;
        } else if (ticks >= quarterTicks * 1 - mingap) {
            durations.push("q");        // Чвертна 
            ticks -= quarterTicks * 1;
        } else if (ticks >= quarterTicks * 0.75 - mingap) {
            durations.push("8.");       // Вісімка з крапкою
            ticks -= quarterTicks * 0.75;
        } else if (ticks >= quarterTicks * 0.5 - mingap) {
            durations.push("8");        // Вісімка
            ticks -= quarterTicks * 0.5;
        } else if (ticks >= quarterTicks * 0.25 - mingap) {
            durations.push("16");       // Шіснадцятка
            ticks -= quarterTicks * 0.25;
        } else if (ticks >= quarterTicks * 0.125 - mingap) {
            durations.push("32");       // 32-га
            ticks -= quarterTicks * 0.125;
        } else {
            console.warn(`Calculated durations: unknown / ${ticks} ticks`);
            break;
        }
    }
    console.log('Calculated durations: ', durations);


    return durations;
}

/**
 * 
 * @param {any} notesString
 * @returns
 */
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
        const hasDot = durationCode.endsWith('.');
        const hasTriplet = durationCode.endsWith('t');
        if (hasDot && hasTriplet) {
            console.warn(`Dotted-triplet not supported: ${durationCode}, ignoring dot.`);
        }
        const raw = (hasDot || hasTriplet) ? durationCode.slice(0, -1) : durationCode;

        const baseValue = reverseDurationMapping[raw];
        console.log(`Base value for duration code ${raw}: ${baseValue}`);
        if (!baseValue) {
            console.warn(`Unknown duration code: ${durationCode}`);
            return 1; // Значення за замовчуванням
        }
        let resultDuration = 4 / baseValue; // у чвертях
        if (hasDot && !hasTriplet) resultDuration *= 1.5;
        if (hasTriplet) resultDuration *= (2 / 3);
        return resultDuration;

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
    const isTriplet = duration.endsWith('t');
    const baseDuration = (isDotted || isTriplet) ? duration.slice(0, -1) : duration;

    try {
        const staveNote = new Vex.Flow.StaveNote({
            keys: ['b/4'], // ключ для паузи неважливий
            duration: `${baseDuration}r` // Форматуємо тривалість як паузу
        });

        if (isDotted && !isTriplet) {
            console.log("Is dotted:", isDotted);
            staveNote.addDot(0); // Додаємо крапку до паузи
        }

        // Позначку тріолі для rests не ставимо; Tuplet працює і без цього для нот

        return staveNote;
    } catch (error) {
        console.error(`Failed to create rest with duration: ${duration}`, error);
        return null; // Return null if creation fails
    }
}

// --------------------------------
// ФУНКЦІЯ ДЛЯ СТВОРЕННЯ НОТИ
// приймає note
// noteKey: 'c4', 'd#5', 'gb3' і т.д.
// duration: 'q', 'h.', '8t' і т.д.
// повертає Vex.Flow.StaveNote або null
// --------------------------------

function createNote(noteKey, duration) {
    console.log("FOO: midiparser_ext.js - createNote");
    //console.log(`Creating note with noteKey ${noteKey} duration: ${duration}`);
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
        const staveNote = new Vex.Flow.StaveNote({
            keys: [key],
            duration: baseDuration
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
        //        console.log(`Created note: ${key}, duration: ${baseDuration}`);
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

function isKeySignatureEvent(ev) {
    // Универсальне визначення key signature meta (0x59)
    if (!ev) return false;
    const META_KEY_ID = 0x59; // 89
    const isMeta =
        ev.type === 0xFF ||
        (typeof MIDIEvents !== 'undefined' ? ev.type === MIDIEvents.EVENT_META : false) ||
        ev.meta === true;

    // metaType або subtype або можливі рядкові значення
    let metaId = ev.metaType !== undefined ? ev.metaType :
        ev.subtype !== undefined ? ev.subtype :
            (ev.metaTypeHex || ev.subtypeHex);

    if (typeof metaId === 'string') {
        // Пробуємо як hex або десяткове
        if (/^0x/i.test(metaId)) {
            metaId = parseInt(metaId, 16);
        } else {
            const dec = parseInt(metaId, 10);
            if (!isNaN(dec)) metaId = dec;
        }
    }
    return isMeta && metaId === META_KEY_ID;
}

// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ПОДІЙ KEY SIGNATURE З MIDI
// Використання: const keySignatures = getKeySignature(midiEvents);
// Повертає масив об'єктів з параметрами key signature
// { param1: sf, param2: mi, sf, mode, name, absTime, raw }
// sf: -7..+7, mi: 0=major, 1=minor, name: 'C', 'Gm' і т.д., absTime: абсолютний час події
// raw: оригінальні байти [sf, mi]
// Підтримує різні форми подій key signature
// Потрібні глобальні функції: normalizeMetaEvent, decodeKeySignature
//---------------------------------------------------------------------
function getKeySignature(midiEvents) {
    if (!Array.isArray(midiEvents)) return [];
    const result = [];
    midiEvents.forEach(ev => {
        if (!ev) return;
        // Normalize meta if possible
        if (ev.type === 0xFF && typeof normalizeMetaEvent === 'function') {
            ev = normalizeMetaEvent(ev);
        }
        // Detect key signature meta forms
        const isKS =
            (ev.type === 0xFF && (ev.metaType === 0x59 || ev.subtype === 0x59)) ||
            (ev.type === 0x59); // flattened variant
        if (!isKS) return;

        let decoded = null;
        // Preferred: global decoder
        if (typeof decodeKeySignature === 'function') {
            decoded = decodeKeySignature(ev);
        }
        // Fallback: data array
        if (!decoded) {
            let bytes = Array.isArray(ev.data) ? ev.data.slice() : [];
            if (bytes.length === 3 && bytes[0] === 2) bytes = bytes.slice(1);
            if (bytes.length >= 2) {
                let sf = bytes[0]; if (sf > 127) sf -= 256;
                const mi = bytes[1];
                const majors = ['Cb', 'Gb', 'Db', 'Ab', 'Eb', 'Bb', 'F', 'C', 'G', 'D', 'A', 'E', 'B', 'F#', 'C#'];
                const minors = ['Abm', 'Ebm', 'Bbm', 'Fm', 'Cm', 'Gm', 'Dm', 'Am', 'Em', 'Bm', 'F#m', 'C#m', 'G#m', 'D#m', 'A#m'];
                const idx = sf + 7;
                const name = (idx >= 0 && idx < majors.length) ? (mi === 0 ? majors[idx] : minors[idx]) : `sf=${sf}`;
                decoded = { sf, mi, name, raw: bytes };
            }
        }
        // Fallback: param1 / param2 fields (MIDIFile library representation)
        if (!decoded && ev.param1 !== undefined && ev.param2 !== undefined) {
            let sf = ev.param1; if (sf > 127) sf -= 256;
            const mi = ev.param2;
            const majors = ['Cb', 'Gb', 'Db', 'Ab', 'Eb', 'Bb', 'F', 'C', 'G', 'D', 'A', 'E', 'B', 'F#', 'C#'];
            const minors = ['Abm', 'Ebm', 'Bbm', 'Fm', 'Cm', 'Gm', 'Dm', 'Am', 'Em', 'Bm', 'F#m', 'C#m', 'G#m', 'D#m', 'A#m'];
            const idx = sf + 7;
            const name = (idx >= 0 && idx < majors.length) ? (mi === 0 ? majors[idx] : minors[idx]) : `sf=${sf}`;
            decoded = { sf, mi, name, raw: [ev.param1, ev.param2] };
        }

        if (decoded) {
            result.push({
                param1: decoded.sf,
                param2: decoded.mi,
                sf: decoded.sf,
                mode: decoded.mi,
                name: decoded.name,
                absTime: ev.absTime ?? 0,
                raw: decoded.raw
            });
        }
    });
    return result;
}
if (typeof window !== 'undefined') window.getKeySignature = getKeySignature;

//---------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ВИЗНАЧЕННЯ АБСОЛЮТНОГО ЧАСУ ВСІХ ПОДІЙ MIDI
// Використання: const allEvents = SetEventsAbsoluteTime(midiData);
// Повертає масив всіх подій з абсолютним часом absTime
//---------------------------------------------------------------------
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
        else if (eventType === 0xC) eventType = 'ProgramChange';
        else if (event.type === 0xA) eventType = 'Aftertouch';
        else if (event.type === 0xE) eventType = 'PitchBend';
        else eventType = 'Other';
        console.log(`LE: Track ${event.track}, Type: ${eventType}${noteInfo}, AbsTime: ${event.absTime}`);
    });
}
//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ПОШУКУ ПОДІЇ END OF TRACK
// Використання: const hasEndEvent = findEndEvent(midiEvents, element);
// Повертає true, якщо подія знайдена, і false, якщо ні
// Виводить інформацію в HTML-елемент
//--------------------------------------------------------------------
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
//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ЗАВАНТАЖЕННЯ ФАЙЛУ З ШЛЯХУ
// Використання: const file = await getFileFromPath(filepath);
// Повертає об'єкт File або null у разі помилки
//--------------------------------------------------------------------

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

//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ РОЗРАХУНКУ КІЛЬКОСТІ ТАКТІВ У MIDI
// Використання: getMeasureCount(midiEvents, ticksPerBeat);
// Повертає кількість тактів у наборі подій MIDI
//--------------------------------------------------------------------
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
        const ticksPerMeasure = ticksPerBeat * numerator * (4 / denominator); // Обчислення тиках на такт

        const intervalTicks = endTime - startTime;
        const measuresInInterval = Math.ceil(intervalTicks / ticksPerMeasure);

        measureCount += measuresInInterval;
    }

    return measureCount;
}
//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ РОЗРАХУНКУ АБСОЛЮТНОГО ЧАСУ ПОЧАТКУ КОЖНОГО ТАКТУ
// Використання: calculateBarStartAbsTimes(midiEvents, ticksPerBeat);
// Повертає масив абсолютних значень часу початку тактів
//--------------------------------------------------------------------

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
//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ПОДІЙ РОЗМІРУ ТАКТУ
// Використання: getTimeSignatureEvents(midiEvents);
// Повертає масив подій розміру такту
//--------------------------------------------------------------------
function getTimeSignatureEvents(midiEvents) {
    console.log("FOO: midiparser_ext.js - getTimeSignatureEvents");
    return midiEvents.filter(event =>
        event.type === 0xFF && event.metaType === 0x58
    );
}
//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПОДІЇ 4/4 НА ПОЧАТОК, ЯКЩО ЇЇ НЕМАЄ
// Використання: ensureTimeSignature(midiEvents);
//--------------------------------------------------------------------
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

// ФУНКЦІЯ ПЕРЕТВОРЕННЯ НОТИ З ФОРМАТУ VEXFLOW В MIDI
// Приклад: "c/4" -> 60, "d#/5" -> 75, "gb/3" -> 54
// Використання: midiNoteFromVexKey("c/4") поверне 60
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
//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ПЕРЕКОНАННЯ, ЩО ПОДІЯ END OF TRACK Є
// Якщо події немає, додає її в кінець
// Використання: ensureEndEvent(midiEvents, element);
//--------------------------------------------------------------------
function ensureEndEvent(midiEvents, element) {
    console.log("FOO: midiparser_ext.js - ensureEndEvent");
    // Robust check without relying on MIDIEvents global
    const hasEndTrack = midiEvents.some(e => e && (e.type === 0xFF || e.meta === true || e.type === 'meta') && (e.metaType === 0x2F || e.subtype === 0x2F));
    if (!hasEndTrack) {
        let lastEvent = midiEvents[midiEvents.length - 1] || {};
        let endTime = lastEvent.absTime || lastEvent.playTime || 0;
        midiEvents.push({
            type: 0xFF,
            metaType: 0x2F,
            deltaTime: 0,
            absTime: endTime,
            playTime: endTime,
            track: lastEvent.track || 0
        });
        if (element)
            element.innerHTML += `End of Track event added at ${endTime}.<br>`;
    }
}

// MIDI meta event utilities
//Функція повертає об'єкт з утилітами для роботи з MIDI мета-подіями
// (normalize, decode key signature, decode tempo)
// Використання:
//   const meta = MidiMeta.normalizeMetaEvent(ev);
//   const keySig = MidiMeta.decodeKeySignature(ev);
//   const tempo = MidiMeta.decodeTempo(ev);
// --------------------------------------------------------------------

function normalizeMetaEvent(ev) {
    if (!ev || ev.type !== 0xFF) return ev;
    let data = ev.data;
    if (Array.isArray(data)) { /* ok */ }
    else if (data == null) data = [];
    else if (ArrayBuffer.isView(data)) data = Array.from(data);
    else if (typeof data === 'number') {
        if (ev.metaType === 0x51) {
            data = [(data >> 16) & 0xFF, (data >> 8) & 0xFF, data & 0xFF];
        } else if (ev.metaType === 0x59) {
            data = [(data >> 8) & 0xFF, data & 0xFF];
        } else data = [data & 0xFF];
    } else if (typeof data === 'object' && typeof data.length === 'number') {
        try { data = Array.from(data); } catch { data = []; }
    } else data = [];
    return { ...ev, data };
}



// Helper to normalize all meta events in an array (wraps normalizeMetaEvent if available)
function normalizeMetaEvents(events) {
    if (!Array.isArray(events)) return events;
    if (typeof normalizeMetaEvent === 'function') {
        return events.map(ev => (ev && ev.type === 0xFF) ? normalizeMetaEvent(ev) : ev);
    }
    return events;
}

// --------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ДЕКОДУВАННЯ KEY SIGNATURE META (0x59)
// Returns { sf, mi, name, raw } or null if cannot decode
// sf: -7..+7 (flats/sharps), mi: 0=major, 1=minor
// name: e.g. "C", "Gm", etc.
// raw: original data bytes array
// Reference: https://www.recordingblogs.com/wiki/midi-meta-event-ff-59-key-signature
// --------------------------------------------------------------------
function decodeKeySignature(ev) {
    console.log("FOO: Decoding Key Signature from event:", ev);
    if (!ev || (ev.metaType !== 0x59 && ev.subtype !== 0x59)) return null;

    let bytes = ev.data;
    if (!Array.isArray(bytes)) bytes = [];

    // Some libs keep a leading length byte (0x02)
    if (bytes.length === 3 && bytes[0] === 2) bytes = bytes.slice(1);
    if (bytes.length < 2) return null;

    let sf = bytes[0]; if (sf > 127) sf -= 256;

    // Clamp mi to 0 or 1; treat any non-zero (incl. 255) as 1
    const miRaw = bytes[1];
    const mi = (miRaw === 0 || miRaw === 1) ? miRaw : (miRaw ? 1 : 0);

    const majors = ['Cb','Gb','Db','Ab','Eb','Bb','F','C','G','D','A','E','B','F#','C#'];
    const minors = ['Abm','Ebm','Bbm','Fm','Cm','Gm','Dm','Am','Em','Bm','F#m','C#m','G#m','D#m','A#m'];
    const idx = sf + 7;
    const name = (idx >= 0 && idx < majors.length) ? (mi === 0 ? majors[idx] : minors[idx]) : `sf=${sf}`;
    return { sf, mi, name, raw: ev.data };
}

// Returns { microsecondsPerQuarter, bpm } or null if cannot decode
// Reference: https://www.recordingblogs.com/wiki/midi-meta-event-ff-51-set-tempo
function decodeTempo(ev) {
    if (!ev || ev.metaType !== 0x51 || ev.data.length !== 3) return null;
    const us = (ev.data[0] << 16) | (ev.data[1] << 8) | (ev.data[2]);
    return { microsecondsPerQuarter: us, bpm: +(60000000 / us).toFixed(2) };
}

// --------------------------------------------------------------------
// Асинхронно отримує всі події з MIDI файлу
// Параметр:
// - file - об'єкт File (з input type="file" або створений вручну)
// Повертає:
// {midiObj, allEvents} або null у разі помилки
// - midiObj - об'єкт, повернутий MidiParser.parse
// - allEvents - масив подій з абсолютним часом (absTime)
// Використання: const { midiObj, allEvents } = await getAllEvents(file);
// --------------------------------------------------------------------
async function getAllEvents(file) {
    console.log("FOO: midiparser_ext.js - getAllEvents");
    try {
        const arrayBuffer = await file.arrayBuffer();
        const midiObj = MidiParser.parse(new Uint8Array(arrayBuffer));
        let allEvents = SetEventsAbsoluteTime(midiObj) || [];
        // Безпечна нормалізація (спершу перевірка глобального MidiMeta, потім локальної функції)
        if (typeof MidiMeta !== 'undefined' && MidiMeta && typeof MidiMeta.normalizeMetaEvent === 'function') {
            allEvents = allEvents.map(MidiMeta.normalizeMetaEvent);
        } else if (typeof normalizeMetaEvent === 'function') {
            allEvents = allEvents.map(normalizeMetaEvent);
        }
        return { midiObj, allEvents };
    } catch (err) {
        console.error('parsing MIDIFile error:', err);
        return null;
    }
}
if (typeof window !== 'undefined') window.getAllEvents = getAllEvents;


// Рахує кількість NoteOn (velocity > 0) у такті
function getNumberOfNotes(measure) {
    console.log("FOO: midiparser_ext.js - getNumberOfNotes");
    if (!Array.isArray(measure)) return 0;
    let count = 0;
    for (const ev of measure) {
        if (ev && ev.type === 0x9 && Array.isArray(ev.data) && ev.data.length > 1 && ev.data[1] > 0) {
            count++;
        }
    }
    return count;
}
if (typeof window !== 'undefined') window.getNumberOfNotes = getNumberOfNotes;


// Розраховує середню ширину такту на основі кількості нот у кожному такті
function GetMeanBarWidth(BARWIDTH, measures) {

    // If measures is not a non-empty array — just return default BARWIDTH without noisy warnings
    if (!Array.isArray(measures) || measures.length === 0) {
        console.debug("meanBarWidth: measures empty or invalid, using BARWIDTH fallback");
        return BARWIDTH;
    }

    console.log("FOO: midiparser_ext.js - meanBarWidth");
    let meanBarWidth = BARWIDTH;
    let sumBarWidth = 0;
    let currentWidth;

    measures.forEach((m) => {
        let notesamount = getNumberOfNotes(m);
        if (notesamount !== undefined) {
            currentWidth = meanBarWidth / 3 + meanBarWidth * notesamount / 7;
            sumBarWidth += currentWidth;
        }
    });

    meanBarWidth = sumBarWidth / measures.length;
    console.log(`meanBarWidth total: ${meanBarWidth}`);

    return meanBarWidth;
}
if (typeof window !== 'undefined') window.GetMeanBarWidth = GetMeanBarWidth;