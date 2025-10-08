// makeBeams.js
// ГРУПУВАННЯ (BEAMING) КОРОТКИХ НОТ У МЕЖАХ ОДНОГО ТАКТУ
// --------------------------------------------------------------------
// Спрощений виклик: makeBeams(measure, ticksPerBeat)
// --------------------------------------------------------------------

const beamableDurations = new Set(['8', '16', '32', '64', '128']);
const minGroupSize = 2;
const allowDotted = false;

(function() {

    function makeBeams(measure, ticksPerBeat, timeSignature) {
        console.log(`FOO: MB: makeBeams`);
        if (typeof ENABLE_BEAMS !== 'undefined' && !ENABLE_BEAMS) {
            console.warn('MB: Beaming is disabled by global flag ENABLE_BEAMS=false');
            return { beamGroups: [], beams: [] };
        }
        if (!measure || !Array.isArray(measure.notes) || measure.notes.length === 0) {
            return { beamGroups: [], beams: [] };
        }

        //console.log('MB: makeBeams called with measure:', measure, 'ticksPerBeat:', ticksPerBeat);

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

        measure.notes.forEach((note, idx) => {
            //console.log(`MB: analysing note idx=${idx} (raw=${note}) currentGroupLen=${currentGroup.notes.length}`);

            if (!note) {
                if (currentGroup.notes.length) closeGroup(currentGroup, beamGroups, beams, 'null-note');
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

            console.log(`MB: startTick=${note.startTick}, ticks=${noteTicks}, endTick=${note.endTick} (beats start=${note.startBeat}, end=${note.endBeat})`);

            // 4. Перевірка межі біта у beat-одиницях (чверті)
            const noteEndBeat = note.endBeat;
            let splitOnBeat = CheckSplitOnBeat(noteEndBeat, timeSignature);

            // 5. Beamable?
            const beamable = isBeamable(note);
            const next = measure.notes[idx + 1];
            const nextBeamable = next ? isBeamable(next) : false;

            if (!beamable) {
                if (currentGroup.notes.length) closeGroup(currentGroup, beamGroups, beams, 'non-beamable');
                runningTicks += noteTicks;
                return;
            }

            // 6. Формування групи
            if (currentGroup.notes.length === 0) {
                startGroup(currentGroup, note, idx, noteTicks);
            } else {
                addToGroup(currentGroup, note, noteTicks);
            }

            // 7. Логіка закриття групи
            let mustClose = false;
            if (!nextBeamable) mustClose = true;
            if (!mustClose && splitOnBeat) {
                mustClose = true;
            }
            if (idx === measure.notes.length - 1) {
                mustClose = true;
            }
            if (mustClose) {
                closeGroup(currentGroup, beamGroups, beams, 'boundary');
            }

            // 8. Просуваємо глобальний час (у MIDI ticks)
            runningTicks += noteTicks;
        });

        console.log(`MB: return results summary:
 - beamGroups: ${beamGroups.length} groups
 - beams: ${beams.length} beam objects`);

        beamGroups.forEach((group, index) => {
            console.log(`MB: beamGroup[${index}]:`, {
                notesCount: group.notes.length,
                startIndex: group.startIndex,
                endIndex: group.endIndex,
                reasonEnded: group.reasonEnded
            });
        });
        return { beamGroups, beams };
    }

    function defaultDurationResolver(note) {
        console.log(`FOO: MB: defaultDurationResolver`);
        if (note && note.vexNote && typeof note.vexNote.getDuration === 'function') {
            let d = note.vexNote.getDuration(); // напр. '8', '16', '8r', 'q', '8.'
            const isRest = d.endsWith('r');
            const base = d.replace(/r$/, '').replace(/\.+$/, '');
            const dotted = /\./.test(d);
            return { code: base, isRest, dotted };
        }
        if (typeof note.duration === 'number') {
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
    // Узгоджує напрямок штилів усередині групи коли є лише один «відступ»
    // або пара з двох нот з протилежними напрямками, щоб уникнути розриву beam.
    function unifyBeamGroupDirections(currentGroup) {
        try {
            if (!currentGroup || !currentGroup.notes || currentGroup.notes.length < 2) return;
            const notes = currentGroup.notes.map(n => n.vexNote || n).filter(Boolean);
            if (notes.length < 2) return;

            let upCount = 0, downCount = 0, sumLines = 0, totalHeads = 0;
            notes.forEach(vn => {
                if (typeof vn.getStemDirection === 'function') {
                    const dir = vn.getStemDirection();
                    if (dir === 1 || dir === Vex.Flow.Stem.UP) upCount++;
                    else if (dir === -1 || dir === Vex.Flow.Stem.DOWN) downCount++;
                }
                if (typeof vn.getKeyProps === 'function') {
                    const kp = vn.getKeyProps();
                    kp.forEach(k => { sumLines += k.line; totalHeads++; });
                }
            });

            // Якщо напрям однорідний — нічого не робимо
            if (!(upCount && downCount)) return;

            // Визначаємо цільовий напрям
            let targetDir;
            if (upCount !== downCount) {
                targetDir = (upCount > downCount) ? Vex.Flow.Stem.UP : Vex.Flow.Stem.DOWN;
            } else {
                const avgLine = totalHeads ? (sumLines / totalHeads) : 3;
                targetDir = (avgLine >= 3) ? Vex.Flow.Stem.DOWN : Vex.Flow.Stem.UP;
            }

            // Примусово виставляємо однаковий напрям для всіх нот групи
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
        console.log("MB: closeGroup method starts");
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
            console.log(`MB: closegroup, beamGroups length=${beamGroups.length}`)

            if (typeof Vex !== 'undefined' && Vex.Flow && Vex.Flow.Beam) {
                try {
                    // НОВЕ: перед створенням beam – уніфікуємо напрямки за потреби
                    unifyBeamGroupDirections(current);
                    beams.push(new Vex.Flow.Beam(current.notes.map(n => n.vexNote || n)));
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
        console.log(`MB: startGroup method starts, idx=${idx}`);
        currentGroup.notes = [note];
        currentGroup.startIndex = idx;
        currentGroup.ticksAccum = noteTicks;
    }

    function addToGroup(currentGroup, note, noteTicks) {
        console.log("MB: addToGroup method starts");
        currentGroup.notes.push(note);
        currentGroup.ticksAccum += noteTicks;
    }

    if (typeof window !== 'undefined') {
        window.makeBeams = makeBeams;
        console.log(`MB: makeBeams function is loaded and available as window.makeBeams`);
    } else if (typeof globalThis !== 'undefined') {
        globalThis.makeBeams = makeBeams;
        console.log(`MB: makeBeams function is loaded and available as globalThis.makeBeams`);
    }

    function CheckSplitOnBeat(noteEndBeat, timeSignature) {
        console.log(`MB: CheckSplitOnBeat method starts, noteEndBeat=${noteEndBeat}, time sign=${JSON.stringify(timeSignature)}`);
        if (!timeSignature || noteEndBeat == null) return false;

        if (!Array.isArray(timeSignature.beatPositions)) {
            const num = timeSignature.num || 4;
            const den = timeSignature.den || 4;
            // Базові позиції у чвертях
            const barBeats = num * 4 / den;
            timeSignature.beatPositions = [];

            // Складені метри: 6/8, 9/8, 12/8 — крок крапкована чверть (1.5 чверті)
            const isCompound = (den === 8) && (num % 3 === 0) && (num >= 3);
            if (isCompound) {
                const step = 3 * (4 / den); // для den=8 -> 1.5
                for (let p = step; p <= barBeats + 1e-9; p += step) {
                    timeSignature.beatPositions.push(Number(p.toFixed(9)));
                }
            } else {
                // Прості метри: межі на кожну чверть
                for (let b = 1; b <= barBeats; b++) timeSignature.beatPositions.push(b);
            }
            console.log(`MB: computed beatPositions: [${timeSignature.beatPositions.join(', ')}]`);
        }
        // Порівнюємо з точністю до 1e-9 (floating safety)
        return timeSignature.beatPositions.some(bp => Math.abs(bp - noteEndBeat) < 1e-9);
    }

    // --- Triplet detection -------------------------------------------------
    function approx(a, b, eps) { return Math.abs(a - b) <= eps; }

    function getSrcTicksOrFallback(note, localTicksPerBeat) {
        // Якщо ми прикріпили __srcTicks у рендері — використовуємо, інакше оцінюємо за кодом тривалості
        const vn = note.vexNote || note;
        if (vn && typeof vn.__srcTicks === 'number') return vn.__srcTicks;
        return getNoteMidiTicks(note, localTicksPerBeat);
    }

    // Розпізнання тріолей (8-мих, 16-тих і чвертних)
    function detectTuplets(measure, ticksPerBeat) {
        if (!measure || !Array.isArray(measure.notes) || measure.notes.length < 3) return [];
        const local = ticksPerBeat || 480;
        const tuplets = [];

        const isBeamableNote = (n) => {
            const d = durationResolver(n);
            return !d.isRest && (beamableDurations.has(d.code) || d.code === 'q');
        };

        for (let i = 0; i <= measure.notes.length - 3;) {
            const a = measure.notes[i], b = measure.notes[i + 1], c = measure.notes[i + 2];
            const va = a && (a.vexNote || a), vb = b && (b.vexNote || b), vc = c && (c.vexNote || c);

            // Швидкий шлях: усі три мають маркер __isTriplet та однакову базу
            const fastTriplet =
                va && vb && vc &&
                va.__isTriplet && vb.__isTriplet && vc.__isTriplet &&
                va.__tripletBase && vb.__tripletBase && vc.__tripletBase &&
                va.__tripletBase === vb.__tripletBase && vb.__tripletBase === vc.__tripletBase;

            if (fastTriplet) {
                try {
                    const tuplet = new Vex.Flow.Tuplet([va, vb, vc], {
                        num_notes: 3,
                        notes_occupied: 2,
                        ratioed: false
                    });
                    tuplets.push(tuplet);
                } catch (e) {
                    console.warn('Tuplet creation failed (fast path):', e);
                }
                i += 3;
                continue;
            }

            if (!a || !b || !c || !isBeamableNote(a) || !isBeamableNote(b) || !isBeamableNote(c)) {
                i += 1; continue;
            }

            const ta = getSrcTicksOrFallback(a, local);
            const tb = getSrcTicksOrFallback(b, local);
            const tc = getSrcTicksOrFallback(c, local);
            const total = ta + tb + tc;

            const dA = durationResolver(a).code;
            const dB = durationResolver(b).code;
            const dC = durationResolver(c).code;
            const sameDur = (dA === dB && dB === dC);

            const epsTotal = local * 0.06;
            const epsEach  = local * 0.10;

            const is8thTriplet  = sameDur && dA === '8'
                && Math.abs(total - local) <= epsTotal
                && Math.abs(ta - local/3) <= epsEach && Math.abs(tb - local/3) <= epsEach && Math.abs(tc - local/3) <= epsEach;

            const is16thTriplet = sameDur && dA === '16'
                && Math.abs(total - local/2) <= epsTotal
                && Math.abs(ta - local/6) <= epsEach && Math.abs(tb - local/6) <= epsEach && Math.abs(tc - local/6) <= epsEach;

            const isQuarterTriplet = sameDur && dA === 'q'
                && Math.abs(total - (2*local)) <= (epsTotal * 1.2)
                && Math.abs(ta - (2*local/3)) <= (epsEach * 1.2)
                && Math.abs(tb - (2*local/3)) <= (epsEach * 1.2)
                && Math.abs(tc - (2*local/3)) <= (epsEach * 1.2);

            if (is8thTriplet || is16thTriplet || isQuarterTriplet) {
                const vexNotes = [va, vb, vc];
                try {
                    const tuplet = new Vex.Flow.Tuplet(vexNotes, {
                        num_notes: 3,
                        notes_occupied: 2,
                        ratioed: false
                    });
                    tuplets.push(tuplet);
                } catch (e) {
                    console.warn('Tuplet creation failed:', e);
                }
                i += 3;
            } else {
                i += 1;
            }
        }

        return tuplets;
    }


    // Експортуємо для виклику з midiRenderer.js
    if (typeof window !== 'undefined') {
        window.detectTuplets = detectTuplets;
    } else if (typeof globalThis !== 'undefined') {
        globalThis.detectTuplets = detectTuplets;
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
            console.log('applyAutoStem is working');
            if (typeof durationCode === 'string' && /r$/.test(durationCode)) return; // skip rests
            if (typeof note.autoStem === 'function') {
                note.autoStem();
            }
        } catch (e) {
            console.warn('applyAutoStem failed:', e);
        }
    }

    // Експортуємо для виклику з midiRenderer.js
    if (typeof window !== 'undefined') {
        window.applyAutoStem = applyAutoStem;
    } else if (typeof globalThis !== 'undefined') {
        globalThis.applyAutoStem = applyAutoStem;
    }

})();
