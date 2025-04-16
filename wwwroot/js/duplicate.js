
document.addEventListener("DOMContentLoaded", function () {
    let titleInput = document.querySelector("input[name='Melody.Title']");
    let warningField = document.getElementById("warning");

    titleInput.addEventListener("input", async function () {
        let title = titleInput.value.trim();
        console.log("duplicate.js is running");
        if (title.length > 4) {
            try {
                //надсилає запит і отримує відповідь
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
