// Configurazione base
var API_BASE_URL = window.API_BASE_URL || 'http://localhost:5000';

let isRefreshing = false;
let refreshSubscribers = [];

function subscribeTokenRefresh(callback) {
  refreshSubscribers.push(callback);
}

function onTokenRefreshed(token) {
  refreshSubscribers.forEach(callback => callback(token));
  refreshSubscribers = [];
}

function getAuthSafe() {
  return typeof window !== 'undefined' && window.Auth ? window.Auth : null;
}

function normalizeRole(role) {
  if (role == null) return '';
  const value = String(role).trim().toLowerCase();
  if (value === '2' || value === 'admin') return 'admin';
  if (value === '1' || value === 'poweruser') return 'poweruser';
  if (value === '0' || value === 'user') return 'user';
  return value;
}

function isAdminAreaPath(pathname) {
  const adminPaths = new Set([
    '/dashboard.html',
    '/films.html',
    '/registi.html',
    '/cinemas.html',
    '/proiezioni.html',
    '/categorie.html'
  ]);
  return adminPaths.has((pathname || '').toLowerCase());
}

function enforceAdminAreaAccess() {
  const auth = getAuthSafe();
  if (!auth) return true;
  if (!isAdminAreaPath(window.location.pathname)) return true;

  if (!auth.isLoggedIn()) {
    const redirectUrl = window.location.pathname + window.location.search;
    auth.redirectToLogin(redirectUrl);
    return false;
  }

  const role = normalizeRole(auth.getUserRole?.());
  if (role === 'admin' || role === 'poweruser') {
    return true;
  }

  window.location.href = '/index.html?forbidden=true';
  return false;
}

async function parseSuccessfulResponse(response) {
  if (response.status === 204) return null;

  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    return response.json();
  }

  return response.text();
}

