document.addEventListener("DOMContentLoaded", function () {
    const notationDiv = document.getElementById("notation");
    console.log("notationFromMidi.js  +++ starts working");
    console.log("midiNotes:", midiNotes);
    console.log("Array.isArray(midiNotes):", Array.isArray(midiNotes));

    if (typeof midiNotes !== 'undefined' && Array.isArray(midiNotes)) {
        console.log("notationFromMidi.js starts rendering");
        const VF = Vex.Flow;
        const renderer = new VF.Renderer(notationDiv, VF.Renderer.Backends.SVG);
        
        const width = 800;
        const height = 200;
        renderer.resize(width, height);
        const context = renderer.getContext();

        const stave = new VF.Stave(10, 40, width - 20);
        stave.addClef("treble").setContext(context).draw();

        function midiNoteToPitch(noteNumber) {
            const notes = ['c', 'c#', 'd', 'd#', 'e', 'f', 'f#', 'g', 'g#', 'a', 'a#', 'b'];
            let number = noteNumber;

            // Якщо noteNumber є об'єктом з властивістю value, отримуємо її
            if (typeof noteNumber === 'object' && noteNumber !== null && 'value' in noteNumber) {
                number = noteNumber.value;
            }

            if (typeof number !== 'number' || isNaN(number)) {
                console.warn(`Недійсний noteNumber: ${JSON.stringify(noteNumber)}`);
                return null;
            }

            const octave = Math.floor(number / 12) - 1;
            const note = notes[number % 12];
            return `${note}/${octave}`;
        }




        function getDuration(durationMs) {
            if (typeof durationMs !== 'number' || isNaN(durationMs)) {
                console.warn(`Недійсна тривалість: ${durationMs}`);
                return "q"; // Значення за замовчуванням
            }
            if (durationMs < 250) return "16";
            if (durationMs < 500) return "8";
            if (durationMs < 1000) return "q";
            if (durationMs < 2000) return "h";
            return "w";
        }

        const notes = midiNotes.map(n => {
            const pitch = midiNoteToPitch(n.NoteNumber);
            const duration = getDuration(n.Duration?.TotalMilliseconds);
            if (!pitch || !duration) {
                console.warn(`Пропущено ноту через некоректні дані: ${JSON.stringify(n)}`);
                return null;
            }
            try {
                return new VF.StaveNote({ clef: "treble", keys: [pitch], duration: duration });
            } catch (error) {
                console.error(`Помилка при створенні ноти для NoteNumber ${n.NoteNumber}:`, error);
                return null;
            }
        }).filter(note => note !== null);

        if (notes.length === 0) {
            console.warn("Жодна нота не була створена для візуалізації.");
            return;
        }

        const voice = new VF.Voice({ num_beats: notes.length, beat_value: 4 });
        voice.addTickables(notes);

        new VF.Formatter().joinVoices([voice]).format([voice], width - 40);
        voice.draw(context, stave);
    } else {
        console.error("midiNotes не визначено або не є масивом.");
    }
});
