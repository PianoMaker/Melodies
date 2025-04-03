export function shiftNoteUp(index, inputString) {

    console.log(`moveUp function is working, index = ${index}, keys = ${inputString}`);
    // Визначаємо мапу для підвищення нот на півтону (з урахуванням диезів і бемолів)
    const noteMap = {
        'c': 'cis', 'cis': 'd', 'd': 'dis', 'dis': 'e', 'e': 'f', 'f': 'fis',
        'fis': 'g', 'g': 'gis', 'gis': 'a', 'a': 'ais', 'ais': 'h', 'h': 'c',
        'b': 'h', 'as': 'a', 'ges': 'g', 'es': 'e', 'des': 'd'
    };

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
        let originalNote = match[1]; // Знайдена нота
        let octaveModifier = match[2];
        let durationModifier = match[3];

        //зсув на півтону
        let modifiedNote = noteMap[originalNote] || originalNote; // Заміна на підвищену ноту
        parts[index] = notesPart.replace(originalNote, modifiedNote); // Замінюємо ноту в рядку


        //перехід через октаву
        if (originalNote === 'h' && octaveModifier === undefined) {
            parts[index] = originalNote + "'" + durationModifier;
        }
        else if (originalNote === 'h' && octaveModifier.match(/,+/)) {
            console.log("octave down");
            parts[index] = parts[index].replace(",", "");
        }
        else if (originalNote === 'h' && !octaveModifier.match(/'+/))
            parts[index] = modifiedNote + octaveModifier + "'" + durationModifier;

    }
    console.log(`changed to ${parts[index]}`);
    // Збираємо назад змінений рядок
    return parts.join("_");
}
