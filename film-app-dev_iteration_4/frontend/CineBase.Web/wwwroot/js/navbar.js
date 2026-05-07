function setActiveNavLink() {
  const currentPath = window.location.pathname;

  const desktopLinks = document.querySelectorAll('nav .nav-link');
  desktopLinks.forEach(link => {
    const href = link.getAttribute('href');
    const isActive = href === currentPath || (currentPath === '/' && href === '/index.html');

    if (isActive) {
      link.classList.add('text-ferrari-primary', 'border-b-2', 'border-ferrari-primary');
      link.classList.remove('text-body', 'hover:text-ferrari-primary');
    } else {
      link.classList.remove('text-ferrari-primary', 'border-b-2', 'border-ferrari-primary');
      link.classList.add('text-body', 'hover:text-ferrari-primary');
    }
  });

  const mobileLinks = document.querySelectorAll('#mobile-menu a');
  mobileLinks.forEach(link => {
    const href = link.getAttribute('href');
    const isActive = href === currentPath || (currentPath === '/' && href === '/index.html');

    if (isActive) {
      link.classList.add('text-ferrari-primary', 'bg-canvas-elevated');
      link.classList.remove('text-ink', 'hover:bg-canvas-elevated');
    } else {
      link.classList.remove('text-ferrari-primary', 'bg-canvas-elevated');
      link.classList.add('text-ink', 'hover:bg-canvas-elevated');
    }
  });
}

function setupMobileMenu() {
  const menuToggle = document.getElementById('mobile-menu-toggle');
  const mobileMenu = document.getElementById('mobile-menu');
  const backdrop = document.getElementById('mobile-menu-backdrop');

  if (!menuToggle || !mobileMenu) return;
  if (menuToggle.dataset.menuBound === 'true') return;

  function openMenu() {
    mobileMenu.classList.add('open');
    menuToggle.classList.add('hamburger-open');
    if (backdrop) backdrop.classList.add('open');
  }
  function closeMenu() {
    mobileMenu.classList.remove('open');
    menuToggle.classList.remove('hamburger-open');
    if (backdrop) backdrop.classList.remove('open');
  }

  menuToggle.addEventListener('click', () => {
    if (mobileMenu.classList.contains('open')) { closeMenu(); } else { openMenu(); }
  });

  if (backdrop) {
    backdrop.addEventListener('click', closeMenu);
  }

  const mobileLinks = mobileMenu.querySelectorAll('a');
  mobileLinks.forEach(link => {
    if (link.dataset.menuCloseBound === 'true') return;
    link.addEventListener('click', closeMenu);
    link.dataset.menuCloseBound = 'true';
  });

  menuToggle.dataset.menuBound = 'true';
}

function initializeNavbar() {
  setActiveNavLink();
  setupMobileMenu();

  if (typeof window.updateAuthUI === 'function') {
    window.updateAuthUI();
  }
}

window.initializeNavbar = initializeNavbar;

document.addEventListener('components:loaded', initializeNavbar);

document.addEventListener('DOMContentLoaded', () => {
  if (document.querySelector('nav')) {
    initializeNavbar();
  }
});
