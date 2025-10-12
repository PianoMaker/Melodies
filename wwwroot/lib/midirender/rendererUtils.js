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

  // Computes total ticks for a VexFlow note/rest taking dots into account
  function getTotalTicksForNote(note, ticksPerBeat) {
    try {
      if (!note || !ticksPerBeat) return 0;
      const duration = (typeof note.getDuration === 'function') ? String(note.getDuration()) : String(note.duration || 'q');
      const dots = (duration.match(/\./g) || []).length;
      const baseDuration = duration.replace(/\./g, '');
      // calculateTicksFromDuration comes from midiparser_ext.js
      let baseTicks = (typeof calculateTicksFromDuration === 'function')
        ? calculateTicksFromDuration(baseDuration, ticksPerBeat)
        : 0;
      let totalTicks = baseTicks;
      let currentAddition = baseTicks;
      for (let i = 0; i < dots; i++) { currentAddition /= 2; totalTicks += currentAddition; }
      return totalTicks;
    } catch (e) {
      console.warn('getTotalTicksForNote failed:', e);
      return 0;
    }
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


  function calculateRequiredHeight(measures, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING = 20, CLEFZONE = 60, Xmargin = 10) {
    const rows = calculateRows(measures, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin);
    return Math.ceil(rows * HEIGHT) + TOPPADDING;
  }

  ensureGlobal('getTotalTicksForNote', getTotalTicksForNote);
  ensureGlobal('calculateRows', calculateRows);
  ensureGlobal('calculateRequiredHeight', calculateRequiredHeight);
})();