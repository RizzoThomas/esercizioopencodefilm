// ============================================================================
// api.js — CLIENT HTTP PER COMUNICAZIONE CON IL BACKEND
// ============================================================================
// Questo file gestisce TUTTE le chiamate API al backend.
// Caratteristiche principali:
//   - Aggiunge automaticamente il Bearer token JWT alle richieste
//   - Gestisce il refresh automatico del token su 401
//   - Fornisce metodi tipizzati per ogni endpoint del backend
//   - Normalizza le risposte (gestisce $values per serializzazione EF)
//   - Coda di richieste in attesa durante il refresh (evita race condition)
// ============================================================================

// URL base del backend (configurabile, default localhost:5000)
var API_BASE_URL = window.API_BASE_URL || 'http://localhost:5000';

// ─── GESTIONE REFRESH TOKEN ──────────────────────────────────────────────
// Quando una richiesta riceve 401, apiFetch tenta automaticamente il refresh.
// isRefreshing impedisce refresh concorrenti.
// refreshSubscribers gestisce la coda di richieste in attesa.
let isRefreshing = false;
// Variabile refreshSubscribers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let refreshSubscribers = [];

// Funzione subscribeTokenRefresh: registra un callback da richiamare quando l'operazione termina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function subscribeTokenRefresh(callback) {
  refreshSubscribers.push(callback);
}

// Funzione onTokenRefreshed: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function onTokenRefreshed(token) {
  refreshSubscribers.forEach(callback => callback(token));
  refreshSubscribers = [];
}

// Funzione getAuthSafe: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getAuthSafe() {
  return typeof window !== 'undefined' && window.Auth ? window.Auth : null;
}

// Normalizza il ruolo (accetta sia stringhe che numeri dal backend)
// Funzione normalizeRole: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
function normalizeRole(role) {
  if (role == null) return '';
  // Variabile value: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const value = String(role).trim().toLowerCase();
  if (value === '2' || value === 'admin') return 'admin';
  if (value === '1' || value === 'poweruser') return 'poweruser';
  if (value === '0' || value === 'user') return 'user';
  return value;
}

// Controlla se il path corrente è un'area admin
// Funzione isAdminAreaPath: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
function isAdminAreaPath(pathname) {
  // Variabile adminPaths: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const adminPaths = new Set([
    '/dashboard.html', '/films.html', '/registi.html',
    '/cinemas.html', '/proiezioni.html', '/categorie.html'
  ]);
  return adminPaths.has((pathname || '').toLowerCase());
}

// Blocca l'accesso alle pagine admin per utenti non autorizzati
// Funzione enforceAdminAreaAccess: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
function enforceAdminAreaAccess() {
  // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const auth = getAuthSafe();
  if (!auth) return true;
  if (!isAdminAreaPath(window.location.pathname)) return true;

  if (!auth.isLoggedIn()) {
    auth.redirectToLogin(window.location.pathname + window.location.search);
    return false;
  }

  // Variabile role: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const role = normalizeRole(auth.getUserRole?.());
  if (role === 'admin' || role === 'poweruser') return true;

  window.location.href = '/index.html?forbidden=true';
  return false;
}

// Parsing della risposta HTTP (JSON o testo)
// Funzione parseSuccessfulResponse: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
async function parseSuccessfulResponse(response) {
  if (response.status === 204) return null;
  // Variabile contentType: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) return response.json();
  return response.text();
}

