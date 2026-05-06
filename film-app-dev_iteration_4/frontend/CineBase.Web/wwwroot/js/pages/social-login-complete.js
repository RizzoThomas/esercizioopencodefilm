/**
 * social-login-complete.js
 * Scambia l'exchange code ricevuto dal backend con JWT + refresh token.
 * Poi reindirizza al path specificato (solo path interni).
 */
(function () {
    'use strict';

    var API_BASE = (window.API && window.API.getBaseUrl) 
        ? window.API.getBaseUrl() 
        : (window.location.hostname === 'localhost' ? 'http://localhost:5000' : '');

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

    function showError(message) {
        var spinner = document.getElementById('spinner');
        var statusText = document.getElementById('status-text');
        var statusSub = document.getElementById('status-sub');
        var errorMsg = document.getElementById('error-msg');

        if (spinner) spinner.style.display = 'none';
        if (statusText) statusText.textContent = 'Errore di accesso';
        if (statusSub) statusSub.textContent = '';
        if (errorMsg) {
            errorMsg.textContent = message;
            errorMsg.className = 'error-message show';
        }
    }

    async function exchangeCode(code) {
        var statusSub = document.getElementById('status-sub');

        try {
            var response = await fetch(API_BASE + '/auth/external/exchange', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ code: code })
            });

            if (!response.ok) {
                var errData = await response.json().catch(function() { return {}; });
                throw new Error(errData.error || 'Scambio codice fallito (HTTP ' + response.status + ')');
            }

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
    document.addEventListener('DOMContentLoaded', function () {
        var params = new URLSearchParams(window.location.search);
        var code = params.get('code');

        if (!code) {
            showError('Codice di autorizzazione mancante. Torna alla pagina di login e riprova.');
            return;
        }

        exchangeCode(code);
    });
})();
