// auth.js - Gestione autenticazione

const Auth = {
    // Storage keys
    ACCESS_TOKEN_KEY: 'cinebase_access_token',
    REFRESH_TOKEN_KEY: 'cinebase_refresh_token',
    USER_KEY: 'cinebase_user',

    // Login
    async login(email, password) {
        const response = await apiFetch('/auth/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });

        this.setTokens(response.accessToken, response.refreshToken);
        this.setUser(response.user);

        return response.user;
    },

    // Logout
    async logout() {
        try {
            await apiFetch('/auth/logout', { method: 'POST' });
        } catch (error) {
            console.log('Logout API error:', error);
        } finally {
            this.clearAuth();
        }
    },

    // Refresh token
    async refreshToken() {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) throw new Error('No refresh token');

        try {
            const response = await apiFetch('/auth/refresh', {
                method: 'POST',
                body: JSON.stringify({ refreshToken })
            });

            this.setTokens(response.accessToken, response.refreshToken);
            this.setUser(response.user);

            return response.accessToken;
        } catch (error) {
            this.clearAuth();
            throw error;
        }
    },

    // Token management
    getAccessToken() { return localStorage.getItem(this.ACCESS_TOKEN_KEY); },
    getRefreshToken() { return localStorage.getItem(this.REFRESH_TOKEN_KEY); },
    getUser() {
        const user = localStorage.getItem(this.USER_KEY);
        return user ? JSON.parse(user) : null;
    },
    getUserRole() {
        const user = this.getUser();
        if (!user) return null;

        return user.ruolo ?? user.role ?? user.Role ?? null;
    },

    getNormalizedRole() {
        const rawRole = this.getUserRole();
        if (rawRole === null || rawRole === undefined) return null;

        if (typeof rawRole === 'number') {
            if (rawRole === 0) return 'Admin';
            if (rawRole === 1) return 'PowerUser';
            if (rawRole === 2) return 'User';
            return null;
        }

        const role = String(rawRole).trim().toLowerCase();
        if (role === '0') return 'Admin';
        if (role === '1') return 'PowerUser';
        if (role === '2') return 'User';
        if (role === 'admin') return 'Admin';
        if (role === 'poweruser' || role === 'power_user' || role === 'power user') return 'PowerUser';
        if (role === 'user' || role === 'utente' || role === 'basicuser' || role === 'basic_user' || role === 'basic user') return 'User';

        return null;
    },

    setTokens(access, refresh) {
        localStorage.setItem(this.ACCESS_TOKEN_KEY, access);
        localStorage.setItem(this.REFRESH_TOKEN_KEY, refresh);
    },

    setUser(user) {
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    },

    clearAuth() {
        localStorage.removeItem(this.ACCESS_TOKEN_KEY);
        localStorage.removeItem(this.REFRESH_TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);
    },

    isAuthenticated() {
        return !!this.getAccessToken();
    },

    isAdmin() { return this.getNormalizedRole() === 'Admin'; },
    isPowerUser() { return this.getNormalizedRole() === 'PowerUser'; },
    isUser() { return this.getNormalizedRole() === 'User'; },

    // Can access admin area
    canAccessAdmin() {
        return this.isAdmin() || this.isPowerUser();
    },

    // Can manage cinemas (solo Admin)
    canManageCinemas() {
        return this.isAdmin();
    },

    // Can manage films/proiezioni/registi
    canManageContent() {
        return this.isAdmin() || this.isPowerUser();
    }
};

// Make Auth globally available
window.Auth = Auth;
