import { formatMidiNotesForDisplay, getVexFlowNoteName, roundDuration, ticksToDuration, ticksToRestDuration, getNoteName2 } from './formatMidiNotesForDisplay.js';


document.addEventListener('DOMContentLoaded', function () {
    const select = document.getElementById('midifiles');
    const notationDiv = document.getElementById('notation');
    const codeDiv = document.getElementById('code');
    const timesignDiv = document.getElementById('timesignature');
    console.log("notationFetch.js starts ##");

    select.addEventListener('change', async function () {
        const selectedFile = select.value;
        console.log(`selectedFile = ${selectedFile}`);

        // Очищаємо попередній нотний запис
        notationDiv.innerHTML = '';
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

            console.log('MIDI data length:', midiData.length);
            console.log('MIDI file tracks:', midiFile.tracks);

            drawNotation(midiFile);
            const formattedData = formatMidiNotesForDisplay(midiFile);
            codeDiv.textContent = formattedData;
            const timeSignaturesText = timeSignatures.map(signature => `${signature.numerator} / ${signature.denominator}`).join(", ");
            timesignDiv.innerText = timeSignaturesText;
        } catch (error) {
            console.error('Помилка завантаження або розбору:', error);
            codeDiv.textContent = `Помилка: ${error.message}`;
        }
    });
});

function drawNotation(midiFile) {
    console.log(`starting drawNotation`);
    // Додаємо Rest до списку імпортованих класів
    const { Renderer, Stave, StaveNote, Rest, Formatter } = Vex.Flow;
    const div = document.getElementById('notation');
    const renderer = new Renderer(div, Renderer.Backends.SVG);

    renderer.resize(800, 1000);
    const context = renderer.getContext();

    const ticksPerBeat = midiFile.header.getTicksPerBeat();
    const timeSignatures = getTimeSignatures(midiFile);
    const notesData = getNotesData(midiFile);

    // 1. Визначаємо паузи між нотами
    let lastEndTick = 0;
    const rests = [];

    notesData.forEach(note => {
        if (note.startTick > lastEndTick) {
            rests.push({
                startTick: lastEndTick,
                durationTicks: note.startTick - lastEndTick,
                isRest: true
            });
        }
        lastEndTick = Math.max(lastEndTick, note.startTick + note.durationTicks);
    });

    // 2. Об'єднуємо ноти та паузи
    const allElements = [...notesData, ...rests]
        .sort((a, b) => a.startTick - b.startTick);

    // 3. Створюємо VexFlow елементи
    const vexflowElements = allElements.map(element => {
        if (element.isRest) {            
            return new StaveNote({
                keys: ["b/4"], // Нота поза межами видимості
                duration: "16r",
            });       
        } else {
            const noteName = getVexFlowNoteName(element.noteNumber);
            const duration = getDurationSymbol(element.durationTicks, ticksPerBeat);

            const staveNote = new StaveNote({
                keys: [noteName],
                duration: duration,
                auto_stem: true
            });

            if (noteName.includes('#')) {
                staveNote.addAccidental(0, new Vex.Flow.Accidental("#"));
            } else if (noteName.includes('b') && !noteName.startsWith('b')) {
                staveNote.addAccidental(0, new Vex.Flow.Accidental("b"));
            }

            return staveNote;
        }
    });


    // 4. Відображення на нотоносці
    const staveWidth = 700;
    const staveHeight = 100;
    let x = 10;
    let y = 40;
    let stave = new Stave(x, y, staveWidth);

    if (timeSignatures.length > 0) {
        const ts = timeSignatures[0];
        stave.addTimeSignature(`${ts.numerator}/${ts.denominator}`);
    } else {
        stave.addTimeSignature('4/4');
    }

    stave.addClef('treble');
    stave.setContext(context).draw();

    const formatter = new Formatter();
    let elementsPerStave = [];
    let widthUsed = 0;
    const elementSpacing = 40;

    vexflowElements.forEach((element, idx) => {
        elementsPerStave.push(element);
        widthUsed += elementSpacing;

        if (widthUsed >= staveWidth - 50 || idx === vexflowElements.length - 1) {
            Formatter.FormatAndDraw(context, stave, elementsPerStave);
            x = 10;
            y += staveHeight;
            stave = new Stave(x, y, staveWidth);
            stave.setContext(context).draw();
            widthUsed = 0;
            elementsPerStave = [];
        }
    });
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

