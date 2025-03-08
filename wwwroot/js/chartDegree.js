window.addEventListener("load", function () {
    var degreesCtx = document.getElementById("degreesChart").getContext("2d");
    var weightCtx = document.getElementById("weightChart").getContext("2d");


    var degreesChart = new Chart(degreesCtx, {
        type: 'bar',
        data: {
            labels: window.chartLabels,
            datasets: [{
                label: 'Частка у мелодії (без урахування тривалості)',
                data: window.chartValues,
                backgroundColor: [
                    'rgba(255, 99, 132, 0.2)',
                    'rgba(54, 162, 235, 0.2)',
                    'rgba(255, 206, 86, 0.2)',
                    'rgba(75, 192, 192, 0.2)'
                ],
                borderColor: [
                    'rgba(255, 99, 132, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)',
                    'rgba(75, 192, 192, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });

    var weightChart = new Chart(weightCtx, {
        type: 'bar',
        data: {
            labels: window.chartLabels,
            datasets: [{
                label: 'Частка у мелодії (без урахування тривалості)',
                data: window.chartWeightValues,
                backgroundColor: [
                    'rgba(255, 99, 132, 0.2)',
                    'rgba(54, 162, 235, 0.2)',
                    'rgba(255, 206, 86, 0.2)',
                    'rgba(75, 192, 192, 0.2)'
                ],
                borderColor: [
                    'rgba(255, 99, 132, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)',
                    'rgba(75, 192, 192, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });

    // Функція перемикання графіків
    function toggleCharts() {
        var degreesCanvas = document.getElementById("degreesChart");
        var weightCanvas = document.getElementById("weightChart");



        console.log("Width of degreesChart:", degreesCanvas.width);
        console.log("Height of degreesChart:", degreesCanvas.height);
        console.log("Width of weightChart:", weightCanvas.width);
        console.log("Height of weightChart:", weightCanvas.height);
        if (document.getElementById("degreesRadio").checked) {

            degreesCanvas.style.display = "block";
            weightCanvas.style.display = "none";
        } else {
            degreesCanvas.style.display = "none";
            weightCanvas.style.display = "block";
        }
    }

    // Додаємо обробники подій для радіокнопок
    document.getElementById("degreesRadio").addEventListener("change", toggleCharts);
    document.getElementById("weightRadio").addEventListener("change", toggleCharts);
});
