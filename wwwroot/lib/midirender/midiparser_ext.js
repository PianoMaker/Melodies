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
let currentKeyMode = 0;      // 0=major, 1=minor   <-- ADDED
let enharmonicPreference = 'auto'; // 'auto' | 'sharps' | 'flats'
function setEnharmonicPreference(pref) {
	console.debug("FOO: midiparser_ext.js - setEnharmonicPreference");
	if (['auto', 'sharps', 'flats'].includes(pref)) {
		enharmonicPreference = pref;
		console.log('Enharmonic preference =', pref);
	} else console.warn('Unknown enharmonic preference', pref);
}
if (typeof window !== 'undefined') window.setEnharmonicPreference = setEnharmonicPreference;



function splitEventsIntoMeasures(midiEvents, ticksPerBeat) {
	console.debug("FOO: midiparser_ext.js - splitEventsIntoMeasures");
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
	console.debug("FOO: midiparser_ext.js - calculateTotalDuration");
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
	console.debug("FOO: midiparser_ext.js - calculateTicksFromDuration");

	let result = getDurationFromCode(duration) * ticksPerBeat;
	console.debug(`Calculating ticks for duration: ${duration}, result: ${result}`);
	return result;
}

/**
 * Повертає код тривалості ноти
 * @param {any} ticks        - тривалість у тіках
 * @param {any} ticksPerBeat - поточний TPQN
 * @returns
 */

function getDurationFromTicks(ticks, ticksPerBeat) {
	console.debug("FOO: midiparser_ext.js - getDurationFromTicks");
	if (typeof ticks !== "number" || typeof ticksPerBeat !== "number" || ticks <= 0 || ticksPerBeat <= 0) {
		console.warn("Invalid input to getDurationFromTicks:", { ticks, ticksPerBeat });
		return [];                      // Повертаємо порожній масив у разі некоректних даних
	}
	console.debug('Calculating duration for ticks: ' + ticks + ', ticksPerBeat: ' + ticksPerBeat);
	var quarterTicks = ticksPerBeat;    // Тривалість чверті дорівнює TPQN
	let mingap = ticksPerBeat / 16;     // Мінімальний проміжок між нотами

	const durations = [];

	// Значення для тріолей
	const triQ = quarterTicks * (2 / 3);  // 'qt'
	const tri8 = quarterTicks / 3;        // '8t'
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
	console.debug('Calculated durations: ', durations);


	return durations;
}

/**
 * 
 * @param {any} notesString
 * @returns
 */
function calculateEndless(notesString) {
	console.debug("FOO: midiparser_ext.js - calculateEndless");

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
	console.debug("FOO: midiparser_ext.js - getBarDuration");
	const [numerator, denominator] = timeSignature.split('/').map(Number);
	return numerator * (4 / denominator);
}

