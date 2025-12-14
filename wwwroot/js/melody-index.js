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
			setTimeout(() => { content.hidden = true; }, 300); // sync with CSS transition
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
			const useShort = window.innerWidth < 700;
			const desired = useShort ? short : full;
			if (el.innerText.trim() !== desired) {
				el.innerText = desired;
			}
		});
	}

	updateSongTitles();
	window.addEventListener('resize', updateSongTitles);

	// --- Letters hamburger expand behavior (integrated) ---
	const lettersBtn = document.getElementById('lettersToggleBtn');
	const lettersPanel = document.getElementById('lettersbutton');
	const container = lettersBtn ? (lettersBtn.closest ? lettersBtn.closest('.infocontainer') : (function(){ let p = lettersBtn.parentElement; while(p && !p.classList.contains('infocontainer')) p = p.parentElement; return p; })()) : null;

	if (lettersBtn && lettersPanel) {
		lettersBtn.addEventListener('click', function (ev) {
			const opened = lettersPanel.classList.toggle('open');
			lettersBtn.setAttribute('aria-expanded', opened ? 'true' : 'false');

			// toggle expanded class on the surrounding infocontainer
			if (container) container.classList.toggle('letters-expanded', opened);

			// focus first letter when opened
			if (opened) {
				const first = lettersPanel.querySelector('.letter');
				if (first && typeof first.focus === 'function') first.focus();

				// ensure container scrolls into view on smaller viewports
				setTimeout(() => {
					try { container && container.scrollIntoView({ behavior: 'smooth', block: 'start' }); } catch (e) {}
				}, 120);
			}
		});

		// Close on resize to desktop to avoid stale open state
		window.addEventListener('resize', function () {
			if (window.innerWidth > 768 && lettersPanel.classList.contains('open')) {
				lettersPanel.classList.remove('open');
				lettersBtn.setAttribute('aria-expanded', 'false');
				if (container) container.classList.remove('letters-expanded');
			}
		});
	}
});
