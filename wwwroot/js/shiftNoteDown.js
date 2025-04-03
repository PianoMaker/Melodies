function shiftNoteDown(index, inputString) {

    console.log(`moveUp function is working, index = ${index}, keys = ${inputString}`);
    // Визначаємо мапу для підвищення нот на півтону (з урахуванням диезів і бемолів)
    const noteMap = {
        'cis': 'c', 'd': 'cis', 'dis': 'd', 'e': 'dis', 'f': 'e', 'fis': 'f',
        'g': 'fis', 'gis': 'g', 'a': 'gis', 'ais': 'a', 'b': 'a', 'h': 'b',
        'c': 'h', 'as': 'g', 'ges': 'f', 'es': 'd', 'des': 'c'
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
    if (match) {
        let originalNote = match[1]; // Знайдена нота
        let octaveModifier = match[2];
        let durationModifier = match[3];
        console.log(`match[0] = ${match}, note = ${match[1]}, octave: ${octaveModifier}, duration: ${durationModifier}`);

        // зсув на півтону
        let modifiedNote = noteMap[originalNote] || originalNote; // Заміна на підвищену ноту
        parts[index] = notesPart.replace(originalNote, modifiedNote); // Замінюємо ноту в рядку


        //перехід через октаву
        if (originalNote === 'c' && octaveModifier === undefined) {
            parts[index] = originalNote + "," + durationModifier;
        }
        else if (originalNote === 'c' && octaveModifier.match(/'+/)) {
            console.log("octave down");
            parts[index] = parts[index].replace("'", "");
        }
        else if (originalNote === 'c' && !octaveModifier.match(/,+/))
            parts[index] = modifiedNote + octaveModifier + "," + durationModifier;
    }
    console.log(`changed to ${parts[index]}`);
    // Збираємо назад змінений рядок
    return parts.join("_");
}
