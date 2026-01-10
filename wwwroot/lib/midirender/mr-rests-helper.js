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
	
	// USE getDurationFromTicks which supports triplets, NOT ticksToCleanDurations
	const restDurations = getDurationFromTicks(remainingTicks, ticksPerBeat);

	console.debug(`MR: got durations: ${Array.isArray(restDurations) ? restDurations.join(',') : restDurations}`);

	let timeadded = 0;
	restDurations.forEach((restDuration) => {
		const r = createRest(restDuration);
		if (r) {
			// Set __srcTicks for tuplet detection
			const ticks = calculateTicksFromDuration(restDuration, ticksPerBeat);
			r.__srcTicks = ticks;
			notes.push(r);
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
// ------------------------------------
function addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes) {
	console.debug("MR: FOO: midiRenderer.js - addRestsBetween");
	if (lastNoteOffTime > 0 && event.absTime > lastNoteOffTime) {
		const gapTicks = event.absTime - lastNoteOffTime;
		
		// USE getDurationFromTicks which supports triplets
		const restDurations = getDurationFromTicks(gapTicks, ticksPerBeat);
		
		if (gapTicks > thresholdGap) {
			restDurations.forEach((restDuration) => {
				console.log(`Rest added: current event abs time ${event.absTime} vs lastNoteOffTime: ${lastNoteOffTime}: rest is needed: ${restDuration}`);
				const r = createRest(restDuration);
				if (r) {
					// Set __srcTicks for tuplet detection
					const ticks = calculateTicksFromDuration(restDuration, ticksPerBeat);
					r.__srcTicks = ticks;
					notes.push(r);
				}
			});
		}
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПАУЗИ НА ПОЧАТКУ ТАКТУ
// ----------------------
function AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime) {
	console.debug("MR: FOO: midiRenderer.js - AddStartRest");
	const relTime = event.absTime - barStartAbsTime;
	if (relTime <= 0) {
		console.log("AddStartRest: relTime <= 0, no leading rest needed");
		return;
	}

	// USE getDurationFromTicks which supports triplets
	const restDurations = getDurationFromTicks(relTime, ticksPerBeat);

	console.log(`AddStartRest: inserting leading rest totalTicks=${relTime}, parts=[${restDurations.join(',')}]`);

	restDurations.forEach(dur => {
		const rest = createRest(dur);
		if (rest) {
			// Set __srcTicks for tuplet detection
			const ticks = calculateTicksFromDuration(dur, ticksPerBeat);
			rest.__srcTicks = ticks;
			notes.push(rest);
			console.log(`MR: Leading rest added duration=${dur}`);
		} else {
			console.warn(`AddStartRest: failed to create rest for duration=${dur}`);
		}
	});
}