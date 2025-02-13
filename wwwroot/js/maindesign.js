
document.getElementById("singerellaCheckbox").addEventListener("change", function () {
    console.log("changing picture");
    document.getElementById("myForm").submit(); // Відправка форми при зміні стану чекбокса
});


document.addEventListener("DOMContentLoaded", function () {
    var navbar = document.getElementById("navbarstrip");
    var checkbox = document.getElementById("singerellaCheckbox");
    if (navbar) {

        if (checkbox.checked) {
            console.log("changing navbar");
            navbar.style.backgroundColor = "#54423a"; // змінюємо фон
            navbar.classList.remove("bg-sky");
            navbar.style.borderBottom = "5px solid black"; // додаємо рамку
        }
        else {
            console.log("Reverting navbar style");
            navbar.style.backgroundColor = "";
            navbar.style.borderBottom = "";
            navbar.classList.add("bg-sky");
            
        }
    }

});
