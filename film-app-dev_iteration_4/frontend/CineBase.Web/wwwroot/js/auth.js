const Auth = {
  STORAGE_KEYS: {
    ACCESS_TOKEN: 'cb_access_token',
    REFRESH_TOKEN: 'cb_refresh_token',
    USER: 'cb_user',
    DEVICE_ID: 'cb_device_id'
  },

  getAccessToken() {
    return localStorage.getItem(this.STORAGE_KEYS.ACCESS_TOKEN);
  },

  getRefreshToken() {
    return localStorage.getItem(this.STORAGE_KEYS.REFRESH_TOKEN);
  },

  getOrCreateDeviceId() {
    let deviceId = localStorage.getItem(this.STORAGE_KEYS.DEVICE_ID);
    if (deviceId) return deviceId;

    const hasLegacyRefreshToken = !!this.getRefreshToken();
    if (hasLegacyRefreshToken) {
      deviceId = 'web-default';
      localStorage.setItem(this.STORAGE_KEYS.DEVICE_ID, deviceId);
      return deviceId;
    }

    if (window.crypto?.randomUUID) {
      deviceId = window.crypto.randomUUID();
    } else {
      deviceId = `dev-${Date.now()}-${Math.random().toString(36).slice(2, 11)}`;
    }

    localStorage.setItem(this.STORAGE_KEYS.DEVICE_ID, deviceId);
    return deviceId;
  },

  saveTokens(accessToken, refreshToken) {
    localStorage.setItem(this.STORAGE_KEYS.ACCESS_TOKEN, accessToken);
    localStorage.setItem(this.STORAGE_KEYS.REFRESH_TOKEN, refreshToken);
  },

  saveUser(user) {
    localStorage.setItem(this.STORAGE_KEYS.USER, JSON.stringify(user));
  },

  clearAuth() {
    localStorage.removeItem(this.STORAGE_KEYS.ACCESS_TOKEN);
    localStorage.removeItem(this.STORAGE_KEYS.REFRESH_TOKEN);
    localStorage.removeItem(this.STORAGE_KEYS.USER);
  },

  getUser() {
    const userStr = localStorage.getItem(this.STORAGE_KEYS.USER);
    if (!userStr) return null;
    try {
      return JSON.parse(userStr);
    } catch {
      return null;
    }
  },

  isLoggedIn() {
    const token = this.getAccessToken();
    if (!token) { console.log('[auth] isLoggedIn: nessun token'); return false; }
    try {
      const payload = this.parseJwt(token);
      if (!payload) { console.log('[auth] isLoggedIn: parseJwt fallito'); return false; }
      const now = Math.ceil(Date.now() / 1000);
      const valid = payload.exp > now;
      console.log('[auth] isLoggedIn: exp=' + payload.exp + ' now=' + now + ' valid=' + valid + ' sub=' + payload.sub);
      return valid;
    } catch (e) {
      console.log('[auth] isLoggedIn: errore parse', e);
      return false;
    }
  },

  getUserRole() {
    const user = this.getUser();
    if (user?.ruolo != null) return user.ruolo;

    const token = this.getAccessToken();
    const payload = token ? this.parseJwt(token) : null;
    return payload?.role || null;
  },

  parseJwt(token) {
    try {
      const base64Url = token.split('.')[1];
      if (!base64Url) return null;
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch {
      return null;
    }
  },

  async login(email, password) {
    try {
      const headers = { 'Content-Type': 'application/json' };
      const trustedDevice = localStorage.getItem('cb_trusted_device');
      if (trustedDevice) {
        headers['X-Trusted-Device'] = trustedDevice;
        console.log('[auth] login: invio header X-Trusted-Device');
      }

      const response = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: JSON.stringify({ email, password, deviceId: this.getOrCreateDeviceId() })
      });

      if (!response.ok) {
        let message = 'Credenziali non valide';
        if (response.status === 401) {
          const err = await response.json().catch(() => null);
          message = err?.message || message;
        }
        throw { status: response.status, message };
      }

      const data = await response.json();

      // Se richiede 2FA, restituisci il temp token senza salvare
      if (data.requiresTwoFactor) {
        console.log('[auth] login: 2FA richiesto, restituisco tempToken');
        return { requiresTwoFactor: true, tempToken: data.tempToken };
      }

      this.saveTokens(data.accessToken, data.refreshToken);
      this.saveUser(data.user);
      console.log('[auth] login: token salvato, redirect...');
      return data;
    } catch (err) {
      if (err.status) throw err;
      throw { status: 500, message: 'Impossibile connettersi al server. Riprova piu tardi.' };
    }
  },

  async loginWith2Fa(tempToken, code, trustDevice) {
    try {
      const response = await fetch(`${API_BASE_URL}/auth/login-2fa`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          tempToken,
          code,
          trustDevice,
          deviceId: this.getOrCreateDeviceId()
        })
      });

      if (!response.ok) {
        let message = 'Codice 2FA non valido';
        const err = await response.json().catch(() => null);
        message = err?.error || err?.message || message;
        throw { status: response.status, message };
      }

      const data = await response.json();
      this.saveTokens(data.accessToken, data.refreshToken);
      this.saveUser(data.user);
      if (data.trustedDeviceToken) {
        localStorage.setItem('cb_trusted_device', data.trustedDeviceToken);
        console.log('[auth] loginWith2Fa: trusted device salvato in localStorage');
      }
      console.log('[auth] loginWith2Fa: token salvato, redirect...');
      return data;
    } catch (err) {
      if (err.status) throw err;
      throw { status: 500, message: 'Impossibile connettersi al server.' };
    }
  },

  async register(registerData) {
    try {
      const response = await fetch(`${API_BASE_URL}/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...registerData, deviceId: this.getOrCreateDeviceId() })
      });

      if (!response.ok) {
        let message = 'Errore durante la registrazione';
        let errors;
        if (response.status === 409) {
          const err = await response.json().catch(() => null);
          message = err?.message || 'Email gia registrata';
        } else if (response.status === 400) {
          const err = await response.json().catch(() => null);
          message = err?.message || message;
          errors = err?.errors;
        }
        throw { status: response.status, message, errors };
      }

      const data = await response.json();
      this.saveTokens(data.accessToken, data.refreshToken);
      this.saveUser(data.user);
      return data;
    } catch (err) {
      if (err.status) throw err;
      throw { status: 500, message: 'Impossibile connettersi al server. Riprova piu tardi.' };
    }
  },

  async refreshAccessToken() {
    try {
      const refreshToken = this.getRefreshToken();
      if (!refreshToken) {
        throw { status: 401, message: 'Nessun refresh token disponibile' };
      }

      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken, deviceId: this.getOrCreateDeviceId() })
      });

      if (!response.ok) {
        this.clearAuth();
        throw { status: response.status, message: 'Sessione scaduta' };
      }

      const data = await response.json();
      this.saveTokens(data.accessToken, data.refreshToken);
      if (data.user) {
        this.saveUser(data.user);
      }
      return data;
    } catch (err) {
      if (err.status) throw err;
      throw { status: 500, message: 'Impossibile connettersi al server.' };
    }
  },

  async logout() {
    try {
      await fetch(`${API_BASE_URL}/auth/logout`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          refreshToken: this.getRefreshToken(),
          deviceId: this.getOrCreateDeviceId()
        })
      });
    } catch {
      // ignore network errors during logout
    } finally {
      this.clearAuth();
    }
  },

  redirectToLogin(redirectUrl) {
    const url = new URL('/login.html', window.location.origin);
    if (redirectUrl) {
      url.searchParams.set('redirect', redirectUrl);
    }
    window.location.href = url.toString();
  },

  redirectAfterLogin() {
    const params = new URLSearchParams(window.location.search);
    const redirect = params.get('redirect');
    // Usa sanitizeRedirectPath se disponibile (da route-guard), altrimenti fallback safe
    var safe = '/index.html';
    if (redirect && typeof window.RouteGuard !== 'undefined' && window.RouteGuard.sanitizeRedirectPath) {
      safe = window.RouteGuard.sanitizeRedirectPath(redirect);
    } else if (redirect && redirect.charAt(0) === '/' && redirect.indexOf('://') === -1 && redirect.indexOf('..') === -1) {
      safe = redirect;
    }
    window.location.href = safe;
  },

  /**
   * Avvia il flusso di login esterno (Google, Microsoft, Facebook).
   */
  startExternalLogin(provider, redirectPath) {
    var safe = '/index.html';
    if (typeof window.RouteGuard !== 'undefined' && window.RouteGuard.sanitizeRedirectPath) {
      safe = window.RouteGuard.sanitizeRedirectPath(redirectPath);
    }
    var providerMap = { google: 'login-google', microsoft: 'login-microsoft', facebook: 'login-facebook' };
    var endpoint = providerMap[provider.toLowerCase()] || 'login-google';
    window.location.href = API_BASE_URL + '/auth/' + endpoint + '?redirect=' + encodeURIComponent(safe);
  },

  async changePassword(currentPassword, newPassword) {
    var accessToken = this.getAccessToken();
    var response = await fetch(API_BASE_URL + '/auth/change-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + accessToken },
      body: JSON.stringify({ currentPassword, newPassword })
    });
    if (!response.ok) {
      var err = await response.json().catch(function() { return {}; });
      throw { status: response.status, message: err.error || err.message || 'Errore cambio password' };
    }
    return response.json();
  },

  async requestSetPassword() {
    var accessToken = this.getAccessToken();
    var response = await fetch(API_BASE_URL + '/auth/set-password/request', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + accessToken }
    });
    if (!response.ok) {
      var err = await response.json().catch(function() { return {}; });
      throw { status: response.status, message: err.error || 'Errore richiesta setup password' };
    }
    return response.json();
  },

  async getAccountSecurity() {
    var accessToken = this.getAccessToken();
    var response = await fetch(API_BASE_URL + '/auth/security/me', {
      method: 'GET',
      headers: { 'Authorization': 'Bearer ' + accessToken }
    });
    if (!response.ok) throw { status: response.status, message: 'Errore caricamento sicurezza account' };
    return response.json();
  },

  async forgotPassword(email) {
    var response = await fetch(API_BASE_URL + '/auth/forgot-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email })
    });
    // Sempre OK per non enumerare email
    return response.json();
  },

  async resetPassword(token, newPassword) {
    var response = await fetch(API_BASE_URL + '/auth/reset-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token, newPassword })
    });
    var data = await response.json();
    if (!response.ok) throw { status: response.status, message: data.error || 'Token non valido o scaduto' };
    return data;
  }
};

window.Auth = Auth;
