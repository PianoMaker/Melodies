﻿import { shiftNoteUp, shiftNoteDown } from './shiftNote.js';
import { deleteNote } from './deleteNote.js';
import { doubleNote, halfNote } from './changeDuration.js';


const notesElements = document.querySelectorAll('.notebox');
const pauseElements = document.querySelectorAll('.pausebox');

let currentIndex = -1; // Індекс активного елемента
let pianodisplay = document.getElementById('pianodisplay');
const notesLabels = document.querySelectorAll('.notaname')
let saver = document.getElementById('saver');
let keyline = pianodisplay.value;
const createMIDIButton = document.getElementById('createMIDI');//кнопка "зберегти"
const playButton = document.getElementById('melodyPlayBtn');
console.log(`noteTable.js started. Current keyline is ${keyline}, rests detected: ${pauseElements.length}`)


// Функція для оновлення виділення
function highlightNote(index) {
    notesElements.forEach(note => note.style.backgroundColor = "antiquewhite"); // Скидаємо всі виділення
    pauseElements.forEach(note => note.style.backgroundColor = "white"); // Скидаємо всі виділення
    if (index >= 0 && index < notesElements.length) {
        notesElements[index].style.backgroundColor = "yellow"; // Виділяємо новий елемент
        currentIndex = index; // Оновлюємо поточний індекс
    }
}


// Додаємо обробник події для кліку на нотатки
notesElements.forEach((note, index) => {
    note.addEventListener('click', function () {
        highlightNote(index);
        console.log(`note ${index} Element highlighted`);
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
        processValue(newValue);
        
        
    }
    else if (event.key === "ArrowDown") {
        keyline = pianodisplay.value;
        let newValue = shiftNoteDown(currentIndex, keyline);
        processValue(newValue);
        
    }
    else if (event.key === "Delete") {
        keyline = pianodisplay.value;
        let newValue = deleteNote(currentIndex, keyline);
        processValue(newValue);
        
    }
    else if (event.key === "+") {
        keyline = pianodisplay.value;
        let newValue = doubleNote(currentIndex, keyline);
        processValue(newValue);
    }
    else if (event.key === "-") {
        keyline = pianodisplay.value;
        let newValue = halfNote(currentIndex, keyline);
        processValue(newValue);
    }
    
});


function processValue(newValue) {
    console.log("Старе значення:", pianodisplay.value);
    console.log("Нове значення:", newValue);
    createMIDIButton.style.background = "lightgreen";
    playButton.style.background = "lightgray";
    document.querySelector('.fas.fa-play').style.color = "gray";
    pianodisplay.value = newValue;
    saver.innerText = newValue;    
    
}


