// ============================================================================
// route-guard.js — SISTEMA DI PROTEZIONE DELLE ROTTE FRONTEND
// ============================================================================
// Questo script è un IIFE (Immediately Invoked Function Expression) che
// viene eseguito nell'<head> di OGNI pagina HTML, PRIMA che il body venga
// parsato e renderizzato. In questo modo:
//   1. Non c'è flash di pagina non autorizzata
//   2. Il redirect avviene prima che l'utente veda qualsiasi contenuto
//   3. window.location.replace() evita di lasciare pagine nella history
//
// PER OGNI PAGINA definisce:
//   - roles[]: quali ruoli possono accedere
//   - authRequired: se serve autenticazione
//   - anonymousOnly: se è solo per utenti non loggati (login/registrazione)
//
// IMPORTANTE: è SELF-CONTAINED (non dipende da auth.js) perché viene
// eseguito prima che auth.js sia caricato. Parsa il JWT direttamente
// da localStorage.
// ============================================================================

var RouteGuard = (function () {
  // ─── MAPPA DI AUTORIZZAZIONE PER OGNI PAGINA ───────────────────────
  // Ogni voce definisce:
  //   roles: array di ruoli permessi ('anonimo', 'user', 'poweruser', 'admin')
  //   authRequired: se true, l'utente DEVE essere autenticato
  //   anonymousOnly: se true, SOLO utenti non autenticati possono accedere
  var PAGE_PERMISSIONS = {
    // PAGINE PUBBLICHE (tutti possono accedere)
    '/index.html':              { roles: ['anonimo', 'user', 'poweruser', 'admin'], authRequired: false },
    '/programmazione.html':     { roles: ['anonimo', 'user', 'poweruser', 'admin'], authRequired: false },
    '/scheda-film.html':        { roles: ['anonimo', 'user', 'poweruser', 'admin'], authRequired: false },
    '/my-cinemas.html':         { roles: ['anonimo', 'user', 'poweruser', 'admin'], authRequired: false },
    '/forgot-password.html':    { roles: ['anonimo', 'user', 'poweruser', 'admin'], authRequired: false },
    '/reset-password.html':     { roles: ['anonimo', 'user', 'poweruser', 'admin'], authRequired: false },
    '/social-login-complete.html': { roles: ['anonimo', 'user', 'poweruser', 'admin'], authRequired: false },

    // PAGINE SOLO ANONIMO (redirect a home se già loggati)
    '/login.html':              { roles: ['anonimo'], authRequired: false, anonymousOnly: true },
    '/registrazione.html':      { roles: ['anonimo'], authRequired: false, anonymousOnly: true },

    // PAGINE POWERUSER+ (gestione cinema)
    '/dashboard.html':          { roles: ['poweruser', 'admin'], authRequired: true },
    '/films.html':              { roles: ['poweruser', 'admin'], authRequired: true },
    '/registi.html':            { roles: ['poweruser', 'admin'], authRequired: true },
    '/cinemas.html':            { roles: ['poweruser', 'admin'], authRequired: true },
    '/proiezioni.html':         { roles: ['poweruser', 'admin'], authRequired: true },
    '/categorie.html':          { roles: ['poweruser', 'admin'], authRequired: true },

    // PAGINE USER+ (acquisti e profilo)
    '/profilo.html':            { roles: ['user', 'poweruser', 'admin'], authRequired: true },
    '/acquista.html':           { roles: ['user', 'poweruser', 'admin'], authRequired: true },
    '/pagamento.html':          { roles: ['user', 'poweruser', 'admin'], authRequired: true },
    '/esito-acquisto.html':     { roles: ['user', 'poweruser', 'admin'], authRequired: true },
    '/tmdb-search.html':        { roles: ['user', 'poweruser', 'admin'], authRequired: true },
    '/enable-2fa.html':         { roles: ['user', 'poweruser', 'admin'], authRequired: true },

    // PAGINE SOLO ADMIN
    '/utenti.html':             { roles: ['admin'], authRequired: true },
    '/utenti-detail.html':      { roles: ['admin'], authRequired: true }
  };

  var ACCESS_TOKEN_KEY = 'cb_access_token';
  var REFRESH_TOKEN_KEY = 'cb_refresh_token';

  // Normalizza il ruolo da vari formati (numero, stringa) a stringa
  function normalizeRole(role) {
    if (role == null) return 'anonimo';
    var value = String(role).trim().toLowerCase();
    if (value === '2' || value === 'admin') return 'admin';
    if (value === '1' || value === 'poweruser') return 'poweruser';
    if (value === '0' || value === 'user') return 'user';
    return 'anonimo';
  }

  // Previene redirect a URL esterni (sicurezza)
  function sanitizeRedirectPath(path) {
    if (!path || typeof path !== 'string') return '/index.html';
    if (path.indexOf('://') !== -1 || path.indexOf('//') === 0) return '/index.html';
    if (path.indexOf('..') !== -1) return '/index.html';
    if (path.charAt(0) !== '/') return '/index.html';
    return path;
  }

  // Decodifica JWT senza verificare la firma (solo per leggere ruolo e scadenza)
  function parseJwt(token) {
    try {
      var parts = token.split('.');
      if (parts.length < 2) return null;
      var base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      var jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(function (c) { return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2); })
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (e) { return null; }
  }

  function getAccessToken() {
    try { return localStorage.getItem(ACCESS_TOKEN_KEY); } catch (e) { return null; }
  }

  function getRefreshToken() {
    try { return localStorage.getItem(REFRESH_TOKEN_KEY); } catch (e) { return null; }
  }

  function getAuthSafe() {
    if (typeof window === 'undefined') return null;
    return window.Auth || null;
  }

  async function tryProactiveRefresh() {
    var auth = getAuthSafe();
    if (!auth || typeof auth.refreshAccessToken !== 'function') return false;
    if (!getRefreshToken()) return false;

    try {
      await auth.refreshAccessToken();
      return true;
    } catch (e) {
      return false;
    }
  }

  function isTokenValid() {
    var token = getAccessToken();
    if (!token) return false;
    var payload = parseJwt(token);
    if (!payload) return false;
    return payload.exp > Math.ceil(Date.now() / 1000);
  }

  function getRoleFromToken() {
    var token = getAccessToken();
    if (!token) return null;
    var payload = parseJwt(token);
    if (!payload) return null;
    return payload.role || null;
  }

  async function check() {
    var pathname = window.location.pathname;
    var pageKey = pathname.toLowerCase();
    var permission = PAGE_PERMISSIONS[pageKey];
    if (!permission) return true;

    // On anonymousOnly pages (login, register), NEVER refresh — let user log in fresh
    var isLoggedIn = isTokenValid();
    if (!isLoggedIn && !permission.anonymousOnly) {
      var refreshed = await tryProactiveRefresh();
      if (refreshed) {
        isLoggedIn = isTokenValid();
      }
    }

    var role = normalizeRole(isLoggedIn ? getRoleFromToken() : null);

    if (permission.anonymousOnly && isLoggedIn) {
      var params = new URLSearchParams(window.location.search);
      var redirect = sanitizeRedirectPath(params.get('redirect'));
      window.location.replace(redirect || '/index.html');
      return false;
    }

    if (permission.authRequired && !isLoggedIn) {
      var redirectUrl = pathname + window.location.search;
      window.location.replace('/login.html?redirect=' + encodeURIComponent(redirectUrl));
      return false;
    }

    if (!permission.roles.includes(role)) {
      window.location.replace('/index.html?forbidden=true');
      return false;
    }

    return true;
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      check();
    });
  } else {
    check();
  }

  return { check: check, normalizeRole: normalizeRole, sanitizeRedirectPath: sanitizeRedirectPath, PAGE_PERMISSIONS: PAGE_PERMISSIONS };
})();

if (typeof window !== 'undefined') {
  window.RouteGuard = RouteGuard;
}
