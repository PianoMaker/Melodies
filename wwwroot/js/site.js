// АНІМАЦІЯ НОТОК

const maxNotes = 30; // максимальна кількість нот

const addNote = () => {
    if (window.location.pathname !== "/" && window.location.pathname !== "/Index") {
        return; // Якщо ми не на головній сторінці — виходимо
    }

    const notes = document.querySelectorAll('.notka'); // Отримуємо всі нотки
    if (notes.length >= maxNotes) {
        return; // Якщо ноток вже 20, не додаємо нову
    }

    const note = document.createElement('div');
    note.classList.add('notka');
    document.body.appendChild(note);

    // початкова позиція (у % від розміру екрана)
    const randomHorizontalStart = Math.random() * 100 - 100;
    const randomVerticalStart = Math.random() * 50 + 50; // 

    // кінцева позиція (зміна позиції віждносно початковоґ у %)
    const randomHorizontalEnd = Math.random() * 100 + 100;
    const randomVerticalEnd = Math.random() * 100 - 80;  
    const randomRotate = Math.round(Math.random() * 60) - 30

    // Розташування нотки випадковим чином
    note.style.top = `${randomVerticalStart}vh`; // по вертикалі
    note.style.left = `${randomHorizontalStart}vw`; // по горизонталі

    // Випадкова тривалість анімації
    note.style.animationDuration = `${Math.random() * 10 + 10}s`;

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

// Додаємо нотки кожні 0.5 секунди
setInterval(addNote, 500);
