//----------------------
// УТИЛІТИ для роботи з тональностями (знаки біля ключа) у MIDI
//----------------------

function getInitialKeySignatures(allEvents, targetAbsTime, rawData) {
	console.debug("MR: FOO: [KS] mr-key-signatures-helper.js - getInitialKeySignatures");
	let initialKeySig = null;
	try {
		// get all key signature events and pick the last one with absTime <= targetAbsTime
		if (typeof getKeySignature === 'function') {
			const keySigs = getKeySignature(allEvents, rawData); // returns array with absTime, sf, mode/name
			if (Array.isArray(keySigs) && keySigs.length) {
				const candidates = keySigs.filter(k => (k.absTime || 0) <= targetAbsTime);
				const last = candidates.length ? candidates[candidates.length - 1] : null;
				if (last) initialKeySig = { sf: last.sf, mi: last.mode ?? 0 };
			}
		}
	} catch (ksErr) {
		console.warn('[KS] Could not determine initialKeySig for segment:', ksErr);
		initialKeySig = null;
	}
	return initialKeySig;
}


// ----------------------
// Функція оновлення тональності з подій ключових підписів у такті
// ----------------------
function getKeySignatureChanges(measure, currentKeySig) {
	let ks = updateKeySignatureFromEvents(measure);
	if (ks) { console.log(`[KS]: Tonality: ${ks.sf}, Mode: ${ks.mi}`); }
	let keySignatureChanged = false;
	if (ks) {
		if (!currentKeySig || currentKeySig.sf !== ks.sf || currentKeySig.mi !== ks.mi) {
			currentKeySig = ks;
			keySignatureChanged = true;
			console.log(`Key signature changed -> sf:${ks.sf} mi:${ks.mi}`);
		}
	}
	const keySigName = currentKeySig ? mapKeySignatureName(currentKeySig.sf, currentKeySig.mi) : null;
	return { keySignatureChanged, keySigName, currentKeySig };
}

// ----------------------
// УТИЛІТИ для визначення тоніки і PC з назви ключа (для мінорної енгармонізації)
// ----------------------
function ksNoteNameToPc(name) {
	const map = {
		'C': 0, 'B#': 0,
		'C#': 1, 'Db': 1,
		'D': 2,
		'D#': 3, 'Eb': 3,
		'E': 4, 'Fb': 4,
		'E#': 5, 'F': 5,
		'F#': 6, 'Gb': 6,
		'G': 7,
		'G#': 8, 'Ab': 8,
		'A': 9,
		'A#': 10, 'Bb': 10,
		'B': 11, 'Cb': 11
	};
	return map[name] ?? null;
}

function getMinorTonicPc(currentKeySig) {
	if (!currentKeySig) return null;
	const name = mapKeySignatureName(currentKeySig.sf, currentKeySig.mi); // напр. 'Am','Ebm'
	if (!name) return null;
	const root = name.replace(/m$/, ''); // прибрати 'm' у мінорі
	const tonicPc = ksNoteNameToPc(root);
	console.debug(`[KS] Tonic pitch-class for ${root}: ${tonicPc}`);
	return tonicPc;
}

// Мапінг sf/mi у рядок для VexFlow addKeySignature
function mapKeySignatureName(sf, mi) {
	// sf: -7..+7 (кількість бемолів (від'ємні) або дієзів (додатні)), mi: 0=major,1=minor
	const majors = ['Cb', 'Gb', 'Db', 'Ab', 'Eb', 'Bb', 'F', 'C', 'G', 'D', 'A', 'E', 'B', 'F#', 'C#'];
	const minors = ['Abm', 'Ebm', 'Bbm', 'Fm', 'Cm', 'Gm', 'Dm', 'Am', 'Em', 'Bm', 'F#m', 'C#m', 'G#m', 'D#m', 'A#m'];
	const idx = sf + 7;
	if (idx < 0 || idx >= majors.length) return null;
	return mi === 0 ? majors[idx] : minors[idx];
}

