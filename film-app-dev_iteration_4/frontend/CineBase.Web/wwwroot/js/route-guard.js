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

  // Variabile ACCESS_TOKEN_KEY: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var ACCESS_TOKEN_KEY = 'cb_access_token';
  // Variabile REFRESH_TOKEN_KEY: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var REFRESH_TOKEN_KEY = 'cb_refresh_token';

  // Normalizza il ruolo da vari formati (numero, stringa) a stringa
  function normalizeRole(role) {
    if (role == null) return 'anonimo';
    // Variabile value: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
      // Variabile parts: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var parts = token.split('.');
      if (parts.length < 2) return null;
      // Variabile base64: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      // Variabile jsonPayload: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(function (c) { return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2); })
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (e) { return null; }
  }

  // Funzione getAccessToken: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function getAccessToken() {
    try { return localStorage.getItem(ACCESS_TOKEN_KEY); } catch (e) { return null; }
  }

  // Funzione getRefreshToken: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function getRefreshToken() {
    try { return localStorage.getItem(REFRESH_TOKEN_KEY); } catch (e) { return null; }
  }

  // Funzione getAuthSafe: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function getAuthSafe() {
    if (typeof window === 'undefined') return null;
    return window.Auth || null;
  }

  // Funzione tryProactiveRefresh: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  async function tryProactiveRefresh() {
    // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

  // Funzione isTokenValid: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function isTokenValid() {
    // Variabile token: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var token = getAccessToken();
    if (!token) return false;
    // Variabile payload: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var payload = parseJwt(token);
    if (!payload) return false;
    return payload.exp > Math.ceil(Date.now() / 1000);
  }

  // Funzione getRoleFromToken: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function getRoleFromToken() {
    // Variabile token: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var token = getAccessToken();
    if (!token) return null;
    // Variabile payload: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var payload = parseJwt(token);
    if (!payload) return null;
    return payload.role || null;
  }

  // Funzione check: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  async function check() {
    // Variabile pathname: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var pathname = window.location.pathname;
    // Variabile pageKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var pageKey = pathname.toLowerCase();
    // Variabile permission: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var permission = PAGE_PERMISSIONS[pageKey];
    if (!permission) return true;

    // On anonymousOnly pages (login, register), NEVER refresh — let user log in fresh
    var isLoggedIn = isTokenValid();
    if (!isLoggedIn && !permission.anonymousOnly) {
      // Variabile refreshed: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var refreshed = await tryProactiveRefresh();
      if (refreshed) {
        isLoggedIn = isTokenValid();
      }
    }

    // Variabile role: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var role = normalizeRole(isLoggedIn ? getRoleFromToken() : null);

    if (permission.anonymousOnly && isLoggedIn) {
      // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var params = new URLSearchParams(window.location.search);
      // Variabile redirect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var redirect = sanitizeRedirectPath(params.get('redirect'));
      window.location.replace(redirect || '/index.html');
      return false;
    }

    if (permission.authRequired && !isLoggedIn) {
      // Variabile redirectUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
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
