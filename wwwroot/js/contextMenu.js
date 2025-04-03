// Отримуємо контекстне меню
const contextMenu = document.getElementById('contextMenu');

// Функція для показу контекстного меню
function showContextMenu(event) {
    event.preventDefault(); // Забороняємо стандартне контекстне меню

    // Встановлюємо позицію меню
    const x = event.clientX;
    const y = event.clientY;

    contextMenu.style.display = 'block';
    contextMenu.style.left = `${x}px`;
    contextMenu.style.top = `${y}px`;
}

// Функція для приховування контекстного меню
function hideContextMenu() {
    contextMenu.style.display = 'none';
}

// Додаємо слухач подій для правого кліку
document.addEventListener('contextmenu', showContextMenu);

// Слухач для приховування меню, коли користувач клацне в будь-якому місці
document.addEventListener('click', hideContextMenu);

// Приклад для вибору опцій меню
document.getElementById('option1').addEventListener('click', function () {
    alert('Option 1 selected');
    hideContextMenu(); // Закриваємо меню після вибору
});

document.getElementById('option2').addEventListener('click', function () {
    alert('Option 2 selected');
    hideContextMenu(); // Закриваємо меню після вибору
});

document.getElementById('option3').addEventListener('click', function () {
    alert('Option 3 selected');
    hideContextMenu(); // Закриваємо меню після вибору
});
