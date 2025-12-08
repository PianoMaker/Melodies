function detailsModule() {
	function midiPitchToSearchToken(pitch) {
		console.log("FOO: [FS] find-similar.js - midiPitchToSearchToken ")
		const sharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
		const pc = ((Number(pitch) % 12) + 12) % 12;
		const name = sharpNames[pc];
		let base;
		if (name === "B") base = "h";
		else if (name.includes("#")) base = name[0].toLowerCase() + "is";
		else base = name.toLowerCase();

		const BASE_OCTAVE = 4;
		let octaveRaw = Math.floor(Number(pitch) / 12) - 1;
		if (!Number.isFinite(octaveRaw)) octaveRaw = BASE_OCTAVE;
		const diff = octaveRaw - BASE_OCTAVE;
		let octaveMarks = "";
		if (diff > 0) octaveMarks = "'".repeat(diff);
		else if (diff < 0) octaveMarks = ",".repeat(Math.abs(diff));

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

		appendAntiForgeryToken(form);

		document.body.appendChild(form);
		form.submit();
	}

	function durationCodeToNumeric(dc) {
		if (!dc) return "4";
		const dotted = dc.endsWith('.');
		const triplet = dc.endsWith('t');
		const base = (dotted || triplet) ? dc.slice(0, -1) : dc;
		let num = null;
		if (typeof reverseDurationMapping !== 'undefined') num = reverseDurationMapping[base];
		if (!num) {
			const fallback = { w: 1, h: 2, q: 4, "8": 8, "16": 16, "32": 32 };
			num = fallback[base] || 4;
		}
		let res = String(num);
		if (dotted) res += '.';
		if (triplet) res += 't';
		return res;
	}

	function getTicksPerBeat(midiData) {
		let ticksPerBeat = null;
		try {
			if (Array.isArray(midiData.timeDivision)) ticksPerBeat = midiData.timeDivision[1] || 480;
			else ticksPerBeat = midiData.timeDivision || 480;
		} catch (e) {
			ticksPerBeat = 480;
		}
		return ticksPerBeat;
	}

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

	function rank(ev) {
		if (!ev) return 2;
		if (ev.type === 0x8) return 0;
		if (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0) return 0;
		if (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0) return 1;
		return 2;
	}

	// Robust NoteOff detection fallback (local)
	function detectNoteOff(ev) {
		if (!ev) return { midiNote: null, isNoteOff: false, velocity: null };
		const t = ev.type;
		let midiNote = null, velocity = null;
		if (Array.isArray(ev.data) && ev.data.length >= 1) {
			midiNote = ev.data[0];
			velocity = ev.data.length >= 2 ? ev.data[1] : (ev.param2 ?? ev.velocity ?? null);
		} else {
			midiNote = ev.param1 ?? ev.note ?? null;
			velocity = ev.param2 ?? ev.velocity ?? null;
		}
		const isMeta = (t === 0xFF || t === 255);
		const explicitOff = (t === 0x8 || t === 8);
		const noteOnWithZeroVel = (t === 0x9 || t === 9 || t === 0x90 || t === 144) && velocity === 0;
		const inferredOff = (!isMeta && typeof velocity === 'number' && velocity === 0);
		const isNoteOff = explicitOff || noteOnWithZeroVel || inferredOff;
		return { midiNote, isNoteOff, velocity };
	}

	async function analyzeMidiAndSearch(midiUrl, actionUrl) {
		try {
			const resp = await fetch(midiUrl);
			if (!resp.ok) throw new Error("[FS] Failed to fetch MIDI: " + resp.status);
			console.debug(`FOO: [FS] find-similar.js - analyzeMidiAndSearch, url = ${midiUrl}`);
			const buf = await resp.arrayBuffer();
			const uint8 = new Uint8Array(buf);

			if (typeof MidiParser === "undefined" && typeof parseMidiPreferMIDIFile === "undefined") {
				console.warn("[FS] Required MIDI parsing helpers not present.");
				window.location.href = actionUrl;
				return;
			}

			// Prefer unified parser, fallback to MidiParser.Uint8 if needed
			let parsed = null;
			if (typeof parseMidiPreferMIDIFile === "function") {
				parsed = parseMidiPreferMIDIFile(uint8);
			}
			let midiData = parsed ? parsed.midiObj : (typeof MidiParser !== 'undefined' ? MidiParser.Uint8(uint8) : null);
			if (!midiData) {
				console.warn("[FS] midiData not available, fallback to search");
				window.location.href = actionUrl;
				return;
			}

			let allEvents = SetEventsAbsoluteTime(midiData) || [];
			if (!allEvents || allEvents.length === 0) {
				console.warn("[FS] no MIDI-events found");
				window.location.href = actionUrl;
				return;
			}
			ensureEndEvent(allEvents);
			const ticksPerBeat = getTicksPerBeat(midiData);
			const { numerator, denominator } = extractTimeSignature(allEvents) || {};

			// diagnostic: sample events
			console.debug("[FS] sample allEvents (first 20):", allEvents.slice(0, 20));

			allEvents.sort((a, b) => (a.absTime - b.absTime) || (rank(a) - rank(b)));

			const tokens = [];
			const active = {}; // pitch -> startAbsTime

			console.debug(`[FS] starting to analyse ${allEvents.length} events`);
			for (const ev of allEvents) {
				if (!ev) continue;

				// Try parser helper first
				let on = { midiNote: null, isNoteOn: false, velocity: null };
				if (typeof detectNoteOn === 'function') {
					try { on = detectNoteOn(ev); } catch (e) { /* ignore */ }
				}
				// reconcile raw data if helper didn't provide note/vel
				if ((on.midiNote === null || on.midiNote === undefined) && Array.isArray(ev.data) && ev.data.length >= 1) on.midiNote = ev.data[0];
				if ((on.velocity === null || on.velocity === undefined) && Array.isArray(ev.data) && ev.data.length >= 2) on.velocity = ev.data[1];

				// NEW: treat non-meta events with velocity>0 as NoteOn (robust fallback)
				try {
					const isMeta = (ev.type === 0xFF || ev.type === 255);
					if (!on.isNoteOn && !isMeta && typeof on.velocity === 'number' && on.velocity > 0) {
						on.isNoteOn = true;
						console.debug("[FS] fallback: inferred NoteOn from ev.data", { midiNote: on.midiNote, velocity: on.velocity, ev });
					}
				} catch (e) { /* ignore */ }

				// compute explicit note-off info using local fallback
				const off = detectNoteOff(ev);

				// If it's a NoteOn -> start tracking
				if (on.isNoteOn && typeof on.midiNote === 'number') {
					const pitch = Number(on.midiNote);
					if (active[pitch] === undefined) active[pitch] = ev.absTime;
					continue;
				}

				// If it's a NoteOff -> close note and produce token
				if (off.isNoteOff && (off.midiNote !== null && !Number.isNaN(off.midiNote))) {
					const pitch = Number(off.midiNote);
					const start = active[pitch];
					if (start !== undefined) {
						const durationTicks = Math.max(0, (ev.absTime || 0) - start);
						let durCodes = [];
						if (typeof getDurationFromTicks === 'function') {
							try { durCodes = getDurationFromTicks(durationTicks, ticksPerBeat) || []; } catch (e) { durCodes = []; }
						}
						const primary = durCodes.length > 0 ? durCodes[0] : "q";
						const numeric = durationCodeToNumeric(primary);
						const noteToken = midiPitchToSearchToken(pitch);
						const token = `${noteToken}${numeric}`;
						tokens.push(token);
						console.debug("[FS] pushed token", { token, pitch, durationTicks, durCodes, ev });
						delete active[pitch];
					}
					continue;
				}

				// Otherwise ignore (meta / controller / program change / other)
			}

			// close still-active notes
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
					const token = `${noteToken}${numeric}`;
					tokens.push(token);
					console.debug("[FS] pushed token (active-end)", { token, pitchStr, durationTicks, durCodes });
				}
			}

			if (tokens.length === 0) {
				console.warn("[FS] find-similar.js - no tokens found ");
				window.location.href = actionUrl;
				return;
			}
			console.debug(`[FS] ${tokens.length} tokens found`);
			const keysString = tokens.join("_");
			postKeysToSearch(actionUrl, keysString, numerator, denominator);
		} catch (err) {
			console.error("[FS] analyzeMidiAndSearch failed", err);
			try { window.location.href = actionUrl; } catch (e) { /* swallow */ }
		}
	}

	function init() {
		const btn = document.getElementById("findSimilarBtn");
		if (!btn) {
			console.error("[FS] btn find similar is not recognised");
			return;
		}

		console.log("[FS] find-similar starts");
		btn.addEventListener("click", function (ev) {
			ev.preventDefault();

			const actionUrl = btn.getAttribute("data-search-url");
			const notationEl = document.getElementById("notation");
			const midiUrl = notationEl ? notationEl.dataset.midiUrl : null;
			if (!midiUrl) {
				window.location.href = actionUrl || "/Melodies/Search";
				return;
			}
			analyzeMidiAndSearch(midiUrl, actionUrl || "/Melodies/Search");
		});
	}

	if (document.readyState === "loading") {
		document.addEventListener("DOMContentLoaded", init);
	} else {
		init();
	}
}

detailsModule();