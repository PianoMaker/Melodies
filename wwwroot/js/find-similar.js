function detailsModule() {
	// Safely run only when DOM loaded and midi parsing libs are present
	function midiPitchToSearchToken(pitch) {
		console.log("FOO: [FS] find-similar.js - midiPitchToSearchToken ")
		// Map pitch class -> name
		const sharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
		const pc = ((Number(pitch) % 12) + 12) % 12;
		const name = sharpNames[pc];
		// Convert base name to site's eu-style tokens:
		// B -> h, C# -> cis, etc.
		let base;
		if (name === "B") base = "h"; // Ukrainian/European 'h' for B
		else if (name.includes("#")) base = name[0].toLowerCase() + "is"; // C# -> cis
		else base = name.toLowerCase(); // C -> c, D -> d, etc.

		// Compute octave marks relative to BASE_OCTAVE used by patternRenderer (4)
		const BASE_OCTAVE = 4;
		// midi octave: MIDI note 60 => octaveRaw = Math.floor(60/12)-1 = 4
		let octaveRaw = Math.floor(Number(pitch) / 12) - 1;
		if (!Number.isFinite(octaveRaw)) octaveRaw = BASE_OCTAVE;
		const diff = octaveRaw - BASE_OCTAVE;

		let octaveMarks = "";
		if (diff > 0) {
			// higher octaves: use apostrophes
			octaveMarks = "'".repeat(diff);
		} else if (diff < 0) {
			// lower octaves: use commas
			octaveMarks = ",".repeat(Math.abs(diff));
		}

		return base + octaveMarks;
	}

	function appendAntiForgeryToken(form) {
		const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
		if (tokenInput && tokenInput.value) {
			const cloned = document.createElement("input");
			cloned.type = "hidden";
			cloned.name = "__RequestVerificationToken";
			cloned.value = tokenInput.value;
			form.appendChild(cloned);
			return true;
		}
		return false;
	}

	function postKeysToSearch(actionUrl, keys, numerator, denominator) {
		console.log("FOO: [FS] find-similar.js - postKeysToSearch");
		const form = document.createElement("form");
		form.method = "post";
		form.action = actionUrl;

		const inputKeys = document.createElement("input");
		inputKeys.type = "hidden";
		inputKeys.name = "Keys";
		inputKeys.value = keys;
		form.appendChild(inputKeys);

		if (typeof numerator !== "undefined") {
			const inNum = document.createElement("input");
			inNum.type = "hidden";
			inNum.name = "TimeSignatureNumerator";
			inNum.value = String(numerator);
			form.appendChild(inNum);
		}

		if (typeof denominator !== "undefined") {
			const inDen = document.createElement("input");
			inDen.type = "hidden";
			inDen.name = "TimeSignatureDenominator";
			inDen.value = String(denominator);
			form.appendChild(inDen);
		}

		// IMPORTANT: include antiforgery token to avoid HTTP 400 (Bad Request)
		appendAntiForgeryToken(form);

		document.body.appendChild(form);
		form.submit();
	}

	/**
	 * convert a duration code like "q", "q.", "8", "16" or "qt" -> numeric string "4", "4.", "8", "16", "4t" 
	 * @param {any} dc
	 * @returns
	 */
	function durationCodeToNumeric(dc) {
		if (!dc) return "4";
		console.debug("FOO: [FS] find-similar.js - durationCodeTo Numeric");
		const dotted = dc.endsWith('.');
		const triplet = dc.endsWith('t');
		const base = (dotted || triplet) ? dc.slice(0, -1) : dc;
		let num = null;
		if (typeof reverseDurationMapping !== 'undefined') {
			num = reverseDurationMapping[base];
		}
		// fallback mapping
		if (!num) {
			const fallback = { w: 1, h: 2, q: 4, "8": 8, "16": 16, "32": 32 };
			num = fallback[base] || 4;
		}
		let res = String(num);
		if (dotted) res += '.';
		if (triplet) res += 't';
		return res;
	}

	/**
	 * Determine ticks per beat (ticks per quarter note) from parsed MIDI data.
	 * Preserves original fallback behavior: prefers array form (frames-per-second mode)
	 * and falls back to 480 if parsing fails or values are missing.
	 * @param {object} midiData - Parsed MIDI data object
	 * @returns {number} ticks per beat (TPQN)
	 */
	function getTicksPerBeat(midiData) {
		let ticksPerBeat = null;
		try {
			if (Array.isArray(midiData.timeDivision)) {
				// frames-per-second mode: second element is ticks per frame; fallback
				ticksPerBeat = midiData.timeDivision[1] || 480;
			} else {
				ticksPerBeat = midiData.timeDivision || 480;
			}
		} catch (e) {
			ticksPerBeat = 480;
		}
		return ticksPerBeat;
	}

	/**
	 * Extract time signature numerator/denominator from MIDI events.
	 * Looks for meta event 0xFF metaType 0x58 and decodes data bytes:
	 * data[0] = numerator, data[1] = denominator as power-of-two exponent.
	 * @param {Array} events - array of MIDI events
	 * @returns {{numerator: number|undefined, denominator: number|undefined}}
	 */
	function extractTimeSignature(events) {
		let numerator, denominator;
		try {
			const tsEvent = events.find(e => e && e.type === 0xFF && e.metaType === 0x58);
			if (tsEvent && Array.isArray(tsEvent.data)) {
				numerator = tsEvent.data[0];
				denominator = Math.pow(2, tsEvent.data[1]);
			}
		} catch (e) { /* ignore */ }
		return { numerator, denominator };
	}

	/**
	 * Визначає ранг міді-події для сортування:
	 * @param {any} ev - міді-подія
	 * @returns
	 */
	function rank(ev) {
		if (!ev) return 2;
		if (ev.type === 0x8) return 0;
		if (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0) return 0; // NoteOn vel=0 => off
		if (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0) return 1;
		return 2;
	}

	// =======================================================================
	// Аналіз міді-файлу та перенаправлення на сторінку пошуку
	// =======================================================================

	async function analyzeMidiAndSearch(midiUrl, actionUrl) {
		try {
			const resp = await fetch(midiUrl);                                         //завантаження міді-файлу
			if (!resp.ok) throw new Error("[FS] Failed to fetch MIDI: " + resp.status);
			console.debug(`FOO: [FS] find-similar.js - analyzeMidiAndSearch, url = ${midiUrl}`);
			const buf = await resp.arrayBuffer();                                      //отримання даних як ArrayBuffer
			const uint8 = new Uint8Array(buf);                                         // конвертація в Uint8Array

			if (typeof MidiParser === "undefined" || typeof SetEventsAbsoluteTime === "undefined") {
				console.warn("[FS] Required MIDI parsing helpers not present.");
				window.location.href = actionUrl; // fallback
				return;
			}

			const parsed = parseMidiPreferMIDIFile(uint8);          // парсинг міді-файлу
			const midiData = parsed.midiObj;                        //отримання об'єкту міді
			let allEvents = SetEventsAbsoluteTime(midiData) || [];  //отримання всіх міді-подій з абсолютним часом
			if (allEvents.length === 0) {
				console.warn("[FS] no MIDI-events found")
				return;
			} 
			ensureEndEvent(allEvents);                              // додавання кінцевої події, якщо відсутня
			let ticksPerBeat = getTicksPerBeat(midiData);           //визначає TPQN      
			let { numerator, denominator } = extractTimeSignature(allEvents) || {};     //отримує музичний розмір такту              

			allEvents.sort((a, b) => (a.absTime - b.absTime) || (rank(a) - rank(b)));     // сортування подій за абсолютним часом та типом (NoteOff передує NoteOn)

			// Build tokens with durations: map active notes and compute durationTicks at NoteOff
			const tokens = [];
			const active = {}; // pitch -> startAbsTime

			console.debug(`[FS] starting to analyse ${allEvents.length} events`)
			for (const ev of allEvents) {
				if (!ev) continue;				
				// Note-on
				if (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0) {
					const pitch = Number(ev.data[0]);                                 //визначає висоту ноти за міді-номером          
					if (active[pitch] === undefined) active[pitch] = ev.absTime;      //активна
				}
				// NoteOff: either explicit 0x8 or NoteOn with vel=0
				else if ((ev.type === 0x8) || (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0)) {
					const pitch = Number(ev.data[0]); 						        //визначає висоту ноти за міді-номером
					const start = active[pitch];                                    // визначає час міді-події NoteOn
					if (start !== undefined) {
						const durationTicks = Math.max(0, (ev.absTime || 0) - start); //визначає тривалість у тіках         
						let durCodes = [];
						if (typeof getDurationFromTicks === 'function') {
							try {
								durCodes = getDurationFromTicks(durationTicks, ticksPerBeat) || [];
							} catch (e) {
								durCodes = [];
							}
						}
						const primary = durCodes.length > 0 ? durCodes[0] : "q";
						const numeric = durationCodeToNumeric(primary); // e.g. "4" or "4." or "8"
						const noteToken = midiPitchToSearchToken(pitch);
						tokens.push(`${noteToken}${numeric}`);
						// clear active
						delete active[pitch];
					}
				}
			}

			// For any still-active notes (no NoteOff) close them at last absTime
			if (Object.keys(active).length > 0) {
				const lastAbs = allEvents.length ? allEvents[allEvents.length - 1].absTime || 0 : 0;
				for (const pitchStr of Object.keys(active)) {
					const start = active[pitchStr];
					const durationTicks = Math.max(0, lastAbs - start);
					let durCodes = [];
					if (typeof getDurationFromTicks === 'function') {
						try { durCodes = getDurationFromTicks(durationTicks, ticksPerBeat) || []; } catch (e) { durCodes = []; }
					}
					const primary = durCodes.length > 0 ? durCodes[0] : "q";
					const numeric = durationCodeToNumeric(primary);
					const noteToken = midiPitchToSearchToken(Number(pitchStr));
					tokens.push(`${noteToken}${numeric}`);
				}
			}

			if (tokens.length === 0) {
				window.location.href = actionUrl;
				console.warn("[FS] find-similar.js - no tokens found ")
				return;
			}
			console.debug(`[FS] ${tokens.length} tokens found`)
			// join with underscore as required by site format
			const keysString = tokens.join("_");
			postKeysToSearch(actionUrl, keysString, numerator, denominator);
		} catch (err) {
			console.error("[FS] analyzeMidiAndSearch failed", err);
			// fallback to search page
			try { window.location.href = actionUrl; } catch (e) { /* swallow */ }
		}
	}

	//======================================================================
	// Функція ініціалізації після завантаження DOM
	// запусає analyzeMidiAndSearch
	//======================================================================
	function init() {
		const btn = document.getElementById("findSimilarBtn");
		if (!btn) {
			console.error("[FS] btn find similar is not recognised");
			return;
		}

		console.log("[FS] find-similar starts");
		btn.addEventListener("click", function (ev) {
			ev.preventDefault();

			const actionUrl = btn.getAttribute("data-search-url");          //адреса сторінки пошуку    
			const notationEl = document.getElementById("notation");         //адреса міді-файлу для нотного запису
			const midiUrl = notationEl ? notationEl.dataset.midiUrl : null; //отримання URL міді-файлу
			if (!midiUrl) {
				// fallback: navigate to search page
				window.location.href = actionUrl || "/Melodies/Search";
				return;
			}
			analyzeMidiAndSearch(midiUrl, actionUrl || "/Melodies/Search"); //аналіз міді та пошук
		});
	}


	if (document.readyState === "loading") {
		document.addEventListener("DOMContentLoaded", init);
	} else {
		init();
	}
}

detailsModule();