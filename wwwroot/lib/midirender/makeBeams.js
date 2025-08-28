// makeBeams.js
// ГРУПУВАННЯ (BEAMING) КОРОТКИХ НОТ У МЕЖАХ ОДНОГО ТАКТУ
// --------------------------------------------------------------------
// Очікувана структура measure:
//   {
//     notes: [
//       {
//         duration: number | undefined,     // опціонально: умовна числова тривалість (менше = коротше) або інша ваша модель
//         vexNote: Vex.Flow.StaveNote,      // (опція) реальний VexFlow об'єкт, з якого можна взяти getDuration()
//         ... інші поля ...
//       }, ...
//     ]
//   }
//
// ФУНКЦІЯ ВИЗНАЧАЄ ГРУПИ ("beam groups") З ПОСЛІДОВНИХ КОРОТКИХ НОТ.
// Вона:
//  1) Визначає, чи нота "beamable" (коротка) – за кодом тривалості або числовим duration.
//  2) Початок групи: перша "beamable" після паузи/довгої ноти/межі долі (якщо splitOnBeat).
//  3) Кінець групи: перед не-beamable нотою, кінцем масиву, або на межі долі (splitOnBeat).
//  4) Позначає першу й останню ноту групи прапорцями:
//       note.__beamStart = true
//       note.__beamEnd   = true
//  5) Формує масив beamGroups (масив масивів нот).
//  6) Створює Vex.Flow.Beam для кожної групи (якщо доступний Vex.Flow і розмір групи >= minGroupSize).
//
// ПОВЕРТАЄ:
//   {
//     beamGroups: Array<{ notes:StaveNote[], startIndex:number, endIndex:number }>,
//     beams:      Array<Vex.Flow.Beam>
//   }
//
// ОПЦІЇ (options):
//   beamableDurations : Set<string>   – коди тривалостей VexFlow без суфікса 'r' (за замовч. 8,16,32,64,128)
//   minGroupSize      : number        – мінімум нот у групі (default: 2)
//   splitOnBeat       : boolean       – чи розбивати групи по межах долей (default: false)
//   ticksPerBeat      : number        – використовується для splitOnBeat (default: 480)
//   durationResolver  : function(note): {code:string,isRest:boolean}
//                        (можна перевизначити власну логіку парсингу)
//   allowDotted       : boolean       – чи дозволяти dotted (перша реалізація: dotted не розбиває, просто код без '.')
//
// ЗАСТЕРЕЖЕННЯ:
//   BEAMING НЕ РОЗШИРЮЄ ноти по тривалостях; лише групує вже створені StaveNote.
//
// --------------------------------------------------------------------

