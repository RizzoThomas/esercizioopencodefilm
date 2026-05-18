// Funzione setActiveNavLink: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setActiveNavLink() {
  // Variabile currentPath: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const currentPath = window.location.pathname;

  // Variabile desktopLinks: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const desktopLinks = document.querySelectorAll('nav .nav-link');
  desktopLinks.forEach(link => {
    // Variabile href: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const href = link.getAttribute('href');
    // Variabile isActive: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const isActive = href === currentPath || (currentPath === '/' && href === '/index.html');

    if (isActive) {
      link.classList.add('text-ferrari-primary', 'border-b-2', 'border-ferrari-primary');
      link.classList.remove('text-body', 'hover:text-ferrari-primary');
    } else {
      link.classList.remove('text-ferrari-primary', 'border-b-2', 'border-ferrari-primary');
      link.classList.add('text-body', 'hover:text-ferrari-primary');
    }
  });

  // Variabile mobileLinks: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const mobileLinks = document.querySelectorAll('#mobile-menu a');
  mobileLinks.forEach(link => {
    // Variabile href: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const href = link.getAttribute('href');
    // Variabile isActive: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione setupMobileMenu: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupMobileMenu() {
  // Variabile menuToggle: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const menuToggle = document.getElementById('mobile-menu-toggle');
  // Variabile mobileMenu: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const mobileMenu = document.getElementById('mobile-menu');
  // Variabile backdrop: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const backdrop = document.getElementById('mobile-menu-backdrop');

  if (!menuToggle || !mobileMenu) return;
  if (menuToggle.dataset.menuBound === 'true') return;

  // Funzione openMenu: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function openMenu() {
    mobileMenu.classList.add('open');
    menuToggle.classList.add('hamburger-open');
    if (backdrop) backdrop.classList.add('open');
  }
  // Funzione closeMenu: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function closeMenu() {
    mobileMenu.classList.remove('open');
    menuToggle.classList.remove('hamburger-open');
    if (backdrop) backdrop.classList.remove('open');
  }

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  menuToggle.addEventListener('click', () => {
    if (mobileMenu.classList.contains('open')) { closeMenu(); } else { openMenu(); }
  });

  if (backdrop) {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    backdrop.addEventListener('click', closeMenu);
  }

  // Variabile mobileLinks: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const mobileLinks = mobileMenu.querySelectorAll('a');
  mobileLinks.forEach(link => {
    if (link.dataset.menuCloseBound === 'true') return;
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    link.addEventListener('click', closeMenu);
    link.dataset.menuCloseBound = 'true';
  });

  menuToggle.dataset.menuBound = 'true';
}

// Funzione initializeNavbar: inizializza stato, timer o interfaccia della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function initializeNavbar() {
  setActiveNavLink();
  setupMobileMenu();

  if (typeof window.updateAuthUI === 'function') {
    window.updateAuthUI();
  }
}

window.initializeNavbar = initializeNavbar;

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('components:loaded', initializeNavbar);

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', () => {
  if (document.querySelector('nav')) {
    initializeNavbar();
  }
});
