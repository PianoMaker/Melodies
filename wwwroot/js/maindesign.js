
document.getElementById("singerellaCheckbox").addEventListener("change", function () {
    console.log("changing picture");
    document.getElementById("myForm").submit(); // Відправка форми при зміні стану чекбокса
});


document.addEventListener("DOMContentLoaded", function () {
    var navbar = document.getElementById("navbarstrip");
    if (navbar) {
        console.log("changing navbar");
        navbar.style.backgroundColor = "red"; // змінюємо фон
        navbar.style.borderBottom = "5px solid black"; // додаємо рамку
    }
});
