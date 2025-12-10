// ----------------------
// ФУНКЦІЯ ДЛЯ СТВОРЕННЯ ПАУЗИ (REST) З ВИКОРИСТАННЯМ VEXFLOW
// ----------------------

function addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat, barStartAbsTime) {
	// If barStartAbsTime provided, convert absolute lastNoteOffTime -> relative offset inside the bar
	let relLastOff = 0;
	if (typeof barStartAbsTime === 'number') {
		relLastOff = Number(lastNoteOffTime) - Number(barStartAbsTime);
	} else {
		relLastOff = Number(lastNoteOffTime) || 0;
	}

	// Defensive clamp
	if (!isFinite(relLastOff) || relLastOff < 0) relLastOff = 0;

	// If already at/after measure end (relative) -> nothing to add
	if (relLastOff >= ticksPerMeasure) {
		console.debug('MR: lastNoteOffTime >= measure (relative), no rests needed');
		return;
	}

	const remainingTicks = ticksPerMeasure - relLastOff;
	if (remainingTicks <= thresholdGap) {
		console.debug('MR: remaining ticks too small, skipping');
		return;
	}

	console.debug(`MR: need rest for ${remainingTicks} ticks (relativeLastOff=${relLastOff})`);
	const restDurations = (typeof ticksToCleanDurations === 'function')
		? ticksToCleanDurations(remainingTicks, ticksPerBeat)
		: getDurationFromTicks(remainingTicks, ticksPerBeat);

	console.debug(`MR: got durations: ${Array.isArray(restDurations) ? restDurations.join(',') : restDurations}`);

	let timeadded = 0;
	restDurations.forEach((restDuration) => {
		const r = createRest(restDuration);
		if (r) {
			notes.push(r);
			const ticks = calculateTicksFromDuration(restDuration, ticksPerBeat);
			timeadded += ticks;
			console.debug(`MR: added rest ${restDuration} (${ticks} ticks)`);
		} else {
			console.warn(`MR: createRest returned null for ${restDuration}`);
		}
	});

	console.debug(`MR: total added ${timeadded} ticks, relativeLastOff(start)=${relLastOff}`);
}

// ------------------------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПАУЗИ МІЖ НОТАМИ
// Якщо між останньою нотою і поточною подією є розрив більше порогового значення
// Параметри:
// - lastNoteOffTime: Абсолютний час відключення останньої ноти.
// - event: Поточна подія MIDI.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - thresholdGap: Мінімальна кількість тіків для додавання паузи.
// - notes: Масив нот для рендерингу.
// Повертає: void
// ------------------------------------
function addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes) {
	console.debug("MR: FOO: midiRenderer.js - addRestsBetween");
	if (lastNoteOffTime > 0 && event.absTime > lastNoteOffTime) {
		const gapTicks = event.absTime - lastNoteOffTime;
		const restDurations = (typeof ticksToCleanDurations === 'function')
			? ticksToCleanDurations(gapTicks, ticksPerBeat)
			: getDurationFromTicks(gapTicks, ticksPerBeat);
		if (gapTicks > thresholdGap) {
			restDurations.forEach((restDuration) => {
				console.log(`Rest added: current event abs time ${event.absTime} vs lastNoteOffTime: ${lastNoteOffTime}: rest is needed: ${restDuration}`);
				notes.push(createRest(restDuration));
			});
		}
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПАУЗИ НА ПОЧАТКУ ТАКТУ
// якщо перша подія в такті починається не з 0
// Параметри:
// - event: Перша подія в такті.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - thresholdGap: Мінімальна кількість тіків для додавання паузи.
// - notes: Масив нот для рендерингу.
// - barStartAbsTime: Абсолютний час початку такту.
// Повертає: void
// ----------------------
function AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime) {
	console.debug("MR: FOO: midiRenderer.js - AddStartRest");
	const relTime = event.absTime - barStartAbsTime;
	if (relTime <= 0) {
		console.log("AddStartRest: relTime <= 0, no leading rest needed");
		return;
	}

	const restDurations = (typeof ticksToCleanDurations === 'function')
		? ticksToCleanDurations(relTime, ticksPerBeat)
		: getDurationFromTicks(relTime, ticksPerBeat);

	console.log(`AddStartRest: inserting leading rest totalTicks=${relTime}, parts=[${restDurations.join(',')}]`);

	restDurations.forEach(dur => {
		const rest = createRest(dur);
		if (rest) {
			notes.push(rest);
			console.log(`MR: Leading rest added duration=${dur}`);
		} else {
			console.warn(`AddStartRest: failed to create rest for duration=${dur}`);
		}
	});
}