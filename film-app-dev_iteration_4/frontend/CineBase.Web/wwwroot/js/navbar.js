function setActiveNavLink() {
  const currentPath = window.location.pathname;

  const desktopLinks = document.querySelectorAll('nav .nav-link');
  desktopLinks.forEach(link => {
    const href = link.getAttribute('href');
    const isActive = href === currentPath || (currentPath === '/' && href === '/index.html');

    if (isActive) {
      link.classList.add('text-brand-gold', 'border-b-2', 'border-brand-gold');
      link.classList.remove('text-brand-on-surface-variant', 'hover:text-brand-gold');
    } else {
      link.classList.remove('text-brand-gold', 'border-b-2', 'border-brand-gold');
      link.classList.add('text-brand-on-surface-variant', 'hover:text-brand-gold');
    }
  });

  const mobileLinks = document.querySelectorAll('#mobile-menu a');
  mobileLinks.forEach(link => {
    const href = link.getAttribute('href');
    const isActive = href === currentPath || (currentPath === '/' && href === '/index.html');

    if (isActive) {
      link.classList.add('text-brand-gold', 'bg-brand-surface-container');
      link.classList.remove('text-brand-on-surface', 'hover:bg-brand-surface-container');
    } else {
      link.classList.remove('text-brand-gold', 'bg-brand-surface-container');
      link.classList.add('text-brand-on-surface', 'hover:bg-brand-surface-container');
    }
  });
}

function setupMobileMenu() {
  const menuToggle = document.getElementById('mobile-menu-toggle');
  const mobileMenu = document.getElementById('mobile-menu');

  if (!menuToggle || !mobileMenu) return;
  if (menuToggle.dataset.menuBound === 'true') return;

  menuToggle.addEventListener('click', () => {
    mobileMenu.classList.toggle('hidden');
  });

  const mobileLinks = mobileMenu.querySelectorAll('a');
  mobileLinks.forEach(link => {
    if (link.dataset.menuCloseBound === 'true') return;

    link.addEventListener('click', () => {
      mobileMenu.classList.add('hidden');
    });

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
