// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', () => {
  if (!window.Auth) return;

  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById('register-form');
  // Variabile nomeInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nomeInput = document.getElementById('nome');
  // Variabile cognomeInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cognomeInput = document.getElementById('cognome');
  // Variabile emailInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const emailInput = document.getElementById('email');
  // Variabile telefonoInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const telefonoInput = document.getElementById('telefono');
  // Variabile passwordInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const passwordInput = document.getElementById('password');
  // Variabile confirmPasswordInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const confirmPasswordInput = document.getElementById('confirm-password');
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
  // Variabile successAlert: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const successAlert = document.getElementById('success-alert');
  // Variabile togglePasswordBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const togglePasswordBtn = document.getElementById('toggle-password');
  // Variabile toggleConfirmPasswordBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const toggleConfirmPasswordBtn = document.getElementById('toggle-confirm-password');
  // Variabile strengthBar: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const strengthBar = document.getElementById('strength-bar');
  // Variabile strengthText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const strengthText = document.getElementById('strength-text');
  // Variabile confirmError: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const confirmError = document.getElementById('confirm-error');

  if (Auth.isLoggedIn()) {
    window.location.href = '/index.html';
    return;
  }

  if (!form) return;

  // Funzione togglePasswordVisibility: commuta uno stato visivo o funzionale tra due modalità. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function togglePasswordVisibility(input, btn) {
    if (!input || !btn) return;
    // Variabile type: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const type = input.type === 'password' ? 'text' : 'password';
    input.type = type;
    // Variabile icon: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const icon = btn.querySelector('i');
    if (icon) icon.className = type === 'password' ? 'fa-solid fa-eye' : 'fa-solid fa-eye-slash';
  }

  if (togglePasswordBtn && passwordInput) {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    togglePasswordBtn.addEventListener('click', () => {
      togglePasswordVisibility(passwordInput, togglePasswordBtn);
    });
  }

  if (toggleConfirmPasswordBtn && confirmPasswordInput) {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    toggleConfirmPasswordBtn.addEventListener('click', () => {
      togglePasswordVisibility(confirmPasswordInput, toggleConfirmPasswordBtn);
    });
  }

  // Funzione checkPasswordStrength: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function checkPasswordStrength(password) {
    // Variabile strength: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let strength = 0;
    // Variabile feedback: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

  // Funzione updatePasswordStrength: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function updatePasswordStrength() {
    if (!strengthBar || !strengthText || !passwordInput) return;
    const { strength, feedback } = checkPasswordStrength(passwordInput.value);
    
    strengthBar.style.width = `${strength}%`;
    
    if (strength === 0) {
      strengthBar.className = 'h-full bg-ferrari-semantic-warning transition-all';
      strengthText.textContent = '';
    } else if (strength === 25) {
      strengthBar.className = 'h-full bg-ferrari-semantic-warning transition-all';
      strengthText.textContent = `Password debole: ${feedback.join(', ')}`;
    } else if (strength === 50) {
      strengthBar.className = 'h-full bg-amber-500 transition-all';
      strengthText.textContent = 'Password discreta';
    } else if (strength === 75) {
      strengthBar.className = 'h-full bg-emerald-500 transition-all';
      strengthText.textContent = 'Password buona';
    } else {
      strengthBar.className = 'h-full bg-emerald-500 transition-all';
      strengthText.textContent = 'Password ottima';
    }
  }

  if (passwordInput) {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    passwordInput.addEventListener('input', updatePasswordStrength);
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
    if (errorAlert) {
      errorAlert.classList.add('hidden');
    }
  }

  // Funzione setLoading: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function setLoading(loading) {
    if (submitBtn) submitBtn.disabled = loading;
    if (btnText) btnText.classList.toggle('hidden', loading);
    if (btnLoader) btnLoader.classList.toggle('hidden', !loading);
  }

  // Funzione validateEmail: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
  function validateEmail(email) {
    // Variabile emailRegex: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError();
    if (confirmError) confirmError.classList.add('hidden');

    // Variabile nome: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const nome = nomeInput?.value.trim();
    // Variabile cognome: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const cognome = cognomeInput?.value.trim();
    // Variabile email: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const email = emailInput?.value.trim();
    // Variabile telefono: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const telefono = telefonoInput?.value.trim();
    // Variabile password: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const password = passwordInput?.value;
    // Variabile confirmPassword: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
      // Variabile registerData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
