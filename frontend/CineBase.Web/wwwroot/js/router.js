// router.js - Controllo accessi pagine

const Router = {
    // Mappa ruoli -> pagine accessibili
    adminPages: ['/dashboard.html', '/cinemas.html', '/registi.html', '/films.html', '/proiezioni.html'],
    userPages: ['/area-personale.html', '/prenotazione.html'],
    publicPages: ['/index.html', '/login.html', '/register.html'],

    normalizePath(path) {
        if (!path || path === '/') return '/index.html';

        let normalized = path.toLowerCase();

        if (normalized.endsWith('/')) {
            normalized = normalized.slice(0, -1);
        }

        const hasExtension = /\.[a-z0-9]+$/i.test(normalized);
        if (!hasExtension) {
            normalized = `${normalized}.html`;
        }

        return normalized;
    },

    redirectWithUnauthorizedToast(targetPath) {
        try {
            sessionStorage.setItem('cinebase_flash_toast', JSON.stringify({
                message: 'Accesso non autorizzato',
                type: 'danger',
                ts: Date.now()
            }));
        } catch {
            // ignore storage errors
        }

        window.location.replace(targetPath);
    },

    showFlashToastIfAny() {
        try {
            const raw = sessionStorage.getItem('cinebase_flash_toast');
            if (!raw) return;

            sessionStorage.removeItem('cinebase_flash_toast');

            let payload;
            try {
                payload = JSON.parse(raw);
            } catch {
                return;
            }

            if (!payload || !payload.message) return;

            if (typeof showToast === 'function') {
                showToast(payload.message, payload.type || 'danger');
            }
        } catch {
            // ignore storage errors
        }
    },

    // Verifica accesso alla pagina corrente
    checkAccess() {
        const currentPage = this.normalizePath(window.location.pathname);
        const isAuthenticated = Auth.isAuthenticated();
        const userRole = Auth.getUserRole();

        const adminPages = this.adminPages.map((p) => this.normalizePath(p));
        const userPages = this.userPages.map((p) => this.normalizePath(p));
        const publicPages = this.publicPages.map((p) => this.normalizePath(p));

        // Pagine pubbliche sono sempre accessibili
        if (publicPages.includes(currentPage)) {
            return true;
        }

        // Pagine admin - richiedono autenticazione
        if (adminPages.includes(currentPage)) {
            if (!isAuthenticated) {
                this.redirectToLogin(currentPage);
                return false;
            }

            // Solo Admin e PowerUser possono accedere all'area admin
            if (!Auth.canAccessAdmin()) {
                this.redirectWithUnauthorizedToast('/index.html');
                return false;
            }

            // PowerUser non può accedere a cinemas.html
            if (currentPage === '/cinemas.html' && !Auth.canManageCinemas()) {
                this.redirectWithUnauthorizedToast('/dashboard.html');
                return false;
            }

            return true;
        }

        // Pagine utente - richiedono autenticazione
        if (userPages.includes(currentPage)) {
            if (!isAuthenticated) {
                this.redirectToLogin(currentPage);
                return false;
            }
            return true;
        }

        // Altre pagine - consentite
        return true;
    },

    redirectToLogin(redirectUrl) {
        const redirectParam = redirectUrl ? `?redirect=${encodeURIComponent(redirectUrl)}` : '';
        window.location.href = `/login.html${redirectParam}`;
    },

    redirectToIndex() {
        window.location.href = '/index.html';
    },

    redirectToDashboard() {
        window.location.href = '/dashboard.html';
    },

  redirectAfterLogin() {
    const urlParams = new URLSearchParams(window.location.search);
    const redirect = urlParams.get('redirect');
    const normalizedRole = Auth.getNormalizedRole();
    const isAdmin = normalizedRole === 'Admin';
    const isPowerUser = normalizedRole === 'PowerUser';
    const isUser = normalizedRole === 'User';

    const target = redirect ? this.normalizePath(redirect) : null;

    if (target) {
      if (this.adminPages.includes(target)) {
        if (isAdmin || isPowerUser) {
          if (target === '/cinemas.html' && !Auth.canManageCinemas()) {
            window.location.href = '/dashboard.html';
            return;
          }

          window.location.href = target;
          return;
        }

        window.location.href = '/index.html';
        return;
      }

      window.location.href = target;
      return;
    }

    if (isAdmin || isPowerUser) {
      window.location.href = '/dashboard.html';
    } else if (isUser) {
      window.location.href = '/index.html';
    } else {
      window.location.href = '/login.html';
    }
  }
};

// Make Router globally available
window.Router = Router;

// Eseguire check all'avvio
document.addEventListener('DOMContentLoaded', () => {
    Router.showFlashToastIfAny();

    // Non eseguire su login/register per evitare loop
    const currentPage = Router.normalizePath(window.location.pathname);
    if (!['/login.html', '/register.html'].includes(currentPage)) {
        Router.checkAccess();
    }
});
