function changeNoteDuration(index, inputString, durationChangeFn) {
    console.log(`changeNoteDuration function is working, index = ${index}, keys = ${inputString}`);

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
        const durationModifier = match[3];
        let duration = parseInt(durationModifier, 10);

        // Змінюємо тривалість за допомогою переданої функції
        duration = durationChangeFn(duration);

        let newDuration = duration.toString();
        parts[index] = notesPart.replace(durationModifier, newDuration); // Замінюємо ноту в рядку
    }

    console.log(`changed to ${parts[index]}`);
    // Збираємо назад змінений рядок
    return parts.join("_");
}

export function doubleNote(index, inputString) {
    return changeNoteDuration(index, inputString, (duration) => {
        if (duration > 1) {
            return duration / 2; // Збільшення тривалості
        }
        return duration;
    });
}

export function halfNote(index, inputString) {
    return changeNoteDuration(index, inputString, (duration) => {
        if (duration <= 32) {
            return duration * 2; // Скорочення тривалості
        }
        return duration;
    });
}
