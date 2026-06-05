/**
 * social-login-complete.js
 * Scambia l'exchange code ricevuto dal backend con JWT + refresh token.
 * Poi reindirizza al path specificato (solo path interni).
 */
(function () {
    'use strict';

    // Variabile API_BASE: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var API_BASE = (window.API_BASE_URL !== undefined && window.API_BASE_URL !== null)
        ? window.API_BASE_URL
        : (window.location.hostname === 'localhost' ? 'http://localhost:5000' : '');

    // Funzione sanitizeRedirectPath: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function sanitizeRedirectPath(path) {
        if (!path || typeof path !== 'string') return '/index.html';
        // Blocca URL esterni
        if (path.indexOf('://') !== -1 || path.indexOf('//') === 0) return '/index.html';
        // Deve iniziare con /
        if (path.charAt(0) !== '/') return '/index.html';
        // Previeni path traversal
        if (path.indexOf('..') !== -1) return '/index.html';
        return path;
    }

    // Funzione showError: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function showError(message) {
        // Variabile spinner: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var spinner = document.getElementById('spinner');
        // Variabile statusText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var statusText = document.getElementById('status-text');
        // Variabile statusSub: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var statusSub = document.getElementById('status-sub');
        // Variabile errorMsg: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var errorMsg = document.getElementById('error-msg');

        if (spinner) spinner.style.display = 'none';
        if (statusText) statusText.textContent = 'Errore di accesso';
        if (statusSub) statusSub.textContent = '';
        if (errorMsg) {
            errorMsg.textContent = message;
            errorMsg.className = 'error-message show';
        }
    }

    // Funzione exchangeCode: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    async function exchangeCode(code) {
        // Variabile statusSub: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var statusSub = document.getElementById('status-sub');

        try {
            // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
            var response = await fetch(API_BASE + '/auth/external/exchange', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ code: code })
            });

            if (!response.ok) {
                // Variabile errData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
                var errData = await response.json().catch(function() { return {}; });
                throw new Error(errData.error || 'Scambio codice fallito (HTTP ' + response.status + ')');
            }

            // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            var data = await response.json();

            // Salva token usando Auth (da auth.js)
            if (window.Auth && window.Auth.saveTokens) {
                window.Auth.saveTokens(data.accessToken, data.refreshToken, data.expiresAt);
                if (window.Auth.saveUser && data.user) {
                    window.Auth.saveUser(data.user);
                }
            } else {
                // Fallback diretto
                try {
                    localStorage.setItem('cb_access_token', data.accessToken);
                    localStorage.setItem('cb_refresh_token', data.refreshToken);
                    if (data.user) {
                        localStorage.setItem('cb_user', JSON.stringify(data.user));
                    }
                } catch (e) {
                    console.error('Errore salvataggio token:', e);
                }
            }

            // Redirect
            var params = new URLSearchParams(window.location.search);
            // Variabile redirect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            var redirect = sanitizeRedirectPath(params.get('redirect'));
            
            if (statusSub) statusSub.textContent = 'Accesso completato. Reindirizzamento...';
            
            setTimeout(function () {
                window.location.replace(redirect);
            }, 500);

        } catch (error) {
            console.error('[SocialLoginComplete] Errore:', error);
            showError(error.message || 'Impossibile completare l\'accesso. Riprova.');
        }
    }

    // Avvia lo scambio
// Listener evento: si attiva quando scatta l'evento e aggiorna UI o stato.
    document.addEventListener('DOMContentLoaded', function () {
        // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var params = new URLSearchParams(window.location.search);
        // Variabile code: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        var code = params.get('code');

        if (!code) {
            showError('Codice di autorizzazione mancante. Torna alla pagina di login e riprova.');
            return;
        }

        exchangeCode(code);
    });
})();
