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

(function(){
    const BASE_OCTAVE = 4; // default octave for tokens without marks

    function mapEuBase(letter) {
        // EU 'h' -> 'B'
        const m = { c:'C', d:'D', e:'E', f:'F', g:'G', a:'A', h:'B' };
        return m[letter] || null;
    }

    function parseToken(rawToken) {
        // returns { isRest, key, accidental, durationCode }
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

    function durationToQuarterUnits(code) {
        // Returns how many quarter-notes this duration represents (q=1, h=2, w=4, 8=0.5, etc.), ignoring dots
        const base = code.replace(/\./g,'');
        switch (base) {
            case 'w': return 4;
            case 'h': return 2;
            case 'q': return 1;
            case '8': return 0.5;
            case '16': return 0.25;
            case '32': return 0.125;
            default: return 1;
        }
    }

    function fillRestsToCapacity(notes, remainingQuarters) {
        // Fill the rest of a measure with rests using largest possible durations
        if (remainingQuarters <= 0 || !Array.isArray(notes)) return;
        const units = [4,2,1,0.5,0.25,0.125];
        const codeMap = { 4:'w', 2:'h', 1:'q', 0.5:'8', 0.25:'16', 0.125:'32' };
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

            // Build measures based on time signature capacity
            const capacityQuarters = numerator * (4 / denominator); // quarters per bar
            const measures = [];
            let current = [];
            let used = 0;
            items.forEach(it => {
                const q = durationToQuarterUnits(it.durationCode);
                if (used + q > capacityQuarters + 1e-9) {
                    // close current with rests if needed
                    fillRestsToCapacity(current, capacityQuarters - used);
                    measures.push(current);
                    current = [];
                    used = 0;
                }
                if (it.isRest) {
                    const rest = createRest(it.durationCode.replace(/\.$/, '')); // rests do not use dotted codes in helper
                    if (rest) current.push(rest);
                } else {
                    const note = processNoteElement(it.durationCode, it.key, it.accidental);
                    if (note) current.push(note);
                }
                used += q;
            });
            // push last measure
            if (current.length > 0 || measures.length === 0) {
                fillRestsToCapacity(current, capacityQuarters - used);
                measures.push(current);
            }

            // Compute width/height similar to MIDI path
            const MIN_SCORE_WIDTH = Math.max(320, CLEFZONE + BARWIDTH + Xmargin * 2);
            const containerWidth = (notationDiv && notationDiv.clientWidth) ? notationDiv.clientWidth : 0;
            const effectiveWidth = Math.max(MIN_SCORE_WIDTH, containerWidth || GENERALWIDTH || 1200);
            const totalHeight = calculateRequiredHeight(measures.length, effectiveWidth, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin);

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
                ({ Xposition, Yposition } = adjustXYposition(Xposition, effectiveWidth, BARWIDTH, Yposition, HEIGHT, Xmargin, i, 0));
                if (Yposition !== previousY) {
                    isFirstMeasureInRow = true;
                }

                let STAVE_WIDTH = adjustStaveWidth(BARWIDTH, i, CLEFZONE, isFirstMeasureInRow, false);
                const stave = setStave(Xposition, Yposition, STAVE_WIDTH, i, numerator, denominator, isFirstMeasureInRow, false, false, null);
                stave.setContext(context).draw();

                // Draw this measure
                const ties = [];
                drawMeasure(measures[i], BARWIDTH, context, stave, ties, i, commentsDiv || { innerHTML: '' }, numerator, denominator, 480);

                Xposition += STAVE_WIDTH;
                isFirstMeasureInRow = false;
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
})();
