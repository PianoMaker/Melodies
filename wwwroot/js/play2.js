document.addEventListener('DOMContentLoaded', function () {
    // Для кожного рядка таблиці
    document.querySelectorAll('.playbutton').forEach(function (playButton) {
        playButton.addEventListener('click', function (e) {
            e.preventDefault(); // Запобігаємо відправці форми
            var filepath = playButton.getAttribute('data-filepath');
            var audioPlayer = document.getElementById('audioPlayer_' + filepath);
            var stopButton = document.getElementById('stopButton_' + filepath);
            
            audioPlayer.style.display = 'none'; // Покажемо аудіоплеєр
            audioPlayer.play();
                        
            stopButton.disabled = false;
                        
            playButton.disabled = true;

            // Додаємо подію на зупинку
            stopButton.addEventListener('click', function () {
                audioPlayer.pause(); // Зупиняємо відтворення
                audioPlayer.currentTime = 0; // Скидаємо програвання на початок
                stopButton.disabled = true; // Вимикаємо кнопку Stop
                playButton.disabled = false; // Дозволяємо натискати Play знову
                audioPlayer.style.display = 'none'; // Приховуємо аудіоплеєр
            });
        });
    });
});
