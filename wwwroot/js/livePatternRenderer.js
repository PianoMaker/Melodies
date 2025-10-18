// livePatternRenderer.js
// Live pattern-to-notation for Melodies/Search using patternRenderer.js

(function(){
  let lastPattern = null;
  let renderTimer = null;
  // Зберігаємо поточний розмір такту
  let currentNumerator = 4;
  let currentDenominator = 4;

  function scheduleRender(){
    console.log('[scheduleRender] Called');
    if (renderTimer) clearTimeout(renderTimer);
    renderTimer = setTimeout(renderFromTextarea, 50);
  }

  function safeRender(pattern){
    try {
      if (typeof window.renderPatternString !== 'function') {
        console.warn('[safeRender] window.renderPatternString is not a function');
        return;
      }
      const renderEl = document.getElementById('liveNotation');
      const commentsEl = document.getElementById('patternComments');
      if (!renderEl || !commentsEl) {
        console.warn('[safeRender] renderEl or commentsEl not found', { renderEl, commentsEl });
        return;
      }
      console.log('[safeRender] Rendering pattern:', pattern, 'with size', currentNumerator, '/', currentDenominator);
      window.renderPatternString(
        pattern || '',
        renderEl.id,
        commentsEl.id,
        currentNumerator,
        currentDenominator,
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
    if (!ta) {
      console.warn('[getPattern] pianodisplay textarea not found');
      return '';
    }
    return ta.value;
  }

  function renderFromTextarea(){
    const current = getPattern();
    console.log('[renderFromTextarea] Current pattern:', current);
    if (current === lastPattern) {
      console.log('[renderFromTextarea] Pattern unchanged, skipping render');
      return;
    }
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
    if (!piano) {
      console.warn('[hookPianoButtons] pianoroll not found');
      return;
    }
    piano.addEventListener('click', (ev) => {
      const btn = ev.target.closest('button[data-key]');
      if (!btn) return;
      console.log('[hookPianoButtons] Piano button clicked:', btn.dataset.key);
      // При кожному натисканні ноти використовуємо збережений розмір
      setTimeout(() => {
        scheduleRender();
      }, 0);
    });
  }

  function hookRestButton(){
    const restBtn = document.getElementById('pausebutton');
    if (!restBtn) {
      console.warn('[hookRestButton] pausebutton not found');
      return;
    }
    restBtn.addEventListener('click', () => {
      console.log('[hookRestButton] Pause button clicked');
      setTimeout(scheduleRender, 0);
    });
  }

  function hookTextareaInput(){
    const ta = document.getElementById('pianodisplay');
    if (!ta) {
      console.warn('[hookTextareaInput] pianodisplay textarea not found');
      return;
    }
    ta.addEventListener('input', scheduleRender);
    ta.addEventListener('keyup', scheduleRender);
    ta.addEventListener('change', scheduleRender);
    console.log('[hookTextareaInput] Textarea input hooks set');
  }

  function observeLettersDisplay(){
    const wrapper = document.getElementById('lettersdisplay');
    if (!wrapper || typeof MutationObserver === 'undefined') {
      console.warn('[observeLettersDisplay] lettersdisplay or MutationObserver not available');
      return;
    }
    const obs = new MutationObserver(() => {
      console.log('[observeLettersDisplay] Mutation detected');
      scheduleRender();
    });
    obs.observe(wrapper, { childList: true, characterData: true, subtree: true });
  }

  function observeSaver(){
    const saver = document.getElementById('saver');
    if (!saver || typeof MutationObserver === 'undefined') {
      console.warn('[observeSaver] saver or MutationObserver not available');
      return;
    }
    const obs = new MutationObserver(() => {
      console.log('[observeSaver] Mutation detected');
      scheduleRender();
    });
    obs.observe(saver, { childList: true, characterData: true, subtree: true });
  }

  function startPolling(){
    // Fallback for programmatic changes to textarea.value which do not trigger events
    setInterval(renderFromTextarea, 200);
    console.log('[startPolling] Polling started');
  }

  function renderOnLoad(){
    console.log('[renderOnLoad] Initial render');
    renderFromTextarea();
  }

  document.addEventListener('DOMContentLoaded', function () {
    // Get references to the time signature controls
    const numeratorInput = document.getElementById('TimeSignatureNumerator');
    const denominatorSelect = document.getElementById('TimeSignatureDenominator');

    // Function to get the current pattern and re-render
    function updatePatternRenderer() {
        const pattern = window.currentPatternString || getPattern() || '';
        const renderId = 'liveNotation';
        const commentsId = 'patternComments';

        // Get current values
        currentNumerator = parseInt(numeratorInput?.value, 10) || 4;
        currentDenominator = parseInt(denominatorSelect?.value, 10) || 4;

        console.log('[updatePatternRenderer] Time signature changed:', { numerator: currentNumerator, denominator: currentDenominator, pattern });

        // Call the renderer
        if (window.renderPatternString) {
            window.renderPatternString(
                pattern,
                renderId,
                commentsId,
                currentNumerator,
                currentDenominator
            );
        } else {
            console.warn('[updatePatternRenderer] window.renderPatternString not found');
        }
    }

    // Listen for changes
    if (numeratorInput) {
      numeratorInput.addEventListener('change', updatePatternRenderer);
      console.log('[DOMContentLoaded] Numerator input found and hooked');
    } else {
      console.warn('[DOMContentLoaded] Numerator input not found');
    }
    if (denominatorSelect) {
      denominatorSelect.addEventListener('change', updatePatternRenderer);
      console.log('[DOMContentLoaded] Denominator select found and hooked');
    } else {
      console.warn('[DOMContentLoaded] Denominator select not found');
    }

    // Optionally, call once on load
    updatePatternRenderer();

    if (!document.getElementById('liveNotation') || !document.getElementById('patternComments')){
      console.warn('[DOMContentLoaded] liveNotation or patternComments not found, aborting');
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
