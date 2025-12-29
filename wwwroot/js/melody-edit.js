document.addEventListener("DOMContentLoaded", function () {

    // ---------------------
    // ВСТАНОВЛЕННЯ ТЕМПУ
    // ---------------------
    var tempoChange = document.getElementById("tempoChange");
    var tempoChangeHidden = document.getElementById("tempoChangeHidden");
    if (tempoChange && tempoChangeHidden) {
        tempoChange.addEventListener("input", function () {
            tempoChangeHidden.value = true;
            console.log("tempoChangeHidden.value = true");
        });
    }
    // -------------------------------------
    // ВСТАНОВЛЕННЯ ТОНАЛЬНОСТІ з MIDI-файлу
    // -------------------------------------
    var getTonalityButton = document.getElementById("getTonality");
    if (getTonalityButton) {
        getTonalityButton.addEventListener("click", async function (event) {
            event.preventDefault();
            console.log("Get Tonality button clicked");

            // беремо id мелодії з hidden input або з URL
            var idInput = document.querySelector("input[name='Melody.ID']");
            var id = idInput ? idInput.value : null;
            if (!id) {
                // спроба з URL ?id=...
                var url = new URL(window.location.href);
                id = url.searchParams.get("id");
            }
            if (!id) {
                console.warn("Melody ID not found");
                return;
            }

            try {
                const response = await fetch(`/Melodies/Edit?handler=DetectTonality&id=${encodeURIComponent(id)}`);
                const data = await response.json();
                if (!data.ok || !data.tonality) {
                    alert('не вдалося визначити тональність автоматично');
                    return;
                }
                const tonality = data.tonality; // наприклад: "Es-dur" або "a-moll"
                console.log("Detected tonality:", tonality);

                // Знайти селект тональностей та встановити значення
                const select = document.querySelector("select[name='Melody.Tonality']");
                if (!select) return;

                // Якщо такої опції немає — додати її
                let option = Array.from(select.options).find(o => (o.value || o.text).toLowerCase() === tonality.toLowerCase());
                if (!option) {
                    option = document.createElement("option");
                    option.value = tonality;
                    option.text = tonality;
                    select.appendChild(option);
                }
                // Вибрати її
                select.value = tonality;
                // Викликати change для можливих прив'язок
                select.dispatchEvent(new Event("change", { bubbles: true }));
            } catch (e) {
                console.error("DetectTonality AJAX error", e);
                alert('не вдалося визначити тональність автоматично');
            }
        });
    }
    //---------------------------
    // ПАРАЛЕЛЬНА ТОНАЛЬНІСТЬ
    //---------------------------
    var parallelBtn = document.getElementById("paralellTonality");
    if (parallelBtn) {
        parallelBtn.addEventListener("click", async function (e) {
            e.preventDefault();
            console.log("Parallel Tonality button clicked");

            let idInput = document.querySelector("input[name='Melody.ID']");
            let id = idInput ? idInput.value : null;
            if (!id) {
                let url = new URL(window.location.href);
                id = url.searchParams.get("id");
            }
            if (!id) {
                console.warn("Melody ID not found");
                return;
            }

            try {
                const resp = await fetch(`/Melodies/Edit?handler=ParallelTonality&id=${encodeURIComponent(id)}`);
                const data = await resp.json();
                if (!data.ok) {
                    if (data.error === "no_tonality") {
                        alert("тональність не визначена");
                    }
                    return;
                }
                const tonality = data.tonality;
                console.log("Parallel tonality:", tonality);

                const select = document.querySelector("select[name='Melody.Tonality']");
                if (!select) return;

                let option = Array.from(select.options).find(o => (o.value || o.text).toLowerCase() === tonality.toLowerCase());
                if (!option) {
                    option = document.createElement("option");
                    option.value = tonality;
                    option.textContent = tonality;
                    select.appendChild(option);
                }
                select.value = tonality;
                select.dispatchEvent(new Event("change", { bubbles: true }));
            } catch (err) {
                console.error("ParallelTonality error", err);
            }
        });
    }
    //---------------------------
    // ТРАНСЛІТ
    //---------------------------
    var translitBtn = document.getElementById("translitbtn")
    if (translitBtn) {
        translitBtn.addEventListener("click", async function (e) {
            e.preventDefault();
			console.log("melody-edit: translit starts")

            try {
                var ukrname = document.getElementById("ukrnameInput")
                var enname = document.getElementById("ukrnameInput")
                if (ukrname) {
                    var name = ukrname.value;
                    var enname = translit(name)
                    console.log(`translit ${name} to ${enname}`)
                    ennameInput.value = enname;

                }
                else {
                    console.warn("no ukr name found")
                }
            }
            catch (err) {
                console.error("failed to transliterate melody name:", err)
            }

		})
    }
    else { console.warn("no translitbtn found") }
});

function translit(text) {

    console.log("Start translit:", text);
    return transliterateToEn(text);

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


}

