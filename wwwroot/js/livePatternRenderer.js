// livePatternRenderer.js
// Live pattern-to-notation for Melodies/Search using patternRenderer.js

(function(){
  let lastPattern = null;
  let renderTimer = null;

  function scheduleRender(){
    if (renderTimer) clearTimeout(renderTimer);
    renderTimer = setTimeout(renderFromTextarea, 50);
  }

  function safeRender(pattern){
    try {
      if (typeof window.renderPatternString !== 'function') return;
      const renderEl = document.getElementById('liveNotation');
      const commentsEl = document.getElementById('patternComments');
      if (!renderEl || !commentsEl) return;
      window.renderPatternString(
        pattern || '',
        renderEl.id,
        commentsEl.id,
        4, 4,
        undefined,
        120,
        20,
        250,
        60,
        10
      );
    } catch(e){
      console.warn('safeRender failed', e);
    }
  }

  function getPattern(){
    const ta = document.getElementById('pianodisplay');
    return ta ? ta.value : '';
  }

  function renderFromTextarea(){
    const current = getPattern();
    if (current === lastPattern) return;
    lastPattern = current;

    // Hide the 'no notes' message once any input appears
    const noMsg = document.getElementById('noNotesMsg');
    if (noMsg) {
      if (current && current.trim().length > 0) {
        noMsg.style.display = 'none';
      } else {
        noMsg.style.display = '';
      }
    }

    safeRender(current);
  }

  function hookPianoButtons(){
    const piano = document.getElementById('pianoroll');
    if (!piano) return;
    piano.addEventListener('click', (ev) => {
      const btn = ev.target.closest('button[data-key]');
      if (!btn) return;
      setTimeout(scheduleRender, 0);
    });
  }

  function hookRestButton(){
    const restBtn = document.getElementById('pausebutton');
    if (!restBtn) return;
    restBtn.addEventListener('click', () => setTimeout(scheduleRender, 0));
  }

  function hookTextareaInput(){
    const ta = document.getElementById('pianodisplay');
    if (!ta) return;
    ta.addEventListener('input', scheduleRender);
    ta.addEventListener('keyup', scheduleRender);
    ta.addEventListener('change', scheduleRender);
  }

  function observeLettersDisplay(){
    const wrapper = document.getElementById('lettersdisplay');
    if (!wrapper || typeof MutationObserver === 'undefined') return;
    const obs = new MutationObserver(() => scheduleRender());
    obs.observe(wrapper, { childList: true, characterData: true, subtree: true });
  }

  function observeSaver(){
    const saver = document.getElementById('saver');
    if (!saver || typeof MutationObserver === 'undefined') return;
    const obs = new MutationObserver(() => scheduleRender());
    obs.observe(saver, { childList: true, characterData: true, subtree: true });
  }

  function startPolling(){
    // Fallback for programmatic changes to textarea.value which do not trigger events
    setInterval(renderFromTextarea, 200);
  }

  function renderOnLoad(){
    renderFromTextarea();
  }

  document.addEventListener('DOMContentLoaded', function(){
    if (!document.getElementById('liveNotation') || !document.getElementById('patternComments')){
      return;
    }
    hookPianoButtons();
    hookRestButton();
    hookTextareaInput();
    observeLettersDisplay();
    observeSaver();
    startPolling();
    renderOnLoad();
  });
})();
