// Variabile templateCache: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const templateCache = {};

// Funzione executeInlineScripts: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function executeInlineScripts(container) {
  // Variabile scripts: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const scripts = container.querySelectorAll('script');
  scripts.forEach(script => {
    // Variabile newScript: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const newScript = document.createElement('script');
    newScript.textContent = script.textContent;
    script.parentNode.replaceChild(newScript, script);
  });
}

// Funzione loadComponent: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadComponent(elementId, componentPath) {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById(elementId);
  if (!container) return;

  try {
    // Always fetch fresh — no caching during development
    const cbPath = componentPath + '?v=' + Date.now();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    const response = await fetch(cbPath, { cache: 'no-store' });
    if (!response.ok) throw new Error(`Errore caricamento ${componentPath}`);
    // Variabile html: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const html = await response.text();

    container.innerHTML = html;
    executeInlineScripts(container);
  } catch (error) {
    console.error('Errore caricamento componente:', error);
  }
}

// Funzione loadLayoutComponents: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadLayoutComponents() {
  // Variabile navbarContainer: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const navbarContainer = document.getElementById('navbar-container');
  // Variabile footerContainer: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const footerContainer = document.getElementById('footer-container');

  if (!navbarContainer && !footerContainer) return;

  // Variabile landingPaths: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const landingPaths = new Set(['/', '/index.html', '/programmazione.html', '/scheda-film.html', '/my-cinemas.html', '/my-watchlist.html', '/login.html', '/registrazione.html', '/profilo.html', '/acquista.html', '/pagamento.html', '/esito-acquisto.html', '/tmdb-search.html', '/forgot-password.html', '/reset-password.html', '/enable-2fa.html', '/offerte.html', '/validazione.html', '/social-login-complete.html']);
  // Variabile adminShellPaths: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const adminShellPaths = new Set(['/films.html', '/registi.html', '/cinemas.html', '/proiezioni.html', '/categorie.html', '/dashboard.html', '/utenti.html', '/utenti-detail.html', '/validazione.html']);
  if (adminShellPaths.has(window.location.pathname)) {
    document.dispatchEvent(new Event('components:loaded'));
    return;
  }
  // Variabile isLandingPage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const isLandingPage = landingPaths.has(window.location.pathname);
  // Variabile navbarPath: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const navbarPath = isLandingPage ? '/components/navbar-landing.html' : '/components/navbar-admin.html';
  // Variabile footerPath: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const footerPath = isLandingPage ? '/components/footer-landing.html' : '/components/footer-admin.html';

  await Promise.all([
    loadComponent('navbar-container', navbarPath),
    loadComponent('footer-container', footerPath)
  ]);

  document.dispatchEvent(new Event('components:loaded'));
}

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', loadLayoutComponents);