// Helper function per fetch con error handling e retry su 401
async function apiFetch(endpoint, options = {}) {
  if (!enforceAdminAreaAccess()) {
    throw { status: 403, message: 'Non autorizzato ad accedere a questa pagina' };
  }

  const defaultOptions = {
    headers: {
      'Content-Type': 'application/json'
    }
  };

  const auth = getAuthSafe();
  const accessToken = auth?.getAccessToken?.();
  if (accessToken) {
    defaultOptions.headers['Authorization'] = `Bearer ${accessToken}`;
  }

  const requestOptions = {
    ...defaultOptions,
    ...options,
    headers: {
      ...defaultOptions.headers,
      ...(options.headers || {})
    }
  };

  let response;

  try {
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
            const retryOptions = {
              ...requestOptions,
              _noRetry: true,
              headers: {
                ...requestOptions.headers,
                Authorization: `Bearer ${newToken}`
              }
            };
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
      const newToken = auth.getAccessToken();
      onTokenRefreshed(newToken);
      isRefreshing = false;

      const retryOptions = {
        ...requestOptions,
        _noRetry: true,
        headers: {
          ...requestOptions.headers,
          Authorization: `Bearer ${newToken}`
        }
      };
      const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, retryOptions);

      if (retryResponse.ok) {
        return parseSuccessfulResponse(retryResponse);
      }

      throw { status: retryResponse.status, message: 'Richiesta fallita dopo refresh' };
    } catch (refreshError) {
      isRefreshing = false;
      auth.clearAuth();
      const redirectUrl = window.location.pathname + window.location.search;
      auth.redirectToLogin(redirectUrl);
      throw { status: 401, message: 'Sessione scaduta' };
    }
  }

  if (!response.ok) {
    const contentType = response.headers.get('content-type') || '';
    let message = 'Errore di rete';
    let errors;

    if (contentType.includes('application/json')) {
      const errorJson = await response.json().catch(() => null);
      if (errorJson) {
        message = errorJson.detail || errorJson.message || errorJson.title || message;
        errors = errorJson.errors;
      }
    } else {
      const errorText = await response.text().catch(() => '');
      if (errorText) {
        message = errorText;
      }
    }

    throw { status: response.status, message, errors };
  }

  if (response.status === 204) return null;

  const contentType = response.headers.get('content-type') || '';
  if (!contentType.includes('application/json')) {
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
    const headers = { 'Content-Type': 'application/json' };
    const auth = getAuthSafe();
    const accessToken = auth?.getAccessToken?.();
    if (accessToken) {
      headers['Authorization'] = `Bearer ${accessToken}`;
    }
    return headers;
  },

  // Registi
  getRegisti: (params = {}) => {
    const query = new URLSearchParams();

    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));

    const queryString = query.toString();
    return apiFetch(`/registi${queryString ? `?${queryString}` : ''}`);
  },
  getRegista: (id) => apiFetch(`/registi/${id}`),
  createRegista: (data) => apiFetch('/registi', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  updateRegista: (id, data) => apiFetch(`/registi/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  deleteRegista: (id) => apiFetch(`/registi/${id}`, { method: 'DELETE' }),
  getFilmsByRegista: (id) => apiFetch(`/registi/${id}/films`),
  
  // Film
  getFilms: (params = {}) => {
    const query = new URLSearchParams();

    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));
    if (params.filter) query.set('filter', String(params.filter));

    const queryString = query.toString();
    return apiFetch(`/films${queryString ? `?${queryString}` : ''}`);
  },
  getFilm: (id) => apiFetch(`/films/${id}`),
  createFilm: (data) => apiFetch('/films', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  updateFilm: (id, data) => apiFetch(`/films/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
deleteFilm: (id) => apiFetch(`/films/${id}`, { method: 'DELETE' }),

    uploadCover: async (file) => {
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_BASE_URL}/media/covers`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
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
    const query = new URLSearchParams();

    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));

    const queryString = query.toString();
    return apiFetch(`/cinemas${queryString ? `?${queryString}` : ''}`);
  },
  getCinema: (id) => apiFetch(`/cinemas/${id}`),
  createCinema: (data) => apiFetch('/cinemas', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  updateCinema: (id, data) => apiFetch(`/cinemas/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  deleteCinema: (id) => apiFetch(`/cinemas/${id}`, { method: 'DELETE' }),
  
  // Proiezioni
  getProiezioni: (params = {}) => {
    const query = new URLSearchParams();

    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));

    const queryString = query.toString();
    return apiFetch(`/proiezioni${queryString ? `?${queryString}` : ''}`);
  },
  getProiezione: (id) => apiFetch(`/proiezioni/${id}`),
  createProiezione: (data) => apiFetch('/proiezioni', { 
    method: 'POST', 
    body: JSON.stringify(data) 
  }),
  updateProiezione: (id, data) => apiFetch(`/proiezioni/${id}`, { 
    method: 'PUT', 
    body: JSON.stringify(data) 
  }),
  deleteProiezione: (id) => apiFetch(`/proiezioni/${id}`, { method: 'DELETE' }),

  // Profilo
  getProfilo: () => apiFetch('/profilo'),
  updateProfilo: (data) => apiFetch('/profilo', {
    method: 'PUT',
    body: JSON.stringify(data)
  }),
  getUserVouchers: () => apiFetch('/profilo/vouchers'),
  getUserSubscription: () => apiFetch('/profilo/subscription'),
  cancelSubscription: () => apiFetch('/profilo/subscription/cancel', { method: 'POST' }),
  toggleAutoRenew: (autoRinnovo) => apiFetch('/profilo/subscription/autorenew', {
    method: 'PUT',
    body: JSON.stringify({ autoRinnovo })
  }),

  // Prenotazioni
  getPrenotazioni: () => apiFetch('/prenotazioni'),
  createPrenotazione: (data) => apiFetch('/prenotazioni', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  deletePrenotazione: (id) => apiFetch(`/prenotazioni/${id}`, { method: 'DELETE' }),

  // Categorie
  getCategorie: () => apiFetch('/categorie'),
  getCategoria: (id) => apiFetch(`/categorie/${id}`),
  createCategoria: (data) => apiFetch('/categorie', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  updateCategoria: (id, data) => apiFetch(`/categorie/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  }),
  deleteCategoria: (id) => apiFetch(`/categorie/${id}`, { method: 'DELETE' }),

  // Admin Utenti
  getUtenti: (params = {}) => {
    var query = new URLSearchParams();
    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', String(params.search));
    if (params.role) query.set('role', String(params.role));
    var qs = query.toString();
    return apiFetch('/admin/utenti' + (qs ? '?' + qs : ''));
  },
  createUtenteInvito: (data) => apiFetch('/admin/utenti/inviti', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  sendPasswordSetup: (userId) => apiFetch('/admin/utenti/' + userId + '/password-setup', {
    method: 'POST'
  }),
  getUtenteSecurity: (userId) => apiFetch('/admin/utenti/' + userId + '/security'),
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

  updateCredito: (id, data) => apiFetch(`/admin/utenti/${id}/credito`, {
    method: 'PUT',
    body: JSON.stringify(data)
  }),

  // Segnalazioni (bug report)
  getSegnalazioni: () => apiFetch('/admin/segnalazioni'),
  createSegnalazione: (data) => apiFetch('/admin/segnalazioni', {
    method: 'POST',
    body: JSON.stringify(data)
  }),
  updateStatoSegnalazione: (id, data) => apiFetch(`/admin/segnalazioni/${id}/stato`, {
    method: 'PUT',
    body: JSON.stringify(data)
  }),

  // Programmazione v2 (film-centric)
  getProgrammazioneFilms: (params = {}) => {
    const query = new URLSearchParams();
    if (params.tab) query.set('tab', params.tab);
    if (params.search) query.set('search', params.search);
    if (params.categoriaId) query.set('categoriaId', String(params.categoriaId));
    if (params.cinemaId) query.set('cinemaId', String(params.cinemaId));
    if (params.page != null) query.set('page', String(params.page));
    if (params.pageSize != null) query.set('pageSize', String(params.pageSize));
    const queryString = query.toString();
    return apiFetch(`/programmazione/films${queryString ? `?${queryString}` : ''}`);
  },
  getProgrammazioneCinemas: (params = {}) => {
    const query = new URLSearchParams();
    if (params.lat != null) query.set('lat', String(params.lat));
    if (params.lng != null) query.set('lng', String(params.lng));
    const queryString = query.toString();
    return apiFetch(`/programmazione/cinemas${queryString ? `?${queryString}` : ''}`);
  },
  getShows: (cinemaId) => {
    const query = cinemaId ? `?cinemaId=${encodeURIComponent(cinemaId)}` : '';
    return apiFetch(`/shows${query}`);
  },
  getCinemaPreferito: () => apiFetch('/profilo/cinema-preferito'),
  setCinemaPreferito: (cinemaId) => {
    if (cinemaId == null) {
      return apiFetch('/profilo/cinema-preferito', { method: 'PUT' });
    }
    return apiFetch(`/profilo/cinema-preferito/${cinemaId}`, { method: 'PUT' });
  },

  // Scheda film
  getFilmScheda: (filmId, cinemaId) => {
    const query = cinemaId ? `?cinemaId=${cinemaId}` : '';
    return apiFetch(`/films/${filmId}/scheda${query}`);
  },

  // My cinemas - lista cinema
  getMyCinemas: () => apiFetch('/my-cinemas'),

  // My cinemas - programmazione giornaliera cinema
  getCinemaSchedule: (cinemaId, date) => {
    const query = date ? `?date=${date}` : '';
    return apiFetch(`/my-cinemas/${cinemaId}/schedule${query}`);
  },

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
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
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
  payOrdine: (orderId, metodoPagamento, importoCreditoRichiesto, idempotencyKey, codiceVoucher) => {
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
    const body = {
      metodoPagamento,
      importoCreditoRichiesto: importoCreditoRichiesto || null,
      idempotencyKey: idempotencyKey || undefined
    };
    if (codiceVoucher) body.codiceVoucher = codiceVoucher;
    return apiFetch(`/checkout/orders/${orderId}/pay`, {
      method: 'POST',
      headers,
      body: JSON.stringify(body)
    });
  },

  getOfferte: (cinemaId) => {
    const query = cinemaId ? `?cinemaId=${encodeURIComponent(cinemaId)}` : '';
    return apiFetch(`/offerte${query}`);
  },
  getAbbonamenti: () => apiFetch('/abbonamenti'),
  acquistaOfferta: (offertaId, showId) => apiFetch('/offerte/' + offertaId + '/acquista', {
    method: 'POST',
    body: JSON.stringify({ showId: showId })
  }),
  createOffertaStripeCheckoutSession: (offertaId, showId, idempotencyKey) => {
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
    return apiFetch('/offerte/' + offertaId + '/stripe-checkout-session', {
      method: 'POST',
      headers,
      body: JSON.stringify({ showId: showId })
    });
  },
  createAbbonamentoStripeCheckoutSession: (abbonamentoId, idempotencyKey) => {
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
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
    const headers = {};
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey;
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
    const auth = getAuthSafe();
    const accessToken = auth?.getAccessToken?.();
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
