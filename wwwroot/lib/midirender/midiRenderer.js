// midiRenderer.js

/**
 * screen log if stepReadEl exists
 */

if (typeof window.logEvent === 'undefined') {
	window.logEvent = function (msg) {
		try {
			const stepReadEl = document.getElementById("stepRead");
			if (stepReadEl) {
				stepReadEl.innerHTML += msg;
			}
		} catch (e) {
			console.warn('logEvent failed:', e);
		}
	};
}


/**
 * renderMidiFromUrl
 * -----------------
 * Convenience helper that loads a MIDI file by URL (fetch), wraps it into a File
 * object (so existing drawScore logic & FileReader path can be reused), then calls drawScore.
 * All layout parameters mirror drawScore and have the same defaults.
 *
 * @param {string} midiUrl - URL (relative or absolute) to .mid file
 * @param {number} [maxBarsToRender=1000] - Maximum number of measures to render (1000 means full rendering).
 * @param {string} [ELEMENT_FOR_RENDERING='notation'] - target element id for notation
 * @param {string} [ELEMENT_FOR_COMMENTS='comments'] - target element id for messages
 * @param {number} [GENERALWIDTH=1200]
 * @param {number} [HEIGHT=200]
 * @param {number} [TOPPADDING=20]
 * @param {number} [BARWIDTH=250]
 * @param {number} [CLEFZONE=60]
 * @param {number} [Xmargin=10]
 * @returns {Promise<void>}
 *
 * Usage example:
 *   await renderMidiFromUrl('/Uploads/example.mid', 8);
 */
async function renderMidiFromUrl(
	midiUrl,
	maxBarsToRender = 1000,
	ELEMENT_FOR_RENDERING = 'notation',
	ELEMENT_FOR_COMMENTS = 'comments',
	GENERALWIDTH = 1200,
	HEIGHT = 170,
	TOPPADDING = 20,
	BARWIDTH = 250,
	CLEFZONE = 60,
	Xmargin = 10,
	RESPONSIVE_THRESHOLD = 800
) {
	try {
		if (!midiUrl) throw new Error('midiUrl is required');
		const resp = await fetch(midiUrl);
		if (!resp.ok) throw new Error(`Failed to fetch MIDI (${resp.status})`);
		const blob = await resp.blob();
		const filename = midiUrl.split('/').pop() || 'remote.mid';
		const file = new File([blob], filename, { type: blob.type || 'audio/midi' });
		console.debug(`MR: FOO: renderMidiFromUrl GW = ${GENERALWIDTH} height =${HEIGHT} maxBarsToRender = ${maxBarsToRender}`)

		drawScore(file, ELEMENT_FOR_RENDERING, ELEMENT_FOR_COMMENTS, GENERALWIDTH, HEIGHT, TOPPADDING, BARWIDTH, CLEFZONE, Xmargin, maxBarsToRender, RESPONSIVE_THRESHOLD);



	} catch (err) {
		console.error('renderMidiFromUrl error:', err);
		const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
		if (commentsDiv) {
			commentsDiv.innerHTML = `Error loading MIDI: ${err.message}`;
		}
	}
}




/**
 * drawScore
 * ----------
 * Renders a musical score from a MIDI file and displays it in the specified HTML element.
 *
 * @param {File} file - The MIDI file to render.
 * @param {string} ELEMENT_FOR_RENDERING - The ID of the HTML element where the score SVG will be rendered.
 * @param {string} ELEMENT_FOR_COMMENTS - The ID of the HTML element where status or error messages will be displayed.
 * @param {number} [GENERALWIDTH=1200] - The width of the rendered score in pixels.
 * @param {number} [HEIGHT=200] - The height of each stave row in pixels.
 * @param {number} [TOPPADDING=20] - The top padding for the score in pixels.
 * @param {number} [BARWIDTH=250] - The width of each measure/bar in pixels.
 * @param {number} [CLEFZONE=60] - The width reserved for the clef and time signature zone in pixels.
 * @param {number} [Xmargin=10] - The left margin for the score in pixels.
 * @param {number} [maxBarsToRender=1000] - Maximum number of measures to render (1000 means full rendering).
 * @param {number} [RESPONSIVE_THRESHOLD=800] - Responsive threshold for narrow screens
 *
 * Механізм - викликає renderMidiFileToNotation, додає повідомлення про успішний або провальний результат.
 */
function drawScore(file, ELEMENT_FOR_RENDERING, ELEMENT_FOR_COMMENTS, GENERALWIDTH = 1200, HEIGHT = 170, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10, maxBarsToRender = 1000, RESPONSIVE_THRESHOLD = 800) {
	console.debug("MR: FOO: midiRenderer.js - drawScore");
	const notationDiv = document.getElementById(ELEMENT_FOR_RENDERING);
	const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);

	if (file) {
		console.debug("drawScore: File selected:", file);
		const reader = new FileReader();
		notationDiv.innerHTML = "";
		commentsDiv.innerHTML = "";
		reader.onload = function (e) {
			const uint8 = new Uint8Array(e.target.result);
			var { allEvents, ticksPerBeat, isMidi0 } = extractEventsFromArray(uint8);
			let initialKeySig = getKeySignatures(allEvents, uint8);
			try {
				renderMidiFileToNotation(
					isMidi0,
					allEvents,
					ticksPerBeat,
					ELEMENT_FOR_RENDERING,
					GENERALWIDTH,
					HEIGHT,
					TOPPADDING,
					BARWIDTH,
					CLEFZONE,
					Xmargin,
					commentsDiv,
					maxBarsToRender,
					0, // star from measure idx 0
					initialKeySig,
					RESPONSIVE_THRESHOLD
				);

				const svg = notationDiv.querySelector("svg");
				if (svg) {
					sessionStorage.setItem("notationSVG", svg.outerHTML);
				}

				// Message depends on maxBarsToRender value
				const msg = (maxBarsToRender === 1000)
					? "нотне зображення виведено повністю"
					: `нотне зображення виведено з обмеженням у ${maxBarsToRender} тактів`;
				commentsDiv.innerHTML += msg;
				sessionStorage.setItem("comment", commentsDiv.innerHTML);
			}
			catch (error) {
				console.error("drawScore: Error rendering MIDI file:", error);
				commentsDiv.innerHTML = `Error rendering MIDI file ${error}`;
				sessionStorage.setItem("comment", commentsDiv.innerHTML);
			}
		};
		reader.readAsArrayBuffer(file);
	}
}


/**
 * renderMidiSegment`FromUrl
 * ------------------------
 * Loads MIDI by URL and renders a segment starting from the measure nearest to (and <=) the
 * measure that contains the startNoteIndex-th NoteOn event. Start measure is snapped to multiples of 4 (0,4,8,...).
 * All layout parameters mirror drawScore and have the same defaults.
 * @param {string} midiUrl - URL (relative or absolute) to .mid file
 * @param {number} [startNoteIndex=0] - zero-based index of NoteOn event to start from
 * @param {string} [ELEMENT_FOR_RENDERING='notation'] - target element id for notation
 * @param {string} [ELEMENT_FOR_COMMENTS='comments'] - target element id for messages
 * @param {number} [GENERALWIDTH=1200]
 * @param {number} [HEIGHT=200]
 * @param {number} [TOPPADDING=20]
 * @param {number} [BARWIDTH=250]
 * @param {number} [CLEFZONE=60]
 * @param {number} [Xmargin=10]
 * @param {number} [barsToRender=12] - number of measures to render in the segment
 * @param {numner} [RESPONSIVE_THRESHOLD=800] - respnosive thrshold for narrow screens
 * @returns {Promise<void>}
 * Usage example:
 *  await renderMidiSegmentFromUrl('/Uploads/example.mid', 10, 'notation', 'comments', 1200, 200, 20, 250, 60, 10, 12);
  ====================
  Механізм: 
  1. обчислює startAtMeasureIndex, що вказує з якого такту стартує рендер 
  2. запускає renderMidiFileToNotation із заданим тактом початку
 */
async function renderMidiSegmentFromUrl(
	midiUrl,
	startNoteIndex = 0,
	ELEMENT_FOR_RENDERING = 'notation',
	ELEMENT_FOR_COMMENTS = 'comments',
	GENERALWIDTH = 1200,
	HEIGHT = 150,
	TOPPADDING = 20,
	BARWIDTH = 250,
	CLEFZONE = 60,
	Xmargin = 10,
	barsToRender = 12,
	RESPONSIVE_THRESHOLD = 800
) {
	try {
		//HEIGHT = 150;
		console.info(`MR: FOO: renderMidiSegmentFromUrl GW = ${GENERALWIDTH} height =${HEIGHT}`)
		if (!midiUrl) throw new Error('midiUrl is required');
		const resp = await fetch(midiUrl);
		if (!resp.ok) throw new Error(`Failed to fetch MIDI (${resp.status})`);
		const arrayBuf = await resp.arrayBuffer();
		const uint8 = new Uint8Array(arrayBuf);

		// Pre-parse to find measure index for the matched note
		var { allEvents, ticksPerBeat, isMidi0 } = extractEventsFromArray(uint8);

		// ро
		const measureMap = createMeasureMap(allEvents, ticksPerBeat);

		// Знаходимо абсолютний час події, з якої починаємо
		let targetAbsTime = getTargetAbsTime(allEvents, startNoteIndex);

		// знаходимо з якого такту починаємо
		let startAtMeasureIndex = getStartMeasuerIndex(measureMap, targetAbsTime);

		// --- determine initial key signature active at targetAbsTime ---
		let initialKeySig = getInitialKeySignatures(allEvents, targetAbsTime, uint8);		

		// Render the segment		
		const ksDisplay = initialKeySig ? `${initialKeySig.sf} : ${initialKeySig.mi}` : 'null';
		+		console.debug(`renderMidiSegmentFromUrl(220): startNoteIndex=${startNoteIndex}, targetAbsTime=${targetAbsTime}, startAtMeasureIndex=${startAtMeasureIndex} height=${HEIGHT}, KeySig = ${ksDisplay}`);
		const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
		renderMidiFileToNotation(
			isMidi0,
			allEvents,
			ticksPerBeat,
			ELEMENT_FOR_RENDERING,
			GENERALWIDTH,
			HEIGHT,
			TOPPADDING,
			BARWIDTH,
			CLEFZONE,
			Xmargin,
			commentsDiv,
			barsToRender,
			startAtMeasureIndex,
			initialKeySig,
			RESPONSIVE_THRESHOLD
		);
	} catch (err) {
		console.error('renderMidiSegmentFromUrl error:', err);
		const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
		if (commentsDiv) commentsDiv.innerHTML = `Error loading MIDI: ${err.message}`;
	}
}



/**
 * createmeasureMap
 * ----------------
 * Generates a mapping of measure indexes to the starting ticks for each measure.
 *
 * @param {Array} midiEvents - Array of MIDI event objects (each should have an "absTime" property).
 * @param {number} ticksPerBeat - The number of ticks per quarter note.
 * @returns {Object} An object where each key is a measure index (starting at 0) and the value is the start tick.
 *
 * The function handles time signature changes by processing all time signature events.
 * If no time signature event is found at tick 0, a default 4/4 is inserted.
 */
