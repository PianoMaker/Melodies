//поведінка таблиці нот внизу 
const notesElements = document.querySelectorAll('.notebox');  // Змінили ім'я змінної

notesElements.forEach(note =>
    note.addEventListener('click', function () {
        notesElements.forEach(note => note.style.backgroundColor = "antiquewhite");  // Використовуємо нову змінну
        this.style.backgroundColor="yellow";
        console.log("note Element highlighted");
    })
);
