// mr-durations-helper 

// Safe dot counter that checks both duration string and VexFlow modifiers
function safeDotCount(note, durationStr) {
	try {
		let dots = (String(durationStr || '').match(/\./g) || []).length;
		if (dots === 0 && note) {
			if (typeof note.getDots === 'function') return (note.getDots() || []).length || 0;
			if (typeof note.getModifiers === 'function') {
				const mods = note.getModifiers() || [];
				return mods.filter(m =>
					(m && (m.getCategory?.() === 'dots' || m.category === 'dots')) ||
					(typeof Vex !== 'undefined' && Vex.Flow && m instanceof Vex.Flow.Dot)
				).length;
			}
			if (Array.isArray(note.modifiers)) {
				return note.modifiers.filter(m => (m && (m.getCategory?.() === 'dots' || m.category === 'dots'))).length;
			}
		}
		return dots;
	} catch { return 0; }
}

// Computes total ticks for a VexFlow note/rest taking dots into account
function getTotalTicksForNote(note, ticksPerBeat) {
	try {
		if (!note || !ticksPerBeat) return 0;



		let codeStr = typeof note.__durationCode === 'string'
			? note.__durationCode
			: String(typeof note.getDuration === 'function' ? note.getDuration() : (note.duration || 'q'));

		console.debug(`Utils: input: ${note.getDuration()}`);

		codeStr = codeStr.replace(/r$/, '');

		let baseTicks = 0;
		if (typeof calculateTicksFromDuration === 'function') {
			baseTicks = calculateTicksFromDuration(codeStr, ticksPerBeat);
		}

		const hasDotInCode = /\./.test(codeStr);
		const isTriplet = /t$/.test(codeStr);
		if (!hasDotInCode && !isTriplet) {
			const dots = safeDotCount(note, codeStr);
			if (dots > 0) {
				let totalTicks = baseTicks;
				let add = Math.floor(baseTicks / 2);
				for (let i = 0; i < dots; i++) { totalTicks += add; add = Math.floor(add / 2); }
				console.debug(`Utils: getTotalTicksForNote: ${codeStr} -> ${totalTicks}`);
				return totalTicks;
			}
		}
		console.debug(`Utils: getTotalTicksForNote: ${codeStr} -> ${baseTicks}`);
		return baseTicks;
	} catch (e) {
		console.warn('getTotalTicksForNote failed:', e);
		return 0;
	}
}

// Узагальнений конвертер тіксів у список тривалостей
// allowDotted=true дозволяє крапковані тривалості (w., h., q., 8., 16., 32., 64.)
function ticksToDurationList(ticks, ticksPerBeat, allowDotted = false) {
	try {
		if (!(ticks > 0) || !(ticksPerBeat > 0)) return [];
		console.debug(`Utils: ticksToDurationList input: ticks=${ticks}, tpb=${ticksPerBeat}, dots=${allowDotted}`);

		const q = ticksPerBeat;
		const cleanTable = [
			[4 * q, 'w'], [2 * q, 'h'], [1 * q, 'q'],
			[0.5 * q, '8'], [0.25 * q, '16'], [0.125 * q, '32'], [0.0625 * q, '64']
		];
		const dottedTable = [
			[6 * q, 'w.'], [4 * q, 'w'],
			[3 * q, 'h.'], [2 * q, 'h'],
			[1.5 * q, 'q.'], [1 * q, 'q'],
			[0.75 * q, '8.'], [0.5 * q, '8'],
			[0.375 * q, '16.'], [0.25 * q, '16'],
			[0.1875 * q, '32.'], [0.125 * q, '32'],
			[0.09375 * q, '64.'], [0.0625 * q, '64']
		];
		const table = allowDotted ? dottedTable : cleanTable;
		console.debug(`Utils: using ${allowDotted ? 'dotted' : 'clean'} table`);

		const tol = q / 32; // допуск
		let rem = ticks;
		const out = [];
		while (rem > tol) {
			let picked = false;
			for (const [step, code] of table) {
				if (rem >= step - tol) {
					out.push(code);
					rem -= step;
					picked = true;
					console.debug(`Utils: picked ${code}, remaining=${rem}`);
					break;
				}
			}
			if (!picked) break;
		}
		console.debug(`Utils: final result=${out.join(',')}`);
		return out;
	} catch (err) {
		console.warn('ticksToDurationList failed:', err);
		return [];
	}
}

// Зворотносумісна "чиста" версія (без крапок)
function ticksToCleanDurations(ticks, ticksPerBeat) {
	return ticksToDurationList(ticks, ticksPerBeat, false);
}



