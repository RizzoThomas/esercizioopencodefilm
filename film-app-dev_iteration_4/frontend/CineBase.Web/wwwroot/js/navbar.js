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
