document.addEventListener("DOMContentLoaded", function () {
    var tempoChange = document.getElementById("tempoChange");
    var tempoChangeHidden = document.getElementById("tempoChangeHidden");
    if (tempoChange && tempoChangeHidden) {
        tempoChange.addEventListener("input", function () {
            tempoChangeHidden.value = true;
            console.log("tempoChangeHidden.value = true");
        });
    }
});

document.addEventListener("DOMContentLoaded", function () {
    var getTonalityButton = document.getElementById("getTonality");
    if (!getTonalityButton) return;

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
});
