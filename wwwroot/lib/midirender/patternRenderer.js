// patternRenderer.js
// Утиліта для рендерингу нот з рядка шаблону (не MIDI)
// --------------------------------------------------------
// Вхідний рядок шаблону: послідовність нот/пауз, кожна з трьох частин:
// 1) Буква ноти (c,d,e,f,g,a,h) або 'p'/'r' для паузи
//    - 'h' в європейській системі відповідає 'B' в англійській
// 2) Маркери октави: апострофи (') для підвищення, коми (,) для пониження
// 3) Код тривалості: цифра (1,2,4,8,16,32) з необов'язковою крапкою (.) для подовження
//	- 1 = ціла, 2 = половинна, 4 = чверть, 8 = восьма, 16 = шістнадцята, 32 = тридцять друга
// 4) Необов'язковий суфікс для альтерації: 'is' для диеза (#), 'es' або 's' для бемоля (b)
//    - 'b' самостійно означає 'H' з бемолем (Bb)
// Параметри:
// - pattern: рядок шаблону, наприклад "c4_d4_e4_f4_g4_a4_h4_c'4"
// - ELEMENT_FOR_RENDERING: id елемента для рендерингу нот
// - ELEMENT_FOR_COMMENTS: id елемента для повідомлень/коментарів
// - numerator, denominator: розмір такту (4/4 за замовчуванням)
// - GENERALWIDTH, HEIGHT, TOPPADDING, BARWIDTH, CLEFZONE, Xmargin: параметри рендерингу
// --------------------------------------------------------

