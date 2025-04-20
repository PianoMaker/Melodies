window.addEventListener("DOMContentLoaded", function () {
    const f = new Vex.Flow.Factory({
        renderer: { elementId: 'notation', width: 600, height: 500 },
    });

    const score = f.EasyScore();
    const system = f.System();

    system.addStave({
        voices: [
            score.voice(score.notes('C#5/q, B4, A4, G#4', { stem: 'up' })),
            score.voice(score.notes('C#4/h, C#4', { stem: 'down' }))
        ]
    }).addClef('treble').addTimeSignature('4/4');

    system.addStave({
        voices: [
            score.voice(score.notes('C#3/q, B2, A2/16, B2, C#3/8, D3/q', { clef: 'bass', stem: 'up' })),
            score.voice(score.notes('C#2/h, C#2', { clef: 'bass', stem: 'down' }))
        ]
    }).addClef('bass').addTimeSignature('4/4');

    system.addConnector()

    f.draw();
});

