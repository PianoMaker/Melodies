document.addEventListener("DOMContentLoaded", function () {
    function updateSongTitles() {
        //console.log("Updating song titles based on window width:", window.innerWidth);
        document.querySelectorAll('.song-title').forEach(el => {
            var full = el.getAttribute('data-full');
            var short = el.getAttribute('data-short');
           // console.log('Current text:', el.innerText.trim(), '| full:', full, '| short:', short);
            if (!full || !short) return;
            if (window.innerWidth < 700) {
                if (el.innerText.trim() !== short) {
                    console.log('Заміна на коротку назву');
                    el.innerText = short;
                }
            } else {
                if (el.innerText.trim() !== full) {
                    console.log('Заміна на повну назву');
                    el.innerText = full;
                }
            }
        });
    }
    updateSongTitles();
    window.addEventListener('resize', updateSongTitles);
    //console.log('--- song-title elements ---');
    //document.querySelectorAll('.song-title').forEach(el => console.log(el.innerText));
});