(function () {
	const BASE_OCTAVE = 4; // default octave for tokens without marks

	function mapEuBase(letter) {
		// EU 'h' -> 'B'
		const m = { c: 'C', d: 'D', e: 'E', f: 'F', g: 'G', a: 'A', h: 'B' };
		return m[letter] || null;
	}

	// Створює об'єкт ноти VexFlow
	// Параметр: durationCode: 'w', 'h', 'q', '8', '16', '32' (може бути з крапкою, напр. 'q.')
	function parseToken(rawToken) {
		if (!rawToken) return null;
		let token = String(rawToken).trim();
		if (!token) return null;

		// Extract duration (digits + optional dot) from the end
		const match = token.match(/(\d+\.?$)/);
		if (!match) return null; // duration is required
		const durStr = match[1];
		const durNum = parseInt(durStr, 10);
		const dotted = /\.$/.test(durStr);
		token = token.slice(0, token.length - durStr.length); // remove duration part

		// Rest detection: allow tokens starting with 'p'|'r' to be rests
		const isRest = /^p|^r/i.test(token);

		// Count octave marks (apostrophes up, commas down)
		const ups = (token.match(/'/g) || []).length;
		const downs = (token.match(/,/g) || []).length;
		const octave = BASE_OCTAVE + ups - downs;
		token = token.replace(/[',]/g, '');

		// Determine accidental from suffix: 'is' -> sharp, 'es' or trailing 's' (not 'is') -> flat
		let accidental = null;
		let base = token.toLowerCase();
		if (base.endsWith('is')) { accidental = '#'; base = base.slice(0, -2); }
		else if (base.endsWith('es')) { accidental = 'b'; base = base.slice(0, -2); }
		else if (base.endsWith('s')) { accidental = 'b'; base = base.slice(0, -1); }

		// Special-case: German notation single 'b' means Bb (B-flat)
		// If user wrote just 'b' (without 'es'/'is'), default to B with flat accidental
		let letterName = null;
		if (base === 'b') {
			letterName = 'B';
			if (!accidental) accidental = 'b';
		} else {
			letterName = mapEuBase(base);
		}

		if (!letterName) {
			// unknown base; treat as rest
			return { isRest: true, durationCode: mapDurationToVex(durNum, dotted) };
		}

		const vexKey = `${letterName}/${octave}`; // VexFlow format
		const durationCode = mapDurationToVex(durNum, dotted);
		return { isRest, key: vexKey, accidental, durationCode };
	}

	function mapDurationToVex(num, dotted) {
		let code;
		switch (num) {
			case 1: code = 'w'; break;
			case 2: code = 'h'; break;
			case 4: code = 'q'; break;
			case 8: code = '8'; break;
			case 16: code = '16'; break;
			case 32: code = '32'; break;
			default: code = 'q'; break; // fallback quarter
		}
		return dotted ? `${code}.` : code;
	}


	// Place near the other helpers (top of file)
	const UNITS_PER_QUARTER = 8; // 1 quarter = 8 units (1 unit = 1/32 note)

	// Core helper: convert duration code (e.g. 'q', 'h.', '16') to 1/32-based units
	function durationToUnits(code) {
		const s = String(code || 'q');
		const dots = (s.match(/\./g) || []).length;   // supports multiple dots
		const base = s.replace(/\./g, '');            // strip dots only

		let units;
		switch (base) {
			case 'w':  units = 32; break;  // whole = 4 quarters
			case 'h':  units = 16; break;  // half  = 2 quarters
			case 'q':  units = 8;  break;  // quarter
			case '8':  units = 4;  break;  // eighth
			case '16': units = 2;  break;  // sixteenth
			case '32': units = 1;  break;  // thirty-second
			default:   units = 8;  break;  // fallback to quarter
		}

		// Apply dot expansion (1.5x, 1.75x, 1.875x, ...)
		let add = Math.floor(units / 2);
		for (let i = 0; i < dots; i++) {
			units += add;
			add = Math.floor(add / 2);
		}
		return units;
	}


	// Greedy decomposition of a units value to Vex duration codes. Prefer dotted codes when allowed
	function unitsToDurationList(units, allowDotted = true) {
		const dottedTable = [
			[48, 'w.'], [32, 'w'],
			[24, 'h.'], [16, 'h'],
			[12, 'q.'], [ 8, 'q'],
			[ 6, '8.'], [ 4, '8'],
			[ 3, '16.'],[ 2, '16'],
			[ 1, '32']
		];
		const cleanTable = [
			[32, 'w'], [16, 'h'], [8, 'q'], [4, '8'], [2, '16'], [1, '32']
		];
		const table = allowDotted ? dottedTable : cleanTable;

		let u = Math.max(0, Math.round(units));
		const out = [];
		for (const [step, code] of table) {
			while (u >= step) { out.push(code); u -= step; }
		}
		return out;
	}

	// Count dots on a VexFlow StaveNote safely
	function getDotCount(note) {
		try {
			if (!note) return 0;
			if (typeof note.getDots === 'function') return (note.getDots() || []).length || 0;
			if (typeof note.getModifiers === 'function') {
				const mods = note.getModifiers() || [];
				return mods.filter(m =>
					(m && (m.getCategory?.() === 'dots' || m.category === 'dots')) ||
					(typeof Vex !== 'undefined' && Vex.Flow && m instanceof Vex.Flow.Dot)
				).length;
			}
			// fallback: direct property (older VexFlow)
			if (Array.isArray(note.modifiers)) {
				return note.modifiers.filter(m => (m && (m.getCategory?.() === 'dots' || m.category === 'dots'))).length;
			}
		} catch { /* ignore */ }
		return 0;
	}

	// Sum measure length in units (including dotted modifiers)
	function sumUnits(notes) {
  console.log("PR: sumUnits is runiing")
  if (!Array.isArray(notes)) return 0;
  let total = 0;
  for (const n of notes) {
    if (!n || typeof n.getDuration !== 'function') continue;

    let codeStr = typeof n.__durationCode === 'string' ? n.__durationCode : String(n.getDuration() || 'q');
    // Strip rest suffix for base mapping (e.g. 'qr' => 'q')
    const baseCode = codeStr.replace(/r$/, '');

    // durationToUnits вже врахує крапки, якщо вони є в baseCode
    let units = durationToUnits(baseCode);

    // Додавати крапки лише якщо їх немає в коді, але VexFlow додав модифікатори
    if (!baseCode.includes('.')) {
      const dotCount = getDotCount(n);
      if (dotCount > 0) {
        const factor = 2 - Math.pow(0.5, dotCount);
        units = Math.round(units * factor);
      }
    }

    total += units;
  }
  return total;
	}

	function fillRestsToCapacity(notes, remainingQuarters) {
		// Fill the rest of a measure with rests using largest possible durations
		if (remainingQuarters <= 0 || !Array.isArray(notes)) return;
		const units = [4, 2, 1, 0.5, 0.25, 0.125];
		const codeMap = { 4: 'w', 2: 'h', 1: 'q', 0.5: '8', 0.25: '16', 0.125: '32' };
		let rem = remainingQuarters;
		for (let u of units) {
			while (rem >= u - 1e-9) {
				const code = codeMap[u];
				const rest = createRest(code);
				if (rest) notes.push(rest);
				rem -= u;
			}
		}
	}

	/**
	 * Renders notation from a pattern string into an element (non-MIDI path)
	 */
	function renderPatternString(
		pattern,
		ELEMENT_FOR_RENDERING,
		ELEMENT_FOR_COMMENTS,
		numerator = 4,
		denominator = 4,
		GENERALWIDTH = 1200,
		HEIGHT = 200,
		TOPPADDING = 20,
		BARWIDTH = 250,
		CLEFZONE = 60,
		Xmargin = 10
	) {
		try {
			const notationDiv = document.getElementById(ELEMENT_FOR_RENDERING);
			const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
			if (!notationDiv) throw new Error(`Element with id ${ELEMENT_FOR_RENDERING} not found.`);
			if (commentsDiv) commentsDiv.innerHTML = '';
			notationDiv.innerHTML = '';

			const tokens = String(pattern || '')
				.split('_')
				.map(t => t.trim())
				.filter(t => t.length > 0);
			if (tokens.length === 0) {
				if (commentsDiv) commentsDiv.innerHTML = 'Порожній шаблон нот';
				return;
			}

			// Parse tokens -> items
			const items = tokens.map(parseToken).filter(Boolean);

			// Build measures with cross-bar splitting + ties
			const capacityQuarters = numerator * (4 / denominator); // quarters per bar
			const capacityUnits = Math.round(capacityQuarters * UNITS_PER_QUARTER);
			const measures = []; // array of { notes: StaveNote[], ties: StaveTie[] }
			let cur = { notes: [], ties: [] };
			let usedUnits = 0;
			measures.push(cur);

			for (const it of items) {
				let remaining = durationToUnits(it.durationCode);
				let prevPieceNote = null; // last piece of the same logical note to tie from

				while (remaining > 0) {
					if (usedUnits >= capacityUnits) {
						// start new bar
						cur = { notes: [], ties: [] };
						measures.push(cur);
						usedUnits = 0;
					}

					const room = capacityUnits - usedUnits;
					const pieceUnits = Math.min(remaining, room);
					const pieceCodes = unitsToDurationList(pieceUnits, /*allowDotted*/ true);

					for (let i = 0; i < pieceCodes.length; i++) {
						const code = pieceCodes[i];
						if (it.isRest) {
							const r = createRest(code);
							if (r) { r.__durationCode = code; cur.notes.push(r); }
							prevPieceNote = null; // rests break ties
						} else {
							const n = processNoteElement(code, it.key, it.accidental);
							if (n) n.__durationCode = code;
							cur.notes.push(n);
							if (prevPieceNote) cur.ties.push(new Vex.Flow.StaveTie({ first_note: prevPieceNote, last_note: n }));
							prevPieceNote = n;
						}
					}

					usedUnits += pieceUnits;
					remaining -= pieceUnits;
				}
			}

			// Drop trailing empty measure if any
			if (measures.length && measures[measures.length - 1].notes.length === 0) {
				measures.pop();
			}

			// Ensure each bar is filled with trailing rests (non‑dotted) to capacity
			for (const m of measures) {
				console.info('PR: calling sumUnits for fill check');
				const used = sumUnits(m.notes);
				const leftUnits = Math.max(0, capacityUnits - used);
				console.info('PR: leftUnits', leftUnits, 'capacityUnits', capacityUnits, 'used', used);
				const restCodes = unitsToDurationList(leftUnits, /*allowDotted*/ false);
				for (const rc of restCodes) {
					const r = createRest(rc);
					if (r) { r.__durationCode = rc; m.notes.push(r); }
				}
			}

			// Compute width/height
			const MIN_SCORE_WIDTH = Math.max(320, CLEFZONE + BARWIDTH + Xmargin * 2);
			const containerWidth = (notationDiv && notationDiv.clientWidth) ? notationDiv.clientWidth : 0;
			const effectiveWidth = Math.max(MIN_SCORE_WIDTH, containerWidth || GENERALWIDTH || 1200);

			// 1) Єдиний bar width для всієї сесії рендера
			// МІНІМАЛЬНА ШИРИНА ТАКТУ, щоб не були вузькими
			const MIN_BARWIDTH = 240; // налаштовуване значення
			const naiveBarWidth = GetMeanBarWidth(BARWIDTH, measures.map(m => m.notes));
			const actualBarWidth = Math.max(MIN_BARWIDTH, naiveBarWidth);

			// 2) Локальний підрахунок кількості рядків за вже обчисленим actualBarWidth (спільна утиліта)
			const rows = (typeof window.calculateRowsFixedWidth === 'function')
				? window.calculateRowsFixedWidth(measures, effectiveWidth, actualBarWidth, CLEFZONE, Xmargin)
				: (function fallbackCalc(measuresArr, generalWidth, fixedBarWidth, clefZone, xMargin){
					let rows = 1;
					let x = xMargin;
					for (let i = 0; i < measuresArr.length; i++) {
						const isFirstInRow = (x === xMargin);
						let staveWidth = fixedBarWidth + (isFirstInRow ? clefZone : 0);
						if (x + staveWidth > generalWidth) {
							rows++;
							x = xMargin;
							staveWidth = fixedBarWidth + clefZone;
						}
						x += staveWidth;
					}
					return rows;
				})(measures, effectiveWidth, actualBarWidth, CLEFZONE, Xmargin);

			// Мінімальний запас по висоті: TOPPADDING (вгорі) + невеликий буфер 10px
			const extra = Math.max(10, TOPPADDING || 0);
			const totalHeight = rows * HEIGHT + extra;

			console.log(`patternRenderer: ${measures.length} measures, ${rows} rows, totalHeight=${totalHeight}, effectiveWidth=${effectiveWidth}, actualBarWidth=${actualBarWidth}`);

			// 3) Передаємо той самий actualBarWidth у рендер (щоб обтікання збігалось)
			const factory = new Vex.Flow.Factory({
				renderer: { elementId: ELEMENT_FOR_RENDERING, width: effectiveWidth, height: totalHeight }
			});
			const context = factory.getContext();
			const score = factory.EasyScore();

			// Render measure by measure similar to renderMeasures
			let Xposition = Xmargin;
			let Yposition = TOPPADDING;
			let isFirstMeasureInRow = true;

			for (let i = 0; i < measures.length; i++) {
				const previousY = Yposition;
				({ Xposition, Yposition } = adjustXYposition(
					Xposition, effectiveWidth, actualBarWidth, Yposition, HEIGHT, Xmargin, i, 0));
				if (Yposition !== previousY) {
					isFirstMeasureInRow = true;
				}

				let STAVE_WIDTH = adjustStaveWidth(actualBarWidth, i, CLEFZONE, isFirstMeasureInRow, false);
				const stave = setStave(Xposition, Yposition, STAVE_WIDTH, i, numerator, denominator, isFirstMeasureInRow, false, false, null);
				stave.setContext(context).draw();

				const ties = measures[i].ties || [];
				drawMeasure(measures[i].notes, actualBarWidth, context, stave, ties, i, commentsDiv || { innerHTML: '' }, numerator, denominator, 480);

				Xposition += STAVE_WIDTH;
				isFirstMeasureInRow = false;
			}

			// Post‑adjust: висоту доводимо до max(totalHeight, bbox.height + extra)
			try {
				const container = document.getElementById(ELEMENT_FOR_RENDERING);
				const svg = container && container.querySelector('svg');
				if (svg && typeof svg.getBBox === 'function') {
					const adjust = () => {
						const bbox = svg.getBBox();
						const contentH = Math.max(totalHeight, Math.ceil((bbox ? bbox.height : 0) + extra));
						if (contentH > 0) {
							const vb = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
							const vbW = vb && vb.width ? vb.width : (parseFloat(svg.getAttribute('width')) || effectiveWidth);
							if (vbW > 0) svg.setAttribute('viewBox', `0 0 ${vbW} ${contentH}`);
							svg.setAttribute('height', String(contentH));
							if (svg.style) {
								svg.style.height = contentH + 'px';
								svg.style.maxHeight = 'none';
							}
							if (container && container.style) {
								container.style.minHeight = contentH + 'px';
								container.style.height = contentH + 'px';
								container.style.maxHeight = 'none';
								container.style.overflow = 'visible';
							}
						}
					};
					adjust();
					if (typeof requestAnimationFrame === 'function') requestAnimationFrame(adjust);
					setTimeout(adjust, 50);
				}
			} catch (e) {
				console.warn('patternRenderer post-fix svg size failed', e);
			}

			if (commentsDiv) {
				commentsDiv.innerHTML += `Рядок нот успішно згенеровано (${measures.length} тактів)`;
			}
		} catch (e) {
			console.error('renderPatternString error:', e);
			const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
			if (commentsDiv) commentsDiv.innerHTML = `Error: ${e.message}`;
		}
	}


	// expose API
	window.renderPatternString = renderPatternString;
	console.info('PR build loaded @', new Date().toISOString());
})();
