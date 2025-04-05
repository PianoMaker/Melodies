//створює мелодію за натисканням клавіш піаніно
//читання нотного рядку відбувається по одній ноті у конструкторі Note(string input) 

document.addEventListener("DOMContentLoaded", function () {

    console.log("create melody is working.");

    const buttons = document.querySelectorAll('#pianoroll button');
    const audioPlayer = document.getElementById('audioPlayer'); // Отримуємо аудіоплеєр
    const audioSource = document.getElementById('audioSource'); // Отримуємо джерело для аудіофайлу     
    let pianodisplay = document.getElementById("pianodisplay");
    const keysInput = document.getElementById("keysInput")
    const createMIDIButton = document.getElementById('createMIDI');//кнопка "зберегти"
    const playButton = document.getElementById('melodyPlayBtn');
    const inputfield = document.getElementById('melodyFileInput');
    const saver = document.getElementById("saver");
    pianodisplay.value = saver.innerText;
    saver.style.display = 'none';
    console.log(`display value = ${pianodisplay.value}`)    


    //обробник кнопок з тривалістю
    let duration = '4';
    const durationbuttons = document.querySelectorAll('.durationbutton');

    durationbuttons.forEach((button, index) => {
        button.addEventListener('click', () => {
            duration = String(2 ** index); // 2^index дає потрібне значення
            console.log(duration);
        });
    });


    // обробник клавіш фортепіано
    // // (треба буде додати запобіжник для любителів грати в стилі Зеленського)
    buttons.forEach(button => {
        button.addEventListener('click', function () {
            const key = this.getAttribute('data-key'); // Отримуємо значення клавіші            
            console.log(`Натиснута клавіша: ${key}`);
            const audioPath = `/sounds/${key}.mp3`;
            if (!audioPlayer.paused) {
                audioPlayer.pause();
            }
            audioSource.src = audioPath;
            audioPlayer.load();
            audioPlayer.play();
            audioPlayer.addEventListener('canplaythrough', function () {
                audioPlayer.play();
            });
            pianodisplay.value += `${key}${duration}_`;
            createMIDIButton.style.background = "lightgreen";
            playButton.style.background = "lightgray";
            document.querySelector('.fas.fa-play').style.color = "gray";
        });

    });

    //кнопка "Відтворення"
    playButton.addEventListener('click', function (e) {


        const previewMp3path = document.getElementById('previewMp3path')
        var filepath = previewMp3path.textContent.trim();      
        var audioPlayer = document.getElementById('audioPlayer');  
        const audioSource = document.getElementById('audioSource'); 
        audioSource.src = filepath;
        console.log(`Play.js play preview from ${audioSource.src}`);
        audioPlayer.load();
        audioPlayer.play().catch(err => {
            console.error("Помилка при програванні:", err);
        });
    });
    


    //кнопка "Зберегти"
    createMIDIButton.addEventListener('click', function (event) {
        event.preventDefault();
        keysInput.value = pianodisplay.value
        console.log("Відправка форми з Keys:", keysInput.value); 
        // Тепер відправимо форму вручну
        document.getElementById('melodyForm').submit();
        

    });

    



});