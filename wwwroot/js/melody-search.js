(function(){
  function calcAdaptiveWidth(){
    const w = window.innerWidth || 1200;
    return Math.min(1200, Math.max(320, w - 40)); // 40px для відступів
  }

  function calcAdaptiveHeight(){
    const baseHeight = 150;
    const screenFactor = (window.innerWidth < 768) ? 0.8 : 1;
    return Math.round(baseHeight * screenFactor);
  }

  function calcAdaptiveBarWidth(){
    const containerWidth = calcAdaptiveWidth();
    const minBarWidth = 180;
    const maxBarWidth = 250;
    // Адаптивна ширина такту залежно від контейнера
    return Math.min(maxBarWidth, Math.max(minBarWidth, containerWidth / 5));
  }

//--------------------
// Оновити рендеринг для всіх нотних блоків на сторінці
//--------------------
  async function rerenderAllNotations(){
    const notationElements = document.querySelectorAll('[id^="notation_match_"]');
    
    for (const el of notationElements) {
      const midiUrl = el.dataset.midiUrl;
      const startPosition = parseInt(el.dataset.startPosition) || 0;
      const commentsId = el.dataset.commentsId;
      
      if (midiUrl && typeof renderMidiSegmentFromUrl === 'function') {
        const width = calcAdaptiveWidth();
        const height = calcAdaptiveHeight();
        const barWidth = calcAdaptiveBarWidth();
        
        try {
          await renderMidiSegmentFromUrl(
            midiUrl,
            startPosition,
            el.id,
            commentsId,
            width,    // GENERALWIDTH
            height,   // HEIGHT  
            20,       // TOPPADDING
            barWidth, // BARWIDTH
            60,       // CLEFZONE
            10,       // Xmargin
            12        // barsToRender
          );
        } catch (e) {
          console.warn(`Failed to render notation for ${el.id}:`, e);
        }
      }
    }
  }

  function init(){
    // ---- закоментовано автоматичний масовий рендер (викликає дублювання з search.js) ----
    // setTimeout(rerenderAllNotations, 100);
    
    // Замість автоматичного масового рендера, можна викликати вручну при потребі:
    // window.MelodySearch && window.MelodySearch.rerenderAllNotations && window.MelodySearch.rerenderAllNotations();

    // ---- закоментовано автоматичний resize-trigger, щоб не повторювати rerender ----
    // let resizeTimer;
    // window.addEventListener('resize', () => {
    //   clearTimeout(resizeTimer);
    //   resizeTimer = setTimeout(rerenderAllNotations, 300);
    // });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  // Експорт для відладки
  window.MelodySearch = Object.assign(window.MelodySearch || {}, {
    calcAdaptiveWidth,
    calcAdaptiveHeight, 
    calcAdaptiveBarWidth,
    rerenderAllNotations
  });
})();

(function(){
   function attachLazyNotesHandlers() {
     // show-notes buttons inside placeholders
     document.querySelectorAll('.show-notes-btn').forEach(btn => {
       btn.addEventListener('click', (ev) => {
         ev.preventDefault();
         const p = btn.closest('.noteslist');
         renderNotesForElement(p);
       });
     });
 
     // clicking a melody block should also render its notes (and allow user to click other melodies)
     document.querySelectorAll('.melody-block').forEach(block => {
       block.addEventListener('click', (ev) => {
         // avoid rendering when clicking buttons/controls inside the block
         if (ev.target.matches('button, a, input, select, textarea')) return;
         const p = block.querySelector('.noteslist');
         if (p) renderNotesForElement(p);
       });
     });

     // next/previous navigation: render target melody's notes when user navigates
     document.querySelectorAll('.song-nav').forEach(navBtn => {
       navBtn.addEventListener('click', (ev) => {
         ev.preventDefault();
         // find current melody-block
         const current = navBtn.closest('.melody-block');
         if (!current) return;
         // choose direction by class (next or prev)
         const isNext = navBtn.classList.contains('next');
         // find sibling melody-block in the chosen direction
         let target = current;
         while (true) {
           target = isNext ? target.nextElementSibling : target.previousElementSibling;
           if (!target) break;
           if (target.classList && target.classList.contains('melody-block')) break;
         }
         if (target && target.classList && target.classList.contains('melody-block')) {
           const targetNotes = target.querySelector('.noteslist');
           if (targetNotes) renderNotesForElement(targetNotes);
         }
       });
     });
   }
})();