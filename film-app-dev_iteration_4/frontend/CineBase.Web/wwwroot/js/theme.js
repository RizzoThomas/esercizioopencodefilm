/**
 * CineBase Theme Manager
 * 
 * Manages light/dark theme with:
 * - localStorage persistence
 * - System preference fallback (prefers-color-scheme)
 * - Exposes window.CineBaseTheme for external access
 */
(function () {
  const STORAGE_KEY = 'cinebase-theme';

  function getSystemTheme() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function getSavedTheme() {
    return localStorage.getItem(STORAGE_KEY);
  }

  function applyTheme(theme) {
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

  function updateHeroBackground(theme) {
    const heroBg = document.getElementById('hero-bg');
    if (!heroBg) return;
    if (theme === 'light') {
      heroBg.style.backgroundImage = "url('/assets/images/cinema.jpg')";
    } else {
      heroBg.style.backgroundImage = "url('https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=1920&q=80')";
    }
  }

  function updateToggleIcons(theme) {
    var iconClass = theme === 'dark' ? 'fa-solid fa-moon' : 'fa-solid fa-sun';
    document.querySelectorAll('#theme-toggle i, #admin-theme-toggle i').forEach(function(el) {
      el.className = iconClass;
    });
  }

  function getCurrentTheme() {
    const saved = getSavedTheme();
    if (saved) return saved;
    return getSystemTheme();
  }

  function toggleTheme() {
    const current = getCurrentTheme();
    const next = current === 'dark' ? 'light' : 'dark';
    localStorage.setItem(STORAGE_KEY, next);
    applyTheme(next);
  }

  function setTheme(theme) {
    localStorage.setItem(STORAGE_KEY, theme);
    applyTheme(theme);
  }

  // Apply immediately (before DOMContentLoaded to prevent flash)
  applyTheme(getCurrentTheme());

  // Re-apply icon states when navbar components load
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