function createMeasureMap(midiEvents, ticksPerBeat) {
	console.debug("MR: FOO: midiRenderer.js - createmeasureMap");
	const stepRead = document.getElementById("stepRead");
	if (stepRead) stepRead.innerHTML = `<p>Step Read</p><br/>`;
	const measure_map = document.getElementById("measureMap");
	if (measure_map) measure_map.innerHTML = `<p>Measure Map</p><br/>`;
	const logMeasure = (msg) => {
		if (measure_map) measure_map.innerHTML += msg + "<br>";
		console.log(msg);
	};


	if (midiEvents.length === 0) return {};

	// Ensure time signature at tick 0
	if (!(midiEvents[0].type === 0xFF && midiEvents[0].metaType === 0x58)) {
		midiEvents.unshift({
			type: 0xFF,
			metaType: 0x58,
			deltaTime: 0,
			absTime: 0,
			data: [4, 2] // 4/4
		});
	}

	const timeSignatureEvents = midiEvents.filter(e => e.type === 0xFF && e.metaType === 0x58);
	const maxAbsTime = midiEvents.reduce((m, e) => e.absTime > m ? e.absTime : m, 0);

	const measureMap = {};
	let barIndex = 0;

	for (let i = 0; i < timeSignatureEvents.length; i++) {
		const ts = timeSignatureEvents[i];
		const numerator = ts.data?.[0] ?? 4;
		const denominator = ts.data?.[1] ? Math.pow(2, ts.data[1]) : 4;
		const ticksPerMeasure = ticksPerBeat * numerator * 4 / denominator;

		const segmentStart = ts.absTime;
		const segmentEnd = (i + 1 < timeSignatureEvents.length)
			? timeSignatureEvents[i + 1].absTime
			: maxAbsTime;

		for (let tick = segmentStart; tick < segmentEnd; tick += ticksPerMeasure) {
			measureMap[barIndex] = tick;
			barIndex++;
			logMeasure(`measureMap: Measure ${barIndex} starts at tick ${tick}`);
		}

		// Keep reference to last segment properties for trailing sentinel
		if (i === timeSignatureEvents.length - 1) {
			const lastStart = measureMap[barIndex - 1];
			const sentinelStart = lastStart + ticksPerMeasure; // always add sentinel
			measureMap[barIndex] = sentinelStart;
			console.debug(`Sentinel (end) start added at tick ${sentinelStart} (index ${barIndex})`);
			logMeasure(`Sentinel measure start at tick ${sentinelStart}`);
		}
	}

	return measureMap;
}

// ----------------------
// Helper: ensure EndTrack event exists
// ----------------------
function groupEventsByMeasure(allEvents, measureMap) {
	console.debug("MR: FOO: midiRenderer.js - groupEventsByMeasure");
	const measures = [];
	const indices = Object.keys(measureMap)
		.map(k => parseInt(k))
		.sort((a, b) => a - b);
	// Last index is sentinel (start of measure after last real), so iterate to length-1
	for (let i = 0; i < indices.length - 1; i++) {
		const startTick = measureMap[indices[i]];
		const nextTick = measureMap[indices[i + 1]];
		const measureEvents = allEvents.filter(ev => ev.absTime >= startTick && ev.absTime < nextTick);
		measures.push(measureEvents);
	}
	return measures;
}

// Helper: check a measure for any Note On with velocity > 0
function hasNoteOn(measure) {
	console.debug("MR: FOO: midiRenderer.js - hasNoteOn");
	return measure.some(ev => ev.type === 0x9 && ev.data && ev.data.length > 1 && ev.data[1] !== 0);
}





// ФУНКЦІЯ ВІЗУАЛІЗАЦІЇ MIDI ФАЙЛУ
// ----------------------
// Приймає Uint8Array з MIDI файлом, ID елемента для рендерингу, ширину і висоту нотного стану та інші параметри.
// Використовує бібліотеки MidiParser и VexFlow для парсингу MIDI та рендерингу нот.
// Рендерить нотний стан у вказаний HTML елемент.
// Параметри:
// - uint8: Uint8Array з MIDI файлом.
// - ELEMENT_FOR_RENDERING: ID HTML елемента для рендерингу нот.
// - GENERALWIDTH: Ширина нотного стану в пікселях.
// - HEIGHT: Висота кожного рядка нотного стану в пікселях.
// - TOPPADDING: Верхній відступ нотного стану в пікселях.
// - BARWIDTH: Ширина кожного такту в пікселях.
// - CLEFZONE: Ширина зони для ключа та розміру такту в пікселях.
// - Xmargin: Лівий відступ нотного стану в пікселях.
// - commentsDiv: HTML елемент для виведення статусу або помилок.
// - maxBarsToRender: Максимальна кількість тактів для рендерингу (1000 - повний рендеринг).
// - startAtMeasureIndex: Індекс такту, з якого починати рендеринг (вирівняний до кратного 4).
// Повертає: void
// ----------------------
// Механізм роботи:
// 1. SetEventsAbsoluteTime впорядковує midi - події,
// 2. groupEventsByMeasure - впорядковує події по тактах
// 3. renderMeasures - рендеринг тактів у циклі forEach

// Рендеринг відбувається асинхронно з використанням setTimeout для уникнення блокування UI.
// ---------------------


function renderMidiFileToNotation(isMidi0, allEvents, ticksPerBeat, ELEMENT_FOR_RENDERING, GENERALWIDTH, HEIGHT = 170, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10, commentsDiv, maxBarsToRender = 1000, startAtMeasureIndex = 0, initialKeySig = null, RESPONSIVE_THRESHOLD = 800) {
	console.debug(`MR: FOO: midiRenderer.js - renderMidiFileToNotation GW = ${GENERALWIDTH}, height = ${HEIGHT}, maxBarsToRender = ${maxBarsToRender}`);
	if (!ELEMENT_FOR_RENDERING) {
		throw new Error(`Element with id ${ELEMENT_FOR_RENDERING} not found.`);
	}
	// Перевірка наявності EndTrack
	ensureEndEvent(allEvents);

	const MIN_SCORE_WIDTH = Math.max(320, CLEFZONE + BARWIDTH + Xmargin * 2);
	const SCALINGFACTOR = 0.5;

	//створюємо список подій по тактах
	const measureMap = createMeasureMap(allEvents, ticksPerBeat);
	const measures = groupEventsByMeasure(allEvents, measureMap);

	//виділяємо такти для відображення
	return renderMeasuresToNotation(startAtMeasureIndex, measures, maxBarsToRender, isMidi0, measureMap, ELEMENT_FOR_RENDERING, MIN_SCORE_WIDTH, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin, RESPONSIVE_THRESHOLD, SCALINGFACTOR, ticksPerBeat, commentsDiv, initialKeySig);
}


function renderMeasuresToNotation(startAtMeasureIndex, measures, maxBarsToRender, isMidi0, measureMap, ELEMENT_FOR_RENDERING, MIN_SCORE_WIDTH, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin, RESPONSIVE_THRESHOLD, SCALINGFACTOR, ticksPerBeat, commentsDiv, initialKeySig) {

	console.debug(`MR: FOO: renderMeasuerToNotation: start from ${startAtMeasureIndex}, length = ${measures.length}, GW = ${GENERALWIDTH}, Scaling = ${SCALINGFACTOR}, Height = ${HEIGHT}`)


    var { measuresWindow, startIdx, remaining } = GetScope(startAtMeasureIndex, measures, maxBarsToRender, isMidi0);
    const { measuresToRender, effectiveCount } = GetBarsWithNotes(measuresWindow);
    const slicedMap = GetSlicedMap(effectiveCount, measureMap, startIdx);

    // Ширина контейнера та рядків
    const { effectiveWidth, containerWidth } = GetWidths(ELEMENT_FOR_RENDERING, MIN_SCORE_WIDTH, GENERALWIDTH);

    // Висота рядків (mr-durations-helper.js)
    const rowsHeight = calculateRequiredHeight(measuresToRender, effectiveWidth, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin);

    const scaleFactor = (containerWidth > 0 && containerWidth <= RESPONSIVE_THRESHOLD) ? SCALINGFACTOR : 1;

    // If we scale the context by scaleFactor, we must allocate a larger renderer
    const { rendererWidth, rendererHeight } = getRenderDimensions(effectiveWidth, scaleFactor, rowsHeight, containerWidth);
	

    setTimeout(() => {
        const factory = new Vex.Flow.Factory({
            renderer: {
                elementId: ELEMENT_FOR_RENDERING,
                width: rendererWidth,
                height: rendererHeight
            }
        });

        const context = factory.getContext();
        const score = factory.EasyScore();
        scaleContext(scaleFactor, context);

        console.debug(`renderMidiFileToNotation: renderer ${rendererWidth}x${rendererHeight}, scaleFactor=${scaleFactor}`);

        const scaledWidth = effectiveWidth / scaleFactor;
		const scaledBARWIDTH = BARWIDTH * Math.sqrt(scaleFactor);

		console.debug(`scaledWidth [RW] = ${scaledWidth} `);

        // РЕНДЕРИНГ ТАКТІВ		
        renderMeasures(slicedMap, measuresToRender, ticksPerBeat, score, context, Xmargin, TOPPADDING, scaledBARWIDTH, CLEFZONE, HEIGHT, scaledWidth, commentsDiv, initialKeySig);

        // ----------------------
        // ПІСЛЯ-ОБРОБКА SVG
        // ----------------------
        // Динамічно підганяє висоту SVG під фактичний вміст, щоб уникнути зайвих відступів.
        try {
            const container = document.getElementById(ELEMENT_FOR_RENDERING);
            const svg = container && container.querySelector('svg');
            adjustSVG(svg);
        } catch (e) { console.warn('post-fix svg size failed', e); }
    }, 0);

    return {
        totalMeasures: measures.length,
        renderedMeasures: effectiveCount,
        limited: effectiveCount < remaining,
        maxBarsToRender,
        startAtMeasureIndex: startIdx
    };
}

function getRenderDimensions(effectiveWidth, scaleFactor, rowsHeight, containerWidth) {
	const rendererWidth = Math.max(Math.round(effectiveWidth / scaleFactor), 200);
	const rendererHeight = Math.max(Math.round(rowsHeight * scaleFactor * scaleFactor ), 100);
	console.log(`renderMidiFileToNotation: rowsHeight = ${rowsHeight}, containerWidth=${containerWidth}`);
	console.log(`renderMidiFileToNotation: rendererHight = ${rowsHeight}, rendererWidth=${containerWidth}`);
	return { rendererWidth, rendererHeight };
}

function adjustSVG(svg) {
	if (svg && typeof svg.getBBox === 'function') {
		const adjust = () => {
			const bbox = svg.getBBox();
			svg.setAttribute('height', bbox.height);
		};
		adjust();
		if (typeof requestAnimationFrame === 'function') requestAnimationFrame(adjust);
		setTimeout(adjust, 50);
	}
}



