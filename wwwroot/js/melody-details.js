document.addEventListener('DOMContentLoaded', function () {
	renderAdaptive();
	let resizeTimer;
	window.addEventListener('resize', () => {
		clearTimeout(resizeTimer);
		resizeTimer = setTimeout(renderAdaptive,300);
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

	// Ensure shared audioHelpers context is resumed on first user gesture
	function initSharedAudioOnGesture() {
		if (typeof window.ensureAudioContext !== 'function') return;
		const resumeHandler = () => {
			try {
				const ctx = window.ensureAudioContext();
				if (ctx && ctx.state === 'suspended') {
					ctx.resume().then(() => console.log('Shared AudioContext resumed')).catch(e => console.warn(e));
				}
			} catch (e) { console.warn('initSharedAudioOnGesture failed', e); }
			window.removeEventListener('pointerdown', resumeHandler, true);
			window.removeEventListener('keydown', resumeHandler, true);
		};
		window.addEventListener('pointerdown', resumeHandler, { capture: true, once: true });
		window.addEventListener('keydown', resumeHandler, { capture: true, once: true });
	}
	initSharedAudioOnGesture();

	// Hover playback: delegate to audioHelpers if available
	window.addEventListener('mouseover', (e) => {
		try {
			const target = e.target.closest('g.vf-stavenote');
			if (!target) return;
			target.classList.add('hover');

			// Prefer using audioHelpers functions exposed on window
			let midiAttr = target.getAttribute('data-midi');
			let played = false;

			if (midiAttr !== null) {
				const midi = Number.parseInt(midiAttr,10);
				if (Number.isFinite(midi) && typeof window.midiToFrequency === 'function' && typeof window.playTone === 'function') {
					const freq = window.midiToFrequency(midi);
					if (freq) {
						window.playTone(freq,1.2, 'sine',0.82);
						played = true;
					}
				}
			}

			if (!played) {
				// try data-key and play via playSlashKey or playNoteFromKey
				const key = target.getAttribute('data-key') || target.getAttribute('data-key');
				if (key) {
					// Prefer playSlashKey if available (handles c/5 etc)
					if (typeof window.playSlashKey === 'function') {
						try { window.playSlashKey(key,1.2); played = true; } catch (e) { /* ignore */ }
					}
					// Fallback: try playNoteFromKey which expects tokens like c, cis, a4 etc
					if (!played && typeof window.playNoteFromKey === 'function') {
						try { window.playNoteFromKey(key); played = true; } catch (e) { /* ignore */ }
					}
				}
			}

			if (!played) {
				// last resort: if audioHelpers.playTone absent, keep previous behaviour but attempt to use midiToFrequency if exposed
				try {
					if (midiAttr !== null && typeof window.midiToFrequency === 'function' && typeof window.playTone === 'undefined') {
						const midi = Number.parseInt(midiAttr,10);
						const freq = window.midiToFrequency(midi);
						if (freq && typeof window.playTone === 'function') window.playTone(freq,1.2);
					}
				} catch (e) { /* ignore */ }
			}

		} catch (ex) {
			console.warn('mouseover handler failed', ex);
		}
	});

	// On mouseout just remove hover class; playback is short and will stop itself
	window.addEventListener('mouseout', (e) => {
		try {
			const target = e.target.closest('g.vf-stavenote');
			if (!target) return;
			target.classList.remove('hover');
		} catch (ex) {
			console.warn('mouseout handler failed', ex);
		}
	});

});

function serializeAttributes(el) {
	const attrs = {};
	for (let i =0; i < el.attributes.length; i++) {
		const a = el.attributes[i];
		attrs[a.name] = a.value;
	}
	return attrs;
}


function calcGeneralWidth() {
	const w = window.innerWidth ||1200;
	return Math.min(1200, Math.max(420, w -80));
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
		const contentHeight = (bbox && bbox.height) ? bbox.height +10 : (vbHeight ||0);

		// Use actual container height values rather than only computed style.
		const containerHeight = container.clientHeight || container.offsetHeight ||0;
		const scrollHeight = container.scrollHeight ||0;

		// If the content is taller than the container, grow it to fit.
		const currentVisible = Math.max(containerHeight, scrollHeight);
		if (contentHeight >0 && contentHeight > currentVisible) {
			container.style.height = Math.ceil(contentHeight) + 'px';
		}
	} catch (e) { console.warn('ensureNotationHeightFitsAll failed', e); }
}

async function renderAdaptive() {
	const notationEl = document.getElementById('notation');
	if (!notationEl) return;

	const midiUrl = notationEl.dataset.midiUrl;
	const isPublicDomain = (notationEl.dataset.publicDomain === 'true');
	const maxBarsToRender = isPublicDomain ?1000 :8;

	if (!midiUrl || typeof renderMidiFromUrl !== 'function') return;

	const width = calcGeneralWidth();
	await renderMidiFromUrl(
		midiUrl, maxBarsToRender,
		'notation', 'comments',
		width,150
	);

	if (isPublicDomain) {
		setTimeout(ensureNotationHeightFitsAll,0);
	}
}

// Backwards-compatibility: expose legacy misspelled global in case other scripts still call it
if (typeof window !== 'undefined') {
	// correct export
	window.MelodyDetails = Object.assign(window.MelodyDetails || {}, {
		renderAdaptive,
		ensureNotationHeightFitsAll
	});
	// legacy misspelling used previously: 'enessureNotationHeightFitsAll'
	try {
		window.enessureNotationHeightFitsAll = ensureNotationHeightFitsAll;
		window.MelodyDetails.enessureNotationHeightFitsAll = ensureNotationHeightFitsAll;
	} catch (e) { /* ignore */ }
}

