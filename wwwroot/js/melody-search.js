
document.addEventListener("DOMContentLoaded", function () {

	// Bind LCS gap display and enable/disable based on algorithm
	const gap = document.getElementById('lcsGap');
	const gapVal = document.getElementById('lcsGapVal');
	const algSub = document.getElementById('algSubstring');
	const algSeq = document.getElementById('algSubsequence');
	const BARSTODISPLAY = 12;


	function calcAdaptiveWidth() {
		const w = window.innerWidth || 1200;
		console.debug(`calcAdaptiveWidth: GW = ${w}`)
		return Math.min(1200, Math.max(320, w - 40)); // 40px для відступів
	}

	//function calcAdaptiveHeight() {
	//	const baseHeight = 150;
	//	const screenFactor = (window.innerWidth < 768) ? 0.8 : 1;
	//	const height = Math.floor(baseHeight * screenFactor);
	//	console.debug(`calcAdaptiveHeight: height = ${height}`)
	//	return height;
	//}

	//function calcAdaptiveBarWidth() {
	//	const containerWidth = calcAdaptiveWidth();
	//	const minBarWidth = 180;
	//	const maxBarWidth = 250;
	//	// Адаптивна ширина такту залежно від контейнера
	//	return Math.min(maxBarWidth, Math.max(minBarWidth, containerWidth / 5));
	//}

	//--------------------
	// Оновити рендеринг для всіх нотних блоків на сторінці
	//--------------------
	//async function rerenderAllNotations() {
	//	const notationElements = document.querySelectorAll('[id^="notation_match_"]');
	//	console.debug("FOO: renderAllNotations - melody-search.js")

	//	for (const el of notationElements) {
	//		const midiUrl = el.dataset.midiUrl;
	//		const startPosition = parseInt(el.dataset.startPosition) || 0;
	//		const commentsId = el.dataset.commentsId;

	//		if (midiUrl && typeof renderMidiSegmentFromUrl === 'function') {
	//			const width = calcAdaptiveWidth();
	//			const height = calcAdaptiveHeight();
	//			const barWidth = calcAdaptiveBarWidth();

	//			console.debug(`Rendering notation for ${el.id} with width=${width}, height=${height}, barWidth=${barWidth}`);
	//			try {
	//				await renderMidiSegmentFromUrl(
	//					midiUrl,
	//					startPosition,
	//					el.id,
	//					commentsId,
	//					width,    // GENERALWIDTH
	//					height,   // HEIGHT  
	//					20,       // TOPPADDING
	//					barWidth, // BARWIDTH
	//					60,       // CLEFZONE
	//					10,       // Xmargin
	//					12        // barsToRender
	//				);
	//			} catch (e) {
	//				console.warn(`Failed to render notation for ${el.id}:`, e);
	//			}
	//		}
	//	}
	//}

	////function init(){
	//  // ---- закоментовано автоматичний масовий рендер (викликає дублювання з search.js) ----
	//  // setTimeout(rerenderAllNotations, 100);

	//  // Замість автоматичного масового рендера, можна викликати вручну при потребі:
	//  // window.MelodySearch && window.MelodySearch.rerenderAllNotations && window.MelodySearch.rerenderAllNotations();

	//  // ---- закоментовано автоматичний resize-trigger, щоб не повторювати rerender ----
	//  // let resizeTimer;
	//  // window.addEventListener('resize', () => {
	//  //   clearTimeout(resizeTimer);
	//  //   resizeTimer = setTimeout(rerenderAllNotations, 300);
	//  // });
	//}

	//if (document.readyState === 'loading') {
	//  document.addEventListener('DOMContentLoaded', init);
	//} else {
	//  init();
	//}

	//// Експорт для відладки
	//window.MelodySearch = Object.assign(window.MelodySearch || {}, {
	//  calcAdaptiveWidth,
	//  calcAdaptiveHeight, 
	//  calcAdaptiveBarWidth,
	//  rerenderAllNotations
	//});



	//function attachLazyNotesHandlers() {
	//	// show-notes buttons inside placeholders
	//	document.querySelectorAll('.show-notes-btn').forEach(btn => {
	//		btn.addEventListener('click', (ev) => {
	//			ev.preventDefault();
	//			const p = btn.closest('.noteslist');
	//			renderNotesForElement(p);
	//		});
	//	});

	//	// clicking a melody block should also render its notes (and allow user to click other melodies)
	//	document.querySelectorAll('.melody-block').forEach(block => {
	//		block.addEventListener('click', (ev) => {
	//			// avoid rendering when clicking buttons/controls inside the block
	//			if (ev.target.matches('button, a, input, select, textarea')) return;
	//			const p = block.querySelector('.noteslist');
	//			if (p) renderNotesForElement(p);
	//		});
	//	});

	//	// next/previous navigation: render target melody's notes when user navigates
	//	document.querySelectorAll('.song-nav').forEach(navBtn => {
	//		navBtn.addEventListener('click', (ev) => {
	//			ev.preventDefault();
	//			// find current melody-block
	//			const current = navBtn.closest('.melody-block');
	//			if (!current) return;
	//			// choose direction by class (next or prev)
	//			const isNext = navBtn.classList.contains('next');
	//			// find sibling melody-block in the chosen direction
	//			let target = current;
	//			while (true) {
	//				target = isNext ? target.nextElementSibling : target.previousElementSibling;
	//				if (!target) break;
	//				if (target.classList && target.classList.contains('melody-block')) break;
	//			}
	//			if (target && target.classList && target.classList.contains('melody-block')) {
	//				const targetNotes = target.querySelector('.noteslist');
	//				if (targetNotes) renderNotesForElement(targetNotes);
	//			}
	//		});
	//	});
	//}



	// Responsive: shorten song-title on narrow screens
	function adjustSongTitles() {
		const breakpoint = 720; // px
		const useShort = window.innerWidth <= breakpoint;
		document.querySelectorAll('.song-title').forEach(el => {
			const full = el.dataset.full ?? el.textContent ?? '';
			const short = el.dataset.short ?? full;
			el.textContent = useShort ? short : full;
		});
	}

	// debounce helper
	function debounce(fn, wait) {
		let t = null;
		return function () {
			clearTimeout(t);
			t = setTimeout(() => fn.apply(this, arguments), wait);
		};
	}

	// Допоміжна: рендер нотації для конкретного контейнера melodyN
	function renderNotationForMelodyContainer(melodyContainer) {
		if (!melodyContainer) return;

		// шукаємо notation-контейнер усередині блоку мелодії
		const notationEl = melodyContainer.querySelector("div[id^='notation_match_']");
		if (!notationEl) return;

		// уникаємо повторного рендеру
		if (notationEl.dataset.rendered === "1") return;

		const midiUrl = notationEl.dataset.midiUrl;
		const startPos = parseInt(notationEl.dataset.startPosition ?? "0", 10) || 0;
		const commentsId = notationEl.dataset.commentsId;
		const commentsElId = commentsId || (function () {
			const id = notationEl.id || "";
			return id.replace("notation_match_", "comments_match_");
		})();

		if (!midiUrl || typeof window.renderMidiSegmentFromUrl !== "function") {
			console.warn("Notation render skipped: midiUrl or renderer is missing");
			return;
		}

		try {
			console.log(`search.js Rendering notation for ${notationEl.id} from ${midiUrl} starting at ${startPos}`);
			// Use the same HEIGHT/TOPPADDING used by Details so scale matches
			window.renderMidiSegmentFromUrl(
				midiUrl,
				startPos,
				notationEl.id,
				commentsElId,
				1200,  // GENERALWIDTH (keep as before)
				150,  // HEIGHT 
				20,   // TOPPADDING -> match Details (was 10)
				250,  // BARWIDTH
				60,   // CLEFZONE
				10,   // Xmargin
				BARSTODISPLAY    // barsToRender
			);
			notationEl.dataset.rendered = "1";
		} catch (e) {
			console.warn("renderMidiSegmentFromUrl failed", e);
		}
	}

	// helper: select melody by index (shared by row click and nav buttons)
	function selectMelodyByIndex(index) {
		if (!melodies || melodies.length === 0) return;
		index = Math.max(0, Math.min(index, melodies.length - 1));

		// hide all
		melodies.forEach(m => m.style.display = 'none');
		// show selected
		const selected = document.getElementById(`melody${index}`);
		if (selected) {
			selected.style.display = 'block';
			renderNotationForMelodyContainer(selected);
		}

		// update row highlight using CSS class to override td backgrounds
		rows.forEach(r => r.classList.remove('selected-row'));
		if (rows[index]) rows[index].classList.add('selected-row');
	}


	function updateGapEnabled() {
		const enabled = algSeq && algSeq.checked;
		if (gap) gap.disabled = !enabled;
		const labelWrap = gap ? gap.nextElementSibling : null; // span with value
		if (labelWrap) {
			if (enabled) labelWrap.classList.remove('text-muted');
			else labelWrap.classList.add('text-muted');
		}
	}

	// СКОРОЧЕННЯ НАЗВ ПІСЕНЬ

	adjustSongTitles();
	window.addEventListener('resize', debounce(adjustSongTitles, 120));

	// ПРИХОВАННЯ/РЕНДЕР НОТАЦІЙ ПО ЗАПИТУ	
	let rows = document.querySelectorAll("table tbody tr");
	let melodies = document.querySelectorAll("div[id^='melody']");
	melodies.forEach(melody => melody.style.display = "none");
	if (melodies.length > 0) {
		selectMelodyByIndex(0);
	}
	// Додаємо обробник подій для кожного рядка
	rows.forEach((row, index) => {
		row.addEventListener("click", function () {
			console.log(`displaying melody ${index}`);
			selectMelodyByIndex(index);
		});
	});

	// Рендер знайдених мелодій по кліку на них
	const container = document.getElementById('matchedMelodiesContainer');
	if (container && typeof window.renderMidiFromUrl === 'function') {
		function clearAllNotations() {
			document.querySelectorAll('[id^="notation_match_"]').forEach(div => div.innerHTML = '');
			document.querySelectorAll('[id^="comments_match_"]').forEach(div => div.innerHTML = '');
		}
		container.addEventListener('click', function (e) {
			const melodyBlock = e.target.closest('.melody-block');
			// navigation buttons handling: prev / next
			const navBtn = e.target.closest('.song-nav');
			if (navBtn && container.contains(navBtn)) {
				e.preventDefault();
				// find current visible index
				const currentIndex = Array.from(melodies).findIndex(m => m.style.display && m.style.display !== 'none');
				if (navBtn.classList.contains('prev')) {
					selectMelodyByIndex((currentIndex === -1 ? 0 : currentIndex) - 1);
				} else if (navBtn.classList.contains('next')) {
					selectMelodyByIndex((currentIndex === -1 ? 0 : currentIndex) + 1);
				}
				return;
			}

			if (!melodyBlock || !container.contains(melodyBlock)) return;
			const notationDiv = melodyBlock.querySelector('[id^="notation_match_"]');
			if (!notationDiv) return;
			const midiUrl = notationDiv.getAttribute('data-midi-url');
			const commentsId = notationDiv.getAttribute('data-comments-id');
			if (!midiUrl) return;
			try {
				clearAllNotations();
				window.renderMidiFromUrl(midiUrl, 12, notationDiv.id, commentsId);
				notationDiv.scrollIntoView({ behavior: 'smooth', block: 'center' });
			} catch (e) {
				console.warn('renderMidiFromUrl failed', e);
			}
		});
	}

	// UI adjustments
	console.log("adjusting NoteSearch UI");
	const saveButton = document.getElementById("createMIDI");
	if (saveButton) {
		saveButton.textContent = "попередній перегляд";
	}



	if (gap && gapVal) {
		gap.addEventListener('input', () => gapVal.textContent = gap.value);
	}
	if (algSub) algSub.addEventListener('change', updateGapEnabled);
	if (algSeq) algSeq.addEventListener('change', updateGapEnabled);
	updateGapEnabled();

	// Hide loading and show results after DOM is ready
	const loadingEl = document.getElementById('searchLoading');
	const resultsEls = document.querySelectorAll('.notescontainer, .table');
	if (loadingEl) loadingEl.style.display = 'none';
	resultsEls.forEach(el => el.classList.remove('hide'));
});