// Побудова карти знаків при ключі для sf (-7..+7)
function buildKeySignatureMap(sf) {
	console.debug("MR: FOO: midiRenderer.js - buildKeySignatureMap");
	const sharpOrder = ['f', 'c', 'g', 'd', 'a', 'e', 'b'];
	const flatOrder = ['b', 'e', 'a', 'd', 'g', 'c', 'f'];
	const map = {};
	if (sf > 0) {
		for (let i = 0; i < sf && i < sharpOrder.length; i++) {
			map[sharpOrder[i]] = '#';
		}
	} else if (sf < 0) {
		const count = Math.min(-sf, flatOrder.length);
		for (let i = 0; i < count; i++) {
			map[flatOrder[i]] = 'b';
		}
	}
	return map;
}


function isKeySignatureEvent(ev) {
	// Универсальне визначення key signature meta (0x59)
	if (!ev) return false;
	const META_KEY_ID = 0x59; // 89
	const isMeta =
		ev.type === 0xFF ||
		(typeof MIDIEvents !== 'undefined' ? ev.type === MIDIEvents.EVENT_META : false) ||
		ev.meta === true;

	// metaType або subtype або можливі рядкові значення
	let metaId = ev.metaType !== undefined ? ev.metaType :
		ev.subtype !== undefined ? ev.subtype :
			(ev.metaTypeHex || ev.subtypeHex);

	if (typeof metaId === 'string') {
		// Пробуємо як hex або десяткове
		if (/^0x/i.test(metaId)) {
			metaId = parseInt(metaId, 16);
		} else {
			const dec = parseInt(metaId, 10);
			if (!isNaN(dec)) metaId = dec;
		}
	}
	return isMeta && metaId === META_KEY_ID;
}

// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ПОДІЙ KEY SIGNATURE З MIDI
// Використання: const keySignatures = getKeySignature(midiEvents);
// Повертає масив об'єктів з параметрами key signature
// { param1: sf, param2: mi, sf, mode, name, absTime, raw }
// sf: -7..+7, mi: 0=major, 1=minor, name: 'C', 'Gm' і т.д., absTime: абсолютний час події
// raw: оригінальні байти [sf, mi]
// Підтримує різні форми подій key signature
// Потрібні глобальні функції: normalizeMetaEvent, decodeKeySignature
//---------------------------------------------------------------------
//function getKeySignature(midiEvents) {
//	if (!Array.isArray(midiEvents)) return [];
//	console.debug("MR: FOO: mr-key-signatures.js - getKeySignature");

//	const result = [];
//	midiEvents.forEach(ev => {
//		if (!ev) return;
//		// Normalize meta if possible
//		if (ev.type === 0xFF && typeof normalizeMetaEvent === 'function') {
//			ev = normalizeMetaEvent(ev);
//		}
//		// Detect key signature meta forms
//		const isKS =
//			(ev.type === 0xFF && (ev.metaType === 0x59 || ev.subtype === 0x59)) ||
//			(ev.type === 0x59); // flattened variant
//		if (!isKS) return;

