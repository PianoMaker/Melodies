// ----------------------
// Розраховує кількість рядків для нотного стану
// ----------------------
function calculateRows(measures, GENERALWIDTH, BARWIDTH, CLEFZONE = 60, Xmargin = 10) {
	try {
		console.debug('calculateRows called with:', { measures, GENERALWIDTH, BARWIDTH, CLEFZONE, Xmargin });
		let rows = 1;
		let x = Xmargin;		
		const actualBarWidth = (typeof GetMeanBarWidth === 'function') ? GetMeanBarWidth(BARWIDTH, GENERALWIDTH) : BARWIDTH;
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
function GetMeanBarWidth(BARWIDTH, effectiveWidth) {
	// Обчислюємо доступну ширину для тактів без CLEFZONE

	const measuresPerRow = Math.floor(effectiveWidth / BARWIDTH) || 1;	

	// Розподіляємо ширину рівномірно	
	const meanBarWidth = Math.floor(effectiveWidth / measuresPerRow);

	console.debug(`MR: GetMeanBarWidth = ${meanBarWidth}, effectiveWidth=${effectiveWidth}, measuresPerRow=${measuresPerRow}`);
	return meanBarWidth;
}
if (typeof window !== 'undefined') window.GetMeanBarWidth = GetMeanBarWidth;


//-----------------------
// Розраховує кількість рядків для нотного стану з фіксованою шириною такту
// ----------------------
function calculateRowsFixedWidth(measuresArr, generalWidth, fixedBarWidth, clefZone = 60, xMargin = 10) {
	try {
		console.debug('calculateRowsFixedWidth called with:', { measuresArr, generalWidth, fixedBarWidth, clefZone, xMargin });
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
	console.debug(`calculateRequiredHeight is called, BARWIDTH = ${BARWIDTH}`);
	const rows = calculateRows(measures, GENERALWIDTH, BARWIDTH, CLEFZONE, Xmargin);
	return Math.ceil(rows * HEIGHT) + TOPPADDING;
}

//ймовірно зайва//----------------------
function calculateRowsFromPattern(measures, GEN_WIDTH, actualBarWidth, CLEFZONE, Xmargin) {
	console.debug(`calculateRowsFromPattern is called with act barwidth = ${actualBarWidth}`)
	return (typeof window.calculateRowsFixedWidth === 'function')
		? window.calculateRowsFixedWidth(measures, GEN_WIDTH, actualBarWidth, CLEFZONE, Xmargin)
		: (function fallbackCalc(measuresArr, generalWidth, fixedBarWidth, clefZone, xMargin) {
			console.debug(`fallbackCalc is called: ${measuresArr, generalWidth, fixedBarWidth}`)
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
		})(measures, GEN_WIDTH, actualBarWidth, CLEFZONE, Xmargin);
}

