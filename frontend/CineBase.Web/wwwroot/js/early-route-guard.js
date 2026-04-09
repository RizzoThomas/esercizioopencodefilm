// early-route-guard.js - early client-side RBAC check to avoid page flicker
(function () {
  function normalizePath(path) {
    if (!path || path === '/') return '/index.html';

    var normalized = String(path).toLowerCase();
    if (normalized.endsWith('/')) {
      normalized = normalized.slice(0, -1);
    }

    if (!/\.[a-z0-9]+$/i.test(normalized)) {
      normalized += '.html';
    }

    return normalized;
  }

  function normalizeRole(rawRole) {
    if (rawRole === null || rawRole === undefined) return null;

    if (typeof rawRole === 'number') {
      if (rawRole === 0) return 'Admin';
      if (rawRole === 1) return 'PowerUser';
      if (rawRole === 2) return 'User';
      return null;
    }

    var role = String(rawRole).trim().toLowerCase();
    if (role === '0' || role === 'admin') return 'Admin';
    if (role === '1' || role === 'poweruser' || role === 'power_user' || role === 'power user') return 'PowerUser';
    if (role === '2' || role === 'user' || role === 'utente' || role === 'basicuser' || role === 'basic_user' || role === 'basic user') return 'User';

    return null;
  }

  function setFlashToast(message, type) {
    try {
      sessionStorage.setItem('cinebase_flash_toast', JSON.stringify({
        message: message,
        type: type || 'danger',
        ts: Date.now()
      }));
    } catch (_) {
      // ignore storage errors
    }
  }

  var adminPages = {
    '/dashboard.html': true,
    '/films.html': true,
    '/registi.html': true,
    '/proiezioni.html': true,
    '/cinemas.html': true
  };
  var userPages = {
    '/area-personale.html': true,
    '/prenotazione.html': true
  };

  var currentPage = normalizePath(window.location.pathname);
  var isAdminPage = !!adminPages[currentPage];
  var isUserPage = !!userPages[currentPage];

  if (!isAdminPage && !isUserPage) {
    return;
  }

  var token = localStorage.getItem('cinebase_access_token');
  if (!token) {
    window.location.replace('/login.html?redirect=' + encodeURIComponent(currentPage));
    return;
  }

  var storedUser = null;
  try {
    storedUser = JSON.parse(localStorage.getItem('cinebase_user') || 'null');
  } catch (_) {
    storedUser = null;
  }

  var rawRole = storedUser ? (storedUser.ruolo ?? storedUser.role ?? storedUser.Role ?? null) : null;
  var role = normalizeRole(rawRole);

  if (isAdminPage) {
    var canAccessAdmin = role === 'Admin' || role === 'PowerUser';
    if (!canAccessAdmin) {
      setFlashToast('Accesso non autorizzato', 'danger');
      window.location.replace('/index.html');
      return;
    }

    if (currentPage === '/cinemas.html' && role !== 'Admin') {
      setFlashToast('Accesso non autorizzato', 'danger');
      window.location.replace('/dashboard.html');
      return;
    }
  }
})();
