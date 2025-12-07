// Manage mobile menu state so media rule for .hideformenu works only when menu open.
// Supports: custom overlay (#mobileMenuToggle / #mobileMenuOverlay) and Bootstrap collapse (#mainNavbar).
document.addEventListener('DOMContentLoaded', function () {
  var toggleBtn = document.querySelector('#mobileMenuToggle'); // optional custom toggle
  var menu = document.querySelector('#mobileMenuOverlay');    // optional overlay menu
  var backdrop = document.querySelector('#mobileMenuBackdrop');

  function setBodyOpen(open) {
    if (open) document.body.classList.add('menu-open'); else document.body.classList.remove('menu-open');
  }

  // Custom overlay toggle
  if (toggleBtn && menu) {
    toggleBtn.addEventListener('click', function (e) {
      e.preventDefault();
      var isOpen = menu.classList.contains('open');
      if (isOpen) {
        menu.classList.remove('open');
        if (backdrop) backdrop.classList.remove('show');
        setBodyOpen(false);
        if (toggleBtn) toggleBtn.setAttribute('aria-expanded', 'false');
        menu.setAttribute('aria-hidden', 'true');
      } else {
        menu.classList.add('open');
        if (backdrop) backdrop.classList.add('show');
        setBodyOpen(true);
        if (toggleBtn) toggleBtn.setAttribute('aria-expanded', 'true');
        menu.setAttribute('aria-hidden', 'false');
      }
    });
    if (backdrop) backdrop.addEventListener('click', function () { 
      menu.classList.remove('open'); 
      backdrop.classList.remove('show'); 
      setBodyOpen(false);
      if (toggleBtn) toggleBtn.setAttribute('aria-expanded', 'false');
      if (menu) menu.setAttribute('aria-hidden', 'true');
    });
  }

  // Bootstrap collapse integration (layout uses #mainNavbar)
  var bsNavbar = document.getElementById('mainNavbar');
  if (bsNavbar) {
    bsNavbar.addEventListener('show.bs.collapse', function () { setBodyOpen(true); });
    bsNavbar.addEventListener('shown.bs.collapse', function () { setBodyOpen(true); });
    bsNavbar.addEventListener('hide.bs.collapse', function () { setBodyOpen(false); });
    bsNavbar.addEventListener('hidden.bs.collapse', function () { setBodyOpen(false); });
  }

  // Fallback: listen toggler clicks and inspect aria-expanded (for cases without Bootstrap events)
  var togglers = document.querySelectorAll('.navbar-toggler[data-bs-toggle="collapse"]');
  if (togglers && togglers.length) {
    togglers.forEach(function (t) {
      t.addEventListener('click', function () {
        setTimeout(function () {
          var expanded = t.getAttribute('aria-expanded') === 'true';
          setBodyOpen(expanded);
        }, 60);
      });
    });
  }

  // Optional: close menu on ESC
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' || e.key === 'Esc') {
      // if menu overlay is open
      if (menu && menu.classList.contains('open')) {
        menu.classList.remove('open');
        if (backdrop) backdrop.classList.remove('show');
        setBodyOpen(false);
        if (toggleBtn) toggleBtn.setAttribute('aria-expanded', 'false');
        if (menu) menu.setAttribute('aria-hidden', 'true');
      }
      // if Bootstrap collapse is open, remove body class (Bootstrap will hide collapse itself)
      var anyExpanded = Array.from(document.querySelectorAll('.navbar-toggler')).some(function (t) {
        return t.getAttribute('aria-expanded') === 'true';
      });
      if (!anyExpanded) setBodyOpen(false);
    }
  });
});