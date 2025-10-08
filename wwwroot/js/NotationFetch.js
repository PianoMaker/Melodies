import { formatMidiNotesForDisplay, getVexFlowNoteName, roundDuration, ticksToDuration, ticksToRestDuration, getNoteName2 } from './formatMidiNotesForDisplay.js';


document.addEventListener('DOMContentLoaded', function () {
    const select = document.getElementById('midifiles');
    const sheetMusicDiv = document.getElementById('SheetMusic');
    const notationDiv = document.getElementById('notation');
    const codeDiv = document.getElementById('code');
    const timesignDiv = document.getElementById('timesignature');
    const keysignDiv = document.getElementById('keysignatures');
    console.log("notationFetch.js starts ##");

    async function loadSelectedMidiFile() {
        const selectedFile = select?.value;
        console.log(`selectedFile = ${selectedFile}`);

        // Очищаємо попередній нотний запис
        sheetMusicDiv.innerHTML = '<p style="text-align: center">Ноти</p>';
        document.getElementById('comments')?.replaceChildren();
        codeDiv.innerHTML = '';
        timesignDiv.innerText = '';
        if (keysignDiv) keysignDiv.innerText = '';

        if (!selectedFile) {
            console.log('No file selected.');
            return;
        }

        try {
            const response = await fetch(`/melodies/${selectedFile}`);
            if (!response.ok) {
                throw new Error('Не вдалося завантажити MIDI-файл');
            }
            console.log('Response status:', response.status);

            const arrayBuffer = await response.arrayBuffer();
            const midiData = new Uint8Array(arrayBuffer);
            const midiFile = new MIDIFile(midiData);
            const timeSignatures = getTimeSignatures(midiFile);
            // pass raw bytes for robust fallback
            const keySignatures = getKeySignatures(midiFile, midiData);

            console.log('MIDI data length:', midiData.length);
            console.log('MIDI file tracks:', midiFile.tracks);

            // Render score via midiRenderer using URL into SheetMusic container
            drawNotation(selectedFile);

            const formattedData = formatMidiNotesForDisplay(midiFile);
            codeDiv.textContent = formattedData;

            const timeSignaturesText = timeSignatures.map(signature => `${signature.numerator} / ${signature.denominator}`).join(", ");
            timesignDiv.innerText = timeSignaturesText;
            const keySignaturesText = keySignatures.map(ks => ks.human).join(', ');
            if (keysignDiv) keysignDiv.innerText = keySignaturesText;
        } catch (error) {
            console.error('Помилка завантаження або розбору:', error);
            codeDiv.textContent = `Помилка: ${error.message}`;
        }
    }

    // Викликаємо завантаження одразу при завантаженні сторінки
    loadSelectedMidiFile();

    // Викликаємо завантаження при зміні вибору
    select?.addEventListener('change', loadSelectedMidiFile);
});
('DOMContentLoaded', function () {
    const select = document.getElementById('midifiles');
    const sheetMusicDiv = document.getElementById('SheetMusic');
    const notationDiv = document.getElementById('notation');
    const codeDiv = document.getElementById('code');
    const timesignDiv = document.getElementById('timesignature');
    const keysignDiv = document.getElementById('keysignatures');
    console.log("notationFetch.js starts ##");

    select?.addEventListener('change', async function () {
        const selectedFile = select.value;
        console.log(`selectedFile = ${selectedFile}`);

        // Очищаємо попередній нотний запис
        sheetMusicDiv.innerHTML = '<p style="text-align: center">Ноти</p>';
        codeDiv.innerHTML = '';

        try {
            const response = await fetch(`/melodies/${selectedFile}`);
            if (!response.ok) {
                throw new Error('Не вдалося завантажити MIDI-файл');
            }
            console.log('Response status:', response.status);

            const arrayBuffer = await response.arrayBuffer();
            const midiData = new Uint8Array(arrayBuffer);
            const midiFile = new MIDIFile(midiData);
            const timeSignatures = getTimeSignatures(midiFile);
            // pass raw bytes for robust fallback
            const keySignatures = getKeySignatures(midiFile, midiData);

            console.log('MIDI data length:', midiData.length);
            console.log('MIDI file tracks:', midiFile.tracks);

            // Use the URL-based renderer so we pass element IDs (strings)
            drawNotation(selectedFile);
            const formattedData = formatMidiNotesForDisplay(midiFile, notationDiv, codeDiv);
            codeDiv.textContent = formattedData;
            const timeSignaturesText = timeSignatures.map(signature => `${signature.numerator} / ${signature.denominator}`).join(", ");
            timesignDiv.innerText = timeSignaturesText;
            const keySignaturesText = keySignatures.map(ks => ks.human).join(', ');
            if (keysignDiv) keysignDiv.innerText = keySignaturesText;
        } catch (error) {
            console.error('Помилка завантаження або розбору:', error);
            codeDiv.textContent = `Помилка: ${error.message}`;
        }
    });
});

