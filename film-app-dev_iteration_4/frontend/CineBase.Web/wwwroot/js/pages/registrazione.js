document.addEventListener('DOMContentLoaded', () => {
  if (!window.Auth) return;

  const form = document.getElementById('register-form');
  const nomeInput = document.getElementById('nome');
  const cognomeInput = document.getElementById('cognome');
  const emailInput = document.getElementById('email');
  const telefonoInput = document.getElementById('telefono');
  const passwordInput = document.getElementById('password');
  const confirmPasswordInput = document.getElementById('confirm-password');
  const submitBtn = document.getElementById('submit-btn');
  const btnText = document.getElementById('btn-text');
  const btnLoader = document.getElementById('btn-loader');
  const errorAlert = document.getElementById('error-alert');
  const errorMessage = document.getElementById('error-message');
  const successAlert = document.getElementById('success-alert');
  const togglePasswordBtn = document.getElementById('toggle-password');
  const toggleConfirmPasswordBtn = document.getElementById('toggle-confirm-password');
  const strengthBar = document.getElementById('strength-bar');
  const strengthText = document.getElementById('strength-text');
  const confirmError = document.getElementById('confirm-error');

  if (Auth.isLoggedIn()) {
    window.location.href = '/index.html';
    return;
  }

  if (!form) return;

  function togglePasswordVisibility(input, btn) {
    if (!input || !btn) return;
    const type = input.type === 'password' ? 'text' : 'password';
    input.type = type;
    const icon = btn.querySelector('i');
    if (icon) icon.className = type === 'password' ? 'fa-solid fa-eye' : 'fa-solid fa-eye-slash';
  }

  if (togglePasswordBtn && passwordInput) {
    togglePasswordBtn.addEventListener('click', () => {
      togglePasswordVisibility(passwordInput, togglePasswordBtn);
    });
  }

  if (toggleConfirmPasswordBtn && confirmPasswordInput) {
    toggleConfirmPasswordBtn.addEventListener('click', () => {
      togglePasswordVisibility(confirmPasswordInput, toggleConfirmPasswordBtn);
    });
  }

  function checkPasswordStrength(password) {
    let strength = 0;
    let feedback = [];

    if (password.length >= 8) {
      strength += 25;
    } else {
      feedback.push('almeno 8 caratteri');
    }

    if (/[a-z]/.test(password)) {
      strength += 25;
    } else {
      feedback.push('lettere minuscole');
    }

    if (/[A-Z]/.test(password)) {
      strength += 25;
    } else {
      feedback.push('lettere maiuscole');
    }

    if (/\d/.test(password)) {
      strength += 25;
    } else {
      feedback.push('numeri');
    }

    return { strength, feedback };
  }

  function updatePasswordStrength() {
    if (!strengthBar || !strengthText || !passwordInput) return;
    const { strength, feedback } = checkPasswordStrength(passwordInput.value);
    
    strengthBar.style.width = `${strength}%`;
    
    if (strength === 0) {
      strengthBar.className = 'h-full bg-brand-error transition-all';
      strengthText.textContent = '';
    } else if (strength === 25) {
      strengthBar.className = 'h-full bg-brand-error transition-all';
      strengthText.textContent = `Password debole: ${feedback.join(', ')}`;
    } else if (strength === 50) {
      strengthBar.className = 'h-full bg-brand-amber-500 transition-all';
      strengthText.textContent = 'Password discreta';
    } else if (strength === 75) {
      strengthBar.className = 'h-full bg-brand-emerald-500 transition-all';
      strengthText.textContent = 'Password buona';
    } else {
      strengthBar.className = 'h-full bg-brand-emerald transition-all';
      strengthText.textContent = 'Password ottima';
    }
  }

  if (passwordInput) {
    passwordInput.addEventListener('input', updatePasswordStrength);
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

  function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError();
    if (confirmError) confirmError.classList.add('hidden');

    const nome = nomeInput?.value.trim();
    const cognome = cognomeInput?.value.trim();
    const email = emailInput?.value.trim();
    const telefono = telefonoInput?.value.trim();
    const password = passwordInput?.value;
    const confirmPassword = confirmPasswordInput?.value;

    if (!nome) {
      showError('Inserisci il nome');
      nomeInput?.focus();
      return;
    }

    if (!cognome) {
      showError('Inserisci il cognome');
      cognomeInput?.focus();
      return;
    }

    if (!email) {
      showError('Inserisci l\'email');
      emailInput?.focus();
      return;
    }

    if (!validateEmail(email)) {
      showError('Inserisci un\'email valida');
      emailInput?.focus();
      return;
    }

    if (!password) {
      showError('Inserisci la password');
      passwordInput?.focus();
      return;
    }

    if (password.length < 8) {
      showError('La password deve essere di almeno 8 caratteri');
      passwordInput?.focus();
      return;
    }

    if (password !== confirmPassword) {
      if (confirmError) {
        confirmError.textContent = 'Le password non coincidono';
        confirmError.classList.remove('hidden');
      }
      confirmPasswordInput?.focus();
      return;
    }

    setLoading(true);

    try {
      const registerData = {
        email,
        password,
        nome,
        cognome
      };

      if (telefono) {
        registerData.telefono = telefono;
      }

      await Auth.register(registerData);
      
      if (successAlert) successAlert.classList.remove('hidden');
      
      setTimeout(() => {
        window.location.href = '/index.html';
      }, 1500);
    } catch (err) {
      setLoading(false);
      showError(err.message || 'Errore durante la registrazione');
    }
  });
});
