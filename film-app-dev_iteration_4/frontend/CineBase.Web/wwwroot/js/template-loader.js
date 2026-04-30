const templateCache = {};

function executeInlineScripts(container) {
  const scripts = container.querySelectorAll('script');
  scripts.forEach(script => {
    const newScript = document.createElement('script');
    newScript.textContent = script.textContent;
    script.parentNode.replaceChild(newScript, script);
  });
}

async function loadComponent(elementId, componentPath) {
  const container = document.getElementById(elementId);
  if (!container) return;

  try {
    let html = templateCache[componentPath];

    if (!html) {
      const response = await fetch(componentPath);
      if (!response.ok) throw new Error(`Errore caricamento ${componentPath}`);
      html = await response.text();
      templateCache[componentPath] = html;
    }

    container.innerHTML = html;
    executeInlineScripts(container);
  } catch (error) {
    console.error('Errore caricamento componente:', error);
  }
}

async function loadLayoutComponents() {
  const navbarContainer = document.getElementById('navbar-container');
  const footerContainer = document.getElementById('footer-container');

  if (!navbarContainer && !footerContainer) return;

  const landingPaths = new Set(['/', '/index.html', '/programmazione.html', '/scheda-film.html', '/my-cinemas.html', '/login.html', '/registrazione.html', '/profilo.html', '/acquista.html', '/pagamento.html', '/esito-acquisto.html']);
  const adminShellPaths = new Set(['/films.html', '/registi.html', '/cinemas.html', '/proiezioni.html', '/categorie.html']);
  if (adminShellPaths.has(window.location.pathname)) {
    document.dispatchEvent(new Event('components:loaded'));
    return;
  }
  const isLandingPage = landingPaths.has(window.location.pathname);
  const navbarPath = isLandingPage ? '/components/navbar-landing.html' : '/components/navbar-admin.html';
  const footerPath = isLandingPage ? '/components/footer-landing.html' : '/components/footer-admin.html';

  await Promise.all([
    loadComponent('navbar-container', navbarPath),
    loadComponent('footer-container', footerPath)
  ]);

  document.dispatchEvent(new Event('components:loaded'));
}

document.addEventListener('DOMContentLoaded', loadLayoutComponents);
