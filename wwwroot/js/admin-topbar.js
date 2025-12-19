document.addEventListener('DOMContentLoaded', function () {
  const toggle = document.getElementById('adminTopbarToggle');
  const mobileMenu = document.getElementById('adminTopbarMobile');

  if (!toggle || !mobileMenu) return;

  function closeMenu() {
    mobileMenu.classList.remove('open');
    mobileMenu.hidden = true;
    toggle.setAttribute('aria-expanded', 'false');
  }

  function openMenu() {
    mobileMenu.classList.add('open');
    mobileMenu.hidden = false;
    toggle.setAttribute('aria-expanded', 'true');
  }

  toggle.addEventListener('click', function (e) {
    const expanded = toggle.getAttribute('aria-expanded') === 'true';
    if (expanded) closeMenu(); else openMenu();
  });

  // close on outside click
  document.addEventListener('click', function (e) {
    if (!mobileMenu.contains(e.target) && !toggle.contains(e.target)) {
      closeMenu();
    }
  });

  // close on Escape
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeMenu();
  });
});