function GetWidths(ELEMENT_FOR_RENDERING, MIN_SCORE_WIDTH, GENERALWIDTH) {
	const target = document.getElementById(ELEMENT_FOR_RENDERING);
	const containerWidth = (target && target.clientWidth) ? target.clientWidth : 0;

	const effectiveWidth = Math.max(
		MIN_SCORE_WIDTH,
		containerWidth || GENERALWIDTH || 1200
	);
	// ДІАГНОСТИКА: перевірка computed styles
	if (target) {
		const computed = window.getComputedStyle(target);
		console.debug(`MR: Container #${ELEMENT_FOR_RENDERING} - clientWidth=${containerWidth}, computed width=${computed.width}, padding=${computed.paddingLeft}+${computed.paddingRight}`);
	}


	console.debug(`MR: FOO: GetWidth: ${effectiveWidth}, ${containerWidth}`)
	return { effectiveWidth, containerWidth };
}

function GetSlicedMap(effectiveCount, measureMap, startIdx) {
	const slicedMap = {};
	for (let i = 0; i <= effectiveCount; i++) {
		slicedMap[i] = measureMap[startIdx + i];
	}
	return slicedMap;
}

function GetBarsWithNotes(measuresWindow) {
	const measuresToRender = [...measuresWindow];
	while (measuresToRender.length > 0 && !hasNoteOn(measuresToRender[measuresToRender.length - 1])) {
		measuresToRender.pop();
	}
	const effectiveCount = measuresToRender.length;
	console.debug(`MR: FOO: GetBarsWithNotes: measuresToRender = ${measuresToRender} effectiveCount = ${effectiveCount}`);
	// If nothing to render then bail out with a helpful comment
	if (effectiveCount === 0) {
		return handleNoNotesToRender(commentsDiv, ELEMENT_FOR_RENDERING, measures, measuresWindow, maxBarsToRender, startIdx);
	}
	return { measuresToRender, effectiveCount };
}

function GetScope(startAtMeasureIndex, measures, maxBarsToRender, isMidi0) {
	let startIdx = getStartIdx(startAtMeasureIndex, measures);

	// обчислюємо скільки тактів залишилось від startIdx до кінця
	const remaining = Math.max(0, measures.length - startIdx);
	const renderCount = (typeof maxBarsToRender === 'number' && isFinite(maxBarsToRender) && maxBarsToRender !== 1000)
		? Math.min(remaining, maxBarsToRender)
		: remaining;

	// Витягуємо вікно тактів для рендерингу
	const measuresWindow = measures.slice(startIdx, startIdx + renderCount);
	console.debug(`MR: FOO: GetScope: startIdx=${startIdx}, renderCount=${renderCount}, measuresWindow=${measuresWindow.length}`);

	// For MIDI format 0: trim leading empty measures before the first NoteOn so no empty bars appear before music.
	startIdx = adjustStartIdxForMidi0Format(isMidi0, measuresWindow, startIdx);
	return { measuresWindow, startIdx, remaining };
}

function adjustStartIdxForMidi0Format(isMidi0, measuresWindow, startIdx) {
	if (isMidi0) {
		let trimmedLeading = 0;
		while (measuresWindow.length > 0 && !hasNoteOn(measuresWindow[0])) {
			measuresWindow.shift();
			trimmedLeading++;
			startIdx++;
		}
		if (trimmedLeading > 0) console.debug(`MR: trimmed ${trimmedLeading} leading empty measures for MIDI 0`);
	}
	return startIdx;
}

// ФУНКЦІЯ РЕНДЕРИНГУ ТАКТІВ
// ----------------------
// Приймає мапу тактів, масив тактів, кількість тіксів на біт, об'єкт score і context з VexFlow та інші параметри.
// Рендерить кожен такт у нотний стан з урахуванням активних нот, пауз, лігатур та інших музичних елементів.
// Параметри:
// - measureMap: Об'єкт, що відображає індекси тактів у початкові тіки.
// - measures: Масив тактів, де кожен такт — це масив MIDI-подій.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - score: Об'єкт score з VexFlow для створення нот.
// - context: Контекст рендерингу з VexFlow.
// - Xmargin: Лівий відступ нотного стану в пікселях.
// - TOPPADDING: Верхній відступ нотного стану в пікселях.
// - BARWIDTH: Ширина кожного такту в пікселях.
// - CLEFZONE: Ширина зони для ключа та розміру такту в пікселях.
// - HEIGHT: Висота кожного рядка нотного стану в пікселях.
// - GENERALWIDTH: Загальна ширина нотного стану в пікселях.
// - commentsDiv: HTML елемент для виведення статусу або помилок.
// Повертає: void
// ----------------------


