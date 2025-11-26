// АНІМАЦІЯ НОТОК

const maxNotes = 50; // максимальна кількість нот
let notesIntervalId = null; // interval handler for addNote

const addNote = () => {
    // Вимикаємо на вузьких екранах
    if (window.innerWidth <= 768) {
        return;
    }

    if (window.location.pathname !== "/" && window.location.pathname !== "/Index") {
        return; // Якщо ми не на головній сторінці — виходимо
    }

    const notes = Array.from(document.querySelectorAll('*')).filter(el =>
        el.classList.contains('notka') || el.classList.contains('notka-blue')
    );


    if (notes.length >= maxNotes) {
        return; // Якщо ноток вже 20, не додаємо нову
    }

    const note = document.createElement('div');
    note.classList.add(Math.random() < 0.8 ? 'notka' : 'notka-blue');
    document.body.appendChild(note);

    // початкова позиція (у % від розміру екрана)
    const randomHorizontalStart = Math.random() * 10 + 95;
    const randomVerticalStart = Math.random() * 10 + 50; // 

    // кінцева позиція (зміна позиції віждносно початковоґ у %)
    const randomHorizontalEnd = Math.random() * 100 - 80;
    const randomVerticalEnd = Math.random() * 20 - 100;  
    const randomRotate = Math.round(Math.random() * 60) - 30

    // Розташування нотки випадковим чином
    note.style.top = `${randomVerticalStart}vh`; // по вертикалі
    note.style.left = `${randomHorizontalStart}vw`; // по горизонталі

    // Випадкова тривалість анімації
    note.style.animationDuration = `${Math.random() * 15 + 10}s`;

    // Додаємо випадкові значення до анімації за допомогою JavaScript
    note.style.animationName = `float`;

    // Встановлюємо змінні для анімації (кінцеве розташування)
    note.style.setProperty('--randomHorizontalEnd', `${randomHorizontalEnd}vw`);
    note.style.setProperty('--randomVerticalEnd', `${randomVerticalEnd}vh`);
    note.style.setProperty('--randomRotate', `${randomRotate}deg`); 

    // Видаляємо нотку після завершення анімації
    note.addEventListener('animationend', () => {
        note.remove();
    });
};

function startNotes() {
    if (notesIntervalId || window.innerWidth <= 768) return;
    notesIntervalId = setInterval(addNote, 400);
}

function stopNotes() {
    if (notesIntervalId) {
        clearInterval(notesIntervalId);
        notesIntervalId = null;
    }
    // При вимкненні на мобільних — прибираємо існуючі нотки
    document.querySelectorAll('.notka, .notka-blue').forEach(n => n.remove());
}

// Ініціалізація після завантаження DOM
window.addEventListener('DOMContentLoaded', () => {
    if (window.innerWidth > 768) startNotes();
});

// Динамічне вмикання/вимикання при зміні ширини
window.addEventListener('resize', () => {
    if (window.innerWidth <= 768) stopNotes();
    else startNotes();
});





