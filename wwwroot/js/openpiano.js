document.addEventListener("DOMContentLoaded", function () {
    console.log("openpiano js is running");
    let openpianoDiv = document.getElementById("openpianoDiv");
    let stainway = document.getElementById("stainway");
    let pianodisplay = document.getElementById("pianodisplay");
    let onpianodisplay = pianodisplay.innerText;    
    let createMIDI = document.getElementById("createMIDI");
    
    if (onpianodisplay) {
        openpianoDiv.style.display = 'none';
        console.log(`${onpianodisplay}`);
    }
    else 
    {
        console.log(`display is empty`);
        stainway.style.display = 'none';
        pianoDiv.style.display = 'none';
        openpianoDiv.style.display = 'flex';
    }
    openpianoDiv.addEventListener("click", () => {
        console.log("opeining piano");
        stainway.style.display = 'inline-flex';
        pianoDiv.style.display = 'inline-flex';
        openpianoDiv.style.display = 'none';
    });

    createMIDI.addEventListener("click", createMIDIHandler);
    
});


function createMIDIHandler() {
   console.log("this function is under construction yet");
}