function renderMeasures(measureMap, measures, ticksPerBeat, score, context, Xmargin, TOPPADDING, BARWIDTH, CLEFZONE, HEIGHT, GENERALWIDTH, commentsDiv, initialKeySig = null) {
	console.debug("MR: FOO: renderMeasures ");
	let Xposition = Xmargin;
	let Yposition = TOPPADDING;
	let thresholdGap = ticksPerBeat / 8;

	// Початковий розмір такту (за замовчуванням 4/4)
	let currentNumerator = 4;
	let currentDenominator = 4;

	// Initialize currentKeySig from provided initialKeySig when rendering slice
	let currentKeySig = initialKeySig ? { sf: initialKeySig.sf, mi: initialKeySig.mi } : null;

	let meanBarWidth = GetMeanBarWidth(BARWIDTH, GENERALWIDTH);
	let isFirstMeasureInRow = true;
	const activeNotes = {};

	// determine global clef (existing logic)
	let globalClefChoice = decideClefForMeasure(measures);

	console.debug(`MR: Global clef for melody determined from first measure => ${globalClefChoice}`);
	if (currentKeySig)
		console.debug(`MR: initialKeySig applied for slice -> sf:${currentKeySig.sf} mi:${currentKeySig.mi}`);

	// iterate measures (rest of original implementation unchanged)...
	measures.forEach((measure, index) => {
		measure = normalizeMetaEvents(measure);

		let keySignatureChanged;
		let keySigName;
		({ keySignatureChanged, keySigName, currentKeySig } = getKeySignatureChanges(measure, currentKeySig));

		const stepRead = document.getElementById("stepRead");
		logEvent(`<br/>LE: Processing measure ${index + 1}`);
		logMeasureEvents(measure);

		// Зберігаємо попередній розмір такту для порівняння
		const previousNumerator = currentNumerator;
		const previousDenominator = currentDenominator;

		({ currentNumerator, currentDenominator } = adjustTimeSignature(measure, currentNumerator, currentDenominator));
		let ticksPerMeasure = CalculateTicksPerMeasure(currentNumerator, ticksPerBeat, currentDenominator);

		// Перевіряємо, чи змінився розмір такту
		const timeSignatureChanged = (currentNumerator !== previousNumerator || currentDenominator !== previousDenominator);

		// Отримуємо абсолютний час початку такту з measureMap
		const barStartAbsTime = (measureMap[index] !== undefined) ? measureMap[index] : index * ticksPerMeasure;

		// --- Встановлюємо startTime для активних нот на початку такту ---
		processActiveNotesFromPreviousBar(activeNotes, index, barStartAbsTime);

		// NEW: флаг, чи починався такт зі звучання (ноти, перенесені з попер. такту)
		const measureStartedWithActive = Object.keys(activeNotes).length > 0;

		// Перевіряємо, чи потрібно перейти на новий рядок
		const oldYposition = Yposition;
		({ Xposition, Yposition } = adjustXYposition(Xposition, GENERALWIDTH, meanBarWidth, Yposition, HEIGHT, Xmargin, index, barStartAbsTime));

		// Якщо Y позиція змінилась, значить почався новий рядок
		if (Yposition !== oldYposition) {
			isFirstMeasureInRow = true;
			console.log(`New row started at measure ${index + 1}, Y position: ${Yposition}`);
		}

		let STAVE_WIDTH = adjustStaveWidth(meanBarWidth, index, CLEFZONE, isFirstMeasureInRow, timeSignatureChanged, currentNumerator, currentDenominator);

		// Use global clefChoice determined from the first measure — do NOT change clef inside melody
		const clefChoice = globalClefChoice;

		// Створюємо нотний стан (stave)
		const stave = setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow, timeSignatureChanged, keySignatureChanged, keySigName, clefChoice);

		// Малюємо нотний стан
		stave.setContext(context).draw();

		const notes = [];
		const ties = [];
		let lastNoteOffTime = 0;
		let lastNoteOnTime = -1;

		// поточний стан показаних альтерацій у межах такту (letter+octave -> '#','b','n')
		const measureAccState = {};

		// ---------------------------------------
		// Аналізує MIDI-події і складає масив нот
		// ---------------------------------------
		renderMeasure();

		//---------------------------------------
		//пост-обробка масиву нот
		//----------------------------------------

		// Якщо є "активні" ноти (activeNotes), домалюємо їх до кінця такту
		AddActiveNotes();

		// Якщо остання нота не доходить до кінця такту, додаємо паузу
		if (Object.keys(activeNotes).length === 0) {
			// pass barStartAbsTime so addMissingRests computes relative offset within the bar
			addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat, barStartAbsTime);
		}

		// Перевіряємо, чи потрібно скоротити останню ноту/паузу, якщо вона виходить за межі такту
		correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat, clefChoice);

		console.log(`start to draw measure ${index + 1}`);

		//-------------------
		// МАЛЮЄМО НОТИ
		//------------------

		drawMeasure(notes, meanBarWidth, context, stave, ties, isFirstMeasureInRow, index, commentsDiv, currentNumerator, currentDenominator, ticksPerBeat);

		
		


		//--------------------
		//ПОСТ-ОБРОБКА 
		//--------------------
		//корегуємо Xposition для наступного такту
		Xposition += STAVE_WIDTH;

		// Скидаємо флаг після обробки першого такту в рядку
		isFirstMeasureInRow = false;

		//================================
		//HELPER_FUNCTIONS
		//===============================

        function AddActiveNotes() {
            if (Object.keys(activeNotes).length > 0) {
                const nextBoundary = measureMap[index + 1];
                const measureEndTick = (nextBoundary !== undefined) ? nextBoundary : barStartAbsTime + ticksPerMeasure; // fallback
                console.debug(`AN: bar ${index + 1}, measureEndTick = ${measureEndTick}`);
                if (stepRead) stepRead.innerHTML += `<i> act.note</i>`;
                drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties, currentKeySig, measureAccState, clefChoice);
            }
        }

		//-----------------------------------------
		// Функція обробки подій для кожного такту
		// аналізує міді-події, складає Note-On і Note-Off в нотний масив notes
		function renderMeasure() {
			console.debug("MR: FOO: midiRenderer.js - renderMeasure");
			let isFirstNoteInMeasure = true;

			const isNoteOn = (ev) => ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0;
			const isNoteOff = (ev) => ev.type === 0x8 || (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0);

			// Сортуємо: за часом; у межах одного часу спершу OFF, потім ON
			const sorted = [...measure].sort((a, b) => {
				if (a.absTime !== b.absTime) return a.absTime - b.absTime;
				const rank = (ev) => isNoteOff(ev) ? 0 : (isNoteOn(ev) ? 1 : 2);
				return rank(a) - rank(b);
			});

			// Захист від дубльованих OFF для однієї ноти на одному тіці
			const closedAtTick = new Set();

			sorted.forEach((event) => {
				console.log("MIDI Event:", event);

				if (isNoteOn(event)) {
					const pitch = event.data[0];
					if (stepRead) stepRead.innerHTML += ` on ${pitch} <span class="tick">[${event.absTime}]</span>`;

					// Add starting rest only if the bar did NOT start with sustained notes
					if (isFirstNoteInMeasure && !measureStartedWithActive) {
						const relTime = event.absTime - barStartAbsTime;
						if (relTime > 0) {
							AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime);
							// mark timeline filled up to this first note (absolute time)
							lastNoteOffTime = event.absTime;
						}
						isFirstNoteInMeasure = false;
					} else if (isFirstNoteInMeasure) {
						// bar began with sustained notes -> just flip the flag
						isFirstNoteInMeasure = false;
					}

					// Add rests between previous end and current event (if any)
					if (Object.keys(activeNotes).length === 0) {
						// No currently sounding notes — it's safe to add rests between last ended note and this event
						addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes);
					} else {
						// There are active (sustained) notes — do not insert rests that would break overlapping voices
						console.log(`MR: Skipping addRestsBetween — activeNotes present: ${Object.keys(activeNotes).join(',')}`);
					}
					checkConcide(event, lastNoteOnTime);

					// Ensure we preserve any existing lastRenderedNote (if we re-on the same pitch unexpectedly)
					const prevEntry = activeNotes[pitch];
					activeNotes[pitch] = {
						startTime: event.absTime,
						lastRenderedNote: prevEntry && prevEntry.lastRenderedNote ? prevEntry.lastRenderedNote : null
					};
					lastNoteOnTime = event.absTime;
				}
				else if (isNoteOff(event)) {
					const pitch = event.data[0];
					const key = `${pitch}@${event.absTime}`;
					if (closedAtTick.has(key)) return; // дублікат
					closedAtTick.add(key);

					const entry = activeNotes[pitch];
					const startTime = entry ? entry.startTime : undefined;
					if (stepRead) stepRead.innerHTML += ` off ${pitch} <span class="tick">[${event.absTime}]</span>`;

					if (startTime !== undefined) {
						const durationTicks = event.absTime - startTime;
						if (durationTicks > 0) {
							const durationsCode = getDurationFromTicks(durationTicks, ticksPerBeat);
							const { key: vexKey, accidental } = midiNoteToVexFlowWithKey(pitch, currentKeySig, clefChoice);

							const nominalTicksArr = durationsCode.map(dc => calculateTicksFromDuration(dc, ticksPerBeat));
							const nominalSum = nominalTicksArr.reduce((a, b) => a + b, 0) || 1;

							let previousNote = entry && entry.lastRenderedNote ? entry.lastRenderedNote : null;
							// If lastRenderedNote exists, it is the staveNote from previous measure — we must not duplicate it.
							// But we still need to create pieces that cover from startTime .. event.absTime.
							// Build pieces and tie them correctly: if previousNote exists, tie previousNote -> firstPiece.

							let firstPiece = null;
							durationsCode.forEach((durationCode, pieceIdx) => {
								const accToDraw = decideAccidentalForNote(vexKey, accidental, currentKeySig, measureAccState, pieceIdx);
								const note = processNoteElement(durationCode, vexKey, accToDraw, clefChoice, pitch);
								applyAutoStem(note, durationCode);

								try {
									note.__pitch = pitch;
									console.log(`Creating note: pitch=${pitch}, dur=${durationCode}, ticks=${durationTicks}, acc=${accToDraw}`);

								} catch (e) { console.warn(`failed to get pitch ${e}`) }

								const allocatedTicks = Math.round(durationTicks * (nominalTicksArr[pieceIdx] / nominalSum));
								note.__srcTicks = allocatedTicks;

								//===============================
								notes.push(note); // ADD NOTE //
								//===============================

								AddTie(previousNote, ties, note);

								previousNote = note;
								if (firstPiece === null) firstPiece = note;
							});

							// After finishing, no longer active — clear entry (note ended)
							// But keep previousNote for possible local ties array (we already pushed ties)
						}
						// lastNoteOffTime is absolute absTime (end of the note we just processed)
						lastNoteOffTime = event.absTime;
					}
					else { console.log(`starttime for ${pitch} is ${startTime}`); }
					if (activeNotes[pitch] !== undefined) {
						console.log(`deleting active note with pitch ${pitch}`);
						delete activeNotes[pitch];
					}
				}
			});
		}

		function deleteStupidTies() {
			//This function removes ties that start and end on the same note in the first measure of a row
					
			console.debug("MR: FOO: midiRenderer.js - deleteStupidTies");
			if (!Array.isArray(ties) || ties.length === 0) return;

			const keep = [];
			for (let i = 0; i < ties.length; i++) {
				const t = ties[i];
				try {
					// Support both Vex.Flow.StaveTie (first_note/last_note) and descriptor {first,last}
					const fn = t && (t.first_note || t.first);
					const ln = t && (t.last_note || t.last);

					// If structure unexpected, keep the tie to avoid data loss
					if (!fn || !ln) {
						keep.push(t);
						continue;
					}

					// Safely read keys arrays
					let firstKeys = [];
					let lastKeys = [];
					if (typeof fn.getKeys === 'function') {
						firstKeys = fn.getKeys() || [];
					} else if (Array.isArray(fn.keys)) {
						firstKeys = fn.keys;
					}
					if (typeof ln.getKeys === 'function') {
						lastKeys = ln.getKeys() || [];
					} else if (Array.isArray(ln.keys)) {
						lastKeys = ln.keys;
					}

					// If both have at least one key and they are identical -> drop tie
					if (firstKeys.length > 0 && lastKeys.length > 0 && firstKeys[0] === lastKeys[0]) {
						console.debug(`deleteStupidTies: removing trivial tie on ${firstKeys[0]}`);
						continue; // skip (remove)
					}

					// Otherwise keep
					keep.push(t);
				} catch (err) {
					console.warn('deleteStupidTies: error processing tie, keeping it', err);
					keep.push(t);
				}
			}

			// Mutate original ties array in-place so subsequent draw uses filtered list
			ties.length = 0;
			for (const t of keep) ties.push(t);
		}
	});

};


// ----------------------
// Допоміжні функції для рендеринга нотного стану з MIDI файлу
// Скорочення нот, якщо виходять за межі такту
// ----------------------

function correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat, clef) {
	console.debug("MR: FOO: midiRenderer.js - correctExtraNotes");
	if (notes.length === 0) return;

	// Обчислюємо загальну тривалість всіх нот у тіках з урахуванням крапок
	let totalTicks = notes.reduce((sum, note) => sum + getTotalTicksForNote(note, ticksPerBeat), 0);

	console.debug(`AN: Total measure ticks: ${totalTicks}, allowed: ${ticksPerMeasure}`);

	// Якщо загальна тривалість перевищує такт, скорочуємо останню ноту
	if (totalTicks > ticksPerMeasure && notes.length > 0) {
		const lastNote = notes[notes.length - 1];
		const lastNoteDuration = lastNote.getDuration();
		const isRest = lastNoteDuration.endsWith('r');

		// Обчислюємо фактичну тривалість останньої ноти з урахуванням крапок
		const lastNoteTicks = getTotalTicksForNote(lastNote, ticksPerBeat);

		// Обчислюємо скільки тіків залишається для останньої ноти
		const excessTicks = totalTicks - ticksPerMeasure;
		const remainingTicks = lastNoteTicks - excessTicks;

		console.log(`Last note excess: ${excessTicks}, remaining: ${remainingTicks}`);

		if (remainingTicks > 0) {
			// Знаходимо найближчу підходящу тривалість для залишкових тіків
			const newDurations = (typeof ticksToCleanDurations === 'function')
				? ticksToCleanDurations(remainingTicks, ticksPerBeat)
				: getDurationFromTicks(remainingTicks, ticksPerBeat);
			if (newDurations.length > 0) {
				const newDuration = newDurations[0]; // Беремо першу (найбільшу) тривалість
				const finalDuration = isRest ? newDuration + 'r' : newDuration;

				// Створюємо нову ноту з скороченою тривалістю
				if (isRest) {
					const newRest = createRest(newDuration);
					if (newRest) {
						notes[notes.length - 1] = newRest;
						console.log(`MR: Rest added (correctExtraNotes - shortened) duration=${newDuration}`);
						console.log(`Shortened last rest to: ${finalDuration}`);
					}
				} else {
					// Для нот потрібно зберегти висоту та знаки альтерації
					const keys = lastNote.getKeys();
					// При створенні укоротженої ноти передаємо clef і pitch
					const newNote = processNoteElement(newDuration, keys[0], null, clef, lastNote.pitch);
					if (newNote) {
						// Копіюємо знаки альтерації з оригінальної ноти
						const modifiers = lastNote.getModifiers();
						modifiers.forEach(modifier => {
							if (modifier.constructor.name === 'Accidental') {
								newNote.addModifier(modifier);
							}
						});
						// APPLY AUTO STEM for shortened note
						applyAutoStem(newNote, newDuration);
						notes[notes.length - 1] = newNote;
						console.debug(`AN: Shortened last note to: ${newDuration}`);
					}
				}
			}
		} else {
			// Якщо залишкових тіків недостатньо, видаляємо останню ноту
			console.debug("AN: Removing last note - no remaining ticks");
			notes.pop();
		}
	}
}

// --- Replace: processActiveNotesFromPreviousBar (preserve lastRenderedNote) ---
function processActiveNotesFromPreviousBar(activeNotes, index, barStartAbsTime) {
	console.debug("MR: FOO: midiRenderer.js - processActiveNotesFromPreviousBar");
	if (Object.keys(activeNotes).length > 0 && index > 0) {
		console.debug("AN: active notes from previous bar:", activeNotes);
		Object.keys(activeNotes).forEach(pitch => {
			const entry = activeNotes[pitch];
			if (!entry) {
				// legacy numeric value present -> normalize to object
				activeNotes[pitch] = { startTime: barStartAbsTime, lastRenderedNote: null };
			} else {
				// preserve lastRenderedNote, but for rendering in this new measure
				// we need startTime to be at measure start so drawActiveNotes will render duration from barStart -> measureEnd
				// store originalStart only if needed later (not required currently)
				entry.startTime = barStartAbsTime;
				// leave entry.lastRenderedNote untouched
			}
		});
	} else {
		console.debug("AN: no active notes from previous bar");
	}
}

