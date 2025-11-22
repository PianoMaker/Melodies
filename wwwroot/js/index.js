// Combined control strip + responsive song title logic (renamed from melodies-index.js)
// Loaded only on Melodies/Index page

window.addEventListener('DOMContentLoaded', () => {
 console.log('index.js (combined) loaded');

 // --- Control strip logic ---
 const strip = document.getElementById('controlStrip');
 const toggle = document.getElementById('controlToggle');
 const content = document.getElementById('controlContent');

 function setExpanded(expanded) {
 if (!strip || !toggle || !content) return;
 strip.classList.toggle('expanded', expanded);
 toggle.setAttribute('aria-expanded', expanded ? 'true' : 'false');
 strip.setAttribute('aria-expanded', expanded ? 'true' : 'false');
 console.log(`Control strip is now ${expanded ? 'expanded' : 'collapsed'}`);

 if (expanded) {
 content.hidden = false;
 content.setAttribute('aria-hidden', 'false');
 toggle.textContent = '\u2212'; // minus
 } else {
 toggle.textContent = '+';
 content.setAttribute('aria-hidden', 'true');
 setTimeout(() => { content.hidden = true; },300); // sync with CSS transition
 }
 }

 if (strip && toggle && content) {
 // default collapsed
 setExpanded(false);
 toggle.addEventListener('click', e => {
 e.preventDefault();
 setExpanded(!strip.classList.contains('expanded'));
 });
 }

 // --- Responsive song title shortening ---
 function updateSongTitles() {
 document.querySelectorAll('.song-title').forEach(el => {
 const full = el.getAttribute('data-full');
 const short = el.getAttribute('data-short');
 if (!full || !short) return;
 const useShort = window.innerWidth <700;
 const desired = useShort ? short : full;
 if (el.innerText.trim() !== desired) {
 el.innerText = desired;
 }
 });
 }

 updateSongTitles();
 window.addEventListener('resize', updateSongTitles);
});