function getDurationFromCode(durationCode) {
	console.debug("FOO: midiparser_ext.js - getDurationFromCode");
	console.debug(`Getting duration value for code: ${durationCode}`);
	try {
		const hasDot = durationCode.endsWith('.');
		const hasTriplet = durationCode.endsWith('t');
		if (hasDot && hasTriplet) {
			console.warn(`Dotted-triplet not supported: ${durationCode}, ignoring dot.`);
		}
		const raw = (hasDot || hasTriplet) ? durationCode.slice(0, -1) : durationCode;

		const baseValue = reverseDurationMapping[raw];
		console.debug(`Base value for duration code ${raw}: ${baseValue}`);
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
	console.debug("FOO: midiparser_ext.js - getDurationValueFromNote");
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


function createRest(duration, clef = 'treble') {
	console.debug("FOO: midiparser_ext.js - createRest");
	try {
		if (!duration || typeof duration !== 'string') {
			console.warn('createRest: invalid duration', duration);
			return null;
		}

		// Normalize duration: strip trailing 'r' if present to avoid 'qrr'
		const hasDot = duration.endsWith('.');
		const hasTriplet = duration.endsWith('t');
		const raw = duration.replace(/r$/i, '');
		const base = (hasDot || hasTriplet) ? raw.slice(0, -1) : raw;

		// VexFlow rest notation requires trailing 'r'
		const vexDur = `${base}r`;

		const rest = new Vex.Flow.StaveNote({
			keys: ['b/4'],     // ignored visually for rests
			duration: vexDur,
			clef: clef
		});

		if (hasDot && !hasTriplet) {
			rest.addDot(0);
		}

		return rest;
	} catch (error) {
		console.error(`Failed to create rest with duration: ${duration}`, error);
		return null;
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
	console.debug("FOO: midiparser_ext.js - createNote");
	//console.log(`Creating note with noteKey ${noteKey} duration: ${duration}`);
	const noteMatch = noteKey.match(/^([a-gA-G])(b|#)?(\d)?$/);
	if (!noteMatch) {
		console.error(`Invalid note key: ${noteKey} (normalized -> ${nk}). Expected e.g. c4, d#5, gb3, a-1`);
		return null;
	}

	const [, letter, accidental, octaveRaw] = noteMatch;
	let octaveNum = typeof octaveRaw !== 'undefined' ? parseInt(octaveRaw, 10) : 4;
	if (Number.isNaN(octaveNum)) octaveNum = 4;

	// NOTE:
	// Removed clef-based octave shifting here to avoid double/conflicting octave adjustments.
	// Clef-aware octave logic should be handled consistently in one place (midiRenderer.js).
	// (previous code added `octaveNum += 2` for 'bass' which caused notes to render on wrong lines)

	const key = `${letter.toLowerCase()}${accidental || ''}/${octaveNum}`;

	const isTriplet = typeof duration === 'string' && duration.endsWith('t');
	const isDotted = typeof duration === 'string' && duration.endsWith('.');
	const baseDuration = (isTriplet || isDotted) ? duration.slice(0, -1) : (duration || 'q');

	try {
		const staveNote = new Vex.Flow.StaveNote({
			keys: [key],
			duration: baseDuration
		});

		if (accidental) {
			staveNote.addAccidental(0, new Vex.Flow.Accidental(accidental));
		}

		if (isDotted && !isTriplet) {
			staveNote.addDot(0);
		}

		if (typeof staveNote.autoStem === 'function') {
			try { staveNote.autoStem(); } catch (e) { /* ignore */ }
		}

		if (isTriplet) {
			staveNote.__isTriplet = true;
			staveNote.__tripletBase = baseDuration;
			staveNote.__durationCode = duration;
		}

		console.debug(`createNote: created key='${key}' duration='${baseDuration}' clef='${clef}'`);
		return staveNote;
	} catch (error) {
		console.error(`Failed to create note with key: ${noteKey} (normalized ${key}) and duration: ${duration}`, error);
		return null;
	}
};// // Аналіз нот із введеного рядку (ноти через коми)
function processNote(element) {
	console.debug("FOO: midiparser_ext.js - processNote");
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
	console.debug("FOO: midiparser_ext.js - getTimeSignatureAndTicksPerMeasure");
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

/**
 * Detect whether a parsed midiData object likely represents MIDI Format 0.
 * Heuristic: explicit format fields or single track array.
 * @param {object} midiData - parsed MIDI object
 * @returns {boolean}
 */
const isMidiFormat0 = (midiData) => {
	if (!midiData) return false;
	// explicit fields some parsers use
	if (midiData.format === 0 || midiData.format === '0') return true;
	if (midiData.fileFormat === 0 || midiData.fileFormat === '0') return true;
	if (midiData.header && (midiData.header.format === 0 || midiData.header.format === '0')) return true;
	// fallback heuristic: single track => likely format 0
	if (Array.isArray(midiData.track) && midiData.track.length === 1) return true;
	return false;
};

//---------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ПЕРЕТВОРЕННЯ MIDIFile В MIDIData
// Використання: const midiData = buildMidiDataFromMIDIFile(input);
// input - Uint8Array або існуючий MIDIFile
//---------------------------------------------------------------------
function buildMidiDataFromMIDIFile(input) {
    // input can be a Uint8Array or an existing MIDIFile instance
    let midiFile = null;
    try {
        if (typeof MIDIFile === 'undefined') return null;
        midiFile = (input && typeof input.getTrackEvents === 'function') ? input : new MIDIFile(input);
    } catch (e) {
        console.warn('buildMidiDataFromMIDIFile: cannot construct MIDIFile', e);
        return null;
    }

    const tracks = [];
    // midiFile.tracks may be array-like; fall back to length if missing
    const trackCount = Array.isArray(midiFile.tracks) ? midiFile.tracks.length : (midiFile.header && midiFile.header.getTracks ? midiFile.header.getTracks() : 0);

    for (let ti = 0; ti < trackCount; ti++) {
        const rawEvents = (typeof midiFile.getTrackEvents === 'function') ? (midiFile.getTrackEvents(ti) || []) : [];
        const events = rawEvents.map(ev => {
            // Normalise shape to what SetEventsAbsoluteTime expects from MidiParser:
            const e = {};
            e.deltaTime = ev.delta ?? ev.deltaTime ?? 0;

            // Meta events
            if (ev.type === 0xFF || ev.subtype !== undefined || ev.metaType !== undefined || ev.meta === true) {
                e.type = 0xFF;
                e.metaType = ev.subtype ?? ev.metaType;
                // prefer array-like data; some libraries use ev.data (Array) or ev.param1/param2
                if (Array.isArray(ev.data)) e.data = ev.data.slice();
                else if (typeof ev.data === 'number') e.data = [ev.data];
                else if (ev.param1 !== undefined || ev.param2 !== undefined) e.data = [ev.param1 ?? 0, ev.param2 ?? 0];
                else e.data = ev.data ?? [];
            } else {
                // Channel / regular events: many parsers supply .type (4-bit), .channel, and .param1/.param2
                // Keep type as-is (0x9, 0x8 etc) when present
                e.type = (typeof ev.type === 'number') ? ev.type : (typeof ev.subtype === 'number' ? ev.subtype : ev.type);
                if (ev.channel !== undefined) e.channel = ev.channel;
                if (Array.isArray(ev.data)) e.data = ev.data.slice();
                else if (ev.param1 !== undefined || ev.param2 !== undefined) e.data = [ev.param1 ?? 0, ev.param2 ?? 0];
                else if (ev.param1 === undefined && typeof ev.data === 'number') e.data = [ev.data];
                else e.data = ev.data ?? [];
            }

            // preserve originals for debugging
            if (ev.rawBytes) e._raw = ev.rawBytes;
            return e;
        });

        tracks.push({ event: events });
    }

    // try to provide timeDivision similar to MidiParser output
    let timeDivision = 480;
    try {
        if (midiFile.header && typeof midiFile.header.getTicksPerBeat === 'function') timeDivision = midiFile.header.getTicksPerBeat();
        else if (midiFile.header && midiFile.header.ticksPerBeat) timeDivision = midiFile.header.ticksPerBeat;
    } catch { /* ignore */ }

    return { track: tracks, timeDivision, format: 0 };
}

//---------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ВИЗНАЧЕННЯ АБСОЛЮТНОГО ЧАСУ ВСІХ ПОДІЙ MIDI
// Використання: const allEvents = SetEventsAbsoluteTime(midiData);
// Повертає масив всіх подій з абсолютним часом absTime
//---------------------------------------------------------------------
function SetEventsAbsoluteTime(midiData) {
    console.debug("FOO: midiparser_ext.js - SetEventsAbsoluteTime (format-aware)");
    const allEvents = [];

    // If caller passed a raw Uint8Array or a MIDIFile instance, convert it first
    try {
        if ((midiData instanceof Uint8Array) || (midiData && typeof midiData.getTrackEvents === 'function') || (midiData && midiData.header && typeof midiData.header.getTicksPerBeat === 'function')) {
            const converted = buildMidiDataFromMIDIFile(midiData);
            if (converted) midiData = converted;
        }
    } catch (e) {
        console.warn('SetEventsAbsoluteTime: conversion via MIDIFile failed', e);
    }

    if (!midiData || !Array.isArray(midiData.track)) return allEvents;

    // Головний біль з форматом MIDI0: деякі парсери не встановлюють коректно deltaTime для першої події після великого initial delta
    const isMidi0 = isMidiFormat0(midiData);
    console.debug(`MIDI format0 detected: ${isMidi0}`);

    midiData.track.forEach((track, trackIndex) => {
        if (!Array.isArray(track.event)) return;
        let absTime = 0;

        track.event.forEach((event, idx) => {

            // Виявлення неадекватних delta time
            const deltaAdded = (event && (event.deltaTime ?? event.delta ?? event.deltaTicks ?? event.delta_time)) ?? 0;
            try { if (event && event.deltaTime === undefined) event.deltaTime = deltaAdded; } catch {}

            // Detect NoteOn (type0x9) with velocity >0
            const { midiNote, isNoteOn, velocity } = detectNoteOn(event);

            let deltaToAdd = deltaAdded;
            if (isMidi0) {
                const LARGE_DELTA_THRESHOLD = 1000; //
                const prevAreMeta = idx === 0 ? true : track.event.slice(0, idx).every(e => e && e.type === 0xFF);
                if (deltaAdded > LARGE_DELTA_THRESHOLD && prevAreMeta) {
                    deltaToAdd = 0;
                    console.info(`Clamped large initial delta ${deltaAdded} ->0 (track ${trackIndex}, idx ${idx})`);
                }
            }

            // Accumulate absTime once (use deltaToAdd)
            absTime += deltaToAdd;


            if (isNoteOn) {
                console.debug(`ABSTIME: NoteOn: midi=${midiNote}, absTime=${absTime}, deltaAdded=${deltaAdded}, deltaUsed=${deltaToAdd}, track=${trackIndex}, eventIndex=${idx}`);
            } else {
                console.debug('RAW EVENT DIAG', { index: idx, type: event?.type, metaType: event?.metaType, deltaTime: event?.deltaTime, delta: event?.delta, data: event?.data, param1: event?.param1, param2: event?.param2 });
            }

            allEvents.push({ ...event, absTime, track: trackIndex, __trackIndexEvent: idx });
        });
    });

    // Stable sort by absTime, then track, then original order
    allEvents.sort((a, b) => {
        if (a.absTime !== b.absTime) return a.absTime - b.absTime;
        if (a.track !== b.track) return a.track - b.track;
        return (a.__trackIndexEvent ?? 0) - (b.__trackIndexEvent ?? 0);
    });

    return allEvents.map(ev => {
        const { __trackIndexEvent, ...rest } = ev;
        return rest;
    });
}

//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ЛОГУВАННЯ ВСІХ ПОДІЙ MIDI
// Використання: logMeasureEvents(allEvents);
//--------------------------------------------------------------------
function logMeasureEvents(allEvents) {
	console.debug("FOO: midiparser_ext.js - logEvents");
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
		console.debug(`LE: Track ${event.track}, Type: ${eventType}${noteInfo}, AbsTime: ${event.absTime}`);
	});
}
//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ПОШУКУ ПОДІЇ END OF TRACK
// Використання: const hasEndEvent = findEndEvent(midiEvents, element);
// Повертає true, якщо подія знайдена, і false, якщо ні
// Виводить інформацію в HTML-елемент
//--------------------------------------------------------------------
function findEndEvent(midiEvents, element) {
	console.debug("FOO: midiparser_ext.js - findEndEvent");
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
	console.debug("FOO: midiparser_ext.js - getFileFromPath");
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
	console.debug("FOO: midiparser_ext.js - getMeasureCount");
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
	console.debug("FOO: midiparser_ext.js - calculateBarStartAbsTimes");
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
	console.debug("FOO: midiparser_ext.js - getTimeSignatureEvents");
	return midiEvents.filter(event =>
		event.type === 0xFF && event.metaType === 0x58
	);
}
//--------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПОДІЇ 4/4 НА ПОЧАТКУ, ЯКЩО ЇЇ НЕМАЄ
// Використання: ensureTimeSignature(midiEvents);
//--------------------------------------------------------------------
function ensureTimeSignature(midiEvents) {
	console.debug("FOO: midiparser_ext.js - ensureTimeSignature");
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
	console.debug("FOO: midiparser_ext.js - getMidiEventFromArray");

	const midiFile = new MIDIFile(arrayBuffer);
	return midiFile.getMidiEvents();

}

// ФУНКЦІЯ ПЕРЕТВОРЕННЯ НОТИ З ФОРМАТУ VEXFLOW В MIDI
// Приклад: "c/4" -> 60, "d#/5" -> 75, "gb/3" -> 54
// Використання: midiNoteFromVexKey("c/4") поверне 60
function midiNoteFromVexKey(key) {
	console.debug("FOO: midiparser_ext.js - midiNoteFromVexKey");
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
	console.debug("FOO: midiparser_ext.js - ensureEndEvent");
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
// raw: оригінальні байти події
// Reference: https://www.recordingblogs.com/wiki/midi-meta-event-ff-59-key-signature
// --------------------------------------------------------------------
function decodeKeySignature(ev) {
	console.debug("FOO: Decoding Key Signature from event:", ev);
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

	const majors = ['Cb', 'Gb', 'Db', 'Ab', 'Eb', 'Bb', 'F', 'C', 'G', 'D', 'A', 'E', 'B', 'F#', 'C#'];
	const minors = ['Abm', 'Ebm', 'Bbm', 'Fm', 'Cm', 'Gm', 'Dm', 'Am', 'Em', 'Bm', 'F#m', 'C#m', 'G#m', 'D#m', 'A#m'];
	const idx = sf + 7;
	const name = (idx >= 0 && idx < majors.length) ? (mi === 0 ? majors[idx] : minors[idx]) : `sf=${sf}`;
	console.debug(`[KS]Decoded Key Signature: sf=${sf}, mi=${mi}, name=${name}`);
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
// ФУНКЦІЯ ДЛЯ ПАРСИНГУ MIDI З ПЕРЕВАГОЮ MIDIFile
// Використання: const { midiObj, parser } = parseMidiPreferMIDIFile(uint8);
// Повертає об'єкт з полями:
// - midiObj - об'єкт, повернутий MIDIFile або MidiParser.parse
// - parser - рядок 'MIDIFile' або 'MidiParser', що вказує, який парсер було використано
// --------------------------------------------------------------------
function parseMidiPreferMIDIFile(uint8) {
	console.debug("FOO: midiparser_ext.js - parseMidiPreferMIDIFile");
	let midiObj = null;

	// Prefer the MIDIFile parser (used by Admin/Notation)
	if (typeof MIDIFile !== 'undefined') {
		try {
			const mf = new MIDIFile(uint8);
			if (typeof buildMidiDataFromMIDIFile === 'function') {
				try {
					const converted = buildMidiDataFromMIDIFile(mf);
					if (converted) {
						return { midiObj: converted, parser: 'MIDIFile' };
					}
					// fall through to return raw mf if conversion returned null
				} catch (convErr) {
					console.warn('parseMidiPreferMIDIFile: buildMidiDataFromMIDIFile failed', convErr);
				}
			}
			// keep raw MIDIFile instance (SetEventsAbsoluteTime will convert if needed)
			return { midiObj: mf, parser: 'MIDIFile' };
		} catch (e) {
			console.warn('parseMidiPreferMIDIFile: MIDIFile construct failed, will try MidiParser', e);
		}
	}

	// Fallback to legacy MidiParser
	if (typeof MidiParser !== 'undefined' && typeof MidiParser.parse === 'function') {
		try {
			midiObj = MidiParser.parse(uint8);
			return { midiObj, parser: 'MidiParser' };
		} catch (e) {
			console.warn('parseMidiPreferMIDIFile: MidiParser.parse failed', e);
		}
	}

	throw new Error('No MIDI parser available (MIDIFile or MidiParser).');
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
	console.debug("FOO: midiparser_ext.js - getAllEvents");
	try {
		const arrayBuffer = await file.arrayBuffer();
		const uint8 = new Uint8Array(arrayBuffer);

		// Use the unified parser helper
		let parsed = parseMidiPreferMIDIFile(uint8);
		const midiObj = parsed.midiObj;
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
	console.debug("FOO: midiparser_ext.js - getNumberOfNotes");
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



// Detection of NoteOn events extracted to reusable function
/**
 * Detect whether an event is a NoteOn and extract midi note + velocity.
 * Returns { midiNote, isNoteOn, velocity }
 */
function detectNoteOn(event) {
	let isNoteOn = false, midiNote = null, velocity = null;
	if (!event) return { midiNote, isNoteOn, velocity };

	// Robust detection: accept 0x9 / 9, full 0x90 status byte (144) and channel range 0x90..0x9F.
	const t = event.type;
	const isStatusNoteOn = (
		t === 0x9 || t === 9 ||
		t === 0x90 || t === 144 ||
		(typeof t === 'number' && t >= 0x90 && t <= 0x9F)
	);

	// Extract note/velocity from common shapes (array or fields)
	if (Array.isArray(event.data) && event.data.length >= 2) {
		midiNote = event.data[0];
		velocity = event.data[1];
	} else {
		midiNote = event.param1 ?? event.note ?? null;
		velocity = event.param2 ?? event.velocity ?? null;
	}

	// If the status indicates NoteOn family, treat velocity>0 as NoteOn
	if (isStatusNoteOn) {
		isNoteOn = (typeof velocity === 'number' && velocity > 0);
	} else {
		// Conservative inference for parsers that use different type values:
		// if it's a channel message (0x80..0xEF) and velocity>0 + note present, infer NoteOn.
		if (typeof velocity === 'number' && velocity > 0 && (typeof t === 'number' && t >= 0x80 && t <= 0xEF)) {
			isNoteOn = true;
		} else {
			isNoteOn = false;
		}
	}

	console.debug(`LE: detectNoteOn: midiNote=${midiNote}, isNoteOn=${isNoteOn}, velocity=${velocity}, type=${t}`);
	return { midiNote, isNoteOn, velocity };
}