// --- Replace: drawActiveNotes to set and use lastRenderedNote for cross-bar ties ---
function drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties, currentKeySig, measureAccState, clef = 'treble') {
	console.debug("MR: FOO: midiRenderer.js - drawActiveNotes");
	// Домалювати всі ноти, які залишилися активними до кінця такту
	Object.keys(activeNotes).forEach(pitchKey => {
		const stepRead = document.getElementById("stepRead");
		const pitch = Number(pitchKey);
		console.debug(`AN: trying to process ${pitch}, measureEndTick = ${measureEndTick}`);
		const entry = activeNotes[pitch];
		const startTime = entry ? entry.startTime : undefined;
		const prevRendered = entry ? entry.lastRenderedNote : null;

		const durationTicks = (startTime !== undefined) ? (measureEndTick - startTime) : -1;
		if (durationTicks >= 0) {
			const durationsCode = getDurationFromTicks(durationTicks, ticksPerBeat);
			const { key, accidental } = midiNoteToVexFlowWithKey(pitch, currentKeySig, clef);
			console.debug(`AN: found activeNote ${pitch} (${key}${accidental})`);

			const nominalTicksArr = durationsCode.map(dc => calculateTicksFromDuration(dc, ticksPerBeat));
			const nominalSum = nominalTicksArr.reduce((a, b) => a + b, 0) || 1;

			let previousNote = null;
			let firstPiece = null;
			durationsCode.forEach((durationCode, pieceIdx) => {
				const accToDraw = decideAccidentalForNote(key, accidental, currentKeySig, measureAccState, pieceIdx);
				const note = processNoteElement(durationCode, key, accToDraw, clef, pitch);
				applyAutoStem(note, durationCode);

				try { note.__pitch = pitch; } catch (e) { /* ignore */ }

				const allocatedTicks = Math.round(durationTicks * (nominalTicksArr[pieceIdx] / nominalSum));
				note.__srcTicks = allocatedTicks;

				// If this is the first piece in this measure and there was a note created in previous measure,
				// tie from that previous measure's lastStaveNote to this first piece.
				if (pieceIdx === 0 && prevRendered) {
					try {
						AddTie(prevRendered, ties, note);
					} catch (e) {
						console.warn("Failed to add cross-bar tie:", e);
					}
				}

				notes.push(note);

				// internal ties within this measure parts
				AddTie(previousNote, ties, note);

				previousNote = note;
				if (!firstPiece) firstPiece = note;
			});

			// Save last rendered stave note for future cross-bar tie (next measure)
			if (entry) {
				entry.lastRenderedNote = previousNote; // last piece's StaveNote
			}
		} else {
			console.debug(`AN: could not process activeNote, duration:  ${durationTicks} ticks`);
			if (stepRead) stepRead.innerHTML += `=${durationTicks} ticks: ${measureEndTick} - ${startTime}`
		}
	});
}
function checkConcide(event, lastNoteOnTime) {
	console.debug("MR: FOO: midiRenderer.js - checkConcide");
	if (event.absTime == lastNoteOnTime) {
		console.log(`NoteOn coincides with previous NoteOn: ${event.absTime}`);
	}
}

function CalculateTicksPerMeasure(currentNumerator, ticksPerBeat, currentDenominator) {
	console.debug("MR: FOO: midiRenderer.js - CalculateTicksPerMeasure");
	return currentNumerator * ticksPerBeat * 4 / currentDenominator;
}

