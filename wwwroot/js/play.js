document.addEventListener('DOMContentLoaded', function () {
    console.log("Play.js starts");

    
    document.querySelectorAll('.playbutton').forEach(function (playButton) {
        playButton.addEventListener('click', function (e) {
            e.preventDefault(); // Запобігаємо відправці форми
            console.log("Play.js play button works");
            
            var filepath = playButton.getAttribute('data-filepath');
            var audioPlayer = document.getElementById('audioPlayer_' + filepath);
            var stopButton = document.getElementById('stopButton_' + filepath);
            
            audioPlayer.style.display = 'none'; 
            audioPlayer.play();
                        
            stopButton.disabled = false;
                        
            playButton.disabled = true;

            // Додаємо подію на зупинку
            stopButton.addEventListener('click', function () {
                console.log("Play.js stop button works");
                audioPlayer.pause(); 
                audioPlayer.currentTime = 0; 
                stopButton.disabled = true; 
                playButton.disabled = false;                 
            });
        });
    });
});