// ====================================================================
// apiFetch — FUNZIONE PRINCIPALE PER CHIAMATE HTTP
// ====================================================================
// Pattern:
//   1. Aggiunge header Authorization: Bearer <token>
//   2. Invia richiesta al backend
//   3. Se 401 → tenta refresh token → riprova richiesta
//   4. Se errore → throw con status code
// ====================================================================
// Funzione apiFetch: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
async function apiFetch(endpoint, options = {}) {
  if (!enforceAdminAreaAccess()) {
    throw { status: 403, message: 'Non autorizzato ad accedere a questa pagina' };
  }

  // Variabile defaultOptions: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const defaultOptions = {
    headers: {
      'Content-Type': 'application/json'
    }
  };

  // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const auth = getAuthSafe();
  // Variabile accessToken: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const accessToken = auth?.getAccessToken?.();
  if (accessToken) {
    defaultOptions.headers['Authorization'] = `Bearer ${accessToken}`;
  }

  // Variabile requestOptions: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const requestOptions = {
    ...defaultOptions,
    ...options,
    headers: {
      ...defaultOptions.headers,
      ...(options.headers || {})
    }
  };

  // Variabile response: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let response;

  try {
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    response = await fetch(`${API_BASE_URL}${endpoint}`, requestOptions);
  } catch {
    throw {
      status: 0,
      message: 'Impossibile connettersi al server. Verifica che il servizio sia attivo.'
    };
  }

  if (response.status === 401 && !options._noRetry && auth) {

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        subscribeTokenRefresh(async (newToken) => {
          try {
            // Variabile retryOptions: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const retryOptions = {
              ...requestOptions,
              _noRetry: true,
              headers: {
                ...requestOptions.headers,
                Authorization: `Bearer ${newToken}`
              }
            };
            // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
            const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, retryOptions);
            if (retryResponse.ok) {
              resolve(parseSuccessfulResponse(retryResponse));
            } else {
              throw { status: retryResponse.status, message: 'Richiesta fallita dopo refresh' };
            }
          } catch (err) {
            reject(err);
          }
        });
      });
    }

    isRefreshing = true;

    try {
      await auth.refreshAccessToken();
      // Variabile newToken: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const newToken = auth.getAccessToken();
      onTokenRefreshed(newToken);
      isRefreshing = false;

      // Variabile retryOptions: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const retryOptions = {
        ...requestOptions,
        _noRetry: true,
        headers: {
          ...requestOptions.headers,
          Authorization: `Bearer ${newToken}`
        }
      };
      // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
      const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, retryOptions);

      if (retryResponse.ok) {
        return parseSuccessfulResponse(retryResponse);
      }

      throw { status: retryResponse.status, message: 'Richiesta fallita dopo refresh' };
    } catch (refreshError) {
      isRefreshing = false;
      auth.clearAuth();
      // Variabile redirectUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const redirectUrl = window.location.pathname + window.location.search;
      auth.redirectToLogin(redirectUrl);
      throw { status: 401, message: 'Sessione scaduta' };
    }
  }

  if (!response.ok) {
    // Variabile contentType: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const contentType = response.headers.get('content-type') || '';
    // Variabile message: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let message = 'Errore di rete';
    // Variabile errors: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let errors;

    if (contentType.includes('application/json')) {
      // Variabile errorJson: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const errorJson = await response.json().catch(() => null);
      if (errorJson) {
        message = errorJson.detail || errorJson.message || errorJson.title || message;
        errors = errorJson.errors;
      }
    } else {
      // Variabile errorText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const errorText = await response.text().catch(() => '');
      if (errorText) {
        message = errorText;
      }
    }

    throw { status: response.status, message, errors };
  }

  if (response.status === 204) return null;

  // Variabile contentType: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const contentType = response.headers.get('content-type') || '';
  if (!contentType.includes('application/json')) {
    // Variabile bodyText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const bodyText = await response.text().catch(() => '');
    throw {
      status: 502,
      message: `Risposta non valida dal backend (${contentType || 'content-type assente'})`,
      details: bodyText.slice(0, 200)
    };
  }

  return response.json();
}

