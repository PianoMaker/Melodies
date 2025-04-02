// Отримуємо всі кнопки з класом .durationbutton
const buttons = document.querySelectorAll('.durationbutton');

// Додаємо обробник подій для кожної кнопки
buttons.forEach(button => {
    button.addEventListener('click', function () {
        // Видаляємо клас 'highlight' з усіх кнопок
        buttons.forEach(btn => btn.classList.remove('highlight'));

        // Додаємо клас 'highlight' до кнопки, по якій клікнули
        this.classList.add('highlight');
    });
});

const quarterNoteButton = buttons[2]; // Четвертна нота знаходиться на 3-й позиції
quarterNoteButton.classList.add('highlight');
