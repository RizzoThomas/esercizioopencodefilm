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

  const params = new URLSearchParams(window.location.search);
  const expired = params.get('expired');
  const redirect = params.get('redirect');

  if (expired === 'true' && expiredAlert) {
    expiredAlert.classList.remove('hidden');
  }

  if (Auth.isLoggedIn()) {
    if (redirect) {
      window.location.href = redirect;
    } else {
      window.location.href = '/index.html';
    }
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
    if (errorAlert) {
      errorAlert.classList.add('hidden');
    }
  }

  function setLoading(loading) {
    if (submitBtn) submitBtn.disabled = loading;
    if (btnText) btnText.classList.toggle('hidden', loading);
    if (btnLoader) btnLoader.classList.toggle('hidden', !loading);
  }

  if (!form) return;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError();

    const email = emailInput?.value.trim();
    const password = passwordInput?.value;

    if (!email) {
      showError('Inserisci la tua email');
      emailInput?.focus();
      return;
    }

    if (!password) {
      showError('Inserisci la password');
      passwordInput?.focus();
      return;
    }

    setLoading(true);

    try {
      await Auth.login(email, password);
      
      if (redirect) {
        const decodedRedirect = decodeURIComponent(redirect);
        window.location.href = decodedRedirect;
      } else {
        window.location.href = '/index.html';
      }
    } catch (err) {
      setLoading(false);
      showError(err.message || 'Credenziali non valide');
    }
  });
});
