// ----------------------
// Розраховує кількість рядків для нотного стану
// ----------------------
function calculateRows(measures, GENERALWIDTH, BARWIDTH, CLEFZONE = 60, Xmargin = 10) {
	try {
		let rows = 1;
		let x = Xmargin;
		const actualBarWidth = (typeof GetMeanBarWidth === 'function') ? GetMeanBarWidth(BARWIDTH, measures) : BARWIDTH;
		const measuresCount = (measures && measures.length) || 0;
		for (let i = 0; i < measuresCount; i++) {
			const isFirstInRow = (x === Xmargin);
			let staveWidth = actualBarWidth + (isFirstInRow ? CLEFZONE : 0);
			if (x + staveWidth > GENERALWIDTH) {
				rows++;
				x = Xmargin;
				staveWidth = actualBarWidth + CLEFZONE;
			}
			x += staveWidth;
		}
		return rows;
	} catch (e) {
		console.warn('calculateRows failed:', e);
		return 1;
	}
}


// ----------------------
// Розраховує середню ширину такту на основі кількості нот у кожному такті
// ----------------------
function GetMeanBarWidth(BARWIDTH, measures) {

	// If measures is not a non-empty array — just return default BARWIDTH without noisy warnings
	if (!Array.isArray(measures) || measures.length === 0) {
		console.debug("meanBarWidth: measures empty or invalid, using BARWIDTH fallback");
		return BARWIDTH;
	}

	console.debug("MR: FOO: midiparser_ext.js - meanBarWidth");
	let meanBarWidth = BARWIDTH;
	let sumBarWidth = 0;
	let currentWidth;

	measures.forEach((m) => {
		let notesamount = getNumberOfNotes(m);
		if (notesamount !== undefined) {
			currentWidth = meanBarWidth / 3 + meanBarWidth * notesamount / 7;
			sumBarWidth += currentWidth;
		}
	});

	meanBarWidth = sumBarWidth / measures.length;
	console.log(`meanBarWidth total: ${meanBarWidth}`);

	return meanBarWidth;
}
if (typeof window !== 'undefined') window.GetMeanBarWidth = GetMeanBarWidth;


//-----------------------
// Розраховує кількість рядків для нотного стану з фіксованою шириною такту
// ----------------------
function calculateRowsFixedWidth(measuresArr, generalWidth, fixedBarWidth, clefZone = 60, xMargin = 10) {
	try {
		let rows = 1;
		let x = xMargin;
		const count = Array.isArray(measuresArr) ? measuresArr.length : 0;
		for (let i = 0; i < count; i++) {
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
	} catch (e) {
		console.warn('calculateRowsFixedWidth failed:', e);
		return 1;
	}
}

// ----------------------
// Розраховує необхідну висоту для нотного стану на основі кількості рядків
// ----------------------
function calculateRequiredHeight(measures, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING = 20, CLEFZONE = 60, Xmargin = 10) {
	const rows = calculateRows(measures, GENERALWIDTH, BARWIDTH, CLEFZONE, Xmargin);
	return Math.ceil(rows * HEIGHT) + TOPPADDING;
}

