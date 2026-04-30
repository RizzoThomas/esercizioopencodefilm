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
    if (theme === 'dark') {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
    updateToggleIcons(theme);
  }

  function updateToggleIcons(theme) {
    document.querySelectorAll('.theme-toggle-icon-sun').forEach(el => {
      el.style.display = theme === 'dark' ? 'inline' : 'none';
    });
    document.querySelectorAll('.theme-toggle-icon-moon').forEach(el => {
      el.style.display = theme === 'dark' ? 'none' : 'inline';
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
