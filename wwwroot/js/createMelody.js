document.addEventListener("DOMContentLoaded", function () {

    console.log("create melody is working.");
    
    const buttons = document.querySelectorAll('#pianoroll button');
    const audioPlayer = document.getElementById('audioPlayer'); // Отримуємо аудіоплеєр
    const audioSource = document.getElementById('audioSource'); // Отримуємо джерело для аудіофайлу     
    let pianodisplay = document.getElementById("pianodisplay");
    const keysInput = document.getElementById("keysInput")
    const createMIDIButton = document.getElementById('createMIDI');
    const inputfield = document.getElementById('melodyFileInput');
    const saver = document.getElementById("saver");    
    pianodisplay.value = saver.innerText;
    console.log(`display value = ${pianodisplay.value}`)
    

    // Додаємо обробник події для кожної кнопки
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
            pianodisplay.value += `${key}_`;
            
        });

    });

    //кнопка "Зберегти"
    createMIDIButton.addEventListener('click', function (event) {
        event.preventDefault();
        keysInput.value = pianodisplay.value
        console.log("Відправка форми з Keys:", keysInput.value); 
        // Тепер відправимо форму вручну
        document.getElementById('melodyForm').submit();
        inputfield.style.backgroundColor = "yellow";
        pianodisplay.value = "data sent";
    });

    



});