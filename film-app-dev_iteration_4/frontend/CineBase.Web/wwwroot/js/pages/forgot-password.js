/**
 * Forgot Password Page
 */
// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', () => {
    // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const form = document.getElementById('forgot-form');
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
    // Variabile successMsg: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const successMsg = document.getElementById('success-message');

    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        // Variabile email: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const email = document.getElementById('email').value.trim();
        if (!email) return;

        setLoading(true);
        hideAlerts();

        try {
            // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
            const response = await fetch(`${API.baseUrl}/auth/forgot-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });

            // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const data = await response.json().catch(() => ({}));

            if (response.ok) {
                successMsg.textContent = data.message || 'Email inviata! Controlla la tua casella di posta.';
                successAlert.classList.remove('hidden');
                form.classList.add('hidden');
            } else {
                showError(data.error || 'Errore durante l\'invio. Riprova.');
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

    // Funzione hideAlerts: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function hideAlerts() {
        errorAlert.classList.add('hidden');
        successAlert.classList.add('hidden');
    }

    // Funzione showError: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    function showError(msg) {
        errorMsg.textContent = msg;
        errorAlert.classList.remove('hidden');
    }
});
