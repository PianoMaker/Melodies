document.addEventListener('DOMContentLoaded', function() {
    // Підсвітка тривалостей (duration buttons)
    const regularDurationButtons = document.querySelectorAll('.durationbutton');
    const dotButton = document.getElementById('dotbutton');

    // Regular duration buttons except dot
    regularDurationButtons.forEach(button => {
        if (button !== dotButton) {
            button.addEventListener('click', function() {
                regularDurationButtons.forEach(btn => {
                    if (btn !== dotButton) {
                        btn.classList.remove('highlight');
                    }
                });
                this.classList.add('highlight');
            });
        }
    });

    // Початково підсвітити четвертну
    const quarterNoteButton = document.querySelector('.durationbutton:nth-child(3)');
    if (quarterNoteButton) quarterNoteButton.classList.add('highlight');

    // Dot button handling using the same highlight class
    if (dotButton) {
        dotButton.addEventListener('click', function(e) {
            // prevent any other dotbutton listeners from toggling again
            e.stopImmediatePropagation();
            this.classList.toggle('highlight');
        });
    }
});
