const COPYRIGHT_RENDERING_BARS = 16;

document.addEventListener('DOMContentLoaded', function () {
	renderAdaptive();
	let resizeTimer;
	window.addEventListener('resize', () => {
		clearTimeout(resizeTimer);
		resizeTimer = setTimeout(renderAdaptive, 300);
	});
	// Keyboard shortcut: E to edit
	window.addEventListener('keypress', (e) => {
		try {			
			const active = document.activeElement;
			if (active && (active.tagName === 'INPUT' || active.tagName === 'TEXTAREA' || active.isContentEditable)) return;

			if (e.key === 'e' || e.key === 'E') {
				const editLink = Array.from(document.querySelectorAll('a[href]'))
					.find(a => a.href && a.href.indexOf('/Melodies/Edit') !== -1);
				if (editLink) {
					window.location.href = editLink.href;
				}
			}
		} catch (ex) {
			console.warn('keypress handler failed', ex);
		}
	});
});


function calcGeneralWidth() {
	const w = window.innerWidth || 1200;
	return Math.min(1200, Math.max(420, w - 80));
}

function ensureNotationHeightFitsAll() {
	try {
		const container = document.getElementById('notation');
		if (!container) return;
		const svg = container.querySelector('svg');
		if (!svg) return;
		const vb = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
		const vbHeight = vb ? vb.height : null;
		const bbox = svg.getBBox ? svg.getBBox() : null;
		const contentHeight = (bbox && bbox.height) ? bbox.height + 10 : (vbHeight || 0);

		// Use actual container height values rather than only computed style.
		const containerHeight = container.clientHeight || container.offsetHeight || 0;
		const scrollHeight = container.scrollHeight || 0;

		// If the content is taller than the container, grow it to fit.
		const currentVisible = Math.max(containerHeight, scrollHeight);
		if (contentHeight > 0 && contentHeight > currentVisible) {
			container.style.height = Math.ceil(contentHeight) + 'px';
		}
	} catch (e) { console.warn('ensureNotationHeightFitsAll failed', e); }
}

async function renderAdaptive() {
	const notationEl = document.getElementById('notation');
	if (!notationEl) return;

	const midiUrl = notationEl.dataset.midiUrl;
	const isPublicDomain = (notationEl.dataset.publicDomain === 'true');
	const maxBarsToRender = isPublicDomain ? 1000 : COPYRIGHT_RENDERING_BARS;

	if (!midiUrl || typeof renderMidiFromUrl !== 'function') return;

	const width = calcGeneralWidth();
	await renderMidiFromUrl(
		midiUrl, maxBarsToRender,
		'notation', 'comments',
		width, 150
	);

	if (isPublicDomain) {
		setTimeout(ensureNotationHeightFitsAll, 0);
	}
}


// Expose for debugging/manual re-render
window.MelodyDetails = Object.assign(window.MelodyDetails || {}, {
	renderAdaptive,
	ensureNotationHeightFitsAll
});