//		let decoded = null;
//		// Preferred: global decoder
//		if (typeof decodeKeySignature === 'function') {
//			decoded = decodeKeySignature(ev);
//		}
//		// Fallback: data array
//		if (!decoded) {
//			let bytes = Array.isArray(ev.data) ? ev.data.slice() : [];
//			if (bytes.length === 3 && bytes[0] === 2) bytes = bytes.slice(1);
//			if (bytes.length >= 2) {
//				let sf = bytes[0]; if (sf > 127) sf -= 256;
//				const mi = bytes[1];
//				const majors = ['Cb', 'Gb', 'Db', 'Ab', 'Eb', 'Bb', 'F', 'C', 'G', 'D', 'A', 'E', 'B', 'F#', 'C#'];
//				const minors = ['Abm', 'Ebm', 'Bbm', 'Fm', 'Cm', 'Gm', 'Dm', 'Am', 'Em', 'Bm', 'F#m', 'C#m', 'G#m', 'D#m', 'A#m'];
//				const idx = sf + 7;
//				const name = (idx >= 0 && idx < majors.length) ? (mi === 0 ? majors[idx] : minors[idx]) : `sf=${sf}`;
//				decoded = { sf, mi, name, raw: ev.data };
//			}
//		}
//		// Fallback: param1 / param2 fields (MIDIFile library representation)
//		if (!decoded && ev.param1 !== undefined && ev.param2 !== undefined) {
//			let sf = ev.param1; if (sf > 127) sf -= 256;
//			const mi = ev.param2;
//			const majors = ['Cb', 'Gb', 'Db', 'Ab', 'Eb', 'Bb', 'F', 'C', 'G', 'D', 'A', 'E', 'B', 'F#', 'C#'];
//			const minors = ['Abm', 'Ebm', 'Bbm', 'Fm', 'Cm', 'Gm', 'Dm', 'Am', 'Em', 'Bm', 'F#m', 'C#m', 'G#m', 'D#m', 'A#m'];
//			const idx = sf + 7;
//			const name = (idx >= 0 && idx < majors.length) ? (mi === 0 ? majors[idx] : minors[idx]) : `sf=${sf}`;
//			decoded = { sf, mi, name, raw: [ev.param1, ev.param2] };
//		}

//		if (decoded) {
//			result.push({
//				param1: decoded.sf,
//				param2: decoded.mi,
//				sf: decoded.sf,
//				mode: decoded.mi,
//				name: decoded.name,
//				absTime: ev.absTime ?? 0,
//				raw: decoded.raw
//			});
//		}
//	});
//	return result;
//}
//if (typeof window !== 'undefined') window.getKeySignature = getKeySignature;


