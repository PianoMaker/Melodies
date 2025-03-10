// працює для сторінки розширеного пошуку (search)

document.addEventListener("DOMContentLoaded", function () {
    // Отримуємо всі рядки таблиці та всі контейнери мелодій
    let rows = document.querySelectorAll("table tbody tr");
    let melodies = document.querySelectorAll("div[id^='melody']");

    // Спочатку ховаємо всі мелодії
    melodies.forEach(melody => melody.style.display = "none");

    // Відображаємо першу знайдену мелодію (якщо є)
    if (melodies.length > 0) {
        melodies[0].style.display = "block";
        rows[0].style.backgroundColor = "lightyellow";
    }

    // Додаємо обробник подій для кожного рядка
    rows.forEach((row, index) => {
        row.addEventListener("click", function () {
            console.log(`displaying melody ${index}`)
            // Ховаємо всі мелодії
            melodies.forEach(melody => melody.style.display = "none");
            
            // Відображаємо обрану мелодію
            let selectedMelody = document.getElementById(`melody${index}`);
            if (selectedMelody) {
                selectedMelody.style.display = "block";
            }

            rows.forEach(r => r.style.backgroundColor = "");
            row.style.backgroundColor = "lightyellow";
        });
    });
});
