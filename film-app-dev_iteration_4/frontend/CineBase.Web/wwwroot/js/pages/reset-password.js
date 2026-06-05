/**
 * Reset Password Page
 * Legge il token dall'URL e invia la nuova password.
 */
// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', () => {
    // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const params = new URLSearchParams(window.location.search);
    // Variabile token: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const token = params.get('token');

    // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const form = document.getElementById('reset-form');
    // Variabile submitBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const submitBtn = document.getElementById('submit-btn');
    // Variabile btnText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const btnText = document.getElementById('btn-text');
    // Variabile btnLoader: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const btnLoader = document.getElementById('btn-loader');
    // Variabile errorAlert: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const errorAlert = document.getElementById('error-alert');
    // Variabile errorMsg: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const errorMsg = document.getElementById('error-message');
    // Variabile successAlert: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const successAlert = document.getElementById('success-alert');
    // Variabile loginLink: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const loginLink = document.getElementById('login-link');
    // Variabile loginLinkText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const loginLinkText = document.getElementById('login-link-text');
    // Variabile toggleBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const toggleBtn = document.getElementById('toggle-password');
    // Variabile passwordInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const passwordInput = document.getElementById('password');

    // Se non c'è token, mostra errore
    if (!token) {
        errorMsg.textContent = 'Link di reset non valido. Richiedi un nuovo reset.';
        errorAlert.classList.remove('hidden');
        form.classList.add('hidden');
        return;
    }

    // Toggle password visibility
    toggleBtn?.addEventListener('click', () => {
        // Variabile isPassword: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const isPassword = passwordInput.type === 'password';
        passwordInput.type = isPassword ? 'text' : 'password';
        toggleBtn.querySelector('i').className = isPassword ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye';
    });

    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        // Variabile password: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const password = passwordInput.value.trim();
        if (password.length < 8) {
            showError('La password deve essere di almeno 8 caratteri.');
            return;
        }

        setLoading(true);
        errorAlert.classList.add('hidden');

        try {
            // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
            const response = await fetch(`${API.baseUrl}/auth/reset-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ token, newPassword: password })
            });

            // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const data = await response.json().catch(() => ({}));

            if (response.ok) {
                successAlert.classList.remove('hidden');
                form.classList.add('hidden');
                loginLink.classList.remove('hidden');
                loginLinkText.classList.remove('hidden');
            } else {
                showError(data.error || 'Token non valido o scaduto. Richiedi un nuovo reset.');
            }
        } catch (err) {
            showError('Impossibile connettersi al server.');
        } finally {
            setLoading(false);
        }
    });

    // Funzione setLoading: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function setLoading(loading) {
        btnText.classList.toggle('hidden', loading);
        btnLoader.classList.toggle('hidden', !loading);
        submitBtn.disabled = loading;
    }

    // Funzione showError: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function showError(msg) {
        errorMsg.textContent = msg;
        errorAlert.classList.remove('hidden');
    }
});
