import { formatMidiNotesForDisplay, getVexFlowNoteName, roundDuration, ticksToDuration, ticksToRestDuration, getNoteName2 } from './formatMidiNotesForDisplay.js';

function initNotationPage() {
    const select = document.getElementById('midifiles');
    const sheetMusicDiv = document.getElementById('SheetMusic');
    const notationDiv = document.getElementById('notation');
    const codeDiv = document.getElementById('code');
    const timesignDiv = document.getElementById('timesignature');
    const keysignDiv = document.getElementById('keysignatures');
    const prevBtn = document.getElementById('prevMidi');
    const nextBtn = document.getElementById('nextMidi');
    const editBtn = document.getElementById('editMidi');
    console.log('notationFetch.js init');

    if (!select || !sheetMusicDiv) {
        console.warn('Notation page elements not found');
        return;
    }

    async function loadSelectedMidiFile() {
        const selectedFile = select.value;
        console.log(`Load file: ${selectedFile}`);

        // Очищення попереднього рендера
        sheetMusicDiv.innerHTML = '<p style="text-align: center">Ноти</p>';
        document.getElementById('comments')?.replaceChildren();
        if (codeDiv) codeDiv.innerHTML = '';
        if (timesignDiv) timesignDiv.innerText = '';
        if (keysignDiv) keysignDiv.innerText = '';

        if (!selectedFile) return;

        try {
            const response = await fetch(`/melodies/${selectedFile}`);
            if (!response.ok) throw new Error('Не вдалося завантажити MIDI-файл');

            const arrayBuffer = await response.arrayBuffer();
            const midiData = new Uint8Array(arrayBuffer);
            const midiFile = new MIDIFile(midiData);
            const timeSignatures = getTimeSignatures(midiFile);
            const keySignatures = getKeySignatures(midiFile, midiData);

            // Рендер партитури
            drawNotation(selectedFile);

            const formattedData = formatMidiNotesForDisplay(midiFile);
            if (codeDiv) codeDiv.textContent = formattedData;

            const timeSignaturesText = timeSignatures.map(signature => `${signature.numerator} / ${signature.denominator}`).join(', ');
            if (timesignDiv) timesignDiv.innerText = timeSignaturesText;
            const keySignaturesText = keySignatures.map(ks => ks.human).join(', ');
            if (keysignDiv) keysignDiv.innerText = keySignaturesText;
        } catch (error) {
            console.error('Помилка завантаження або розбору:', error);
            if (codeDiv) codeDiv.textContent = `Помилка: ${error.message}`;
        }
    }

    function selectByIndex(newIndex) {
        const count = select.options.length;
        if (count === 0) return;
        const clamped = Math.max(0, Math.min(count - 1, newIndex));
        if (select.selectedIndex !== clamped) {
            select.selectedIndex = clamped;
        }
        // Викликаємо рендер напряму для надійності
        loadSelectedMidiFile();
    }

    prevBtn?.addEventListener('click', () => selectByIndex(select.selectedIndex - 1));
    nextBtn?.addEventListener('click', () => selectByIndex(select.selectedIndex + 1));

    // Edit: open Melodies/Edit/{id} in new tab (if mapping exists)
    editBtn?.addEventListener('click', () => {
        const opt = select.options[select.selectedIndex];
        const idAttr = opt?.getAttribute('data-id');
        const id = idAttr ? parseInt(idAttr, 10) : NaN;
        if (Number.isFinite(id) && id > 0) {
            window.open(`/Melodies/Edit?id=${id}`, '_blank');
        } else {
            alert('Для цього файлу не знайдено запис Melody у базі (немає ID).');
        }
    });

    // Підвантаження при ручній зміні вибору
    select.addEventListener('change', loadSelectedMidiFile);

    // Початкове завантаження
    loadSelectedMidiFile();
}

// Ініціалізація як для стану loading, так і для вже готового DOM
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initNotationPage);
} else {
    initNotationPage();
}

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
const noteNames = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];

function getNoteName(noteNumber) {
    const octave = getOctave(noteNumber);
    const note = noteNames[noteNumber % 12];
    return `${note}${octave}`;
}

function isNoteOnWithVelocity(event) {
    return event.type === 8 && event.subtype === 9 && event.param2 > 0;
}

function isNoteOff(event) {
    return (event.type === 8 && event.subtype === 8) || (event.type === 8 && event.subtype === 9 && event.param2 === 0);
}

function processMidiEvent(event, currentTick, activeNotes, notes) {
    if (isNoteOnWithVelocity(event)) { handleNoteOn(event, currentTick, activeNotes, notes); }
    else if (isNoteOff(event)) { handleNoteOff(event, currentTick, activeNotes, notes); }
}

function handleNoteOn(event, currentTick, activeNotes, notes) {
    activeNotes[event.param1] = { startTick: currentTick };
}

function handleNoteOff(event, currentTick, activeNotes, notes) {
    if (activeNotes[event.param1]) {
        const note = activeNotes[event.param1];
        const durationTicks = currentTick - note.startTick;
        const noteName = getNoteName(event.param1);
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

    notes.sort((a, b) => a.startTick - b.startTick);
    return notes;
}

function getDurationSymbol(durationTicks, ticksPerBeat) {
    const ratio = durationTicks / ticksPerBeat;
    if (ratio <= 0.25) return '16';
    else if (ratio <= 0.5) return '8';
    else if (ratio <= 1.5) return 'q';
    else if (ratio <= 2.5) return 'h';
    else return 'w';
}

