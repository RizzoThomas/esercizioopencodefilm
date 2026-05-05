document.addEventListener('DOMContentLoaded', () => {
  if (!window.Auth) return;

  const form = document.getElementById('login-form');
  const emailInput = document.getElementById('email');
  const passwordInput = document.getElementById('password');
  const submitBtn = document.getElementById('submit-btn');
  const btnText = document.getElementById('btn-text');
  const btnLoader = document.getElementById('btn-loader');
  const errorAlert = document.getElementById('error-alert');
  const errorMessage = document.getElementById('error-message');
  const expiredAlert = document.getElementById('expired-alert');
  const togglePasswordBtn = document.getElementById('toggle-password');

  // Container 2FA (creato dinamicamente)
  let twoFaContainer = null;

  const params = new URLSearchParams(window.location.search);
  const expired = params.get('expired');
  const redirect = params.get('redirect');
  const socialToken = params.get('token');
  const socialRefresh = params.get('refresh');
  const socialError = params.get('error');

  // Social login callback: salva token e redirect
  if (socialToken && socialRefresh) {
    Auth.saveTokens(socialToken, socialRefresh);
    // Fetch user info
    fetch(`${API.baseUrl}/auth/me`, { headers: API.getAuthHeaders() })
      .then(r => r.json())
      .then(user => { if (user?.id) Auth.saveUser(user); })
      .catch(err => console.error('Failed to fetch user info after social login:', err))
      .finally(() => {
        const target = redirect ? decodeURIComponent(redirect) : '/index.html';
        window.location.href = target;
      });
    return;
  }

  // Errore social login
  if (socialError) {
    if (errorAlert && errorMessage) {
      errorMessage.textContent = socialError === 'no_email' 
        ? 'Il provider non ha fornito un\'email. Riprova con un altro metodo.'
        : 'Accesso con social network fallito. Riprova.';
      errorAlert.classList.remove('hidden');
    }
    // Pulisci URL
    const url = new URL(window.location);
    url.searchParams.delete('error');
    url.searchParams.delete('token');
    url.searchParams.delete('refresh');
    window.history.replaceState({}, '', url);
  }

  if (expired === 'true' && expiredAlert) {
    expiredAlert.classList.remove('hidden');
  }

  if (Auth.isLoggedIn()) {
    window.location.href = redirect ? decodeURIComponent(redirect) : '/index.html';
    return;
  }

  if (togglePasswordBtn && passwordInput) {
    togglePasswordBtn.addEventListener('click', () => {
      const type = passwordInput.type === 'password' ? 'text' : 'password';
      passwordInput.type = type;
      const icon = togglePasswordBtn.querySelector('i');
      if (icon) {
        icon.className = type === 'password' ? 'fa-solid fa-eye' : 'fa-solid fa-eye-slash';
      }
    });
  }

  function showError(message) {
    if (errorAlert && errorMessage) {
      errorMessage.textContent = message;
      errorAlert.classList.remove('hidden');
    }
  }

  function hideError() {
    if (errorAlert) errorAlert.classList.add('hidden');
  }

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

    const codeInput = document.getElementById('twofa-code');
    const trustCheckbox = document.getElementById('trust-device');
    const twoFaSubmit = document.getElementById('twofa-submit');
    const twoFaBtnText = document.getElementById('twofa-btn-text');
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

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError();

    const email = emailInput?.value.trim();
    const password = passwordInput?.value;

    if (!email) { showError('Inserisci la tua email'); emailInput?.focus(); return; }
    if (!password) { showError('Inserisci la password'); passwordInput?.focus(); return; }

    setLoading(true);

    try {
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