function drawNotation(selectedFile) {
    // Ensure the comments element exists (outside of SheetMusic)
    if (!document.getElementById('comments')) {
        const host = document.body;
        const c = document.createElement('div');
        c.id = 'comments';
        host.appendChild(c);
    }

    // Measure left panel width and render into SheetMusic
    const sheet = document.getElementById('SheetMusic');
    const width = Math.max(320, Math.floor(sheet?.clientWidth || sheet?.getBoundingClientRect().width || 1200));

    // Render directly into SheetMusic element with correct argument order
    renderMidiFromUrl(`/melodies/${selectedFile}`, 1000, 'SheetMusic', 'comments', width);
}
function getTimeSignatures(midiFile) {
    const signatures = [];
    console.log('All Time Signatures:');
    midiFile.tracks.forEach((track, index) => {
        const events = midiFile.getTrackEvents(index);
        events.forEach(event => {
            if (event.subtype === 0x58) {
                const numerator = event.data[0];
                const denominator = Math.pow(2, event.data[1]);
                signatures.push({ numerator, denominator });
                console.log(`${numerator} / ${denominator}`);
            }
        });
    });
    
    return signatures;
}

function getKeySignatures(midiFile, rawBytes) {
    const keys = [];

    const pushKs = (sf, mi) => {
        if (sf == null || mi == null) return;
        // normalize signed [-7..7]
        const sfs = sf > 127 ? sf - 256 : sf;
        const clamped = Math.max(-7, Math.min(7, sfs));
        const mode = mi ? 1 : 0;
        keys.push({ sf: clamped, mi: mode, human: mapKeyToHuman(clamped, mode) });
    };

    // 1) Primary: через MIDIFile події (підтримка різних форм)
    midiFile.tracks.forEach((_, index) => {
        const events = midiFile.getTrackEvents(index);
        events.forEach(ev => {
            const isKs = (ev.subtype === 0x59) || (ev.type === 0xFF && ev.metaType === 0x59);
            if (!isKs) return;

            if (ev.data && ev.data.length >= 2) {
                pushKs(ev.data[0], ev.data[1]); // data[0]=sf, data[1]=mi
            } else if (typeof ev.key !== 'undefined' && typeof ev.scale !== 'undefined') {
                pushKs(ev.key, ev.scale); // деякі збірки MIDIFile
            }
        });
    });

    // 2) Fallback: якщо нічого не знайдено — скануємо сирі байти на FF 59 02
    if (keys.length === 0 && rawBytes && rawBytes.length > 5) {
        for (let i = 0; i < rawBytes.length - 4; i++) {
            if (rawBytes[i] === 0xFF && rawBytes[i + 1] === 0x59) {
                const len = rawBytes[i + 2];
                if (len >= 2) {
                    const sf = rawBytes[i + 3];
                    const mi = rawBytes[i + 4];
                    pushKs(sf, mi);
                    i += 4; // стрибок далі
                }
            }
        }
    }

    // 3) Прибрати поспіль однакові
    const result = [];
    let prev = null;
    for (const k of keys) {
        const sig = `${k.sf}:${k.mi}`;
        if (sig !== prev) {
            result.push(k);
            prev = sig;
        }
    }
    return result;
}