function getKeySignatures(midiFile, rawBytes) {
	const keys = [];
	console.debug("MR: FOO: mr-key-signatures.js - getKeySignatures");
	const pushKs = (sf, mi) => {
		if (sf == null || mi == null) return;
		// normalize signed [-7..7]
		const sfs = sf > 127 ? sf - 256 : sf;
		const clamped = Math.max(-7, Math.min(7, sfs));
		const mode = mi ? 1 : 0;
		keys.push({ sf: clamped, mi: mode, human: mapKeyToHuman(clamped, mode) });
	};

	// Defensive: determine track count / iterate tracks in a robust way
	let trackCount = 0;
	try {
		if (!midiFile) {
			console.warn('getKeySignatures: midiFile is falsy');
			trackCount = 0;
		}
		// Common case: MIDIFile instance with .tracks array
		else if (Array.isArray(midiFile.tracks)) {
			trackCount = midiFile.tracks.length;
		}
		// Some parsers expose .track (array) instead
		else if (Array.isArray(midiFile.track)) {
			trackCount = midiFile.track.length;
		}
		// Some MIDIFile implementations expose header.getTracks()
		else if (midiFile.header && typeof midiFile.header.getTracks === 'function') {
			trackCount = midiFile.header.getTracks();
		}
		// If getTrackEvents exists but no explicit count, probe until undefined/exception (limited)
		else if (typeof midiFile.getTrackEvents === 'function') {
			for (let i = 0; i < 256; i++) {
				try {
					const evs = midiFile.getTrackEvents(i);
					// Stop if parser signals out-of-range by returning null/undefined
					if (evs == null) break;
					// If it returns an array (even empty), count it as a track
					trackCount++;
				} catch (e) {
					// parser threw for out-of-range index -> stop probing
					break;
				}
			}
		} else {
			console.warn('getKeySignatures: unknown midiFile shape', midiFile);
		}
	} catch (tcErr) {
		console.warn('getKeySignatures: failed to determine trackCount', tcErr);
	}

	// 1) Primary: iterate tracks via getTrackEvents when available
	if (trackCount > 0 && typeof midiFile.getTrackEvents === 'function') {
		for (let index = 0; index < trackCount; index++) {
			let events;
			try {
				events = midiFile.getTrackEvents(index);
			} catch (e) {
				console.warn(`getKeySignatures: getTrackEvents(${index}) threw:`, e);
				events = null;
			}
			// Normalize events to an array
			if (!Array.isArray(events)) {
				// Some parsers wrap under { event: [...] } or similar
				if (events && Array.isArray(events.event)) events = events.event;
				else events = [];
			}
			events.forEach(ev => {
				const isKs = (ev && (ev.subtype === 0x59)) || (ev && ev.type === 0xFF && ev.metaType === 0x59);
				if (!isKs) return;

				if (ev.data && ev.data.length >= 2) {
					pushKs(ev.data[0], ev.data[1]); // data[0]=sf, data[1]=mi
				} else if (typeof ev.key !== 'undefined' && typeof ev.scale !== 'undefined') {
					pushKs(ev.key, ev.scale); // some MIDIFile builds
				}
			});
		}
	} else {
		// If no trackCount or getTrackEvents not available, try parsing rawBytes fallback directly below
		console.debug('getKeySignatures: skipping primary track scan, falling back to raw-bytes scan');
	}

	// 2) Fallback: якщо нічого не знайдено — скануємо сирі байти на FF 59 02
	if (keys.length === 0 && rawBytes && rawBytes.length > 5) {
		for (let i = 0; i < rawBytes.length - 4; i++) {
			if (rawBytes[i] === 0xFF && rawBytes[i + 1] === 0x59) {
				const len = rawBytes[i + 2];
				if (len >= 2) {
					const sf = rawBytes[i + 3];
					const mi = rawBytes[i + 4];
					pushKs(sf, mi);
					i += 4; // стрибок далі
				}
			}
		}
	}

	// 3) Прибрати поспіль однакові
	const result = [];
	let prev = null;
	for (const k of keys) {
		const sig = `${k.sf}:${k.mi}`;
		if (sig !== prev) {
			result.push(k);
			prev = sig;
		}
	}
	console.log(`[KS] Detected key signatures:`, result);
	return result;
}




// ---------------------------------------------------------------------
// ФУНКЦІЯ ДЛЯ ОНОВЛЕННЯ ТОНАЛЬНОСТІ З ПОДІЙ MIDI
// Використання: updateKeySignatureFromEvents(midiEvents);
// Повертає об'єкт { sf, mi } або null, якщо подій Key Signature немає
// sf: -7..+7, mi: 0=major, 1=minor
// Потрібні глобальні функції: normalizeMetaEvent, decodeKeySignature, isKeySignatureEvent
// currentKeySignature - глобальна змінна
// enharmonicPreference - 'auto' | 'sharps' | 'flats'
// ---------------------------------------------------------------------

