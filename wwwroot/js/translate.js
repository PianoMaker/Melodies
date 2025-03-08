function translit(authorName, authorSurname) {

    let nameUk = document.getElementById("nameUk");
    let surnameUk = document.getElementById("surnameUk");

    let nameEn = document.getElementById("nameEn");
    let surnameEn = document.getElementById("surnameEn");
    if (!nameEn || !surnameEn) {
        console.error("Елементи nameEn або surnameEn не знайдені!");
        return;
    }
    console.log(`${authorName}` + `${authorSurname}`);
    nameEn.value = `${authorName}`;
    surnameEn.value = `${authorSurname}`;

    nameUk.value = transliterate(nameEn.value);
    surnameUk.value = transliterate(surnameEn.value);

}

function transliterate(text) {
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

    return result.charAt(0).toUpperCase() + result.slice(1); // Перша літера велика
}