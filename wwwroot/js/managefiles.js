document.addEventListener("DOMContentLoaded", function () {

    console.log("managefiles.js is running");
    let midifilesDiv = document.getElementById("midifilesDiv");
    let midifilesBtn = document.getElementById("midifilesBtn");
    let showmidiFiles = document.getElementById("showmidiFiles");
    let midiFilesBox = document.getElementById("midiFilesBox");

    midifilesDiv.style.display = "none";
    showmidiFiles.innerText = "показати";
    midiFilesBox.style.width = "150px";

    midifilesBtn.addEventListener("click", function () {
        console.log("managefilesBtn is pressed");
        midifilesBtn.addEventListener("click", function () {
            console.log("managefilesBtn is pressed");
            if (midifilesBtn.innerText === "+") {
                midifilesBtn.innerText = "-";
                midifilesDiv.style.display = "block";
                showmidiFiles.innerText = "сховати";
                midiFilesBox.style.width = "100%";
            } else {
                midifilesBtn.innerText = "+";
                midifilesDiv.style.display = "none";
                showmidiFiles.innerText = "показати";
                midiFilesBox.style.width = "150px";
            }
        });
        

        if (midifilesBtn.innerText = "-") {
            midifilesBtn.innerText = "+";
            midifilesDiv.style.display = "none";
        }

    });

    let tempfilesDiv = document.getElementById("tempfilesDiv");
    let tempfilesBtn = document.getElementById("tempfilesBtn");
    let showtempFiles = document.getElementById("showtempFiles");
    let tempFilesBox = document.getElementById("tempFilesBox");

    tempfilesDiv.style.display = "none";
    showtempFiles.innerText = "показати";
    tempFilesBox.style.width = "150px";

    tempfilesBtn.addEventListener("click", function () {
        console.log("managefilesBtn is pressed");
        tempfilesBtn.addEventListener("click", function () {
            console.log("managefilesBtn is pressed");
            if (tempfilesBtn.innerText === "+") {
                tempfilesBtn.innerText = "-";
                tempfilesDiv.style.display = "block";
                showtempFiles.innerText = "сховати";
                tempFilesBox.style.width = "100%";
            } else {
                tempfilesBtn.innerText = "+";
                tempfilesDiv.style.display = "none";
                showtempFiles.innerText = "показати";
                tempFilesBox.style.width = "150px";
            }
        });


        if (tempfilesBtn.innerText = "-") {
            tempfilesBtn.innerText = "+";
            tempfilesDiv.style.display = "none";
        }

    });


    let mp3filesDiv = document.getElementById("mp3filesDiv");
    let mp3filesBtn = document.getElementById("mp3filesBtn");
    let showmp3Files = document.getElementById("showmp3Files");
    let mp3FilesBox = document.getElementById("mp3FilesBox");

    mp3filesDiv.style.display = "none";
    showmp3Files.innerText = "показати";
    mp3FilesBox.style.width = "150px";

    mp3filesBtn.addEventListener("click", function () {
        console.log("managefilesBtn is pressed");
        mp3filesBtn.addEventListener("click", function () {
            console.log("managefilesBtn is pressed");
            if (mp3filesBtn.innerText === "+") {
                mp3filesBtn.innerText = "-";
                mp3filesDiv.style.display = "block";
                showmp3Files.innerText = "сховати";
                mp3FilesBox.style.width = "100%";
            } else {
                mp3filesBtn.innerText = "+";
                mp3filesDiv.style.display = "none";
                showmp3Files.innerText = "показати";
                mp3FilesBox.style.width = "150px";
            }
        });


        if (mp3filesBtn.innerText = "-") {
            mp3filesBtn.innerText = "+";
            mp3filesDiv.style.display = "none";
        }

    });


});
