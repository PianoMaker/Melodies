
async function checkBeforeSubmit() {
    const key = document.getElementById("melodyKey").value;
    const response = await fetch(`/Melodies/CheckFile?key=${encodeURIComponent(key)}`);
    const fileExists = await response.json();

    if (fileExists) {
        const confirmOverwrite = confirm("Файл вже існує. Перезаписати?");
        if (!confirmOverwrite) {
            return;
        }
    }

    // якщо не існує, або користувач погодився – надсилаємо форму
    document.getElementById("melodyForm").submit();
}
