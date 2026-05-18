// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', () => {
  if (!window.Auth) return;

  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById('login-form');
  // Variabile emailInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const emailInput = document.getElementById('email');
  // Variabile passwordInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const passwordInput = document.getElementById('password');
  // Variabile submitBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const submitBtn = document.getElementById('submit-btn');
  // Variabile btnText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnText = document.getElementById('btn-text');
  // Variabile btnLoader: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnLoader = document.getElementById('btn-loader');
  // Variabile errorAlert: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const errorAlert = document.getElementById('error-alert');
  // Variabile errorMessage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const errorMessage = document.getElementById('error-message');
  // Variabile expiredAlert: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const expiredAlert = document.getElementById('expired-alert');
  // Variabile togglePasswordBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const togglePasswordBtn = document.getElementById('toggle-password');

  // Container 2FA (creato dinamicamente)
  let twoFaContainer = null;

  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const params = new URLSearchParams(window.location.search);
  // Variabile expired: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const expired = params.get('expired');
  // Variabile redirect: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const redirect = params.get('returnUrl') || params.get('redirect');
  // Variabile socialError: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const socialError = params.get('error');

  // Errore social login
  if (socialError) {
    if (errorAlert && errorMessage) {
      // Variabile errorMap: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var errorMap = {
        'no_email': 'Il provider non ha fornito un\'email. Riprova con un altro metodo.',
        'email_not_verified': 'L\'email Google non risulta verificata. Usa un account Google con email verificata.',
        'domain_not_allowed': 'Accesso Microsoft riservato al dominio @issgreppi.it.',
        'elevated_role': 'Gli account con privilegi elevati devono usare la password. Accedi con email e password.',
        'access_denied': 'Accesso negato dal provider. Riprova.'
      };
      errorMessage.textContent = errorMap[socialError] || 'Accesso con social network fallito. Riprova.';
      errorAlert.classList.remove('hidden');
    }
    // Pulisci URL
    var url = new URL(window.location);
    url.searchParams.delete('error');
    window.history.replaceState({}, '', url);
  }

  if (expired === 'true' && expiredAlert) {
    expiredAlert.classList.remove('hidden');
  }

  if (Auth.isLoggedIn()) {
    Auth.redirectAfterLogin();
    return;
  }

  if (togglePasswordBtn && passwordInput) {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    togglePasswordBtn.addEventListener('click', () => {
      // Variabile type: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const type = passwordInput.type === 'password' ? 'text' : 'password';
      passwordInput.type = type;
      // Variabile icon: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const icon = togglePasswordBtn.querySelector('i');
      if (icon) {
        icon.className = type === 'password' ? 'fa-solid fa-eye' : 'fa-solid fa-eye-slash';
      }
    });
  }

  // Funzione showError: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function showError(message) {
    if (errorAlert && errorMessage) {
      errorMessage.textContent = message;
      errorAlert.classList.remove('hidden');
    }
  }

  // Funzione hideError: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function hideError() {
    if (errorAlert) errorAlert.classList.add('hidden');
  }

  // Funzione setLoading: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function setLoading(loading) {
    if (submitBtn) submitBtn.disabled = loading;
    if (btnText) btnText.classList.toggle('hidden', loading);
    if (btnLoader) btnLoader.classList.toggle('hidden', !loading);
  }

  // ─── Crea UI 2FA ────────────────────────────────────────────────

  function showTwoFactorUI(tempToken) {
    // Nascondi form credenziali
    form.style.display = 'none';
    document.getElementById('submit-btn').parentElement.querySelector('.text-right')?.classList.add('hidden');

    // Crea container 2FA
    twoFaContainer = document.createElement('div');
    twoFaContainer.id = 'twofa-container';
    twoFaContainer.className = 'space-y-5 mt-2';
    twoFaContainer.innerHTML = `
      <div class="text-center mb-2">
        <i class="fa-solid fa-shield-halved text-4xl text-ferrari-primary mb-2"></i>
        <p class="text-body text-sm">Inserisci il codice a 6 cifre dalla tua app authenticator</p>
      </div>
      <div>
        <input type="text" id="twofa-code" class="input-ferrari w-full px-4 py-3 text-center text-2xl tracking-[8px]"
          placeholder="000000" maxlength="6" autocomplete="off" inputmode="numeric" pattern="[0-9]{6}">
      </div>
      <div class="flex items-center gap-2">
        <input type="checkbox" id="trust-device" class="w-4 h-4 accent-ferrari-primary">
        <label for="trust-device" class="text-body text-sm">Ricorda questo dispositivo per 3 giorni</label>
      </div>
      <button id="twofa-submit" class="btn-primary w-full py-3 text-base font-semibold">
        <span id="twofa-btn-text">Verifica</span>
        <span id="twofa-btn-loader" class="hidden">
          <i class="fa-solid fa-spinner fa-spin mr-2"></i>Verifica in corso...
        </span>
      </button>
      <button id="twofa-back" class="btn-tertiary w-full text-center text-sm">
        <i class="fa-solid fa-arrow-left mr-1"></i>Torna al login
      </button>
    `;

    form.parentElement.insertBefore(twoFaContainer, form.nextSibling);

    // Variabile codeInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const codeInput = document.getElementById('twofa-code');
    // Variabile trustCheckbox: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const trustCheckbox = document.getElementById('trust-device');
    // Variabile twoFaSubmit: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const twoFaSubmit = document.getElementById('twofa-submit');
    // Variabile twoFaBtnText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const twoFaBtnText = document.getElementById('twofa-btn-text');
    // Variabile twoFaBtnLoader: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const twoFaBtnLoader = document.getElementById('twofa-btn-loader');

    codeInput?.focus();

    // Torna al login
    document.getElementById('twofa-back')?.addEventListener('click', () => {
      twoFaContainer?.remove();
      form.style.display = '';
      document.getElementById('submit-btn').parentElement.querySelector('.text-right')?.classList.remove('hidden');
      twoFaContainer = null;
    });

    // Submit 2FA
    twoFaSubmit?.addEventListener('click', async () => {
      // Variabile code: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const code = codeInput?.value.trim();
      if (!code || code.length !== 6) {
        showError('Inserisci il codice a 6 cifre.');
        return;
      }

      hideError();
      twoFaBtnText.classList.add('hidden');
      twoFaBtnLoader.classList.remove('hidden');
      twoFaSubmit.disabled = true;

      try {
        await Auth.loginWith2Fa(tempToken, code, trustCheckbox?.checked || false);

        // Successo → redirect
        window.location.href = redirect ? decodeURIComponent(redirect) : '/index.html';
      } catch (err) {
        twoFaBtnText.classList.remove('hidden');
        twoFaBtnLoader.classList.add('hidden');
        twoFaSubmit.disabled = false;
        showError(err.message || 'Codice non valido. Riprova.');
      }
    });

    // Invio con Enter
    codeInput?.addEventListener('keypress', (e) => {
      if (e.key === 'Enter') twoFaSubmit?.click();
    });
  }

  // ─── Login Form ──────────────────────────────────────────────────

  if (!form) return;

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError();

    // Variabile email: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const email = emailInput?.value.trim();
    // Variabile password: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const password = passwordInput?.value;

    if (!email) { showError('Inserisci la tua email'); emailInput?.focus(); return; }
    if (!password) { showError('Inserisci la password'); passwordInput?.focus(); return; }

    setLoading(true);

    try {
      // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const result = await Auth.login(email, password);

      if (result.requiresTwoFactor) {
        // Mostra UI 2FA
        setLoading(false);
        showTwoFactorUI(result.tempToken);
        return;
      }

      // Login diretto riuscito
      window.location.href = redirect ? decodeURIComponent(redirect) : '/index.html';
    } catch (err) {
      setLoading(false);
      showError(err.message || 'Credenziali non valide');
    }
  });
});
