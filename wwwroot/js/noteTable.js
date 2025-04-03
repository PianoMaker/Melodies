const notesElements = document.querySelectorAll('.notebox');
let currentIndex = -1; // Індекс активного елемента
let pianodisplay = document.getElementById('pianodisplay');
let saver = document.getElementById('saver');
let keyline = pianodisplay.value;
console.log(`noteTable.js started. Current keyline is ${keyline}`)

// Функція для оновлення виділення
function highlightNote(index) {
    notesElements.forEach(note => note.style.backgroundColor = "antiquewhite"); // Скидаємо всі виділення
    if (index >= 0 && index < notesElements.length) {
        notesElements[index].style.backgroundColor = "yellow"; // Виділяємо новий елемент
        currentIndex = index; // Оновлюємо поточний індекс
    }
}

function shiftNoteUp(index, inputString) {

    console.log(`moveUp function is working, index = ${index}, keys = ${inputString}`)
    // Визначаємо мапу для підвищення нот на півтону (з урахуванням диезів і бемолів)
    const noteMap = {
        'c': 'cis', 'cis': 'd', 'd': 'dis', 'dis': 'e', 'e': 'f', 'f': 'fis',
        'fis': 'g', 'g': 'gis', 'gis': 'a', 'a': 'ais', 'ais': 'h', 'h': 'c',
        'b': 'h', 'as': 'a', 'ges': 'g', 'es': 'e', 'des': 'd'
    };

    // Регулярний вираз для пошуку нот (включаючи диези і бемолі)
    let regex = /^(cis|dis|fis|gis|ais|as|ges|es|des|b|c|d|e|f|g|a|h)([',]*)([0-9]*)/;

    // Розбиваємо рядок на частини
    let parts = inputString.split("_");

    if (index < 0 || index >= parts.length - 1) {
        console.warn("Індекс виходить за межі доступних елементів.");
        return inputString; // Якщо index некоректний, повертаємо оригінальний рядок
    }

    // Отримуємо ноту
    let notesPart = parts[index];
    console.log(`found note: ${notesPart}`);
    // Шукаємо першу ноту в частині
    let match = notesPart.match(regex);
    console.log(`found match: ${match}`);
    if (match) {
        let originalNote = match[1]; // Знайдена нота
        let octaveModifier = match[2];
        let durationModifier = match[3];        

        //зсув на півтону
        let modifiedNote = noteMap[originalNote] || originalNote; // Заміна на підвищену ноту
        parts[index] = notesPart.replace(originalNote, modifiedNote); // Замінюємо ноту в рядку

        //перехід через октаву
        if (originalNote === 'h' && octaveModifier === undefined) {
            parts[index] = originalNote + "'" + durationModifier;
        }
        else if (originalNote === 'h' && octaveModifier.match(/,+/)) {
            console.log("octave down");
            parts[index] = parts[index].replace(",", "")
        }
        else if (originalNote === 'h' && !octaveModifier.match(/'+/))
            parts[index] = modifiedNote + octaveModifier + "'" + durationModifier;

    }
    console.log(`changed to ${parts[index]}`)
    // Збираємо назад змінений рядок
    return parts.join("_");
}



function shiftNoteDown(index, inputString) {

    console.log(`moveUp function is working, index = ${index}, keys = ${inputString}`)
    // Визначаємо мапу для підвищення нот на півтону (з урахуванням диезів і бемолів)
    const noteMap = {
        'cis': 'c', 'd': 'cis', 'dis': 'd', 'e': 'dis', 'f': 'e', 'fis': 'f',
        'g': 'fis', 'gis': 'g', 'a': 'gis', 'ais': 'a', 'b': 'a', 'h': 'b',
        'c': 'h', 'as': 'g', 'ges': 'f', 'es': 'd', 'des': 'c'
    };

    // Регулярний вираз для пошуку нот (включаючи диези і бемолі)
    let regex = /^(cis|dis|fis|gis|ais|as|ges|es|des|b|c|d|e|f|g|a|h)([',]*)([0-9]*)/;

    // Розбиваємо рядок на частини
    let parts = inputString.split("_");

    if (index < 0 || index >= parts.length - 1) {
        console.warn("Індекс виходить за межі доступних елементів.");
        return inputString; // Якщо index некоректний, повертаємо оригінальний рядок
    }

    // Отримуємо ноту
    let notesPart = parts[index];
    console.log(`found note: ${notesPart}`);
    // Шукаємо першу ноту в частині
    let match = notesPart.match(regex);    
    if (match) {
        let originalNote = match[1]; // Знайдена нота
        let octaveModifier = match[2];
        let durationModifier = match[3];
        console.log(`match[0] = ${match}, note = ${match[1]}, octave: ${octaveModifier}, duration: ${durationModifier}`);

        // зсув на півтону
        let modifiedNote = noteMap[originalNote] || originalNote; // Заміна на підвищену ноту
        parts[index] = notesPart.replace(originalNote, modifiedNote); // Замінюємо ноту в рядку

        //перехід через октаву
        if (originalNote === 'c' && octaveModifier === undefined) {
            parts[index] = originalNote + "," + durationModifier;
        }
        else if (originalNote === 'c' && octaveModifier.match(/'+/)) {
            console.log("octave down");
            parts[index] = parts[index].replace("'", "")
        }
        else if (originalNote === 'c' && !octaveModifier.match(/,+/))
            parts[index] = modifiedNote + octaveModifier + "," + durationModifier;
    }
    console.log(`changed to ${parts[index]}`)
    // Збираємо назад змінений рядок
    return parts.join("_");
}



// Приклад використання:
let input = "a4_cis'4_dis'4_";
console.log(shiftNoteUp(1, input)); // Очікуваний результат: "a4_cis'4_dis'4_fis'4_"


function movedown(index) {
    console.log('movedown function is working')

}

// Додаємо обробник події для кліку на нотатки
notesElements.forEach((note, index) => {
    note.addEventListener('click', function () {
        highlightNote(index);
        console.log("note Element highlighted");
    });
});

// Додаємо обробник подій для клавіш стрілок
document.addEventListener('keydown', function (event) {
    if (currentIndex === -1) return; // Якщо жоден елемент не вибраний, ігноруємо

    if (event.key === "ArrowLeft") {
        highlightNote(Math.max(0, currentIndex - 1)); // Переміщуємося назад (ліворуч або вгору)
    }
    else if (event.key === "ArrowRight") {
        highlightNote(Math.min(notesElements.length - 1, currentIndex + 1)); // Переміщуємося вперед (праворуч або вниз)
    } 
    else if (event.key === "ArrowUp") {        
        keyline = pianodisplay.value;
        let newValue = shiftNoteUp(currentIndex, keyline);
        console.log("Старе значення:", pianodisplay.value);
        console.log("Нове значення:", newValue);
        pianodisplay.value = newValue; saver.innerText = newValue;
        
    }
    else if (event.key === "ArrowDown") {
        keyline = pianodisplay.value;
        let newValue = shiftNoteDown(currentIndex, keyline);
        console.log("Старе значення:", pianodisplay.value);
        console.log("Нове значення:", newValue);
        pianodisplay.value = newValue; saver.innerText = newValue;
    }
});
