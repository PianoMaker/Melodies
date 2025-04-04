import { shiftNoteUp } from './shiftNoteUp.js';
import { shiftNoteDown } from './shiftNoteDown.js';
import { deleteNote } from './deleteNote.js';
import { doubleNote, halfNote } from './changeDuration.js';


// Отримуємо всі елементи контекстного меню та нот
const contextMenus = document.querySelectorAll('.context-menu');
const notesElements = document.querySelectorAll('.notebox');
const saver = document.getElementById('saver');
const createMIDIButton = document.getElementById('createMIDI');
let pianodisplay = document.getElementById('pianodisplay');
let keyline = pianodisplay.value;

console.log(`ContextMenu.js started`);

// Функція для показу контекстного меню
function showContextMenu(event, index) {
    event.preventDefault();
    const x = event.clientX;
    const y = event.clientY;

    const contextMenu = document.querySelector(`.context-menu[data-index="${index}"]`);
    if (contextMenu) {
        hideContextMenu(); // Закриваємо всі інші меню перед відкриттям
        contextMenu.style.display = 'block';
        contextMenu.style.left = `${x}px`;
        contextMenu.style.top = `${y}px`;
    }
}

// Функція для приховування всіх контекстних меню
function hideContextMenu() {
    contextMenus.forEach(menu => {
        menu.style.display = 'none';
    });
}

// Функція для обробки нових значень
function processValue(newValue) {
    console.log("Старе значення:", pianodisplay.value);
    console.log("Нове значення:", newValue);
    createMIDIButton.style.background = "pink";
    pianodisplay.value = newValue;
    saver.innerText = newValue;
}

// Додаємо обробник для кожного нотного блоку
notesElements.forEach((note, index) => {
    note.addEventListener('contextmenu', function (event) {
        console.log(`Context menu for element ${index} clicked`);
        showContextMenu(event, index);
    });
});

// Додаємо обробники для опцій контекстного меню тільки один раз
contextMenus.forEach((menu, index) => {
    const options = menu.querySelectorAll('li');
    options.forEach(option => {
        option.addEventListener('click', function (event) {
            event.stopPropagation();
            const optionId = option.id;

            switch (optionId) {
                case 'option1':
                    keyline = pianodisplay.value;
                    let newValueUp = shiftNoteUp(index, keyline);
                    processValue(newValueUp);
                    break;
                case 'option2':
                    keyline = pianodisplay.value;
                    let newValueDown = shiftNoteDown(index, keyline);
                    processValue(newValueDown);
                    break;
                case 'option3':
                    alert('Option 3 selected');
                    break;
                case 'option4':
                    alert(`Option 4 selected, index = ${index}`);
                    break;
                case 'option5':
                    alert(`Option 5 selected, index = ${index}`);
                    break;
                default:
                    alert('Unknown option');
            }

            hideContextMenu();  // Закриваємо меню після вибору
        });
    });
});

// Ховаємо меню при кліку в будь-якому місці документа
document.addEventListener('click', hideContextMenu);
