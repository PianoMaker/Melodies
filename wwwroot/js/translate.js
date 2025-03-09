function translit(authorName, authorSurname) {

    console.log("Start translit:", authorName, authorSurname);

    let nameUk = document.getElementById("nameUk");
    let surnameUk = document.getElementById("surnameUk");

    let nameEn = document.getElementById("nameEn");
    let surnameEn = document.getElementById("surnameEn");    
    

    if (isLatin(authorName) && isLatin(authorSurname)) {
        nameEn.value = `${authorName}`;
        surnameEn.value = `${authorSurname}`;
        nameUk.value = transliterateToUk(nameEn.value);
        surnameUk.value = transliterateToUk(surnameEn.value);
        console.log(`${authorName}` + `${authorSurname}` + "латинка");
    }
    else if (!nameEn.value && !surnameEn.value && !IsLatin(authorName) && !IsLatin(authorSurname))
    {
        console.log(`${authorName}` + `${authorSurname}` + "кирилиця");
        nameEn.value = transliterateToEn(authorName)
        surnameEn.value = transliterateToEn(authorSurname)
    }

}

function isLatin(word) {

    return /^[A-Za-z_ ]+$/.test(word);
}

function transliterateToUk(text) {
    const translitMap = {
        'a': 'а', 'b': 'б', 'c': 'к', 'd': 'д', 'e': 'е', 'f': 'ф', 'g': 'ґ',
        'h': 'г', 'i': 'і', 'j': 'дж', 'k': 'к', 'l': 'л', 'm': 'м', 'n': 'н',
        'o': 'о', 'p': 'п', 'q': 'к', 'r': 'р', 's': 'с', 't': 'т', 'u': 'у',
        'v': 'в', 'w': 'в', 'x': 'кс', 'y': 'й', 'z': 'з', 'ph': 'ф', 'zh': 'ж',
        'ch': 'ч', 'sh': 'ш', 'sch': 'щ', 'ye': 'є', 'yi': 'ї', 'yo': 'йо', 'yu': 'ю', 'ya': 'я',
        
    };

    text = text.toLowerCase(); // Перетворюємо текст у нижній регістр для простоти
    let result = "";

    for (let i = 0; i < text.length; i++) {
        let bigram = text.substr(i, 2); // Перевіряємо біграми (2 літери разом)
        if (translitMap[bigram]) {
            result += translitMap[bigram];
            i++; // Пропускаємо наступну літеру, бо вона вже врахована в біграмі
        } else {
            let letter = text[i];
            result += translitMap[letter] || letter; // Якщо літери нема в словнику, залишаємо її незмінною
        }
    }

    let cleanedResult = result.replace(/_+$/, ""); // Видаляємо підкреслення в кінці

    return cleanedResult.charAt(0).toUpperCase() + result.slice(1); // Перша літера велика
}


function transliterateToEn(input) {
    const map = {
        'А': "A", 'Б': "B", 'В': "V", 'Г': "H", 'Ґ': "G", 'Д': "D",
        'Е': "E", 'Є': "Ye", 'Ж': "Zh", 'З': "Z", 'И': "Y", 'І': "I",
        'Ї': "Ji", 'Й': "J", 'К': "K", 'Л': "L", 'М': "M", 'Н': "N",
        'О': "O", 'П': "P", 'Р': "R", 'С': "S", 'Т': "T", 'У': "U",
        'Ф': "F", 'Х': "Kh", 'Ц': "Ts", 'Ч': "Ch", 'Ш': "Sh", 'Щ': "Shch",
        'Ь': "", 'Ю': "Yu", 'Я': "Ya",
        'а': "a", 'б': "b", 'в': "v", 'г': "h", 'ґ': "g", 'д': "d",
        'е': "e", 'є': "ie", 'ж': "zh", 'з': "z", 'и': "y", 'і': "i",
        'ї': "i", 'й': "j", 'к': "k", 'л': "l", 'м': "m", 'н': "n",
        'о': "o", 'п': "p", 'р': "r", 'с': "s", 'т': "t", 'у': "u",
        'ф': "f", 'х': "kh", 'ц': "ts", 'ч': "ch", 'ш': "sh", 'щ': "shch",
        'ю': "yu", 'я': "ya",
        ' ': "_", '-': "_", ',': "_", '!': "_", '?': "_"
    };

    let result = "";
    let prevChar = null; // Для збереження попереднього символу

    for (let i = 0; i < input.length; i++) {
        let c = input[i];

        // Перевірка "ьо"
        if (c === 'ь' && i + 1 < input.length && input[i + 1] === 'о') {
            result += "io";
            i++; // Пропускаємо наступний 'о'
            prevChar = 'о'; // Оновлюємо попередній символ
            continue;
        }

        // Пропускаємо всі м'які знаки, ъ, наголоси та ё
        if (c === 'ь' || c === 'ъ' || c === '́' || c === 'ё') continue;

        // Перевірка "я" після приголосної
        if (c === 'я') {
            if (prevChar !== null && !"аеєиіїоуюяь".includes(prevChar)) {
                result += "ia";
            } else {
                result += "ya";
            }
        } else {
            if (map.hasOwnProperty(c)) {
                result += map[c];
            } else if (/[a-zA-Z0-9]/.test(c)) {
                result += c;
            }
        }

        prevChar = c; // Оновлюємо попередній символ
    }

    let cleanedResult = result.replace(/_+$/, ""); // Видаляємо підкреслення в кінці

    console.log(`${input} transliterated to ${cleanedResult}`);

    return cleanedResult;
}

// Приклад використання
console.log(transliterate("Привіт, як справи?")); // "Pryvit_yak_spravy"
