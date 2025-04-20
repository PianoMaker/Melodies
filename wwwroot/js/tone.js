document.addEventListener("DOMContentLoaded", () => {
    let synth = new Tone.Synth().toDestination();
    Tone.start();

    let currentOctave = 1;

    const noteBaseMap = {
        "c": "C",
        "cis": "C#",
        "d": "D",
        "dis": "D#",
        "e": "E",
        "f": "F",
        "fis": "F#",
        "g": "G",
        "gis": "G#",
        "a": "A",
        "b": "A#", // або Bb, залежно від бажаного написання
        "h": "B"
    };

    document.querySelectorAll("#pianoroll button").forEach(button => {
        button.addEventListener("mousedown", () => {
            const key = button.getAttribute("data-key").replace("'", "").replace("''", "");
            const baseNote = noteBaseMap[key.replace(/['’]/g, "")];
            if (!baseNote) return;

            // Визначаємо фактичну октаву
            const octaveOffset = (button.getAttribute("data-key").match(/'/g) || []).length;
            const note = baseNote + (currentOctave + octaveOffset);
            synth.triggerAttack(note);
        });

        button.addEventListener("mouseup", () => {
            synth.triggerRelease();
        });

        button.addEventListener("mouseleave", () => {
            synth.triggerRelease(); // на випадок якщо миша покинула кнопку до відпускання
        });
    });

    // Вибір тембру
    document.getElementById("synthSelect")?.addEventListener("change", (e) => {
        const selectedType = e.target.value;
        if (synth) synth.dispose();
        synth = new Tone[selectedType]().toDestination();
        console.log(`Обрано тембр: ${selectedType}`);
    });

    // Транспозиція
    function updateOctaveDisplay() {
        document.getElementById("octave-display").textContent = `Октава: ${currentOctave}`;
    }

    document.getElementById("octave-up")?.addEventListener("click", () => {
        if (currentOctave < 7) currentOctave++;
        updateOctaveDisplay();
    });

    document.getElementById("octave-down")?.addEventListener("click", () => {
        if (currentOctave > 0) currentOctave--;
        updateOctaveDisplay();
    });

    updateOctaveDisplay();
});
