document.addEventListener("DOMContentLoaded", function () {

    console.log("managefiles.js is running");
    let midiFilesDiv = document.getElementById("midifilesDiv");
    let midiFilesBtn = document.getElementById("midifilesBtn");
    let showmidifiles = document.getElementById("showmidifiles");
    let midifilesBox = document.getElementById("midifilesBox");

    midiFilesDiv.style.display = "none";
    showmidifiles.innerText = "показати";
    midifilesBox.style.width = "150px";


    midiFilesBtn.addEventListener("click", function () {
        console.log("midifilesBtn is pressed");
        if (midiFilesBtn.innerText === "+") {
            midiFilesBtn.innerText = "-";
            midiFilesDiv.style.display = "block";
            showmidifiles.innerText = "сховати";
            midifilesBox.style.width = "100%";
        } else {
            midiFilesBtn.innerText = "+";
            midiFilesDiv.style.display = "none";
            showmidifiles.innerText = "показати";
            midifilesBox.style.width = "150px";
        }
    });




});


let mp3filesDiv = document.getElementById("mp3filesDiv");
let mp3filesBtn = document.getElementById("mp3filesBtn");
let showmp3files = document.getElementById("showmp3files");
let mp3filesBox = document.getElementById("mp3filesBox");

mp3filesDiv.style.display = "none";
showmp3files.innerText = "показати";
mp3filesBox.style.width = "150px";

mp3filesBtn.addEventListener("click", function () {

    console.log("mp3filesBtn is pressed");
    if (mp3filesBtn.innerText === "+") {
        mp3filesBtn.innerText = "-";
        mp3filesDiv.style.display = "block";
        showmp3files.innerText = "сховати";
        mp3filesBox.style.width = "100%";
    } else {
        mp3filesBtn.innerText = "+";
        mp3filesDiv.style.display = "none";
        showmp3files.innerText = "показати";
        mp3filesBox.style.width = "150px";
    }
});




let tempfilesDiv = document.getElementById("tempfilesDiv");
let tempfilesBtn = document.getElementById("tempfilesBtn");
let showtempfiles = document.getElementById("showtempfiles");
let tempfilesBox = document.getElementById("tempfilesBox");

tempfilesDiv.style.display = "none";
showtempfiles.innerText = "показати";
tempfilesBox.style.width = "150px";

tempfilesBtn.addEventListener("click", function () {
    console.log("tempfilesBtn is pressed");
        if (tempfilesBtn.innerText === "+") {
            tempfilesBtn.innerText = "-";
            tempfilesDiv.style.display = "block";
            showtempfiles.innerText = "сховати";
            tempfilesBox.style.width = "100%";
        } else {
            tempfilesBtn.innerText = "+";
            tempfilesDiv.style.display = "none";
            showtempfiles.innerText = "показати";
            tempfilesBox.style.width = "150px";
        }
 });

