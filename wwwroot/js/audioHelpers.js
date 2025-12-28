//========================================
// Audio helpers and token→MIDI mapping
//========================================
// NOTE: extracted from melody-create.js to a shared module to be reused across pages.



const AudioContextClass = window.AudioContext || window.webkitAudioContext;
let __audioCtx = null;

function ensureAudioContext() {
	try {
		if (!__audioCtx) __audioCtx = new AudioContextClass();
		console.log('[createMelody] ensureAudioContext -> state:', __audioCtx.state);
		return __audioCtx;
	} catch (e) {
		console.warn('[createMelody] cannot create AudioContext', e);
		return null;
	}
}

function midiToFrequency(midi) {
	return 440 * Math.pow(2, (midi - 69) / 12);
}

// Map piano token (site tokens: c, cis, d, dis, e, f, fis, g, gis, a, b, h and apostrophes or explicit octave like a4)
// Returns MIDI number or null
function pianoTokenToMidi(token) {
	if (!token) return null;

	const apostrophes = (token.match(/['’]+/g) || []).join('').length;
	const baseToken = token.replace(/['’]+/g, '').toLowerCase();

	// site tokens mapping (matches _pianoKeys.cshtml)
	const tokenMap = {
		c: 0, cis: 1, d: 2, dis: 3, e: 4,
		f: 5, fis: 6, g: 7, gis: 8, a: 9,
		b: 10, // 'b' mapped to A# in tone.js mapping used in project
		h: 11
	};

	if (!(baseToken in tokenMap)) return null;

	const semitone = tokenMap[baseToken];
	const BASE_OCTAVE = 4; // UI labels show C1 first row; adjust if you want different default
	const octave = BASE_OCTAVE + apostrophes;
	console.debug('[createMelody][piano] final MIDI number', (octave + 1) * 12 + semitone);
	return (octave + 1) * 12 + semitone;
}

// Parse keys like "c/5", "C/4", "cis/5", "b/4" and return MIDI number (or null)
function parseSlashKeyToMidi(key) {
	try {
		if (!key || typeof key !== 'string') return null;
		const raw = key.trim().replace(/\s+/g, '').toLowerCase();
		// Accept "c/5" or "c5"
		const parts = raw.includes('/') ? raw.split('/') : raw.match(/^([a-gh][#b]?|[a-gh](is|es|s))(-?\d+)?$/i)?.slice(1);
		let notePart, octavePart;
		if (Array.isArray(parts) && parts.length >= 2) {
			// if split('/') -> parts = [note, octave]
			if (raw.includes('/')) { notePart = parts[0]; octavePart = parts[1]; }
			else { notePart = parts[0]; octavePart = parts[2] || null; }
		} else {
			// fallback: try simple split by '/'
			const sp = raw.split('/');
			notePart = sp[0];
			octavePart = sp[1];
		}

		// Normalize accidental forms: 'is' -> '#', 'es'/'s' -> 'b'
		notePart = notePart.replace(/is$/i, '#').replace(/(es|s)$/i, 'b');

		// Map letter+accidental -> semitone (C=0 .. B=11). 'h' treated as B (european).
		const map = {
			'c': 0, 'c#': 1, 'db': 1,
			'd': 2, 'd#': 3, 'eb': 3,
			'e': 4, 'e#': 5, 'fb': 4,
			'f': 5, 'f#': 6, 'gb': 6,
			'g': 7, 'g#': 8, 'ab': 8,
			'a': 9, 'a#': 10, 'bb': 10,
			'b': 11, 'h': 11, 'cb': 11
		};

		// Accept forms like "cis" (handled earlier) or "c#" etc.
		const base = notePart.toLowerCase();
		const semitone = map[base];
		if (semitone === undefined) return null;

		// parse octave (C/4 -> octave 4). If missing fallback to 4.
		let octave = 4;
		if (octavePart !== undefined && octavePart !== null && octavePart !== '') {
			const parsed = parseInt(octavePart, 10);
			if (!Number.isNaN(parsed)) octave = parsed;
		}

		// MIDI number: (octave + 1) * 12 + semitone  -> C4 == 60
		const midi = (octave + 1) * 12 + semitone;
		return Number.isFinite(midi) ? midi : null;
	} catch (e) {
		console.warn('parseSlashKeyToMidi failed for', key, e);
		return null;
	}
}

// Convenience: accept "c/5" and play via WebAudio using existing helpers
function playSlashKey(key, durationSeconds = 1.0, type = 'sine', volume = 0.82) {
	try {
		const midi = parseSlashKeyToMidi(key);
		if (midi === null) {
			console.warn('playSlashKey: could not parse key', key);
			return false;
		}
		const freq = midiToFrequency(midi);
		// ensure AudioContext resumed by playTone internal helper
		playTone(freq, durationSeconds, type, volume);
		return true;
	} catch (e) {
		console.error('playSlashKey failed', e);
		return false;
	}
}

//==========================================
// Відтворення звуків через Web Audio API
// Convert MIDI number to frequency
// Audio player for fallback audio files
// Note: AudioContext must be resumed on first user gesture (pointerdown) due to browser policies
//========================================== 
function playTone(freq, duration = 1.0, type = 'sine', volume = 0.82) {
	try {
		const ctx = ensureAudioContext();
		if (!ctx) { console.debug('[createMelody][piano] no AudioContext'); return; }
		const osc = ctx.createOscillator();
		const gain = ctx.createGain();
		osc.type = type;
		osc.frequency.value = freq;
		gain.gain.value = volume;
		osc.connect(gain);
		gain.connect(ctx.destination);
		const now = ctx.currentTime;
		osc.start(now);
		// smooth release
		gain.gain.setValueAtTime(volume, now);
		gain.gain.exponentialRampToValueAtTime(0.0001, now + duration);
		osc.stop(now + duration + 0.02);
		console.debug('[createMelody][piano] playTone', { freq: +freq.toFixed(2), duration, type });
	} catch (e) {
		console.warn('[createMelody][piano] playTone failed', e);
	}
}

// Play all notes from `pianodisplay.value` sequentially.
// Expected token format (as used elsewhere): <note><duration><optionalDot>_
// Examples: "c4_", "cis8._", "r4_" (rest), "a4'4_" (apostrophes or explicit octave supported)
function playPianodisplay() {
	try {
		if (!pianodisplay) {
			console.warn('[createMelody] playPianodisplay: pianodisplay not found');
			return;
		}
		const raw = (pianodisplay.value || '').trim();
		if (!raw) {
			console.debug('[createMelody] playPianodisplay: nothing to play');
			return;
		}

		// Ensure AudioContext ready
		const ctx = ensureAudioContext();
		if (ctx && ctx.state === 'suspended') {
			ctx.resume().catch(e => console.warn('[createMelody] resume audio failed', e));
		}

		// tokens separated by underscore; ignore empty tokens
		const tokens = raw.split('_').map(t => t.trim()).filter(t => t.length > 0);
		if (tokens.length === 0) {
			console.debug('[createMelody] no tokens parsed');
			return;
		}

		const quarterSeconds = 1.0; // quarter note = 1.0s (adjust if you want different tempo)
		let elapsedMs = 0;

		tokens.forEach(token => {
			// parse duration at end: digits possibly followed by dot
			const m = token.match(/(\d+)(\.)?$/);
			if (!m) {
				console.warn('[createMelody] cannot parse token duration:', token);
				return;
			}
			const durationNum = parseInt(m[1], 10);
			const dotted = !!m[2];
			const notePart = token.slice(0, m.index);

			// compute seconds length: durations are expressed like 1(whole),2(half),4(quarter),8(eighth)...
			let seconds = (4 / durationNum) * quarterSeconds;
			if (dotted) seconds *= 1.5;

			// schedule play or rest
			if (notePart && notePart.startsWith('r')) {
				// rest: nothing to play, just advance time
				console.debug('[createMelody] scheduling rest', { token, seconds });
			} else {
				const midi = pianoTokenToMidi(notePart);
				if (midi === null) {
					console.warn('[createMelody] unknown note token:', notePart);
				} else {
					const freq = midiToFrequency(midi);
					// schedule call to playTone at the right elapsed time
					setTimeout(() => {
						playTone(freq, seconds, 'sine', 0.82);
					}, elapsedMs);
					console.debug('[createMelody] scheduled note', { notePart, midi, freq: +freq.toFixed(2), seconds, startInMs: elapsedMs });
				}
			}

			// advance elapsed time by this note/rest duration (ms)
			elapsedMs += Math.round(seconds * 1000);
		});
	} catch (e) {
		console.error('[createMelody] playPianodisplay error', e);
	}
}

// Expose to global for easy calling from console or other scripts
window.playPianodisplay = playPianodisplay;


function playNoteFromKey(key) {
	try {
		console.debug('[createMelody][piano] playNoteFromKey token=', key);
		const midi = pianoTokenToMidi(key);
		if (midi !== null) {
			const freq = midiToFrequency(midi);
			// play exactly 1 second per requirement
			playTone(freq, 2.0, 'sine', 0.82);
			return true;
		}

		// fallback: play audio file if provided on button
		const btn = document.querySelector(`#pianoroll button[data-key="${key}"]`);
		if (btn) {
			const src = btn.getAttribute('data-audiosrc');
			if (src && audioPlayer && audioSource) {
				audioSource.src = src;
				audioPlayer.currentTime = 0;
				audioPlayer.play().catch(err => console.warn('[createMelody] audio fallback play failed', err));
				return true;
			}
		}

		console.warn('[createMelody] cannot resolve token to note:', key);
		return false;
	} catch (e) {
		console.error('[createMelody] playNoteFromKey error', e);
		return false;
	}
}

//
// Handlers: resume AudioContext on first user gesture and play on pointerdown
//
const pianoArea = document.getElementById('pianoroll');
if (pianoArea) {
	pianoArea.addEventListener('pointerdown', function () {
		try {
			const ctx = ensureAudioContext();
			if (ctx && ctx.state === 'suspended') {
				ctx.resume().then(() => console.debug('[createMelody] AudioContext resumed on pointerdown')).catch(e => console.warn(e));
			} else {
				console.debug('[createMelody] AudioContext state:', ctx ? ctx.state : 'none');
			}
		} catch (e) { /* ignore */ }
	}, { once: true, passive: true });
}

// Expose helpers if needed elsewhere
window.ensureAudioContext = ensureAudioContext;
window.pianoTokenToMidi = pianoTokenToMidi;
window.midiToFrequency = midiToFrequency;
window.playTone = playTone;
window.playNoteFromKey = playNoteFromKey;
window.parseSlashKeyToMidi = parseSlashKeyToMidi;
window.playSlashKey = playSlashKey;
