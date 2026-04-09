// Cache per i template caricati
const templateCache = {};

async function loadComponent(elementId, componentPath) {
  const el = document.getElementById(elementId);
  if (!el) return;

  // Evita di ricaricare se il container ha già contenuto (pagine che caricano manualmente)
  if (el.innerHTML && el.innerHTML.trim().length > 0) return;

  if (templateCache[componentPath]) {
    el.innerHTML = templateCache[componentPath];
    executeInlineScripts(el);
    return;
  }

  try {
    const response = await fetch(componentPath);
    if (!response.ok) throw new Error(`Errore caricamento ${componentPath}`);

    const html = await response.text();
    templateCache[componentPath] = html;
    el.innerHTML = html;

    // Esegui script inline se presenti
    executeInlineScripts(el);
  } catch (error) {
    console.error('Errore caricamento componente:', error);
  }
}

function executeInlineScripts(containerEl) {
  const scripts = containerEl.querySelectorAll('script');
  scripts.forEach(script => {
    const newScript = document.createElement('script');
    if (script.src) {
      newScript.src = script.src;
    } else {
      newScript.textContent = script.textContent;
    }
    script.parentNode.replaceChild(newScript, script);
  });
}

// Auto-load navbar/footer solo per index.html (landing), le altre pagine gestiscono il caricamento manualmente
document.addEventListener('DOMContentLoaded', async () => {
  const path = window.location.pathname || '/';
  const isLanding = path === '/' || path === '/index.html' || path.endsWith('index.html');
  
  if (!isLanding) return;

  const navbarEl = document.getElementById('navbar-container');
  const footerEl = document.getElementById('footer-container');

  if (navbarEl) await loadComponent('navbar-container', '/components/navbar-landing.html');
  if (footerEl) await loadComponent('footer-container', '/components/footer-landing.html');

  document.dispatchEvent(new Event('components:loaded'));
});