function mapKeyToHuman(sf, mi) {
    // Align with server-side MapKsToName in C#
    const majors = ["Ces","Ges","Des","As","Es","B","F","C","G","D","A","E","H","Fis","Cis"];
    const minors = ["as","es","b","f","c","g","d","a","e","h","fis","cis","gis","dis","ais"];
    const idx = sf + 7;
    if (idx < 0 || idx >= majors.length) return `sf=${sf}, mi=${mi}`;
    return mi === 0 ? `${majors[idx]}-dur` : `${minors[idx]}-moll`;
}


// Читання нотних подій
// Музичні ноти без вказівки октави
const noteNames = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];

//// Функція для визначення октави по номеру ноти
//function getOctave(noteNumber) {
//    const octave = Math.floor(noteNumber / 12) - 1; // Октавний діапазон для MIDI (0-127)
//    console.log(`Note number: ${noteNumber} -> Octave: ${octave}`);
//    return octave;
//}

// Функція для перетворення MIDI номера на ноту з урахуванням октави
function getNoteName(noteNumber) {
    const octave = getOctave(noteNumber);
    const note = noteNames[noteNumber % 12]; // Визначаємо ноту в межах октави
    return `${note}${octave}`;
}

// Функція для визначення, чи це подія Note On з velocity > 0
function isNoteOnWithVelocity(event) {
    return event.type === 8 && event.subtype === 9 && event.param2 > 0;
}

// Функція для визначення, чи це подія Note Off
function isNoteOff(event) {
    return (event.type === 8 && event.subtype === 8) || (event.type === 8 && event.subtype === 9 && event.param2 === 0);
}

// Основна функція для обробки MIDI подій
function processMidiEvent(event, currentTick, activeNotes, notes) {
    if (isNoteOnWithVelocity(event)) { // Note On з velocity > 0
        // console.log(`Note On - Note: ${event.param1} +`);
        handleNoteOn(event, currentTick, activeNotes, notes);
    } else if (isNoteOff(event)) { // Note Off або Note On з velocity 0
        //console.log(`Note Off - Note: ${event.param1}, Velocity: ${event.param2}, Delta: ${event.delta}, Current Tick: ${currentTick}`);
        handleNoteOff(event, currentTick, activeNotes, notes);
    }
}

// Обробка події Note On
function handleNoteOn(event, currentTick, activeNotes, notes) {
    activeNotes[event.param1] = { startTick: currentTick };
}

// Обробка події Note Off
function handleNoteOff(event, currentTick, activeNotes, notes) {
    if (activeNotes[event.param1]) {
        const note = activeNotes[event.param1];
        const durationTicks = currentTick - note.startTick;
        const noteName = getNoteName(event.param1); // Отримуємо ім'я ноти з врахуванням октави
        notes.push({ noteName, noteNumber: event.param1, durationTicks });
        delete activeNotes[event.param1];
    }
}


function getNotesData(midiFile) {
    const notes = [];
    const activeNotes = {};
    let currentTick = 0;

    midiFile.tracks.forEach((track, index) => {
        const events = midiFile.getTrackEvents(index);
        let trackCurrentTick = 0;

        events.forEach(event => {
            trackCurrentTick += event.delta || 0;
            currentTick = trackCurrentTick;

            if (isNoteOnWithVelocity(event)) {
                activeNotes[event.param1] = {
                    startTick: currentTick,
                    channel: event.channel,
                    velocity: event.param2
                };
            } else if (isNoteOff(event)) {
                if (activeNotes[event.param1]) {
                    const note = activeNotes[event.param1];
                    const durationTicks = currentTick - note.startTick;
                    notes.push({
                        noteNumber: event.param1,
                        durationTicks: durationTicks,
                        startTick: note.startTick,
                        channel: note.channel,
                        velocity: note.velocity
                    });
                    delete activeNotes[event.param1];
                }
            }
        });
    });

    // Сортуємо ноти по часу їх появи
    notes.sort((a, b) => a.startTick - b.startTick);
    return notes;
}



function getDurationSymbol(durationTicks, ticksPerBeat) {
    const ratio = durationTicks / ticksPerBeat;

    if (ratio <= 0.25) {
        return "16";
    } else if (ratio <= 0.5) {
        return "8";
    } else if (ratio <= 1.5) {
        return "q";
    } else if (ratio <= 2.5) {
        return "h";
    } else {
        return "w";
    }
}

