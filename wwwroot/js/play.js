document.addEventListener('DOMContentLoaded', function () {
    console.log("Play.js starts");

    document.querySelectorAll('.playbutton').forEach(function (playButton) {
        playButton.addEventListener('click', function (e) {
            e.preventDefault(); // Запобігаємо відправці форми
            console.log("Play.js play button works");

            // corrected: remove stray token and read data-filepath
            var filepath = playButton.getAttribute('data-filepath');
            if (!filepath) {
                console.warn('play.js: data-filepath is empty for play button', playButton);
                return;
            }

            var audioPlayer = document.getElementById('audioPlayer_' + filepath);
            var stopButton = document.getElementById('stopButton_' + filepath);

            if (!audioPlayer) {
                console.warn('play.js: audioPlayer not found for', filepath);
                return;
            }

            // ensure audio hidden but usable
            audioPlayer.style.display = 'none';

            // play returns a promise — handle rejection for autoplay policy or other errors
            audioPlayer.play().then(function() {
                stopButton && (stopButton.disabled = false);
                playButton.disabled = true;
            }).catch(function(err) {
                console.warn('play.js: audio.play() failed', err);
            });

            // Додаємо подію на зупинку (register once)
            if (stopButton) {
                var stopHandler = function () {
                    console.log("Play.js stop button works");
                    audioPlayer.pause();
                    try { audioPlayer.currentTime = 0; } catch (e) { }
                    stopButton.disabled = true;
                    playButton.disabled = false;
                    stopButton.removeEventListener('click', stopHandler);
                };
                stopButton.addEventListener('click', stopHandler);
            }
        });
    });
});