// Повертає accidental для відображення з урахуванням key signature
// Якщо знак збігається з ключем — повертає null (не показувати)
function filterAccidentalByKeySignature(vexKey, accidental, currentKeySig) {
	console.debug("MR: FOO: midiRenderer.js - filterAccidentalByKeySignature");
	// If no key signature context, keep whatever was decided upstream
	if (!currentKeySig) return accidental;

	const letterPart = (vexKey.split('/')[0] || '').replace(/[#b]/g, '').toLowerCase();
	const ksMap = buildKeySignatureMap(currentKeySig.sf);
	const ksAcc = ksMap[letterPart]; // '#', 'b', or undefined

	// No explicit accidental on note, but key signature would alter it -> show natural
	if (accidental == null) {
		if (ksAcc) return 'n';
		return null; // nothing to show if key sig does not alter this letter
	}

	// If explicit accidental matches key signature, don't display a redundant sign
	if (ksAcc && ksAcc === accidental) {
		return null;
	}

	// Otherwise, display the explicit accidental (# or b)
	return accidental;
}

// NEW: Визначення знаку альтерації у межах такту з урахуванням KS і попередніх показаних знаків
function decideAccidentalForNote(key, spelledAccidental, currentKeySig, measureAccState, pieceIdx = 0) {
	// Avoid repeating accidental on tied pieces (only on the first piece)
	if (pieceIdx > 0) return null;

	// key like 'C/4' -> letter 'c', octave '4'
	const [rawLetter, rawOct] = (key || 'c/4').split('/');
	const letter = (rawLetter || '').replace(/[#b]/g, '').toLowerCase();
	const octave = rawOct || '4';
	const id = `${letter}/${octave}`;

	// Key signature accidental for this letter
	const ksMap = currentKeySig ? buildKeySignatureMap(currentKeySig.sf) : {};
	const ksAcc = ksMap[letter]; // '#', 'b', or undefined

	// spelledAccidental is the enharmonic choice for this pitch: '#', 'b', or null (natural)
	const spelled = spelledAccidental ?? null;

	// Previous accidental shown in this measure for this pitch letter+octave
	// One of '#', 'b', 'n' or undefined (nothing shown yet)
	const prev = measureAccState[id];

	let toPrint = null;

	if (prev === undefined) {
		// First occurrence in the bar for this pitch letter+octave
		if (spelled === '#' || spelled === 'b') {
			// Print only if different from key signature
			if (spelled !== ksAcc) toPrint = spelled; // e.g. show '#' if KS doesn't already sharpen
		} else {
			// spelled is natural
			// If key signature alters this letter, we must show natural to cancel KS
			if (ksAcc) toPrint = 'n';
		}
	} else {
		// Subsequent occurrence in the same bar
		if (spelled === '#' || spelled === 'b') {
			// If we previously cancelled (n) or had a different accidental, we must show this accidental again
			if (prev !== spelled) toPrint = spelled;
		} else {
			// spelled is natural — cancel any prior accidental (#/b) or explicit 'n'
			if (prev !== 'n') toPrint = 'n';
		}
	}

	// Update measure state only when we explicitly print something
	if (toPrint) {
		measureAccState[id] = toPrint;
	}

	return toPrint; // '#', 'b', 'n' or null
}


//  ----------------------
// ФУНКЦІЯ ПЕРЕТВОРЕННЯ MIDI НОТИ У ФОРМАТ VEXFLOW
// Параметри:
// - midiNote: Ціле число MIDI ноти (0-127).
// - currentKeySig: Поточний об'єкт тональності {sf: -7..+7, mi: 0|1} або null.
// Повертає: Об'єкт {key: 'C/4', accidental: '#', 'b', 'n' або null}.
// ----------------------

// Updated midiNoteToVexFlowWithKey to accept clef and removed clef-based octave adjustment from createNote

// Replaced implementation for midiNoteToVexFlowWithKey — safe, complete, returns { key, accidental }.
// Replaced midiNoteToVexFlowWithKey: return key WITHOUT embedded accidental.
// Accidental is returned separately so rendering logic (decideAccidentalForNote / processNoteElement)
// controls whether an accidental is drawn (prevents repeating KS accidentals next to notes).
function midiNoteToVexFlowWithKey(midiNote, currentKeySig, clef = 'treble') {
	console.debug("MR: FOO: midiRenderer.js - midiNoteToVexFlowWithKey, midiNote =", midiNote, "currentKeySig=", currentKeySig);

	const m = Number.isFinite(Number(midiNote)) ? Number(midiNote) : 0;
	const sharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
	const flatNames = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
	const pc = ((m % 12) + 12) % 12;
	let outOctave = Math.floor(m / 12) - 1;

	// Safe key-signature extraction
	const sf = currentKeySig && typeof currentKeySig.sf === 'number' ? currentKeySig.sf : 0;
	const mi = currentKeySig && typeof currentKeySig.mi === 'number' ? currentKeySig.mi : 0; // 0=major,1=minor

	// Minor tonic pitch-class (optional)
	let tonicPc = null;
	try { tonicPc = (typeof getMinorTonicPc === 'function') ? getMinorTonicPc(currentKeySig) : null; } catch (e) { tonicPc = null; }

	// Defaults
	let chosenSharp = sharpNames[pc];
	let chosenFlat = flatNames[pc];
	let chosen = (sf < 0) ? chosenFlat : chosenSharp;

	// Minor-key enharmonic preferences (preserve previous heuristics)
	if (mi === 1 && tonicPc != null) {
		const raised4 = (tonicPc + 6) % 12;  // #IV
		const raised6 = (tonicPc + 9) % 12;  // #VI
		const raised7 = (tonicPc + 11) % 12; // #VII
		const lowered2 = (tonicPc + 1) % 12; // bII

		if (pc === raised7) chosen = chosenSharp;
		if (pc === raised4 || pc === raised6) chosen = chosenSharp;
		if (pc === lowered2) chosen = chosenFlat;

		// Extreme edge cases preserved
		if (sf <= -6 && pc === 11) { chosen = 'Cb'; outOctave = outOctave + 1; }
		else if (sf <= -6 && pc === 4) { chosen = 'Fb'; }
		else if (sf >= 6 && pc === 5) { chosen = 'E#'; }
		else if (sf >= 6 && pc === 0) { chosen = 'B#'; outOctave = outOctave - 1; }
		else if (sf === 3 && mi === 1 && pc === 5) { chosen = 'E#'; }
	}

	// Additional general-edge adjustments (matching legacy behaviour)
	if (sf <= -6 && pc === 11) { chosen = 'Cb'; outOctave = outOctave + 1; }
	else if (sf <= -6 && pc === 4) { chosen = 'Fb'; }
	else if (sf >= 6 && pc === 5) { chosen = 'E#'; }
	else if (sf >= 6 && pc === 0) { chosen = 'B#'; outOctave = outOctave - 1; }

	// Robust accidental detection:
	let spelledAcc = null;
	if (typeof chosen === 'string') {
		if (chosen.indexOf('#') !== -1) spelledAcc = '#';
		else if (chosen.indexOf('b') !== -1) spelledAcc = 'b';
	}

	// Fallback: if chosen has no explicit accidental, prefer flat/sharp based on key-signature preference
	if (!spelledAcc) {
		// If key signature strongly suggests flats, and flatNames has 'b' for this pc, prefer 'b'
		if (sf < 0 && flatNames[pc].indexOf('b') !== -1) spelledAcc = 'b';
		// Else if key signature non-negative and sharpNames has '#', prefer '#'
		else if (sf >= 0 && sharpNames[pc].indexOf('#') !== -1) spelledAcc = '#';
		// else leave null (natural)
	}

	// Ensure we return letter (without accidental) + octave as key string for createNote
	const letterMatch = (typeof chosen === 'string') ? chosen.match(/^([A-G])/i) : null;
	const letterPart = letterMatch ? letterMatch[1].toUpperCase() : 'C';
	const key = `${letterPart}/${outOctave}`;

	console.debug(`midiNoteToVexFlowWithKey -> chosen=${chosen}, key=${key}, spelledAcc=${spelledAcc}, clef=${clef}`);
	return { key, accidental: spelledAcc };
}

//----------------
// ФУНКЦІЯ створення графічного об'єкту stave (бібліотека vexFlow)
//----------------
function setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow = false, timeSignatureChanged = false, keySignatureChanged = false, keySigName = null, clef = "treble") {
	console.debug("MR: FOO: midiRenderer.js - setStave");
	const stave = new Vex.Flow.Stave(Xposition, Yposition, STAVE_WIDTH);

	try {
		stave.clef = clef;
	} catch (e) {
		console.warn("MR: Could not set stave.clef directly:", e);
	}

	// Додаємо ключ для першого такту взагалі або для першого такту в кожному рядку
	if (index === 0 || isFirstMeasureInRow) {
		stave.addClef(clef);
		console.log(`Adding clef '${clef}' to measure ${index + 1}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
	}

	// Додаємо знаки при ключі (key signature) якщо доступні і це перший такт, або перший у рядку, або вона змінилась
	if (keySigName && (index === 0 || keySignatureChanged || isFirstMeasureInRow)) {
		try {
			console.log(`About to add key signature: ${keySigName} (index=${index}, changed=${keySignatureChanged}, firstRow=${isFirstMeasureInRow})`);
			stave.addKeySignature(keySigName);
			console.log(`Adding key signature ${keySigName} to measure ${index + 1}`);
		} catch (e) {
			console.warn(`Failed to add key signature ${keySigName} to measure ${index + 1}`, e);
		}
	}

	// Додаємо розмір такту для першого такту або коли розмір змінився
	if (index === 0 || timeSignatureChanged) {
		stave.addTimeSignature(`${currentNumerator}/${currentDenominator}`);
		console.log(`Adding time signature ${currentNumerator}/${currentDenominator} to measure ${index + 1}, timeSignatureChanged: ${timeSignatureChanged}`);
	}

	return stave;
}

function adjustStaveWidth(BARWIDTH, index, CLEFZONE, isFirstMeasureInRow = false, timeSignatureChanged = false, currentNumerator = 4, currentDenominator = 4) {
	console.debug("MR: FOO: midiRenderer.js - adjustStaveWidth");
	let STAVE_WIDTH = BARWIDTH;
	if (currentNumerator / currentDenominator > 1) {
		STAVE_WIDTH *= (currentNumerator / currentDenominator) * 0.95; // Додатковий простір для більших розмірів такту
		console.log(`Increased STAVE_WIDTH for larger time signature: ${STAVE_WIDTH}`);
	}
	else if (currentNumerator / currentDenominator < 1) {
		STAVE_WIDTH *= (currentNumerator / currentDenominator) * 1.05; // Зменшення простору для менших розмірів такту
		console.log(`Decreased STAVE_WIDTH for smaller time signature: ${STAVE_WIDTH}`);
	}
	else console.log(`STAVE_WIDTH remains standard: ${STAVE_WIDTH}`);


	// Додаємо CLEFZONE для першого такту взагалі або для першого такту в кожному рядку
	if (index === 0 || isFirstMeasureInRow) {
		STAVE_WIDTH += CLEFZONE;
		console.log(`Adding CLEFZONE to measure ${index + 1}, total width: ${STAVE_WIDTH}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
	}

	// Додаємо додатковий простір для розміру такту, якщо він змінився (але не для першого такту, бо там вже врахований CLEFZONE)
	if (timeSignatureChanged && index > 0 && !isFirstMeasureInRow) {
		STAVE_WIDTH += 30; // Додатковий простір для розміру такту
		console.log(`Adding extra space for time signature change to measure ${index + 1}, total width: ${STAVE_WIDTH}`);
	}

	return STAVE_WIDTH;
}

// ----------------------
// ФУНКЦІЯ ДЛЯ КОРЕКТУВАННЯ ПОЗИЦІЇ X, Y ПРИ РЕНДЕРИНГУ
// ----------------------
// Параметри:
// - Xposition: Поточна позиція X для рендерингу.
// - GENERALWIDTH: Загальна ширина доступного простору для рендерингу.
// - BARWIDTH: Ширина одного такту.
// - Yposition: Поточна позиція Y для рендерингу.
// - HEIGHT: Висота одного рядка.
// - Xmargin: Лівий відступ.
// - index: Індекс поточного такту.
// - barStartAbsTime: Абсолютний час початку такту (для логів).
// Повертає: Об'єкт з оновленими Xposition і Yposition.
// ----------------------
function adjustXYposition(Xposition, GENERALWIDTH, BARWIDTH, Yposition, HEIGHT, Xmargin, index, barStartAbsTime) {
	console.debug("MR: FOO: midiRenderer.js - adjustXYposition");
	// Restore wrapping: when remaining width is not enough for another measure, move to next row
	if (Xposition > GENERALWIDTH - BARWIDTH) {
		Yposition += HEIGHT;
		Xposition = Xmargin;
		console.log("Yposition updated:", Yposition);
	} else {
		console.log(`adjustXYposition GW = ${GENERALWIDTH} vs ${BARWIDTH} + ${Xposition}`);
	}
	console.log(`Processing measure ${index + 1} starting from tick: ${barStartAbsTime} X=${Xposition}  Y=${Yposition}`);
	return { Xposition, Yposition };
}

// Функція drawMeasure
// ----------------------
// Параметри:
// - notes: Масив нот для рендерингу.
// - BARWIDTH: Ширина такту в пікселях.
// - context: Контекст рендерингу з VexFlow.
// - stave: Об'єкт stave з VexFlow для поточного такту.
// - ties: Масив ліг для нот.
// - index: Індекс поточного такту.
// - commentsDiv: HTML елемент для виведення коментарів.
// - currentNumerator: Поточний чисельник розміру такту.
// - currentDenominator: Поточний знаменник розміру такту.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// Повертає: void
// Генерує зображення черех Vex.Flow.Voice()
// ----------------------

function drawMeasure(notes, BARWIDTH, context, stave, ties, isFirstMeasureInRow, index, commentsDiv, currentNumerator, currentDenominator, ticksPerBeat)
 {
	console.debug("MR: FOO: midiRenderer.js - drawMeasure");
	try {
		if (notes.length > 0) {
			// Фільтруємо некоректні ноти
			const validNotes = notes.filter(note => note !== null && note !== undefined);

			//якщо жодної валідної ноти - додаємо паузу на весь такт
			if (validNotes.length === 0) {
				console.warn(`Measure ${index + 1} has no valid notes to render.`);
				const ticksPerMeasure = currentNumerator * ticksPerBeat * 4 / currentDenominator;
				const restDurations = getDurationFromTicks(ticksPerMeasure, ticksPerBeat);
				const rest = createRest(restDurations[0]);
				validNotes.push(rest);
				console.log(`MR: Rest added (drawMeasure - whole measure) duration=${restDurations[0]}`);
			}

			// Обчислення ребер нот (beams) з обробкою помилок
			let beams = calculateBeams(validNotes, ticksPerBeat, index, currentNumerator, currentDenominator);

			// Створюємо voice з урахуванням поточного розміру такту
			const voice = new Vex.Flow.Voice({
				num_beats: currentNumerator,
				beat_value: currentDenominator,
				resolution: Vex.Flow.RESOLUTION
			});

			// Встановлюємо строгий режим у false для більшої гнучкості при обробці тривалостей
			voice.setStrict(false);

			// Додаємо нотні елементи до голосу
			voice.addTickables(validNotes);


			// --- restored formatter + availableWidth calculation (fixes "formatter is not defined") ---
			const formatter = new Vex.Flow.Formatter();
			const staveX = (typeof stave.getX === 'function') ? stave.getX() : stave.x || 0;
			const staveW = (typeof stave.getWidth === 'function') ? stave.getWidth() : stave.width || BARWIDTH;
			const noteStartX = (typeof stave.getNoteStartX === 'function') ? stave.getNoteStartX() : (staveX + 10);
			const rightX = staveX + staveW;
			const rightPadding = 10;
			const availableWidth = Math.max(10, rightX - noteStartX - rightPadding);

			// Diagnostic dump: show stave clef and each note.__srcMidi + note.getKeys()
			console.log('DIAG before format:', {
				staveClef: stave && stave.clef ? stave.clef : '(none)',
				noteKeys: validNotes.map(n => ({
					srcMidi: n && n.__srcMidi,
					keys: (n && typeof n.getKeys === "function") ? n.getKeys() : (n && n.keys)
				}))
			});

			formatter.joinVoices([voice]).format([voice], availableWidth);

			// Малюємо голос
			voice.draw(context, stave);

			addAtributesToNotesInMeasure(validNotes, index);

			// Розпізнаємо й малюємо тріолі
			drawTuplets(currentNumerator, currentDenominator, validNotes, ticksPerBeat, index, context);

			// Домальовуємо ребра нот (beams) 
			drawBeams(beams, context, index);

			// Домальовуємо ліги (ties)
			if (isFirstMeasureInRow)
				deleteStupidTies(ties);


			drawTies(ties, context, index);
						

		} else {
			// Якщо такт порожній - додаємо паузу на весь такт
			const ticksPerMeasure = currentNumerator * ticksPerBeat * 4 / currentDenominator;
			const restDurations = (typeof ticksToCleanDurations === 'function')
				? ticksToCleanDurations(ticksPerMeasure, ticksPerBeat)
				: getDurationFromTicks(ticksPerMeasure, ticksPerBeat);
			const wholeRest = createRest(restDurations[0]);
			if (wholeRest) {
				const voice = new Vex.Flow.Voice({
					num_beats: currentNumerator,
					beat_value: currentDenominator,
					resolution: Vex.Flow.RESOLUTION
				});
				voice.setStrict(false);
				voice.addTickable(wholeRest);

				const formatter = new Vex.Flow.Formatter();
				formatter.joinVoices([voice]).format([voice], stave.getWidth() - 20);
				voice.draw(context, stave);
			}
		}
	} catch (error) {
		console.error(`Error rendering measure ${index + 1}: ${error.message}`);
		commentsDiv.innerHTML += `<br>Error rendering measure ${index + 1}: ${error.message}<br>`;
	}

	function deleteStupidTies(ties) {
		// Видаляємо ліги, які закінчуються на первій ноті такту
		ties.filter(tie => {
			const endNote = tie.getEndNote();
			const endNoteIndex = notes.indexOf(endNote);
			return endNoteIndex !== 0; // Залишаємо ліги, які не закінчуються на першій ноті
		});
	}

}



// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛЮВАННЯ ЛІГ З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.StaveTie
// Параметри:
// - ties: Масив об'єктів StaveTie з VexFlow.
// - context: Контекст рендерингу з VexFlow.
// - index: Індекс поточного такту.
// Повертає: void
// ----------------------
function drawTies(ties, context, index) {
	ties.forEach((tie, tieIndex) => {
		try {
			tie.setContext(context).draw();
		} catch (tieError) {
			console.warn(`Error drawing tie ${tieIndex + 1} in measure ${index + 1}:`, tieError);
		}
	});
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛЮВАННЯ ТРІОЛЕЙ З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.Tuplet
// Параметри:
// - currentNumerator: Поточний чисельник розміру такту.
// - currentDenominator: Поточний знаменник розміру такту.
// - validNotes: Масив об'єктів StaveNote з VexFlow.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - index: Індекс поточного такту.
// - context: Контекст рендерингу з VexFlow.
// Повертає: void
// ----------------------
function drawTuplets(currentNumerator, currentDenominator, validNotes, ticksPerBeat, index, context) {
	let tuplets = [];
	try {
		if (typeof detectTuplets === 'function') {
			const timeSignature = { num: currentNumerator || 4, den: currentDenominator || 4 };
			const measureForTuplets = { notes: validNotes.map(vn => ({ vexNote: vn })) };
			tuplets = detectTuplets(measureForTuplets, ticksPerBeat, timeSignature);
			console.log(`MR:detectTuplets found ${tuplets.length} tuplets for measure ${index + 1}`);
		}
	} catch (te) {
		console.warn(`MR:detectTuplets error in measure ${index + 1}:`, te);
	}
	if (tuplets && tuplets.length) {
		tuplets.forEach((t, tIdx) => {
			try { t.setContext(context).draw(); }
			catch (e) { console.warn(`MR:Error drawing tuplet ${tIdx + 1} in measure ${index + 1}:`, e); }
		});
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛЮВАННЯ BEAMS З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.Beam
// Параметри:
// - beams: Масив об'єктів Beam з VexFlow.
// - context: Контекст рендерингу з VexFlow.
// - index: Індекс поточного такту.
// Повертає: void
// ----------------------
function drawBeams(beams, context, index) {
	if (beams && beams.length > 0) {
		beams.forEach((beam, beamIndex) => {
			try {
				beam.setContext(context).draw();
				console.debug(`MR:Beam ${beamIndex + 1} drawn for measure ${index + 1}`);
			} catch (beamError) {
				console.warn(`MR:Error drawing beam ${beamIndex + 1} in measure ${index + 1}:`, beamError);
			}
		});
	}
	else {
		console.debug(`MR:No beams to draw for measure ${index + 1}`);
	}
}


// //----------------------
// ФУНКЦІЯ ДЛЯ ОНОВЛЕННЯ РОЗМІРУ ТАКТУ ЗА ПОДІЯМИ TIME SIGNATURE
// шукає подію TimeSignature в такті
// повертає оновлені currentNumerator і currentDenominator
// Параметри:
// - measure: Масив MIDI подій поточного такту.
// - currentNumerator: Поточний чисельник розміру такту.
// - currentDenominator: Поточний знаменник розміру такту.
// Повертає: Об'єкт {currentNumerator, currentDenominator}.
// ----------------------
function adjustTimeSignature(measure, currentNumerator, currentDenominator) {
	console.debug("MR: FOO: midiRenderer.js - adjustTimeSignature");
	for (const event of measure) {
		if (event.type === 0xFF && event.metaType === 0x58 && Array.isArray(event.data)) {
			currentNumerator = event.data[0];
			currentDenominator = Math.pow(2, event.data[1]);
			break;
		}
	}
	return { currentNumerator, currentDenominator };
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ НОТ ІЗ ПОПЕРЕДНЬОГО ТАКТУ
// якщо є активні ноти з попереднього такту
// Параметри:
// - previousNote: Попередня нота (Vex.Flow.StaveNote) або null.
// - ties: Масив ліг (Vex.Flow.StaveTie) для рендерингу.
// - note: Поточна нота (Vex.Flow.StaveNote).

// Повертає: void
// ----------------------
function AddNotesFromPreviousBar(activeNotes, measure) {
	console.debug("MR: FOO: midiRenderer.js - AddNotesFromPreviousBar");
	Object.keys(activeNotes).forEach(pitch => {
		measure.unshift({
			type: 0x9,
			channel: 0,
			deltaTime: 0,
			absTime: 0,
			data: [Number(pitch), 100],
		});
		console.log(`MR:add note ${pitch} from previous bar`);
	});
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ПОТОЧНОГО РОЗМІРУ ТАКТУ ЗАПОДІЯМИ TIME SIGNATURE
// шукає подію TimeSignature тільки в першому такті
// повертає {numerator, denominator}
// якщо немає події, повертає 4/4 за замовчуванням
// Параметри:
// - measures: Масив тактів, кожен такт — масив MIDI подій.
// Повертає: Об'єкт {numerator, denominator}.
// ----------------------
function getTimeSignature(measures) {
	console.debug("MR: FOO: midiRenderer.js - getTimeSignature");
	let numerator = 4, denominator = 4;
	if (measures.length > 0 && measures[0].length > 0) {
		for (const event of measures[0]) {
			if (event.type === 0xFF && event.metaType === 0x58 && Array.isArray(event.data)) {
				numerator = event.data[0];
				denominator = Math.pow(2, event.data[1]);
				break;
			}
		}
	}
	return { numerator, denominator };
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ЛІГИ (TIE) МІЖ НОТАМИ
// якщо попередня нота не null
// Параметри:
// - previousNote: Попередня нота (Vex.Flow.StaveNote) або null.
// - ties: Масив ліг (Vex.Flow.StaveTie) для рендерингу.
// - note: Поточний нота (Vex.Flow.StaveNote).
// Повертає: void
// ----------------------
function AddTie(previousNote, ties, note) {
	console.debug("MR: FOO: midiRenderer.js - AddTie");
	if (previousNote) {
		ties.push(new Vex.Flow.StaveTie({
			first_note: previousNote,
			last_note: note
		}));
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ СТВОРЕННЯ НОТИ З ВИКОРИСТАННЯМ VEXFLOW
// Параметри:
// - durationCode: Код тривалості ноти (наприклад, 'q', 'h', '8', '16r' для паузи).
//// - key: Висота ноти у форматі VexFlow (наприклад, 'C/4', 'D#/5').
// / / - accidental: Знак альтерації ('#', 'b', 'n') або null.
// Повертає: Об'єкт Vex.Flow.StaveNote або null у разі помилки.
// ----------------------
function processNoteElement(durationCode, key, accidental, clef = 'treble', srcMidi = null) {
	console.debug("MR: FOO: midiRenderer.js - processNoteElement");
	// key може бути в форматі "C/4" або "C4" — normalize до формату, який очікує createNote (наприклад "c4")
	let normalized = (typeof key === 'string') ? key.replace('/', '') : key;
	normalized = (typeof normalized === 'string') ? normalized.toLowerCase() : normalized;

	// Передаємо clef у опціях StaveNote — це дозволяє VexFlow позиціонувати ноту відносно потрібного ключа
	const note = createNote(normalized, durationCode, clef);
	if (note && accidental) {
		note.addAccidental(0, new Vex.Flow.Accidental(accidental));
	}

	if (srcMidi !== null && srcMidi !== undefined && Number.isFinite(Number(srcMidi))) {
		note.__pitch = Number(srcMidi);
	}

	applyAutoStem(note, durationCode);
	return note;
}


function getStartMeasuerIndex(measureMap, targetAbsTime) {
	const idxKeys = Object.keys(measureMap).map(k => parseInt(k, 10)).sort((a, b) => a - b);
	let matchedMeasureIdx = 0;
	for (let i = 0; i < idxKeys.length - 1; i++) {
		const start = measureMap[idxKeys[i]];
		const next = measureMap[idxKeys[i + 1]];
		if (targetAbsTime >= start && targetAbsTime < next) { matchedMeasureIdx = idxKeys[i]; break; }
	}
	// Snap to nearest lower multiple of 4
	let startAtMeasureIndex = matchedMeasureIdx - (matchedMeasureIdx % 4);
	if (startAtMeasureIndex < 0) startAtMeasureIndex = 0;
	return startAtMeasureIndex;
}

function getStartIdx(startAtMeasureIndex, measures) {
	let startIdx = parseInt(startAtMeasureIndex, 10);
	if (!isFinite(startIdx) || startIdx < 0) startIdx = 0;
	startIdx = startIdx - (startIdx % 4);
	if (startIdx > measures.length - 1) startIdx = Math.max(0, measures.length - 1);
	return startIdx;
}

function getTargetAbsTime(allEvents, startNoteIndex) {
	let targetAbsTime = 0;
	let count = 0;
	for (const ev of allEvents) {
		if (ev && ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0) {
			if (count === (parseInt(startNoteIndex, 10) || 0)) {
				targetAbsTime = ev.absTime || 0;
				break;
			}
			count++;
		}
	}
	return targetAbsTime;
}



// Decide clef for a measure based on pitch range.
// New rule:
// - If any note > 65 -> "treble"
// - Else if any note < 55 -> "bass"
// - Else -> "treble"
function decideClefForMeasure(measures) {
	console.debug("MR: FOO: midiRenderer.js - decideClefForMeasure (updated)");
	// Expect an array of measures where each measure is an array of events.
	if (!Array.isArray(measures) || measures.length === 0) {
		return "treble";
	}

	try {
		// Find the first measure that actually contains note events
		const firstWithNotes = measures.find(m => Array.isArray(m) && hasNoteOn(m)) || [];

		let hasLow = false;   // any note < 55
		let hasHigh = false;  // any note > 65

		for (const ev of firstWithNotes) {
			if (ev && ev.type === 0x9 && Array.isArray(ev.data)) {
				const pitch = ev.data[0];
				if (typeof pitch === 'number') {
					if (pitch < 55) hasLow = true;
					if (pitch > 65) hasHigh = true;
					if (hasHigh) break; // early exit
				}
			}
		}

		if (hasHigh) {
			console.log("Choosing treble clef (found note > 65)");
			return "treble";
		}
		if (hasLow) {
			console.log("Choosing bass clef (found note < 55)");
			return "bass";
		}
		console.log("Choosing treble clef (default)");
		return "treble";
	}
	catch (e) {
		console.warn("MR: Failed to determine global clef, fallback to treble", e);
		return "treble";
	}
}
function extractEventsFromArray(uint8) {
	const midiData = MidiParser.Uint8(uint8);
	const ticksPerBeat = Array.isArray(midiData.timeDivision) ? 480 : midiData.timeDivision;
	let allEvents = SetEventsAbsoluteTime(midiData);
	ensureEndEvent(allEvents);
	const isMidi0 = midiData && (midiData.format === 0 || midiData.format === '0');
	return { allEvents, ticksPerBeat, isMidi0 };
}

/**
 * handleNoNotesToRender
 * ---------------------
 * Extracted helper to centralize behavior when a selected segment contains no NoteOn events.
 *
 * @param {HTMLElement|null} commentsDiv - element to write comments into (may be null)
 * @param {string} ELEMENT_FOR_RENDERING - id of element to render into when commentsDiv absent
 * @param {Array} measures - full measures array
 * @param {Array} measuresWindow - the window/slice of measures considered for render
 * @param {number} maxBarsToRender - original requested max bars to render
 * @param {number} startIdx - computed start index for rendering
 * @returns {Object} summary object compatible with renderMidiFileToNotation's return
 */
function handleNoNotesToRender(commentsDiv, ELEMENT_FOR_RENDERING, measures, measuresWindow, maxBarsToRender, startIdx) {
	const msg = 'No notes to render: selected segment contains no NoteOn events.';
	console.warn(`renderMidiFileToNotation: ${msg} measures.length=${measures.length} measuresWindow.length=${measuresWindow.length}`);
	if (commentsDiv) {
		commentsDiv.innerHTML = msg;
	} else {
		const target = document.getElementById(ELEMENT_FOR_RENDERING);
		if (target) target.innerHTML = `<div class="small text-muted">${msg}</div>`;
	}
	return {
		totalMeasures: measures.length,
		renderedMeasures: 0,
		limited: false,
		maxBarsToRender,
		startAtMeasureIndex: startIdx
	};
}


function addAtributesToNotesInMeasure(validNotes, measureIndex) {
	if (typeof document === 'undefined' || !Array.isArray(validNotes) || typeof measureIndex !== 'number') return;

	function normalizeAcc(a) {
		if (!a && a !== '') return null;
		const s = String(a).trim();
		if (!s) return null;
		if (s === '#' || s === '♯' || /sharp/i.test(s)) return '#';
		if (s === 'b' || s === '♭' || /flat/i.test(s)) return 'b';
		if (s.toLowerCase() === 'n' || /natural/i.test(s)) return 'n';
		if (s.indexOf('#') !== -1 || s.indexOf('♯') !== -1) return '#';
		if (s.indexOf('b') !== -1 || s.indexOf('♭') !== -1) return 'b';
		return null;
	}

	function midiFromKey(rawKey) {
		if (!rawKey || typeof rawKey !== 'string') return null;
		const cleaned = rawKey.trim();
		const parts = cleaned.split('/');
		if (parts.length <2) return null;
		const letterPart = parts[0].toLowerCase();
		const oct = parseInt(parts[1],10);
		if (Number.isNaN(oct)) return null;
		const baseMap = { c:0, d:2, e:4, f:5, g:7, a:9, b:11, h:11 };
		const m = baseMap[letterPart[0]] !== undefined ? baseMap[letterPart[0]] :0;
		let sem = m;
		if (letterPart.indexOf('#') !== -1) sem +=1;
		if (letterPart.indexOf('b') !== -1) sem -=1;
		sem = ((sem %12) +12) %12;
		return Math.round((Number(oct) +1) *12 + sem);
	}

	try {
		const host = document.getElementById('notation');
		let svg = host ? host.querySelector('svg') : null;
		if (!svg) {
			const svgs = Array.from(document.querySelectorAll('svg'));
			svg = svgs.length ? svgs[svgs.length -1] : null;
		}
		if (!svg) return;

		const groups = Array.from(svg.querySelectorAll('g.vf-stavenote'));
		if (!groups.length || !validNotes.length) return;

		// Build centers and read any pre-existing data-midi/title for heuristic
		const groupCenters = groups.map((g, i) => {
			let cx = null;
			try { const bb = g.getBBox(); cx = bb.x + bb.width /2; } catch { cx = null; }
			// read preexisting data-midi if any
			let existingMidi = null;
			try { existingMidi = g.getAttribute('data-midi'); } catch { existingMidi = null; }
			let titleText = null;
			try {
				const titleEl = g.querySelector('title');
				if (titleEl) titleText = titleEl.textContent || null;
			} catch { titleText = null; }
			return { i, g, cx, used: false, existingMidi: existingMidi ? parseInt(existingMidi,10) : null, titleText };
		});

		// Helper: compute expected midi for a staveNote
		function expectedMidiForStaveNote(staveNote) {
			if (!staveNote) return null;
			// prefer explicit field set at creation
			try {
				if (Number.isFinite(Number(staveNote.__pitch))) return Number(staveNote.__pitch);
				if (Number.isFinite(Number(staveNote.__srcMidi))) return Number(staveNote.__srcMidi);
			} catch (e) { /* ignore */ }

			// try getKeyProps
			try {
				if (typeof staveNote.getKeyProps === 'function') {
					const kp = staveNote.getKeyProps();
					if (Array.isArray(kp) && kp.length >0) {
						const first = kp[0];
						if (first && Number.isFinite(Number(first.midi))) return Number(first.midi);
						if (first && first.key) {
							const k = String(first.key || '').replace(/\s+/g, '');
							const mm = midiFromKey(k);
							if (mm !== null) return mm;
						}
					}
				}
			} catch (e) { /* ignore */ }

			// try getKeys string
			try {
				if (typeof staveNote.getKeys === 'function') {
					const keys = staveNote.getKeys();
					if (Array.isArray(keys) && keys.length >0) {
						const k = keys[0];
						const mm = midiFromKey(k);
						if (mm !== null) return mm;
					}
				} else if (staveNote.keys && Array.isArray(staveNote.keys) && staveNote.keys.length >0) {
					const mm = midiFromKey(staveNote.keys[0]);
					if (mm !== null) return mm;
				}
			} catch (e) { /* ignore */ }

			return null;
		}

		// Build list of unassigned groups
		const unassigned = new Set(groupCenters.map(gc => gc.i));

		let annotated =0;

		// First pass: try exact midi match between expectedMidi and group's existingMidi or title content
		for (let noteIdx =0; noteIdx < validNotes.length; noteIdx++) {
			const staveNote = validNotes[noteIdx];
			if (!staveNote) continue;
			const expectedMidi = expectedMidiForStaveNote(staveNote);
			if (expectedMidi == null) continue; // leave for next pass

			// find group with existingMidi == expectedMidi
			let matched = null;
			for (const gc of groupCenters) {
				if (!unassigned.has(gc.i)) continue;
				if (gc.existingMidi !== null && gc.existingMidi === expectedMidi) { matched = gc; break; }
				// try title text containing midi number
				if (gc.titleText && gc.titleText.indexOf(String(expectedMidi)) !== -1) { matched = gc; break; }
			}

			if (matched) {
				const g = matched.g;
				try {
					// write attributes
					if (!g.getAttribute('data-measure-index')) g.setAttribute('data-measure-index', String(measureIndex));
					if (!g.getAttribute('data-note-in-measure')) g.setAttribute('data-note-in-measure', String(noteIdx));
					if (Number.isFinite(expectedMidi)) g.setAttribute('data-midi', String(expectedMidi));
					// mark used
					unassigned.delete(matched.i);
					matched.used = true;
					annotated++;
				} catch (e) { /* ignore */ }
			}
		}

		// Second pass: match by position + expectedMidi if available, else by nearest X
		for (let noteIdx =0; noteIdx < validNotes.length; noteIdx++) {
			const staveNote = validNotes[noteIdx];
			if (!staveNote) continue;

			// if this note already has an assigned group (we set data-midi earlier), skip
			let alreadyAssigned = false;
			try {
				// check if any group has data-note-in-measure == noteIdx and measureIndex
				for (const gc of groupCenters) {
					try {
						const di = gc.g.getAttribute('data-measure-index');
						const ni = gc.g.getAttribute('data-note-in-measure');
						if (di !== null && ni !== null && parseInt(di,10) === measureIndex && parseInt(ni,10) === noteIdx) { alreadyAssigned = true; break; }
					} catch (e) { /* ignore */ }
				}
			} catch (e) { /* ignore */ }
			if (alreadyAssigned) continue;

			let noteX = null;
			try {
				if (typeof staveNote.getAbsoluteX === 'function') noteX = staveNote.getAbsoluteX();
				else if (typeof staveNote.getX === 'function') noteX = staveNote.getX();
			} catch { noteX = null; }

			const expectedMidi = expectedMidiForStaveNote(staveNote);

			// Try find among unassigned a group with title containing expected key or midi
			if (expectedMidi != null) {
				let matched = null;
				for (const gc of groupCenters) {
					if (!unassigned.has(gc.i)) continue;
					if (gc.titleText && (gc.titleText.indexOf(String(expectedMidi)) !== -1 || gc.titleText.indexOf(String(expectedMidi)) !== -1)) { matched = gc; break; }
				}
				if (matched) {
					try {
						const g = matched.g;
						if (!g.getAttribute('data-measure-index')) g.setAttribute('data-measure-index', String(measureIndex));
						if (!g.getAttribute('data-note-in-measure')) g.setAttribute('data-note-in-measure', String(noteIdx));
						if (!g.getAttribute('data-midi')) g.setAttribute('data-midi', String(expectedMidi));
						unassigned.delete(matched.i);
						matched.used = true;
						annotated++;
						continue;
					} catch (e) { /* ignore */ }
				}
			}

			// Fallback: nearest by X among unassigned
			let best = null;
			let bestDist = Infinity;
			if (Number.isFinite(noteX)) {
				for (const gc of groupCenters) {
					if (!unassigned.has(gc.i)) continue;
					if (!Number.isFinite(gc.cx)) continue;
					const d = Math.abs(gc.cx - noteX);
					if (d < bestDist) { bestDist = d; best = gc; }
				}
			} else {
				// if noteX unknown, pick any unassigned
				const anyIdx = unassigned.values().next().value;
				best = anyIdx !== undefined ? groupCenters.find(g => g.i === anyIdx) : null;
			}

			if (best) {
				try {
					const g = best.g;
					if (!g.getAttribute('data-measure-index')) g.setAttribute('data-measure-index', String(measureIndex));
					if (!g.getAttribute('data-note-in-measure')) g.setAttribute('data-note-in-measure', String(noteIdx));
					// if expectedMidi available set it, otherwise if staveNote has __pitch use it
					if (expectedMidi != null) {
						if (!g.getAttribute('data-midi')) g.setAttribute('data-midi', String(expectedMidi));
					} else {
						// try to compute midi from keys as last resort
						let computed = null;
						try {
							if (typeof staveNote.getKeys === 'function') {
								const k = staveNote.getKeys()[0];
								computed = midiFromKey(k);
							} else if (staveNote.keys && staveNote.keys[0]) {
								computed = midiFromKey(staveNote.keys[0]);
							}
						} catch (e) { computed = null; }
						if (computed != null && !g.getAttribute('data-midi')) g.setAttribute('data-midi', String(computed));
					}
					unassigned.delete(best.i);
					best.used = true;
					annotated++;
				} catch (e) { /* ignore per-element failures */ }
			}
		}

		console.debug && console.debug(`Annotated ${annotated}/${validNotes.length} notes for measure ${measureIndex}`);
	} catch (e) {
		console.warn('addAtributesToNotesInMeasure failed', e);
	}
}