(function(){
  function calcGeneralWidth(){
    const w = window.innerWidth || 1200;
    return Math.min(1200, Math.max(420, w - 80));
  }

  function ensureNotationHeightFitsAll() {
    try {
      const container = document.getElementById('notation');
      if (!container) return;
      const svg = container.querySelector('svg');
      if (!svg) return;
      const vb = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;
      const vbHeight = vb ? vb.height : null;
      const bbox = svg.getBBox ? svg.getBBox() : null;
      const contentHeight = (bbox && bbox.height) ? bbox.height + 10 : (vbHeight || 0);
      const styleH = parseFloat(window.getComputedStyle(container).height) || 0;
      if (contentHeight > 0 && styleH > 0 && contentHeight > styleH) {
        container.style.height = Math.ceil(contentHeight) + 'px';
      }
    } catch (e) { console.warn('ensureNotationHeightFitsAll failed', e); }
  }

  async function renderAdaptive(){
    const notationEl = document.getElementById('notation');
    if (!notationEl) return;

    const midiUrl = notationEl.dataset.midiUrl;
    const isPublicDomain = (notationEl.dataset.publicDomain === 'true');
    const maxBarsToRender = isPublicDomain ? 1000 : 8;

    if (!midiUrl || typeof renderMidiFromUrl !== 'function') return;

    const width = calcGeneralWidth();
    await renderMidiFromUrl(
      midiUrl, maxBarsToRender,
      'notation', 'comments',
      width, 150
    );

    if (isPublicDomain) {
      setTimeout(ensureNotationHeightFitsAll, 0);
    }
  }

  function init(){
    renderAdaptive();
    let resizeTimer;
    window.addEventListener('resize', () => {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(renderAdaptive, 300);
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  // Expose for debugging/manual re-render
  window.MelodyDetails = Object.assign(window.MelodyDetails || {}, {
    renderAdaptive,
    ensureNotationHeightFitsAll
  });
})();
