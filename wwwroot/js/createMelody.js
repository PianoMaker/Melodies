//створює мелодію за натисканням клавіш піаніно
//читання нотного рядку відбувається по одній ноті у конструкторі Note(string input) 

document.addEventListener("DOMContentLoaded", function () {

    console.log("createMelody.js starts.");
    

    const buttons = document.querySelectorAll('#pianoroll button');
    const audioPlayer = document.getElementById('audioPlayer'); // аудіоплеєр
    const audioSource = document.getElementById('audioSource'); // джерело для аудіофайлу     
    let pianodisplay = document.getElementById("pianodisplay");
    const keysInput_save = document.getElementById("keysInput-save")
    const keysInput_search = document.getElementById("keysInput-search")
    const createMIDIButton = document.getElementById('createMIDI');//кнопка "зберегти"
    const searchButton = document.getElementById('searchBtn');//кнопка "зберегти"
    const playButton = document.getElementById('melodyPlayBtn');
    const saver = document.getElementById("saver");//тимчасове збереження мелодії
    const authorSaver = document.getElementById("authorSaver");//тимчасове збереження автора    
    const titleInput = document.getElementById("titleInput");
    const selectAuthor = document.getElementById("selectAuthor");
    pianodisplay.value = saver.innerText;
    const saved = localStorage.getItem("savedTitle");
    if (saved) {
        console.log(`restoring saved: ${saved}`)
        document.getElementById("savedTitle").textContent = `savedtitle = ${saved}`;
        titleInput.value = saved;
    }
    else console.log(`saved is null`);
    
    selectAuthor.value = String(authorSaver.innerText);
    saver.style.display = 'none';
    console.log(`display value = ${pianodisplay.value}; title = ${titleInput.value}; authorID = ${selectAuthor.value}`, )

    //обробник кнопок з тривалістю
    let duration = '4';
    const durationbuttons = document.querySelectorAll('.durationbutton');
    const restBtn = document.getElementById('pausebutton');
    //console.log('restBtn:', restBtn);
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

    //обробник паузи
    restBtn.addEventListener('click', function () {
        console.log(`Натиснута клавіша: пауза`);
        pianodisplay.value += `r${duration}_`;
        createMIDIButton.style.background = "lightgreen";
        playButton.style.background = "lightgray";
        document.querySelector('.fas.fa-play').style.color = "gray";
    })

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
        var unique = checkIfunique();
        if (unique) {
            keysInput_save.value = pianodisplay.value
            console.log("Відправка форми з Keys:", keysInput_save.value);
            localStorage.setItem("savedTitle", titleInput.value);
            console.log("Збереження Title:", titleInput.value);
            // Викликає OnPostMelody()
            document.getElementById('melodyForm').submit();
            
        }
        else alert("Мелодія даного автора з такою назвою вже існує");

    });

    //кнопка "Пошук"
    searchButton.addEventListener('click', function (event) {
        event.preventDefault();
        keysInput_search.value = pianodisplay.value
        console.log("Відправка форми з Keys:", keysInput_search.value);
        // Відправка форми
        document.getElementById('notesearchForm').submit();

    });

    async function checkIfunique() {
        var title = document.getElementById("titleInput").value;
        var authorId = document.getElementById("selectAuthor").value;

        try {
            const response = await fetch(`/Melodies/Create?handler=CheckFileExists&title=${encodeURIComponent(title)}&authorId=${authorId}`);
            if (!response.ok) {
                console.error("Помилка сервера:", response.statusText);
                return false;
            }
            const exist = await response.json();
            return !exist;
        } catch (error) {
            console.error("Помилка при виконанні запиту:", error);
            return false;
        }
    }

});