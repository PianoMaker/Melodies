// thin wrapper for Search page — uses the shared setup in /lib/midirender/setupLiveNotationCreate.js

document.addEventListener('DOMContentLoaded', () => {
    if (typeof window.setupLiveNotationOnCreate !== 'function') {
        console.warn('[livePatternRenderer] setupLiveNotationOnCreate not found; ensure /lib/midirender/setupLiveNotationCreate.js is loaded before this script.');
        return;
    }

    const container = document.getElementById('innerNotesContainer');
    const pianodisplay = document.getElementById('pianodisplay');
    const numeratorInput = document.getElementById('TimeSignatureNumerator');
    const denominatorInput = document.getElementById('TimeSignatureDenominator');
    const pianoKeysContainer = document.getElementById('pianoroll');
    const restBtn = document.getElementById('pausebutton');
    const noNotesMsg = document.getElementById('noNotesMsg');

    // Initialize shared implementation with page-specific options (no duplication)
    window.setupLiveNotationOnCreate({
        container,
        pianodisplay,
        numeratorInput,
        denominatorInput,
        pianoKeysContainer,
        restBtn,
        noNotesMsg
    }, {
        autoHookTimeSignature: true,
        observeIds: ['lettersdisplay', 'saver'], // Search page specifics
        startPolling: false // keep polling off by default
    });

    console.log('[livePatternRenderer] wrapper initialized.');
});