(function() {

  function defaultDurationResolver(note) {
    // 1) Якщо є vexNote – беремо звідти
    if (note && note.vexNote && typeof note.vexNote.getDuration === 'function') {
      let d = note.vexNote.getDuration(); // напр. '8', '16', '8r', 'q', '8.'
      const isRest = d.endsWith('r');
      // Відрізаємо суфікс 'r' і крапки для базового групування
      const base = d.replace(/r$/,'').replace(/\.+$/,'')
      const dotted = /\./.test(d);
      return { code: base, isRest, dotted };
    }

    // 2) Якщо є числова duration (ВАША МОДЕЛЬ):
    //    Припустимо: 1=ціла,2=половина,4=чверть,8=восьма,...
    if (typeof note.duration === 'number') {
      // Перетворюємо число в рядок для вашої множини beamableDurations
      return { code: String(note.duration), isRest: !!note.isRest, dotted: false };
    }

    return { code: '', isRest: true, dotted: false };
  }

  function makeBeams(measure, ticksPerBeat, options = {}) {
      if (typeof ENABLE_BEAMS !== 'undefined' && !ENABLE_BEAMS) {
          console.warn('Beaming is disabled by global flag ENABLE_BEAMS=false');
      return { beamGroups: [], beams: [] };
    }

    if (!measure || !Array.isArray(measure.notes) || measure.notes.length === 0) {
      return { beamGroups: [], beams: [] };
      }
      console.log('MB: makeBeams called with measure:', measure, 'ticksPerBeat:', ticksPerBeat, 'options:', options);

    const {
      beamableDurations = new Set(['8','16','32','64','128']),
      minGroupSize = 2,
      splitOnBeat = false,
      durationResolver = defaultDurationResolver,
      allowDotted = false
    } = options;

    const localTicksPerBeat = ticksPerBeat || options.ticksPerBeat || 480;

    const beamGroups = [];
    const beams = [];

    let current = {
      notes: [],
      startIndex: -1,
      ticksAccum: 0 // накопичені тікі групи (для splitOnBeat)
    };

    function getNoteTicksFromVex(note) {
      // Спроба отримати реальні тікі (якщо є tickContext)
      try {
        if (note.vexNote && note.vexNote.getTicks && note.vexNote.getTicks()) {
          return note.vexNote.getTicks().value();
        }
      } catch {}
      // fallback: приблизне обчислення по коду
      const d = durationResolver(note);
      const mapQuarter = { 'w':4, 'h':2, 'q':1, '8':0.5, '16':0.25, '32':0.125, '64':0.0625, '128':0.03125 };
      const baseUnits = mapQuarter[d.code] ?? 1;
      let ticks = baseUnits * localTicksPerBeat;
      if (d.dotted) ticks *= 1.5;
      return ticks;
    }

      // Чи нота піддлягає групуванню (beamable)
    function isBeamable(note) {
      const d = durationResolver(note);
      if (d.isRest) return false;
      if (!allowDotted && d.dotted) return false;
      // Для числової моделі (наприклад '8') – перевіряємо множину
      return beamableDurations.has(d.code);
    }

      // Закриває поточну групу (якщо є) і скидає стан
    function closeGroup(reason) {
      if (current.notes.length >= minGroupSize) {
        const first = current.notes[0];
        const last  = current.notes[current.notes.length - 1];
        first.__beamStart = true;
        last.__beamEnd = true;

        beamGroups.push({
          notes: [...current.notes],
          startIndex: current.startIndex,
            endIndex: current.startIndex + current.notes.length - 1,
          reasonEnded: reason
        });

        if (typeof Vex !== 'undefined' && Vex.Flow && Vex.Flow.Beam) {
          try {
            beams.push(new Vex.Flow.Beam(current.notes.map(n => n.vexNote || n)));
          } catch(e) {
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
      current.notes = [];
      current.startIndex = -1;
      current.ticksAccum = 0;
    }

    function startGroup(note, idx) {
      current.notes = [note];
      current.startIndex = idx;
      current.ticksAccum = getNoteTicksFromVex(note);
    }

    function addToGroup(note) {
      current.notes.push(note);
      current.ticksAccum += getNoteTicksFromVex(note);
    }

    measure.notes.forEach((note, idx) => {
      if (!note) {
        // Розрив: форсуємо закриття поточної групи
        if (current.notes.length) closeGroup('null-note');
        return;
      }

        const beamable = isBeamable(note);
        console.log(`MB: Note idx=${idx} beamable=${beamable}`);
      const next = measure.notes[idx + 1];
      const nextBeamable = next ? isBeamable(next) : false;

      if (!beamable) {
        // Поточну групу закриваємо, якщо є
        if (current.notes.length) closeGroup('non-beamable');
        return;
      }

      // Якщо поточна група порожня – старт
      if (current.notes.length === 0) {
        startGroup(note, idx);
      } else {
        addToGroup(note);
      }

      // Умова завершення:
      let mustClose = false;

      // 1) Наступна нота не beamable
      if (!nextBeamable) mustClose = true;

      // 2) splitOnBeat -> якщо накопичені тікі групи кратні долі
      if (!mustClose && splitOnBeat) {
        if (current.ticksAccum % localTicksPerBeat === 0) {
          mustClose = true;
        }
      }

      // 3) Кінець масиву
      if (idx === measure.notes.length - 1) {
        mustClose = true;
      }

      if (mustClose) {
        closeGroup('boundary');
      }
    });

    return { beamGroups, beams };
  }

  // Публікуємо в глобальній області (без export, не React)
  if (typeof window !== 'undefined') {
    window.makeBeams = makeBeams;
  } else if (typeof globalThis !== 'undefined') {
    globalThis.makeBeams = makeBeams;
  }

})();
