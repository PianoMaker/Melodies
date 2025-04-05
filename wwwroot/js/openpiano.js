//відкриває зображення з пінаніно і зберіга мелодію

document.addEventListener("DOMContentLoaded", function () {
    console.log("openpiano js is running");
    let openpianoDiv = document.getElementById("openpianoDiv");
    let stainway = document.getElementById("stainway");
    let durationspanel = document.getElementById("durationspanel");
    if (durationspanel) {
        console.log("durationspanel знайдено!");
    } else {
        console.warn("durationspanel НЕ знайдено!");
    }
    let pianodisplay = document.getElementById("pianodisplay");
    let onpianodisplay = pianodisplay.value;    
    let createMIDI = document.getElementById("createMIDI");

    if (onpianodisplay) {
        openpianoDiv.style.display = 'none';               
    }
    else 
    {
        console.log(`display is empty`);        
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