function updateKeySignatureFromEvents(events) {
	console.debug("MR: FOO: [KS] mr-key-signatures-helper.js - updateKeySignatureFromEvents");
	if (!Array.isArray(events)) {
		console.warn('[KS] no events for update KS detected')
		return null;
	}
	let ks = null;
	console.debug("[KS] midiparser_ext.js starts");
	for (let i = 0; i < events.length; i++) {
		let ev = events[i];
		// Нормалізуємо meta події для стабільного поля data
		if (ev && ev.type === 0xFF && typeof normalizeMetaEvent === 'function') {
			ev = normalizeMetaEvent(ev);
		}
		// Перевіряємо різні форми key signature
		const isKS = (typeof isKeySignatureEvent === 'function') ? isKeySignatureEvent(ev) : (ev && ev.type === 0xFF && (ev.metaType === 0x59 || ev.subtype === 0x59));
		if (!isKS) continue;

		// Пробуємо розпарсити через decodeKeySignature
		if (typeof decodeKeySignature === 'function') {
			ks = decodeKeySignature(ev);
		}
		// Fallback 1: з data байтів
		if (!ks && ev && Array.isArray(ev.data) && ev.data.length >= 2) {
			let bytes = ev.data.slice();
			if (bytes.length >= 3 && bytes[0] === 2) bytes = bytes.slice(1);
			let sf = bytes[0]; if (sf > 127) sf -= 256;
			const miRaw = bytes[1];
			const mi = (miRaw === 0 || miRaw === 1) ? miRaw : (miRaw ? 1 : 0);
			ks = { sf, mi };

		}

		// Fallback 2: param1/param2
		if (!ks && ev && ev.param1 !== undefined && ev.param2 !== undefined) {
			let sf = ev.param1; if (sf > 127) sf -= 256;
			const miRaw = ev.param2;
			const mi = (miRaw === 0 || miRaw === 1) ? miRaw : (miRaw ? 1 : 0);
			ks = { sf, mi };
		}
		console.log("[KS]Parsed key signature from data bytes:", ks.sf + ":" + ks.mi);

		if (ks) break;
	}
	if (ks && typeof ks.sf === 'number') {
		currentKeySignature = ks.sf;
		// Extra safety
		ks.mi = (ks.mi === 0 || ks.mi === 1) ? ks.mi : (ks.mi ? 1 : 0);
		currentKeyMode = ks.mi; // <-- ADDED: remember mode (major/minor)
		console.log(`[KS]Updated key signature: ${currentKeySignature}, mode: ${currentKeyMode}`); // <-- ADDED
		return { sf: ks.sf, mi: ks.mi };
	}
	else {
		console.log(`[KS] current key signature: ${currentKeySignature}, mode: ${currentKeyMode}`); // <-- ADDED
	}
	return null;
}

// --- helpers to derive tonic pitch-class from sf/mi (for minor leading-tone logic) ---
function keyNameFromSfMi(sf, mi) {
	const majors = ['Cb', 'Gb', 'Db', 'Ab', 'Eb', 'Bb', 'F', 'C', 'G', 'D', 'A', 'E', 'B', 'F#', 'C#'];
	const minors = ['Abm', 'Ebm', 'Bbm', 'Fm', 'Cm', 'Gm', 'Dm', 'Am', 'Em', 'Bm', 'F#m', 'C#m', 'G#m', 'D#m', 'A#m'];
	const idx = (sf | 0) + 7;
	if (idx < 0 || idx >= majors.length) return null;
	return mi === 1 ? minors[idx] : majors[idx];
}
function noteNameToPc(name) {
	const map = {
		'C': 0, 'B#': 0,
		'C#': 1, 'Db': 1,
		'D': 2,
		'D#': 3, 'Eb': 3,
		'E': 4, 'Fb': 4,
		'E#': 5, 'F': 5,
		'F#': 6, 'Gb': 6,
		'G': 7,
		'G#': 8, 'Ab': 8,
		'A': 9,
		'A#': 10, 'Bb': 10,
		'B': 11, 'Cb': 11
	};
	return map[name] ?? null;
}
function tonicPcFromSfMi(sf, mi) {
	const nm = keyNameFromSfMi(sf, mi);
	if (!nm) return null;
	const root = mi === 1 ? nm.replace(/m$/, '') : nm;
	return noteNameToPc(root);
}

function mapKeyToHuman(sf, mi) {
	// Align with server-side MapKsToName in C#
	const majors = ["Ces", "Ges", "Des", "As", "Es", "B", "F", "C", "G", "D", "A", "E", "H", "Fis", "Cis"];
	const minors = ["as", "es", "b", "f", "c", "g", "d", "a", "e", "h", "fis", "cis", "gis", "dis", "ais"];
	const idx = sf + 7;
	if (idx < 0 || idx >= majors.length) return `sf=${sf}, mi=${mi}`;
	return mi === 0 ? `${majors[idx]}-dur` : `${minors[idx]}-moll`;
}