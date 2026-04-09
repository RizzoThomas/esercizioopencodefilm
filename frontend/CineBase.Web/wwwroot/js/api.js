// Configurazione base
const API_BASE_URL = 'http://localhost:5000';

// Helper function per fetch con error handling e auto-refresh token
async function apiFetch(endpoint, options = {}) {
    const token = typeof Auth !== 'undefined' ? Auth.getAccessToken() : null;
    
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            ...(token && { 'Authorization': `Bearer ${token}` })
        }
    };

    let response;

    try {
        response = await fetch(`${API_BASE_URL}${endpoint}`, {
            ...defaultOptions,
            ...options,
            headers: {
                ...defaultOptions.headers,
                ...options.headers
            }
        });
    } catch {
        throw {
            status: 0,
            message: 'Impossibile raggiungere il backend. Verifica che sia avviato su http://localhost:5000.'
        };
    }

    // Gestione token scaduto (401) - tenta refresh
    if (response.status === 401 && typeof Auth !== 'undefined' && Auth.getRefreshToken()) {
        try {
            await Auth.refreshToken();
            // Retry con nuovo token
            const newToken = Auth.getAccessToken();
            response = await fetch(`${API_BASE_URL}${endpoint}`, {
                ...defaultOptions,
                ...options,
                headers: {
                    ...defaultOptions.headers,
                    ...options.headers,
                    'Authorization': `Bearer ${newToken}`
                }
            });
        } catch {
            Auth.clearAuth();
            window.location.href = '/login.html';
            throw { status: 401, message: 'Sessione scaduta. Effettua nuovamente il login.' };
        }
    }

    if (!response.ok) {
        const contentType = response.headers.get('content-type') || '';
        let message = 'Errore di rete';
        let errors;

        if (contentType.includes('application/json')) {
            const errorJson = await response.json().catch(() => null);
            if (errorJson) {
                message = errorJson.message || errorJson.title || message;
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
    // Auth
    login: (email, password) => apiFetch('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
    }),
    register: (data) => apiFetch('/auth/register', {
        method: 'POST',
        body: JSON.stringify(data)
    }),
    logout: () => apiFetch('/auth/logout', { method: 'POST' }),
    refreshToken: (refreshToken) => apiFetch('/auth/refresh', {
        method: 'POST',
        body: JSON.stringify({ refreshToken })
    }),
    getCurrentUser: () => apiFetch('/auth/me'),
    
    // Registi
    getRegisti: () => apiFetch('/registi'),
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
    getFilms: () => apiFetch('/films'),
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

    // Cinema
    getCinemas: () => apiFetch('/cinemas'),
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
    getProiezioni: () => apiFetch('/proiezioni'),
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
    getCategorieByFilm: (filmId) => apiFetch(`/categorie/film/${filmId}`),
    addCategoriaToFilm: (filmId, categoriaId) => apiFetch(`/categorie/film/${filmId}/${categoriaId}`, {
        method: 'POST'
    }),
    removeCategoriaFromFilm: (filmId, categoriaId) => apiFetch(`/categorie/film/${filmId}/${categoriaId}`, {
        method: 'DELETE'
    }),

    // Area Personale
    getSavedProiezioni: () => apiFetch('/me/proiezioni'),
    saveProiezione: (data) => apiFetch('/me/proiezioni', {
        method: 'POST',
        body: JSON.stringify(data)
    }),
    deleteSavedProiezione: (id) => apiFetch(`/me/proiezioni/${id}`, { method: 'DELETE' }),
    
    // Prenotazioni
    getPrenotazioni: () => apiFetch('/me/prenotazioni'),
    getPrenotazioneDisponibilita: (proiezioneId) => apiFetch(`/me/prenotazioni/disponibilita/${proiezioneId}`),
    createPrenotazione: (data) => apiFetch('/me/prenotazioni', {
        method: 'POST',
        body: JSON.stringify(data)
    }),
    annullaPrenotazione: (id) => apiFetch(`/me/prenotazioni/${id}/annulla`, {
        method: 'PUT'
    })
};