// API Object
const API = {
  // Base URL per chiamate dirette
  baseUrl: API_BASE_URL,

  // Utility: ottieni headers autenticati per chiamate fetch dirette
  getAuthHeaders: () => {
    // Variabile headers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const headers = { 'Content-Type': 'application/json' };
    // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const auth = getAuthSafe();
    // Variabile accessToken: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const accessToken = auth?.getAccessToken?.();
    if (accessToken) {
      headers['Authorization'] = `Bearer ${accessToken}`;
    }
    return headers;
  },

  // Registi
  getRegisti: (params = {}) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = new URLSearchParams();

    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));

    // Variabile queryString: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const queryString = query.toString();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/registi${queryString ? `?${queryString}` : ''}`);
  },
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getRegista: (id) => apiFetch(`/registi/${id}`),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  createRegista: (data) => apiFetch('/registi', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateRegista: (id, data) => apiFetch(`/registi/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  deleteRegista: (id) => apiFetch(`/registi/${id}`, { method: 'DELETE' }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getFilmsByRegista: (id) => apiFetch(`/registi/${id}/films`),
  
  // Film
  getFilms: (params = {}) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = new URLSearchParams();

    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));
    if (params.filter) query.set('filter', String(params.filter));

    // Variabile queryString: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const queryString = query.toString();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/films${queryString ? `?${queryString}` : ''}`);
  },
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getFilm: (id) => apiFetch(`/films/${id}`),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  createFilm: (data) => apiFetch('/films', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateFilm: (id, data) => apiFetch(`/films/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
// Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
deleteFilm: (id) => apiFetch(`/films/${id}`, { method: 'DELETE' }),

    uploadCover: async (file) => {
        // Variabile formData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const formData = new FormData();
        formData.append('file', file);
        
        // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
        const response = await fetch(`${API_BASE_URL}/media/covers`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            // Variabile errorText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const errorText = await response.text().catch(() => '');
            throw {
                status: response.status,
                message: errorText || 'Errore durante upload copertina'
            };
        }

        return response.json();
    },

    // Cinema
  getCinemas: (params = {}) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = new URLSearchParams();

    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));

    // Variabile queryString: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const queryString = query.toString();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/cinemas${queryString ? `?${queryString}` : ''}`);
  },
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getCinema: (id) => apiFetch(`/cinemas/${id}`),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  createCinema: (data) => apiFetch('/cinemas', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateCinema: (id, data) => apiFetch(`/cinemas/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  deleteCinema: (id) => apiFetch(`/cinemas/${id}`, { method: 'DELETE' }),
  
  // Proiezioni
  getProiezioni: (params = {}) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = new URLSearchParams();

    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));

    // Variabile queryString: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const queryString = query.toString();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/proiezioni${queryString ? `?${queryString}` : ''}`);
  },
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getProiezione: (id) => apiFetch(`/proiezioni/${id}`),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  createProiezione: (data) => apiFetch('/proiezioni', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateProiezione: (id, data) => apiFetch(`/proiezioni/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  deleteProiezione: (id) => apiFetch(`/proiezioni/${id}`, { method: 'DELETE' }),

  // Profilo
  getProfilo: () => apiFetch('/profilo'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateProfilo: (data) => apiFetch('/profilo', {
    method: 'PUT',
    body: JSON.stringify(data)
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getUserVouchers: () => apiFetch('/profilo/vouchers'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getUserSubscription: () => apiFetch('/profilo/subscription'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  cancelSubscription: () => apiFetch('/profilo/subscription/cancel', { method: 'POST' }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  toggleAutoRenew: (autoRinnovo) => apiFetch('/profilo/subscription/autorenew', {
    method: 'PUT',
    body: JSON.stringify({ autoRinnovo })
  }),

  // Prenotazioni
  getPrenotazioni: () => apiFetch('/prenotazioni'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  createPrenotazione: (data) => apiFetch('/prenotazioni', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  deletePrenotazione: (id) => apiFetch(`/prenotazioni/${id}`, { method: 'DELETE' }),

  // Categorie
  getCategorie: () => apiFetch('/categorie'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getCategoria: (id) => apiFetch(`/categorie/${id}`),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  createCategoria: (data) => apiFetch('/categorie', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateCategoria: (id, data) => apiFetch(`/categorie/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  deleteCategoria: (id) => apiFetch(`/categorie/${id}`, { method: 'DELETE' }),

  // Admin Utenti
  getUtenti: (params = {}) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var query = new URLSearchParams();
    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));
    if (params.role) query.set('role', String(params.role));
    // Variabile qs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var qs = query.toString();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch('/admin/utenti' + (qs ? '?' + qs : ''));
  },
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  createUtenteInvito: (data) => apiFetch('/admin/utenti/inviti', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  sendPasswordSetup: (userId) => apiFetch('/admin/utenti/' + userId + '/password-setup', {
    method: 'POST'
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getUtenteSecurity: (userId) => apiFetch('/admin/utenti/' + userId + '/security'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateRuolo: (id, data) => apiFetch('/admin/utenti/' + id + '/ruolo', {
    method: 'PUT',
    body: JSON.stringify(data)
  }),
  // Admin: biglietti di un utente specifico
  getUtenteBiglietti: (userId) => apiFetch(`/admin/utenti/${userId}/biglietti`),

  // Admin: ordini di un utente specifico
  getUtenteOrdini: (userId) => apiFetch(`/admin/utenti/${userId}/ordini`),

  // Admin: movimenti credito (via email)
  getUtenteMovimenti: (email) => apiFetch(`/admin/credito/ricariche?email=${encodeURIComponent(email || '')}`),

  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateCredito: (id, data) => apiFetch(`/admin/utenti/${id}/credito`, {
    method: 'PUT',
    body: JSON.stringify(data)
  }),

  // Segnalazioni (bug report)
  getSegnalazioni: () => apiFetch('/admin/segnalazioni'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  createSegnalazione: (data) => apiFetch('/admin/segnalazioni', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  updateStatoSegnalazione: (id, data) => apiFetch(`/admin/segnalazioni/${id}/stato`, {
    method: 'PUT',
    body: JSON.stringify(data)
  }),

  // Programmazione v2 (film-centric)
  getProgrammazioneFilms: (params = {}) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = new URLSearchParams();
    if (params.tab) query.set('tab', params.tab);
    if (params.search) query.set('search', params.search);
    if (params.categoriaId) query.set('categoriaId', String(params.categoriaId));
    if (params.cinemaId) query.set('cinemaId', String(params.cinemaId));
    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    // Variabile queryString: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const queryString = query.toString();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/programmazione/films${queryString ? `?${queryString}` : ''}`);
  },
  getProgrammazioneCinemas: (params = {}) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = new URLSearchParams();
    if (params.lat != null) query.set('lat', String(params.lat));
    if (params.lng != null) query.set('lng', String(params.lng));
    // Variabile queryString: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const queryString = query.toString();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/programmazione/cinemas${queryString ? `?${queryString}` : ''}`);
  },
  getShows: (cinemaId) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = cinemaId ? `?cinemaId=${encodeURIComponent(cinemaId)}` : '';
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/shows${query}`);
  },
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getCinemaPreferito: () => apiFetch('/profilo/cinema-preferito'),
  setCinemaPreferito: (cinemaId) => {
    if (cinemaId == null) {
      // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
      return apiFetch('/profilo/cinema-preferito', { method: 'PUT' });
    }
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/profilo/cinema-preferito/${cinemaId}`, { method: 'PUT' });
  },

  // Scheda film
  getFilmScheda: (filmId, cinemaId) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = cinemaId ? `?cinemaId=${cinemaId}` : '';
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/films/${filmId}/scheda${query}`);
  },

  // My cinemas - lista cinema
  getMyCinemas: (params = {}) => {
    // Variabile qs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var qs = '';
    if (params.lat != null && params.lng != null) {
      qs = '?lat=' + params.lat + '&lng=' + params.lng;
    }
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch('/my-cinemas' + qs);
  },

  // My cinemas - programmazione giornaliera cinema
  getCinemaSchedule: (cinemaId, date) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = date ? `?date=${date}` : '';
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/my-cinemas/${cinemaId}/schedule${query}`);
  },

  // Watchlist
  getWatchlist: () => apiFetch('/watchlist'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  checkWatchlist: (filmId) => apiFetch(`/watchlist/check/${filmId}`),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  addToWatchlist: (filmId) => apiFetch(`/watchlist/${filmId}`, { method: 'POST' }),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  removeFromWatchlist: (filmId) => apiFetch(`/watchlist/${filmId}`, { method: 'DELETE' }),

  // Recommendations AI
  getRecommendations: () => apiFetch('/recommendations'),

  // Notifications
  getNotifications: () => apiFetch('/notifications'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  deleteNotification: (id) => apiFetch(`/notifications/${id}`, { method: 'DELETE' }),

  // Checkout - Seat map
  getSeatMap: (showId) => apiFetch(`/checkout/shows/${showId}/seat-map`),

  // Checkout - Hold posti
  createHold: (showId, salaPostoIds) => apiFetch('/checkout/holds', {
    method: 'POST',
    body: JSON.stringify({ showId, salaPostoIds })
  }),

  // Checkout - Refresh hold (keep-alive)
  refreshHold: (holdToken) => apiFetch(`/checkout/holds/${encodeURIComponent(holdToken)}/refresh`, {
    method: 'POST'
  }),

  // Checkout - Release hold
  releaseHold: (holdToken) => apiFetch(`/checkout/holds/${encodeURIComponent(holdToken)}`, {
    method: 'DELETE'
  }),

  // Checkout - Crea ordine pendente
  createOrdine: (holdToken, idempotencyKey) => {
    // Variabile headers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch('/checkout/orders', {
      method: 'POST',
      headers,
      body: JSON.stringify({ holdToken, idempotencyKey: idempotencyKey || undefined })
    });
  },

  // Checkout - Lista ordini utente
  getOrdini: () => apiFetch('/checkout/orders'),

  // Checkout - Dettaglio ordine
  getOrdine: (orderId) => apiFetch(`/checkout/orders/${orderId}`),

  // Checkout - Paga ordine
  payOrdine: (orderId, metodoPagamento, importoCreditoRichiesto, idempotencyKey, codiceVoucher, offertaId) => {
    // Variabile headers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
    // Variabile body: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const body = {
      metodoPagamento,
      importoCreditoRichiesto: importoCreditoRichiesto || null,
      idempotencyKey: idempotencyKey || undefined
    };
    if (codiceVoucher) body.codiceVoucher = codiceVoucher;
    if (offertaId) body.offertaId = parseInt(offertaId);
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/checkout/orders/${orderId}/pay`, {
      method: 'POST',
      headers,
      body: JSON.stringify(body)
    });
  },

  getOfferte: (cinemaId) => {
    // Variabile query: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const query = cinemaId ? `?cinemaId=${encodeURIComponent(cinemaId)}` : '';
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/offerte${query}`);
  },
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  getAbbonamenti: () => apiFetch('/abbonamenti'),
  // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
  acquistaOfferta: (offertaId, showId) => apiFetch('/offerte/' + offertaId + '/acquista', {
    method: 'POST',
    body: JSON.stringify({ showId: showId })
  }),
  createOffertaStripeCheckoutSession: (offertaId, showId, idempotencyKey) => {
    // Variabile headers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch('/offerte/' + offertaId + '/stripe-checkout-session', {
      method: 'POST',
      headers,
      body: JSON.stringify({ showId: showId })
    });
  },
  createAbbonamentoStripeCheckoutSession: (abbonamentoId, idempotencyKey) => {
    // Variabile headers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch('/abbonamenti/' + abbonamentoId + '/stripe-checkout-session', {
      method: 'POST',
      headers
    });
  },
  // Checkout - Annulla ordine pendente
  cancelOrdine: (orderId) => apiFetch(`/checkout/orders/${orderId}/cancel`, {
    method: 'POST'
  }),

  // Checkout - Crea sessione Stripe Checkout hosted
  createStripeCheckoutSession: (orderId, payload, idempotencyKey) => {
    // Variabile headers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    return apiFetch(`/checkout/orders/${orderId}/stripe-checkout-session`, {
      method: 'POST',
      headers,
      body: JSON.stringify(payload || {})
    });
  },

  // Checkout - Stato checkout hosted
  getCheckoutStatus: (orderId) => apiFetch(`/checkout/orders/${orderId}/checkout-status`),

  // Checkout - Riconcilia sessione Stripe
  reconcileCheckoutSession: (orderId) => apiFetch(`/checkout/orders/${orderId}/reconcile-checkout-session`, {
    method: 'POST'
  }),

  // Frontend runtime config
  getFrontendConfig: () => apiFetch('/config/frontend'),

  // Checkout - Download PDF ordine
  getOrdinePdf: async (orderId) => {
    // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const auth = getAuthSafe();
    // Variabile accessToken: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const accessToken = auth?.getAccessToken?.();
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    const response = await fetch(`${API_BASE_URL}/checkout/orders/${orderId}/pdf`, {
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    });
    if (!response.ok) throw { status: response.status, message: 'Errore download PDF' };
    return response.blob();
  },

  // Checkout - Lista biglietti utente
  getBiglietti: () => apiFetch('/checkout/tickets'),

  // Checkout - Dettaglio biglietto
  getBiglietto: (ticketId) => apiFetch(`/checkout/tickets/${ticketId}`),

  // Credito - Saldo e movimenti
  getCreditoMe: () => apiFetch('/credito/me'),

  // Credito - Topup via Stripe Checkout
  createTopupStripeSession: (amount) => apiFetch('/credito/topup/stripe-session', {
    method: 'POST',
    body: JSON.stringify({ amount })
  }),

  // Credito - Riconcilia topup dopo ritorno da Stripe
  reconcileTopup: (sessionId) => apiFetch(`/credito/topup/reconcile?sessionId=${encodeURIComponent(sessionId)}`, {
    method: 'POST'
  })
};
