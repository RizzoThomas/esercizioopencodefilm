/**
 * CineBase Theme Manager
 * 
 * Manages light/dark theme with:
 * - localStorage persistence
 * - System preference fallback (prefers-color-scheme)
 * - Exposes window.CineBaseTheme for external access
 */
(function () {
  // Variabile STORAGE_KEY: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const STORAGE_KEY = 'cinebase-theme';

  // Funzione getSystemTheme: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function getSystemTheme() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  // Funzione getSavedTheme: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function getSavedTheme() {
    return localStorage.getItem(STORAGE_KEY);
  }

  // Funzione applyTheme: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function applyTheme(theme) {
    // Variabile root: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const root = document.documentElement;
    if (theme === 'light') {
      root.classList.add('light');
    } else {
      root.classList.remove('light');
    }
    if (theme === 'dark') {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
    updateToggleIcons(theme);
    updateHeroBackground(theme);
  }

  // Funzione updateHeroBackground: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function updateHeroBackground(theme) {
    // Variabile heroBg: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const heroBg = document.getElementById('hero-bg');
    if (!heroBg) return;
    if (theme === 'light') {
      heroBg.style.backgroundImage = "url('/assets/images/cinema.jpg')";
    } else {
      heroBg.style.backgroundImage = "url('https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=1920&q=80')";
    }
  }

  // Funzione updateToggleIcons: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function updateToggleIcons(theme) {
    // Variabile iconClass: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var iconClass = theme === 'dark' ? 'fa-solid fa-moon' : 'fa-solid fa-sun';
    document.querySelectorAll('#theme-toggle i, #admin-theme-toggle i').forEach(function(el) {
      el.className = iconClass;
    });
  }

  // Funzione getCurrentTheme: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function getCurrentTheme() {
    // Variabile saved: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const saved = getSavedTheme();
    if (saved) return saved;
    return getSystemTheme();
  }

  // Funzione toggleTheme: commuta uno stato visivo o funzionale tra due modalità. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function toggleTheme() {
    // Variabile current: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const current = getCurrentTheme();
    // Variabile next: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const next = current === 'dark' ? 'light' : 'dark';
    localStorage.setItem(STORAGE_KEY, next);
    applyTheme(next);
  }

  // Funzione setTheme: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function setTheme(theme) {
    localStorage.setItem(STORAGE_KEY, theme);
    applyTheme(theme);
  }

  // Apply immediately (before DOMContentLoaded to prevent flash)
  applyTheme(getCurrentTheme());

  // Re-apply icon states when navbar components load
// Listener evento: si attiva quando scatta l'evento e aggiorna UI o stato.
  document.addEventListener('components:loaded', function() {
    applyTheme(getCurrentTheme());
  });

  // Listen for system preference changes
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    // Only react to system changes if user hasn't set a manual preference
    if (!getSavedTheme()) {
      applyTheme(e.matches ? 'dark' : 'light');
    }
  });

  // Export API
  window.CineBaseTheme = {
    toggle: toggleTheme,
    set: setTheme,
    get: getCurrentTheme,
    getSystem: getSystemTheme
  };
})();
