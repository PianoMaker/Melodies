document.addEventListener("DOMContentLoaded", function () {
    console.log("hiding piano");
    let openpiano = document.getElementById("openpiano");
    let stainway = document.getElementById("stainway");
    let pianodisplay = document.getElementById("pianodisplay");
    let onpdisplay = pianodisplay.innerText;
    let createMIDI = document.getElementById("createMIDI");
    if (stainway) console.log("piano found");
    if (onpdisplay) console.log(`${onpdisplay}`);
    else 
    {
        console.log(`display is empty`);
        stainway.style.display = 'none';
        pianoDiv.style.display = 'none';
    }
    openpiano.addEventListener("click", openPianoHandler);
    createMIDI.addEventListener("click", createMIDIHandler);
    
});

function openPianoHandler() {

    console.log("openPianoHandler is working");
    let stainway = document.getElementById("stainway");
    let openpiano = document.getElementById("openpiano");
    let pianoDiv = document.getElementById("pianoDiv");

    stainway.style.display = 'inline-flex';
    openpiano.style.display = 'none';
    pianoDiv.style.display = 'inline-flex';
}

function createMIDIHandler() {
    alert("this function is under construction yet");
}