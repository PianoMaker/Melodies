document.addEventListener("DOMContentLoaded", function () {
    console.log("openpiano js is running");
    let openpianoDiv = document.getElementById("openpianoDiv");
    let stainway = document.getElementById("stainway");
    let durationspanel = document.getElementById("durationspanel");
    let pianodisplay = document.getElementById("pianodisplay");
    let onpianodisplay = pianodisplay.value;    
    let createMIDI = document.getElementById("createMIDI");
    
    if (onpianodisplay) {
        openpianoDiv.style.display = 'none';
        durationspanel.style.display = 'none';
        console.log(`onpianodisplay = ${onpianodisplay}`);
        console.log(`durationspanel = ${durationspanel}`);
    }
    else 
    {
        console.log(`display is empty`);
        console.log(`durationspanel = ${durationspanel}`);
        stainway.style.display = 'none';
        durationspanel.style.display = 'none';
        pianoDiv.style.display = 'none';
        openpianoDiv.style.display = 'flex';
    }
    openpianoDiv.addEventListener("click", () => {
        console.log("opening piano");
        stainway.style.display = 'inline-flex';
        durationspanel.style.display = 'inline-flex';
        pianoDiv.style.display = 'inline-flex';
        openpianoDiv.style.display = 'none';
    });

    createMIDI.addEventListener("click", createMIDIHandler);
    
});


function createMIDIHandler() {
   console.log("this function is under construction yet");
}