document.addEventListener("DOMContentLoaded", function () {
    var tempoChange = document.getElementById("tempoChange");
    var tempoChangeHidden = document.getElementById("tempoChangeHidden");
    tempoChange.addEventListener("input", function () {
        tempoChangeHidden.value = true;
        console.log("tempoChangeHidden.value = true");
    });
});


