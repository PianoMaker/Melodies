function toggleContent(elementId, buttonId) {
    var content = document.getElementById(elementId);
    var button = document.getElementById(buttonId);

    if (content.style.display === "none") {
        content.style.display = "block";
        button.textContent = "-";  // Змінює знак на "-"
    } else {
        content.style.display = "none";
        button.textContent = "+";  // Змінює знак на "+"
    }
}


