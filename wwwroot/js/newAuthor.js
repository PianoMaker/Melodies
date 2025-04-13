
function updateButtons() {
    
    let selectAuthor = document.getElementById("selectAuthor");        

    if (!selectAuthor) {
        console.log("selectAuthor error");
        return;
    }

    let submitMelodyBtn = document.getElementById("submitMelodyBtn");   
    let createAuthorBtn = document.getElementById("createAuthorBtn");//кнопка додати автора  
        
    const selectedOption = selectAuthor.options[selectAuthor.selectedIndex];
    
    if (selectedOption && selectedOption.text === '(невідомо)') {
        console.log("Значення selectOption:", selectedOption.text);
        submitMelodyBtn.style.display = 'none';
        createAuthorBtn.style.display = 'inline-block';
        
    } else if (selectedOption === undefined) {
        console.log("Значення selectOption is undefined");
        submitMelodyBtn.style.display = 'none';
    }
    else {
        console.log("Значення selectOption:", selectedOption);
        createAuthorBtn.style.display = 'none';
        showSubmitBtn();    
    }    
   
}

function searchAuthor() {
    console.log("try to search author");
    let inputAuthor = document.getElementById("inputAuthor");
    let warningField = document.getElementById("authorWarning");
    if (!warningField) 
        console.log("no warning Field!");

    if (inputAuthor) {
        hideSelectBtn();
        inputAuthor.addEventListener("input", async function() {
            let author = inputAuthor.value.trim();
            console.log(`search author for duplicates ${author}`);
            if (author.length > 4) {
                try {
                    let response = await fetch(`/Melodies/Create?handler=CheckAuthor&author=${encodeURIComponent(author)}`);
                    let result = await response.json();
                    console.log(result.exists);
                    if (result.exists) {
                        warningField.innerHTML = "❗ Можливо, такий автор вже існує!";
                        warningField.style.color = "red";
                    } else {
                        warningField.innerHTML = "";
                    }
                } catch (error) {
                    console.error("Помилка перевірки автора:", error);
                }
            } else {
                warningField.innerHTML = "";
                warningField.style.color = "";
            }
        });
        showSubmitBtn();

    }
}

function showSubmitBtn() {

    console.log("show submit Btn");
    let submitMelodyBtn = document.getElementById("submitMelodyBtn");  
    submitMelodyBtn.style.display = 'inline-block';
}

function hideSelectBtn() {
    console.log("Create button pressed => hide select buttons");

    let selectAuthor = document.getElementById("selectAuthor");
    let createAuthorBtn = document.getElementById("createAuthorBtn");
    let submitMelodyBtn = document.getElementById("submitMelodyBtn");
    let selectAuthorLabel = document.getElementById("selectAuthorLabel");

    if (selectAuthor) selectAuthor.style.display = 'none';
    if (createAuthorBtn) createAuthorBtn.style.display = 'none';
    if (submitMelodyBtn) submitMelodyBtn.style.display = 'none';
    if (selectAuthorLabel) selectAuthorLabel.style.display = 'none';    
}


document.addEventListener("DOMContentLoaded", function () {
    console.log("newAuthor.js is running");

    updateButtons();
    searchAuthor();

    let selectAuthor = document.getElementById("selectAuthor");//поле обрати автора
    let createAuthorBtn = document.getElementById("createAuthorBtn");//кнопка додати автора    

    if (selectAuthor) selectAuthor.addEventListener("change", updateButtons);
    if (createAuthorBtn) createAuthorBtn.addEventListener("click", hideSelectBtn);
});