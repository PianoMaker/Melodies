// Guard to prevent double-execution (skip second load)
if (typeof window !== 'undefined' && window.__mr_beams_helper_loaded) {
	console.warn('mr-beams-helper.js already loaded — skipping duplicate evaluation');
} else {
	if (typeof window !== 'undefined') window.__mr_beams_helper_loaded = true;

	// mr-beams-helper.js
	// ГРУПУВАННЯ (BEAMING) КОРОТКИХ НОТ У МЕЖАХ ОДНОГО ТАКТУ
	// --------------------------------------------------------------------
	// Спрощений виклик: makeBeams(measure, ticksPerBeat)
	// --------------------------------------------------------------------

	const beamableDurations = new Set(['8', '16', '32', '64', '128']);
	const minGroupSize = 2;
	const allowDotted = false;


	function makeBeams(measure, ticksPerBeat, timeSignature) {
		console.debug(`FOO: MB: makeBeams`);
		if (typeof ENABLE_BEAMS !== 'undefined' && !ENABLE_BEAMS) {
			console.warn('MB: Beaming is disabled by global flag ENABLE_BEAMS=false');
			return { beamGroups: [], beams: [] };
		}
		if (!measure || !Array.isArray(measure.notes) || measure.notes.length === 0) {
			return { beamGroups: [], beams: [] };
		}

		const localTicksPerBeat = ticksPerBeat || 480;
		const beamGroups = [];
		const beams = [];

		// створюємо поточну групу нот
		let currentGroup = {
			notes: [],
			startIndex: -1,
			ticksAccum: 0 // MIDI ticks inside group
		};

		let runningTicks = 0;
		// Правило (оновлено): зберігаємо інфо про попередню паузу
		let prevWasRest = false;
		let lastRestTicks = 0;            // тривалість останньої паузи (у тiках)
		let lastRestStartOnBeat = false;  // чи починалась пауза з долі

		// NEW: спеціальний прапор для пари 1/8. + 1/16 (закрити групу після наступної ноти)
		let closeAfterNext16 = false;

		measure.notes.forEach((note, idx) => {
			if (!note) {
				if (currentGroup.notes.length) closeGroup(currentGroup, beamGroups, beams, 'null-note');
				prevWasRest = false;
				lastRestTicks = 0;
				lastRestStartOnBeat = false;
				closeAfterNext16 = false;
				return;
			}

			// 1. Тривалість ноти у MIDI ticks (залежно від ticksPerBeat з MIDI файлу)
			const noteTicks = getNoteMidiTicks(note, localTicksPerBeat);

			// 2. Старт у MIDI ticks
			if (note.startTick == null) {
				note.startTick = runningTicks;
				note.startBeat = note.startTick / localTicksPerBeat;
			}

			// 3. Кінець
			note.endTick = note.startTick + noteTicks;
			note.endBeat = note.endTick / localTicksPerBeat;

			console.debug(`MB: startTick=${note.startTick}, ticks=${noteTicks}, endTick=${note.endTick} (beats start=${note.startBeat}, end=${note.endBeat})`);

			// 3a. Властивості елемента та сильні долі
			const dr = durationResolver(note);
			const isRest = !!dr.isRest;
			const startOnBeat = CheckStartOnBeat(note.startBeat, timeSignature);

			// 4. Перевірка межі біта у beat-одиницях (чверті)            
			let splitOnBeat = CheckSplitOnBeat(note, timeSignature);

			// 5. Beamable?
			let beamable = isBeamable(note);
			const next = measure.notes[idx + 1];
			const nextBeamable = next ? isBeamable(next) : false;

			// СПЕЦІАЛЬНИЙ ВИПАДОК: 1/8. + 1/16 повинні бути однією групою під ребром
			// дозволяємо бімувати 1/8. лише якщо за нею безпосередньо йде 1/16 (без крапки і не пауза)
			let specialPairStart = false;
			if (!beamable && dr.dotted && dr.code === '8' && next) {
				const dNext = durationResolver(next);
				if (!dNext.isRest && !dNext.dotted && dNext.code === '16') {
					beamable = true; // дозволяємо почати групу на 1/8.
					specialPairStart = true;
				}
			}

			// Якщо це пауза — закриваємо поточну групу і зберігаємо її параметри
			if (isRest) {
				if (currentGroup.notes.length) closeGroup(currentGroup, beamGroups, beams, 'rest');
				runningTicks += noteTicks;
				prevWasRest = true;
				lastRestTicks = noteTicks;
				lastRestStartOnBeat = startOnBeat;
				closeAfterNext16 = false; // обриваємо можливу пару
				return;
			}

			if (!beamable) {
				if (currentGroup.notes.length) closeGroup(currentGroup, beamGroups, beams, 'non-beamable');
				runningTicks += noteTicks;
				prevWasRest = false;
				lastRestTicks = 0;
				lastRestStartOnBeat = false;
				closeAfterNext16 = false;
				return;
			}

			// НОВЕ ПРАВИЛО ПІСЛЯ ПАУЗИ:
			// - Якщо попередня пауза < чверті і ця перша восьма НЕ на долю -> залишаємо хвостик (без ребра)
			// - Якщо пауза >= чверті і перша восьма на долю -> дозволяємо стандартне групування (під ребро)
			if (prevWasRest) {
				const restIsShorterThanQuarter = lastRestTicks < localTicksPerBeat;
				if (restIsShorterThanQuarter && !startOnBeat) {
					// ізолюємо ноту: хвостик, без об'єднання
					startGroup(currentGroup, note, idx, noteTicks);
					runningTicks += noteTicks;
					prevWasRest = false;
					lastRestTicks = 0;
					lastRestStartOnBeat = false;
					closeAfterNext16 = false;
					return;
				}
				// в інших випадках – дозволяємо стандартне групування
			}

			// Якщо нота (восьма з крапкою) починає долю — вона має починати нову групу
			const isEighthDotted = !isRest && dr.dotted && dr.code === '8';
			if (startOnBeat && isEighthDotted && currentGroup.notes.length > 0) {
				closeGroup(currentGroup, beamGroups, beams, 'dotted-8th-on-beat');
				closeAfterNext16 = false; // скинути, щоб уникнути хибного закриття від попередньої пари
			}

			// Якщо нота (проста восьма) починає долю — вона має починати нову групу
			const isEighthPlain = !isRest && dr.code === '8';
			if (startOnBeat && isEighthPlain && currentGroup.notes.length > 0) {
				closeGroup(currentGroup, beamGroups, beams, 'plain-8th-on-beat');
				closeAfterNext16 = false; // скинути, щоб уникнути хибного закриття від попередньої пари
			}

			// Якщо нота (шістнадцята) починає долю — вона має починати нову групу
			const isSixteenth = !isRest && dr.code === '16';
			if (startOnBeat && isSixteenth && currentGroup.notes.length > 0) {
				closeGroup(currentGroup, beamGroups, beams, 'sixteenth-on-beat');
				closeAfterNext16 = false; // скинути, щоб уникнути хибного закриття від попередньої пари
			}

			// 6. Формування групи
			if (currentGroup.notes.length === 0) {
				startGroup(currentGroup, note, idx, noteTicks);
			} else {
				addToGroup(currentGroup, note, noteTicks);
			}

			// Якщо це старт спеціальної пари (1/8. + 1/16) — закриємо групу після наступної ноти
			if (specialPairStart) {
				closeAfterNext16 = true;
			}

			// 7. Логіка закриття групи
			let mustClose = false;
			if (!nextBeamable) mustClose = true;
			if (!mustClose && splitOnBeat) mustClose = true;

			// Форсоване закриття для другої ноти у парі 1/8. + 1/16
			if (!mustClose && closeAfterNext16 && !specialPairStart) {
				mustClose = true;
				closeAfterNext16 = false;
			}

			if (idx === measure.notes.length - 1) mustClose = true;
			if (mustClose) closeGroup(currentGroup, beamGroups, beams, 'boundary');

			// 8. Просуваємо глобальний час (у MIDI ticks)
			runningTicks += noteTicks;
			prevWasRest = false;
			lastRestTicks = 0;
			lastRestStartOnBeat = false;
		});

		console.debug(`MB: return results summary:\n - beamGroups: ${beamGroups.length} groups\n - beams: ${beams.length} beam objects`);

		beamGroups.forEach((group, index) => {
			console.debug(`MB: beamGroup[${index}]:`, {
				notesCount: group.notes.length,
				startIndex: group.startIndex,
				endIndex: group.startIndex + group.notes.length - 1,
				reasonEnded: group.reasonEnded
			});
		});
		return { beamGroups, beams };
	}

	function defaultDurationResolver(note) {
		console.debug(`FOO: MB: defaultDurationResolver`);
		// Надійніше зчитування параметрів із VexFlow StaveNote
		if (note && note.vexNote) {
			const vn = note.vexNote;
			let code = 'q';
			try { code = String(vn.getDuration ? vn.getDuration() : 'q'); } catch { /* ignore */ }
			// isRest: використати API, або fallback по суфіксу 'r'
			let isRest = false;
			try { if (typeof vn.isRest === 'function') isRest = !!vn.isRest(); } catch { /* ignore */ }
			if (!isRest) isRest = /r$/.test(code);
			// dotted: через getDots()/modifiers, бо getDuration() може не містити крапку або містити 'd'
			let dotted = false;
			try { if (typeof vn.getDots === 'function') dotted = (vn.getDots() || []).length > 0; } catch { /* ignore */ }
			if (!dotted && typeof vn.getModifiers === 'function') {
				try {
					const mods = vn.getModifiers() || [];
					dotted = mods.some(m => (m.getCategory && m.getCategory() === 'dots') || m.category === 'dots');
				} catch { /* ignore */ }
			}
			// Нормалізуємо код тривалості: прибираємо 'r' і суфікси крапок або 'd' (VexFlow іноді повертає '8d')
			const base = code.replace(/r$/i, '').replace(/[.d]+$/i, '');
			return { code: base, isRest, dotted };
		}
		if (typeof note?.duration === 'number') {
			return { code: String(note.duration), isRest: !!note.isRest, dotted: false };
		}
		return { code: '', isRest: true, dotted: false };
	}
	const durationResolver = defaultDurationResolver;

	function isBeamable(note) {
		const d = durationResolver(note);
		if (d.isRest) return false;
		if (!allowDotted && d.dotted) return false;
		return beamableDurations.has(d.code);
	}

	// НОВА функція: обчислення MIDI ticks тільки з тривалості, ігноруємо внутрішні Vex ticks
	function getNoteMidiTicks(note, localTicksPerBeat) {
		// Якщо рендерер прокинув __srcTicks — використаємо його (точніше)
		const vn = note.vexNote || note;
		if (vn && typeof vn.__srcTicks === 'number') return vn.__srcTicks;

		const d = durationResolver(note);
		const mapQuarter = { 'w': 4, 'h': 2, 'q': 1, '8': 0.5, '16': 0.25, '32': 0.125, '64': 0.0625, '128': 0.03125 };
		let fraction = mapQuarter[d.code] ?? 1;
		if (d.dotted) fraction *= 1.5;
		return fraction * localTicksPerBeat;
	}

	// --- STEM NORMALIZATION (added) ---------------------------------------
	function unifyBeamGroupDirections(currentGroup) {
		try {
			if (!currentGroup || !currentGroup.notes || currentGroup.notes.length < 2) return;
			const notes = currentGroup.notes.map(n => n.vexNote || n).filter(Boolean);
			if (notes.length < 2) return;

			let upCount = 0, downCount = 0, sumLines = 0, totalHeads = 0;
			notes.forEach(vn => {
				if (typeof vn.getStemDirection === 'function') {
					const dir = vn.getStemDirection();
					if (dir === 1 || (typeof Vex !== 'undefined' && Vex.Flow && dir === Vex.Flow.Stem.UP)) upCount++;
					else if (dir === -1 || (typeof Vex !== 'undefined' && Vex.Flow && dir === Vex.Flow.Stem.DOWN)) downCount++;
				}
				if (typeof vn.getKeyProps === 'function') {
					const kp = vn.getKeyProps();
					kp.forEach(k => { sumLines += k.line; totalHeads++; });
				}
			});
			if (!(upCount && downCount)) return;
			let targetDir;
			if (upCount !== downCount) {
				targetDir = (upCount > downCount) ? (Vex.Flow ? Vex.Flow.Stem.UP : 1) : (Vex.Flow ? Vex.Flow.Stem.DOWN : -1);
			} else {
				const avgLine = totalHeads ? (sumLines / totalHeads) : 3;
				targetDir = (avgLine >= 3) ? (Vex.Flow ? Vex.Flow.Stem.DOWN : -1) : (Vex.Flow ? Vex.Flow.Stem.UP : 1);
			}
			notes.forEach(vn => {
				if (typeof vn.setStemDirection === 'function') {
					vn.setStemDirection(targetDir);
					if (typeof vn.reset === 'function') { try { vn.reset(); } catch { /* ignore */ } }
				}
			});
		} catch (e) {
			console.warn('Stem normalization failed:', e);
		}
	}

	function closeGroup(current, beamGroups, beams, reason) {
		console.debug("MB: closeGroup method starts");
		if (current.notes.length >= minGroupSize) {
			const first = current.notes[0];
			const last = current.notes[current.notes.length - 1];
			first.__beamStart = true;
			last.__beamEnd = true;

			beamGroups.push({
				notes: [...current.notes],
				startIndex: current.startIndex,
				endIndex: current.startIndex + current.notes.length - 1,
				reasonEnded: reason
			});
			console.debug(`MB: closegroup, beamGroups length=${beamGroups.length}`)

			if (typeof Vex !== 'undefined' && Vex.Flow && Vex.Flow.Beam) {
				try {
					// НОВЕ: перед створенням beam – уніфікуємо напрямки за потреби
					unifyBeamGroupDirections(current);
					// Safety: тільки реальні ноти (не паузи)
					const vexNotes = current.notes
						.map(n => n.vexNote || n)
						.filter(vn => {
							try { return !(typeof vn.isRest === 'function' ? vn.isRest() : /r$/.test(vn.getDuration?.() || '')); }
							catch { return true; }
						});
					if (vexNotes.length >= minGroupSize)
						beams.push(new Vex.Flow.Beam(vexNotes));
				} catch (e) {
					console.warn('Beam creation failed:', e);
				}
			}
		} else {
			// Менше minGroupSize -> не формуємо beam, знімаємо можливі позначки
			current.notes.forEach(n => {
				delete n.__beamStart;
				delete n.__beamEnd;
			});
		}
		current.notes = []; // готуємо нове групування нот
		current.startIndex = -1;
		current.ticksAccum = 0;
	}

	function startGroup(currentGroup, note, idx, noteTicks) {
		console.debug(`MB: startGroup method starts, idx=${idx}`);
		currentGroup.notes = [note];
		currentGroup.startIndex = idx;
		currentGroup.ticksAccum = noteTicks;
	}

	function addToGroup(currentGroup, note, noteTicks) {
		console.debug("MB: addToGroup method starts");
		currentGroup.notes.push(note);
		currentGroup.ticksAccum += noteTicks;
	}



	function CheckSplitOnBeat(note, timeSignature) {
		const noteEndBeat = note.endBeat;
		if (!timeSignature || noteEndBeat == null) return false;

		if (!Array.isArray(timeSignature.beatPositions)) {
			const num = timeSignature.num || 4;
			const den = timeSignature.den || 4;
			const barBeats = num * 4 / den;
			timeSignature.beatPositions = [];
			const isCompound = (den === 8) && (num % 3 === 0) && (num >= 3);
			if (isCompound) {
				const step = 3 * (4 / den);
				for (let p = step; p <= barBeats + 1e-9; p += step) {
					timeSignature.beatPositions.push(Number(p.toFixed(9)));
				}
			} else {
				for (let b = 1; b <= barBeats; b++) timeSignature.beatPositions.push(b);
			}
		}

		// --- Округлення endBeat до найближчої позиції, якщо різниця < 0.075 ---
		const ROUND_EPS = 0.075;
		let roundedEndBeat = noteEndBeat;
		for (const bp of timeSignature.beatPositions) {
			if (Math.abs(bp - noteEndBeat) < ROUND_EPS) {
				roundedEndBeat = bp;
				break;
			}
		}

		const result = timeSignature.beatPositions.some(bp => Math.abs(bp - roundedEndBeat) < 1e-6);

		try {
			console.debug(
				`MB: CheckSplitOnBeat | idx=${note.idx}, pitch=${note.pitch || note.name || '?'}, dur=${note.duration || '?'}, ` +
				`endTick=${note.endTick}, endBeat=${noteEndBeat}, roundedEndBeat=${roundedEndBeat}, beatPositions=[${timeSignature.beatPositions.join(', ')}] => ${result}`
			);
		} catch (e) {
			console.warn(`MB: CheckSplitOnBeat | logging failed: ${e}`);
		}

		return result;
	}

	// NEW: перевірка, що початок ноти/паузи припадає на початок біта (сильну долю)
	function CheckStartOnBeat(note, timeSignature) {
		const noteStartBeat = note.startBeat;
		if (!timeSignature || noteStartBeat == null) return false;
		if (!Array.isArray(timeSignature.beatStarts)) {
			const num = timeSignature.num || 4;
			const den = timeSignature.den || 4;
			const barBeats = num * 4 / den;
			timeSignature.beatStarts = [];
			const isCompound = (den === 8) && (num % 3 === 0) && (num >= 3);
			const step = isCompound ? 3 * (4 / den) : 1;
			for (let p = 0; p <= barBeats + 1e-9; p += step) {
				timeSignature.beatStarts.push(Number(p.toFixed(9)));
			}
		}
		const EPS = 1e-6;
		let onBeat = timeSignature.beatStarts.some(bs => Math.abs(bs - noteStartBeat) < EPS);
		if (onBeat) return true;
		const num = timeSignature.num || 4;
		const den = timeSignature.den || 4;
		const isCompound = (den === 8) && (num % 3 === 0) && (num >= 3);
		const step = isCompound ? 3 * (4 / den) : 1;
		const ratio = noteStartBeat / step;
		const diff = Math.abs(ratio - Math.round(ratio));
		return diff < 1e-3;
	}

	// --- Triplet detection -------------------------------------------------
	function approx(a, b, eps) { return Math.abs(a - b) <= eps; }

	function getSrcTicksOrFallback(note, localTicksPerBeat) {
		const vn = note.vexNote || note;
		if (vn && typeof vn.__srcTicks === 'number') return vn.__srcTicks;
		return getNoteMidiTicks(note, localTicksPerBeat);
	}

	// Замініть функцію detectTuplets на цю версію з підтримкою 2-нотних тріолей:

	function detectTuplets(measure, ticksPerBeat) {
		if (!measure || !Array.isArray(measure.notes) || measure.notes.length < 2) return [];
		const local = ticksPerBeat || 480;
		const tuplets = [];

		// Допоміжна функція: чи є елемент паузою
		const isRestNote = (n) => {
			const d = durationResolver(n);
			return d.isRest;
		};

		// Допоміжна функція: чи є елемент beamable (восьма або коротша)
		const isBeamableNote = (n) => {
			if (isRestNote(n)) return false;
			const d = durationResolver(n);
			return ['8', '16', '32', '64', '128'].includes(d.code);
		};

		// Очікувані тріольні тривалості (в тіках)
		const triplet8thUnit = local / 3;           // тріольна 1/8: ≈ 85.3
		const triplet16thUnit = local / 6;          // тріольна 1/16: ≈ 42.7
		const tripletQuarterUnit = (2 * local) / 3; // тріольна 1/4: ≈ 170.7

		console.debug(`MB: detectTuplets | ticksPerBeat=${local}, triplet8thUnit=${triplet8thUnit.toFixed(1)}, tripletQuarterUnit=${tripletQuarterUnit.toFixed(1)}`);

		// Логуємо всі ноти такту для діагностики
		console.debug(`MB: Measure notes dump:`);
		measure.notes.forEach((n, idx) => {
			const vn = n.vexNote || n;
			const isRest = isRestNote(n);
			const ticks = getSrcTicksOrFallback(n, local);
			console.debug(`  [${idx}] ${isRest ? 'REST' : 'NOTE'} ticks=${ticks} __srcTicks=${vn.__srcTicks}`);
		});

		// Множина індексів, які вже використані в тріолях
		const usedIndices = new Set();

		for (let i = 0; i <= measure.notes.length - 2;) {
			// Пропускаємо вже використані
			if (usedIndices.has(i)) { i++; continue; }

			const a = measure.notes[i];
			const b = measure.notes[i + 1];
			const c = measure.notes[i + 2]; // може бути undefined

			if (!a || !b) { i++; continue; }

			const va = a.vexNote || a;
			const vb = b.vexNote || b;
			const vc = c ? (c.vexNote || c) : null;

			const aIsRest = isRestNote(a);
			const bIsRest = isRestNote(b);
			const cIsRest = c ? isRestNote(c) : true;

			const ta = getSrcTicksOrFallback(a, local);
			const tb = getSrcTicksOrFallback(b, local);
			const tc = c ? getSrcTicksOrFallback(c, local) : 0;

			// === СПРОБА 3-НОТНОЇ ТРІОЛІ ===
			if (c && !usedIndices.has(i + 2)) {
				// Не всі три паузи
				if (!(aIsRest && bIsRest && cIsRest)) {
					const total3 = ta + tb + tc;

					// FAST PATH: прапорець __isTriplet
					if (va.__isTriplet && vb.__isTriplet && vc.__isTriplet &&
						va.__tripletBase === vb.__tripletBase && vb.__tripletBase === vc.__tripletBase) {

						const hasPause = aIsRest || bIsRest || cIsRest;
						const allBeamable = isBeamableNote(a) && isBeamableNote(b) && isBeamableNote(c);
						const needBracket = hasPause || !allBeamable;

						try {
							const tuplet = new Vex.Flow.Tuplet([va, vb, vc], {
								num_notes: 3, notes_occupied: 2, ratioed: false, bracketed: needBracket
							});
							tuplets.push(tuplet);
							console.debug(`MB: Tuplet (fast 3-note) at i=${i}, bracketed=${needBracket}`);
						} catch (e) { console.warn('Tuplet creation failed (fast):', e); }

						usedIndices.add(i); usedIndices.add(i + 1); usedIndices.add(i + 2);
						i += 3;
						continue;
					}

					// Тріоль ВОСЬМИХ: кожен ≈ local/3, сума = local
					const eps8 = local * 0.15;
					const aIs8t = Math.abs(ta - triplet8thUnit) <= eps8;
					const bIs8t = Math.abs(tb - triplet8thUnit) <= eps8;
					const cIs8t = Math.abs(tc - triplet8thUnit) <= eps8;

					if (Math.abs(total3 - local) <= (local * 0.12) && aIs8t && bIs8t && cIs8t) {
						const hasPause = aIsRest || bIsRest || cIsRest;
						const allBeamable = isBeamableNote(a) && isBeamableNote(b) && isBeamableNote(c);
						const needBracket = hasPause || !allBeamable;

						try {
							const tuplet = new Vex.Flow.Tuplet([va, vb, vc], {
								num_notes: 3, notes_occupied: 2, ratioed: false, bracketed: needBracket
							});
							tuplets.push(tuplet);
							console.debug(`MB: ✓ 8th Triplet (3-note) at i=${i}, bracketed=${needBracket}`);
						} catch (e) { console.warn('Tuplet creation failed (8th):', e); }

						usedIndices.add(i); usedIndices.add(i + 1); usedIndices.add(i + 2);
						i += 3;
						continue;
					}

					// Тріоль ШІСТНАДЦЯТИХ: кожен ≈ local/6, сума = local/2
					const eps16 = local * 0.10;
					const aIs16t = Math.abs(ta - triplet16thUnit) <= eps16;
					const bIs16t = Math.abs(tb - triplet16thUnit) <= eps16;
					const cIs16t = Math.abs(tc - triplet16thUnit) <= eps16;

					if (Math.abs(total3 - local / 2) <= (local * 0.10) && aIs16t && bIs16t && cIs16t) {
						const hasPause = aIsRest || bIsRest || cIsRest;
						const allBeamable = isBeamableNote(a) && isBeamableNote(b) && isBeamableNote(c);
						const needBracket = hasPause || !allBeamable;

						try {
							const tuplet = new Vex.Flow.Tuplet([va, vb, vc], {
								num_notes: 3, notes_occupied: 2, ratioed: false, bracketed: needBracket
							});
							tuplets.push(tuplet);
							console.debug(`MB: ✓ 16th Triplet (3-note) at i=${i}, bracketed=${needBracket}`);
						} catch (e) { console.warn('Tuplet creation failed (16th):', e); }

						usedIndices.add(i); usedIndices.add(i + 1); usedIndices.add(i + 2);
						i += 3;
						continue;
					}

					// Тріоль ЧВЕРТНИХ: кожен ≈ 2*local/3, сума = 2*local
					const epsQ = local * 0.15;
					const aIsQt = Math.abs(ta - tripletQuarterUnit) <= epsQ;
					const bIsQt = Math.abs(tb - tripletQuarterUnit) <= epsQ;
					const cIsQt = Math.abs(tc - tripletQuarterUnit) <= epsQ;

					if (Math.abs(total3 - (2 * local)) <= (local * 0.15) && aIsQt && bIsQt && cIsQt) {
						try {
							const tuplet = new Vex.Flow.Tuplet([va, vb, vc], {
								num_notes: 3, notes_occupied: 2, ratioed: false, bracketed: true
							});
							tuplets.push(tuplet);
							console.debug(`MB: ✓ Quarter Triplet (3-note) at i=${i}`);
						} catch (e) { console.warn('Tuplet creation failed (quarter):', e); }

						usedIndices.add(i); usedIndices.add(i + 1); usedIndices.add(i + 2);
						i += 3;
						continue;
					}
				}
			}

			// === СПРОБА 2-НОТНОЇ ТРІОЛІ (нерівномірна: чверть + восьма = 1 біт) ===
			// Патерн: тріольна чверть (≈170) + тріольна восьма (≈85) = 256 = 1 біт
			// Або навпаки: восьма + чверть
			if (!(aIsRest && bIsRest)) {
				const total2 = ta + tb;
				const eps2 = local * 0.15;

				// Варіант 1: чверть (170) + восьма (85)
				const aIsQtriplet = Math.abs(ta - tripletQuarterUnit) <= eps2;
				const bIs8triplet = Math.abs(tb - triplet8thUnit) <= eps2;

				// Варіант 2: восьма (85) + чверть (170)
				const aIs8triplet = Math.abs(ta - triplet8thUnit) <= eps2;
				const bIsQtriplet = Math.abs(tb - tripletQuarterUnit) <= eps2;

				const is2NoteTriplet = Math.abs(total2 - local) <= (local * 0.12) &&
					((aIsQtriplet && bIs8triplet) || (aIs8triplet && bIsQtriplet));

				if (is2NoteTriplet) {
					console.debug(`MB: Found 2-note triplet at i=${i}: [${ta}] + [${tb}] = ${total2} ≈ ${local}`);

					// 2-нотна тріоль завжди потребує дужку
					const hasPause = aIsRest || bIsRest;
					const needBracket = true; // 2-нотні тріолі завжди з дужкою для ясності

					try {
						const tuplet = new Vex.Flow.Tuplet([va, vb], {
							num_notes: 3,        // логічно це "3 восьмих"
							notes_occupied: 2,   // займають місце 2 восьмих
							ratioed: false,
							bracketed: needBracket
						});
						tuplets.push(tuplet);
						console.debug(`MB: ✓ 2-note Triplet CREATED at i=${i}, pattern=${aIsQtriplet ? 'Q+8' : '8+Q'}`);
					} catch (e) {
						console.warn('Tuplet creation failed (2-note):', e);
					}

					usedIndices.add(i); usedIndices.add(i + 1);
					i += 2;
					continue;
				}
			}

			console.debug(`MB: No triplet match at i=${i}`);
			i += 1;
		}

		console.debug(`MB: detectTuplets finished, found ${tuplets.length} tuplets`);
		return tuplets;
	}

	/**
	* (AUTO-STEM HELPER)
	* -------------------------------------------------
	* Added helper to optionally apply VexFlow auto stem logic to every created note
	* without modifying or removing existing functions or comments. Rest durations
	* (codes ending with 'r') are skipped. Safe to call multiple times.
	*/
	function applyAutoStem(note, durationCode) {
		try {
			if (!note) return;
			console.debug('MB: applyAutoStem is working');
			if (typeof durationCode === 'string' && /r$/.test(durationCode)) return; // skip rests
			if (typeof note.autoStem === 'function') {
				note.autoStem();
			}
		} catch (e) {
			console.warn('applyAutoStem failed:', e);
		}
	}



	// ----------------------
	// ФУНКЦІЯ ДЛЯ ОБРОБКИ І ОБЧИСЛЕННЯ НОТНИХ РЕБЕР (BEAMS) З ОБРОБКОЮ ПОМИЛОК
	// виклик з Midirenderer.js
	// ----------------------
	function calculateBeams(validNotes, ticksPerBeat, index, currentNumerator, currentDenominator) {
		let beams = [];
		if (typeof makeBeams === 'function' && validNotes.length > 0 && ticksPerBeat) {
			try {
				// Створюємо правильну структуру для makeBeams
				const measureForBeams = {
					notes: validNotes.map(note => ({
						vexNote: note // Обгортаємо StaveNote в очікувану структуру
					}))
				};
				console.log(`Calling makeBeams for measure ${index + 1} with ${validNotes.length} notes and ticksPerBeat ${ticksPerBeat}`);



				const timeSignature = { num: currentNumerator, den: currentDenominator };

				const beamResult = makeBeams(measureForBeams, ticksPerBeat, timeSignature);

				beams = beamResult.beams || [];
				console.log(`makeBeams found ${beams.length} beam groups for measure ${index + 1}`);
			} catch (beamError) {
				console.warn(`Error in makeBeams for measure ${index + 1}:`, beamError);
				beams = []; // Fallback до порожнього масиву
			}
		} else {
			console.log(`makeBeams skipped for measure ${index + 1}: function=${typeof makeBeams}, notes=${validNotes.length}, ticksPerBeat=${ticksPerBeat}`);
		}
		return beams;
	}
}

