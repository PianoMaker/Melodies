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
    const searchAlgorithmInput = document.getElementById("searchAlgorithmInput");
    const createMIDIButton = document.getElementById('createMIDI');//кнопка "зберегти"
    const searchButton = document.getElementById('searchBtn');//кнопка "зберегти"
    const playButton = document.getElementById('melodyPlayBtn');
    const saver = document.getElementById("saver");//тимчасове збереження мелодії
    const authorSaver = document.getElementById("authorSaver");//тимчасове збереження автора    
    const titleInput = document.getElementById("titleInput");
    const selectAuthor = document.getElementById("selectAuthor");
    const copyBtn = document.getElementById("copyBtn");
    const submitMelodyBtn = document.getElementById("submitMelodyBtn")
    const melodyFileInput = document.getElementById('melodyFileInput');

    if (!pianodisplay) {
        console.warn('pianodisplay element not found – aborting createMelody.js initialization');
        return; // без нотного поля немає сенсу виконувати далі
    }

    // Відновлення значення з saver тільки якщо воно не порожнє
    if (saver) {
        const saved = (saver.innerText || '').trim();
        if (saved.length > 0) {
            pianodisplay.value = saved;
        }
    }
    const savedTitle = sessionStorage.getItem("savedTitle");
    const savedAuthorId = sessionStorage.getItem("selectedAuthorId");
    

    if (titleInput && savedTitle) {
        console.log(`restoring saved title: ${savedTitle}`)
        const savedTitleDiv = document.getElementById("savedTitle");
        if (savedTitleDiv) savedTitleDiv.textContent = `savedtitle = ${savedTitle}`;
        titleInput.value = savedTitle;
    }
    else if (titleInput) {
        console.log(`saved title is null`);
    }
    if (selectAuthor && savedAuthorId) {
        console.log(`restoring saved authorId: ${savedAuthorId}`)
        selectAuthor.value = savedAuthorId;
    }
    else if (selectAuthor) {
        console.log(`saved authorId is null`);  
    }
        
    if (saver) saver.style.display = 'none';    

    if (titleInput && selectAuthor && submitMelodyBtn && savedTitle && savedAuthorId)
        submitMelodyBtn.style.display = 'block';

    //обробник завантажувача файлів (присутній лише на сторінці Create)
    if (melodyFileInput) {
        melodyFileInput.addEventListener('change', function () {
            const fileInput = this;
            if (fileInput.files.length > 0) {
                const copyBtnLocal = document.getElementById('copyBtn');
                if (copyBtnLocal) copyBtnLocal.style.display = 'inline-block';
            }
        });
    }
    

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
            if (audioPlayer && !audioPlayer.paused) {
                audioPlayer.pause();
            }
            if (audioSource) {
                audioSource.src = audioPath;
                if (audioPlayer) {
                    audioPlayer.load();
                    audioPlayer.play();
                    audioPlayer.addEventListener('canplaythrough', function () {
                        audioPlayer.play();
                    });
                }
            }
            pianodisplay.value += `${key}${duration}_`;
            if (createMIDIButton) createMIDIButton.style.background = "lightgreen";
            if (playButton) {
                playButton.style.background = "lightgray";
                const playIcon = document.querySelector('.fas.fa-play');
                if (playIcon) playIcon.style.color = "gray";
            }
        });

    });

    //обробник паузи
    if (restBtn) {
        restBtn.addEventListener('click', function () {
            console.log(`Натиснута клавіша: пауза`);
            pianodisplay.value += `r${duration}_`;
            if (createMIDIButton) createMIDIButton.style.background = "lightgreen";
            if (playButton) {
                playButton.style.background = "lightgray";
                const playIcon = document.querySelector('.fas.fa-play');
                if (playIcon) playIcon.style.color = "gray";
            }
        })
    }

    //кнопка "Відтворення"
    if (playButton) {
        playButton.addEventListener('click', function (e) {

            const previewMp3path = document.getElementById('previewMp3path')
            if (!previewMp3path) return;
            var filepath = previewMp3path.textContent.trim();
            var audioPlayer = document.getElementById('audioPlayer');
            const audioSource = document.getElementById('audioSource');
            if (!audioPlayer || !audioSource) return;
            audioSource.src = filepath;
            console.log(`Play.js play preview from ${audioSource.src}`);
            audioPlayer.load();
            audioPlayer.play().catch(err => {
                console.error("Помилка при програванні:", err);
            });
        });
    }

    //кнопка "Зберегти" (Create) або "попередній перегляд" (Search)
    if (createMIDIButton && titleInput && selectAuthor && submitMelodyBtn) {
        // Create page behavior
        createMIDIButton.addEventListener('click', function (event) {
            event.preventDefault();
            var unique = checkIfunique();
            if (unique) {
                if (keysInput_save) keysInput_save.value = pianodisplay.value
                sessionStorage.setItem("savedTitle", titleInput.value);
                sessionStorage.setItem("selectedAuthorId", selectAuthor.value);
                console.log("Відправка форми з Keys:", keysInput_save ? keysInput_save.value : '(no element)');
                console.log(`Збереження Title: ${titleInput.value}, selectedAuthorId: ${selectAuthor.value}`);
                // Викликає OnPostMelody()
                const melodyForm = document.getElementById('melodyForm');
                if (melodyForm) melodyForm.submit();
            }
            else alert("Мелодія даного автора з такою назвою вже існує");

        });
    } else if (createMIDIButton) {
        // Fallback для сторінок без title/author (наприклад, Search)
        createMIDIButton.addEventListener('click', function (event) {
            // Не відміняємо submit, просто встановлюємо Keys перед відправкою
            if (keysInput_save) keysInput_save.value = pianodisplay.value;
            console.log("Preview submit with Keys:", keysInput_save ? keysInput_save.value : '(no element)');
            // якщо потрібно, можна вручну сабмітнути, але кнопка type=submit вже зробить це
            // const melodyForm = document.getElementById('melodyForm');
            // if (melodyForm) melodyForm.submit();
        });
    }

    //кнопка "Пошук"
    if (searchButton) {
        searchButton.addEventListener('click', function (event) {
            event.preventDefault();
            if (keysInput_search) keysInput_search.value = pianodisplay.value
            console.log("Відправка форми з Keys:", keysInput_search ? keysInput_search.value : '(no element)');

            // синхронізувати вибраний алгоритм пошуку
            const selectedAlg = document.querySelector('input[name="SearchAlgorithm"]:checked');
            if (selectedAlg && searchAlgorithmInput) {
                searchAlgorithmInput.value = selectedAlg.value;
                console.log("Selected algorithm:", selectedAlg.value);
            }

            // Відправка форми
            const searchForm = document.getElementById('notesearchForm');
            if (searchForm) searchForm.submit();

        });
    }

    //перевіряє унікальність назви+композитора
    async function checkIfunique() {
        if (!titleInput || !selectAuthor) return false;
        var title = titleInput.value;
        var authorId = selectAuthor.value;

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

    // відображення кнопки ДОДАТИ МЕЛОДІЮ
    if (titleInput && selectAuthor && submitMelodyBtn) {
        titleInput.addEventListener("input", function () {
            
            if (titleInput.value.length > 2 && selectAuthor.value) {
                submitMelodyBtn.style.display = 'block';
                console.log("displaying submit btn");
            }
            else submitMelodyBtn.style.display = 'none';
        });
    }

});

//копіювальник назви
function copytitle(event) {
    event.preventDefault();
    console.log("copytitle function is running");
    const fileInput = document.getElementById("melodyFileInput");
    const titleInput = document.getElementById("titleInput"); // стандартний id для asp-for="Melody.Title"

    if (fileInput && fileInput.files.length > 0 && titleInput) {
        const filename = fileInput.files[0].name;        
        let nameWithoutExtension = filename.replace(/\.[^/.]+$/, "");        
        let nameCapitalized = nameWithoutExtension.charAt(0).toUpperCase() + nameWithoutExtension.slice(1);
        titleInput.value = nameCapitalized;
        titleInput.dispatchEvent(new Event("input", { bubbles: true }));
    } else {
        alert("Файл не вибрано");
    }
}