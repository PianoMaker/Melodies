
document.addEventListener("DOMContentLoaded", function () {
        let titleInput = document.querySelector("input[name='Melody.Title']");
        let warningField = document.getElementById("warning");

        titleInput.addEventListener("input", async function () {
            let title = titleInput.value.trim();
            console.log("search for duplicates");
            if (title.length > 4) {
                try {
                    let response = await fetch(`/Melodies/Create?handler=CheckTitle&title=${encodeURIComponent(title)}`);
                    let result = await response.json();
                    console.log(result.exists);
                    if (result.exists) {
                        warningField.innerHTML = "❗ Можливо, така пісня вже існує!";
                        warningField.style.color = "red";
                    } else {
                        warningField.innerHTML = "";
                    }
                } catch (error) {
                    console.error("Помилка перевірки заголовка:", error);
                }
            } else {
                warningField.innerHTML = "";
            }
        });
    });


document.addEventListener("DOMContentLoaded", function () {

    console.log("if author eligible");
    let selectAuthor = document.getElementById("selectAuthor");
    let createAuthorBtn = document.getElementById("createAuthorBtn");
    //let createAuthorDiv = document.getElementById("createAuthorDiv");
    let submitMelodyBtn = document.getElementById("submitMelodyBtn");
    /*let selectAuthorLabel = document.getElementById("selectAuthorLabel");*/
    //let submitAuthorBtn = document.getElementById("submitAuthorBtn")
    //let inputAuthor = document.getElementById("inputAuthor");

    //if (!selectAuthor || !createAuthorBtn || !submitAuthorBtn  || !submitMelodyBtn || !createAuthorDiv|| !inputAuthor) {
    //    console.error("Один або кілька елементів не знайдено!");
    //    return;
    //}

    function updateButtons() {
        console.log("starting update buttons");
        console.log("Значення selectAuthor:", selectAuthor.value);
        const selectedOption = selectAuthor.options[selectAuthor.selectedIndex];
        /*        console.log("Значення inputAuthor:", inputAuthor.value);*/
        if (selectedOption && selectedOption.text === '(невідомо)') {
            //createAuthorBtn.style.setProperty('display', 'block');
            //createAuthorDiv.style.setProperty('display', 'block');
            //submitAuthorBtn.style.setProperty('display', 'block');
            submitMelodyBtn.style.display = 'none';
        } else {
            //createAuthorDiv.style.setProperty('display', 'none');
            //createAuthorBtn.style.setProperty('display', 'none');
            //submitAuthorBtn.style.setProperty('display', 'none');
            submitMelodyBtn.style.display = 'inline-block';
        } 
        let inputAuthor = document.getElementById("inputAuthor");
        if (inputAuthor)
            hideSelectBtn();
    }

    function hideSelectBtn() {
        console.log("hide select buttons");
        selectAuthor.style.display = 'none';
        createAuthorBtn.style.display = 'none';
        submitMelodyBtn.style.display = 'none';
        selectAuthorLabel.style.display = 'none';
    }

    updateButtons();

    selectAuthor.addEventListener("change", updateButtons);

    createAuthorBtn.addEventListener("click", hideSelectBtn);

    /*    inputAuthor.addEventListener("input", updateButtons);*/
})