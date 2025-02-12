console.log("STARTING SCRIPT");
var audioPlayer = document.getElementById('audioPlayer');

let stopButton;

const savedStopButtonId = sessionStorage.getItem('stopButtonId');


document.addEventListener("DOMContentLoaded", function () {           

    document.querySelectorAll(".playbutton").forEach(function (playButton) {
        playButton.addEventListener("click", function () {
            var filePath = this.dataset.filepath; 
            console.log("File path:", filePath);
            stopButton = document.getElementById(filePath);            

            if (stopButton) {
                console.log("Stop button found:", stopButton);
                stopButton.disabled = false; // Активуємо кнопку Stop
                console.log("Stop button enabled:", stopButton);
                sessionStorage.setItem('stopButtonId', stopButton.id); 
            } else {
                console.log("Stop button not found for filePath:", filePath);
            }
            
        });
    });
});

// Обробка кнопки Stop
document.querySelectorAll(".stopButton").forEach(function (stopButton) {
    stopButton.addEventListener("click", function () {
        console.log(new Date().toLocaleString() + " playing stopped");
        stopButton.disabled = true;
        audioPlayer.pause();
        audioPlayer.currentTime = 0;
        sessionStorage.removeItem('stopButton'); 

        // Відновлюємо кнопку play
        var playButton = document.querySelector(`.playbutton[data-filepath="${stopButton.id}"]`);
        if (playButton) {
            playButton.disabled = false;
        }
    });
});



audioPlayer.onpause = function () {
    console.log(new Date().toLocaleString() + " audio paused");
};

audioPlayer.onplay = function () {
    console.log(new Date().toLocaleString() + " audio started playing");    
    if (!stopButton) {
        console.log(new Date().toLocaleString() + "try to restore stopButton");          
        stopButton = document.getElementById(savedStopButtonId);
        
    }
    stopButton.disabled = false; // Активуємо кнопку Stop    
};



