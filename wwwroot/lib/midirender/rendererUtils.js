// rendererUtils.js
// Shared helpers for midiRenderer and patternRenderer
(function(){
  function ensureGlobal(name, fn){
    if (typeof window !== 'undefined') {
      if (!window[name]) window[name] = fn;
    } else if (typeof globalThis !== 'undefined') {
      if (!globalThis[name]) globalThis[name] = fn;
    }
  }

  // Safe dot counter that checks both duration string and VexFlow modifiers
  function safeDotCount(note, durationStr) {
    try {
      // 1) dots in string (e.g. 'q.')
      let dots = (String(durationStr || '').match(/\./g) || []).length;

      if (dots === 0 && note) {
        // 2) VexFlow API
        if (typeof note.getDots === 'function') return (note.getDots() || []).length || 0;

        if (typeof note.getModifiers === 'function') {
          const mods = note.getModifiers() || [];
          return mods.filter(m =>
            (m && (m.getCategory?.() === 'dots' || m.category === 'dots')) ||
            (typeof Vex !== 'undefined' && Vex.Flow && m instanceof Vex.Flow.Dot)
          ).length;
        }
        // 3) legacy property
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

      // Prefer explicit duration code if available (has '.'/'t' and no 'r' problems)
      let codeStr = typeof note.__durationCode === 'string'
        ? note.__durationCode
        : String(typeof note.getDuration === 'function' ? note.getDuration() : (note.duration || 'q'));

      // Strip rest suffix for base mapping (e.g. 'qr' => 'q')
      codeStr = codeStr.replace(/r$/, '');

      // Base ticks using our MIDI helpers (handles '.'/'t' when present)
      let baseTicks = 0;
      if (typeof calculateTicksFromDuration === 'function') {
        baseTicks = calculateTicksFromDuration(codeStr, ticksPerBeat);
      }

      // If explicit code had no dot and no triplet, but the note carries dot modifiers, apply dot expansion
      const hasDotInCode = /\./.test(codeStr);
      const isTriplet = /t$/.test(codeStr);
      if (!hasDotInCode && !isTriplet) {
        const dots = safeDotCount(note, codeStr);
        if (dots > 0) {
          let totalTicks = baseTicks;
          let add = Math.floor(baseTicks / 2);
          for (let i = 0; i < dots; i++) { totalTicks += add; add = Math.floor(add / 2); }
          return totalTicks;
        }
      }

      return baseTicks;
    } catch (e) {
      console.warn('getTotalTicksForNote failed:', e);
      return 0;
    }
    }

    function ticksToCleanDurations(ticks, ticksPerBeat) {
        try {
            if (!(ticks > 0) || !(ticksPerBeat > 0)) return [];
            console.log("PR: tickToCleanDurions")
            const table = [
                [ticksPerBeat * 4, 'w'],
                [ticksPerBeat * 2, 'h'],
                [ticksPerBeat * 1, 'q'],
                [ticksPerBeat * 0.5, '8'],
                [ticksPerBeat * 0.25, '16'],
                [ticksPerBeat * 0.125, '32'],
                [ticksPerBeat * 0.0625, '64']
            ];
            const tol = ticksPerBeat / 32; // допуск на округлення
            let rem = ticks;
            const out = [];
            while (rem > tol) {
                let picked = false;
                for (const [step, code] of table) {
                    if (rem >= step - tol) {
                        out.push(code);
                        rem -= step;
                        picked = true;
                        break;
                    }
                }
                if (!picked) break; // захист від зависання на дуже малих залишках
            }
            return out;
        } catch { return []; }
    }


    // ----------------------
    // ФУНКЦІЯ ДЛЯ РОЗРАХУНКУ ПОТРІБНОЇ ВИСОТИ НОТНОГО РЯДКУ В КАНВАСІ
    // Параметри:
    // - measuresCount: Кількість тактів.
    // - GENERALWIDTH: Загальна ширина канвасу.
    // - BARWIDTH: Ширина одного такту.
    // - HEIGHT: Висота одного рядка нотного стану.
    // - TOPPADDING: Верхній відступ (за замовчуванням 20).
    // - CLEFZONE: Додаткова ширина для першого такту в рядку (за замовчуванням 60).
    // - Xmargin: Лівий відступ (за замовчуванням 10).
    // Повертає: Загальну висоту канвасу, необхідну для розміщення всіх тактів.
    // ----------------------
  function calculateRows(measures, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING = 20, CLEFZONE = 60, Xmargin = 10) {
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
  // Fixed-width rows calculation (shared with patternRenderer)
  // Given a fixed bar width (already computed), calculate how many rows are needed
  // ----------------------
  function calculateRowsFixedWidth(measuresArr, generalWidth, fixedBarWidth, clefZone = 60, xMargin = 10){
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


  function calculateRequiredHeight(measures, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING = 20, CLEFZONE = 60, Xmargin = 10) {
    const rows = calculateRows(measures, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin);
    return Math.ceil(rows * HEIGHT) + TOPPADDING;
  }

  // Greedy-декомпозиція в "чисті" тривалості без крапок/тріолей
  
  ensureGlobal('getTotalTicksForNote', getTotalTicksForNote);
  ensureGlobal('calculateRows', calculateRows);
  ensureGlobal('calculateRowsFixedWidth', calculateRowsFixedWidth);
  ensureGlobal('calculateRequiredHeight', calculateRequiredHeight);
  ensureGlobal('ticksToCleanDurations', ticksToCleanDurations);
})();