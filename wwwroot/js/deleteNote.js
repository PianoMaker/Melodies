export function deleteNote(index, inputString) {

    console.log(`delete function is working, index = ${index}, keys = ${inputString}`);

    // –егул€рний вираз дл€ пошуку нот (включаючи диези ≥ бемол≥)
    let regex = /^(cis|dis|fis|gis|ais|as|ges|es|des|b|c|d|e|f|g|a|h)([',]*)([0-9]*)/;

    // –озбиваЇмо р€док на частини
    let parts = inputString.split("_");

    if (index < 0 || index >= parts.length - 1) {
        console.warn("≤ндекс виходить за меж≥ доступних елемент≥в.");
        return inputString; // якщо index некоректний, повертаЇмо ориг≥нальний р€док
    }

    parts[index] = ""; // вилучаЇмо запис ноти
    return parts.join("_");
}
