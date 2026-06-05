/**
 * Enable 2FA Page
 * Carica QR code da backend, verifica codice e attiva 2FA.
 */
// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', () => {
    // Variabile loadingState: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const loadingState = document.getElementById('loading-state');
    // Variabile setupContent: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const setupContent = document.getElementById('setup-content');
    // Variabile qrImage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const qrImage = document.getElementById('qr-image');
    // Variabile manualKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const manualKey = document.getElementById('manual-key');
    // Variabile errorAlert: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const errorAlert = document.getElementById('error-alert');
    // Variabile errorMsg: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const errorMsg = document.getElementById('error-message');
    // Variabile successAlert: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const successAlert = document.getElementById('success-alert');
    // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const form = document.getElementById('verify-form');
    // Variabile submitBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const submitBtn = document.getElementById('submit-btn');
    // Variabile btnText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const btnText = document.getElementById('btn-text');
    // Variabile btnLoader: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const btnLoader = document.getElementById('btn-loader');

    // Carica setup 2FA
    loadSetup();

    // Funzione loadSetup: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
    async function loadSetup() {
        try {
            // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
            const response = await fetch(`${API.baseUrl}/auth/2fa/setup`, {
                method: 'POST',
                headers: API.getAuthHeaders()
            });

            if (!response.ok) {
                // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
                const data = await response.json().catch(() => ({}));
                showError(data.error || 'Errore durante il caricamento del setup 2FA.');
                loadingState.classList.add('hidden');
                return;
            }

            // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const data = await response.json();
            qrImage.src = data.qrCodeBase64;
            manualKey.textContent = data.manualKey;
            loadingState.classList.add('hidden');
            setupContent.classList.remove('hidden');
        } catch (err) {
            showError('Impossibile connettersi al server.');
            loadingState.classList.add('hidden');
        }
    }

    // Copy chiave manuale
    document.getElementById('copy-key-btn')?.addEventListener('click', async () => {
        // Variabile key: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const key = manualKey.textContent.replace(/\s/g, '');
        try {
            await navigator.clipboard.writeText(key);
            // Variabile btn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const btn = document.getElementById('copy-key-btn');
            btn.innerHTML = '<i class="fa-solid fa-check mr-1"></i>Copiata!';
            setTimeout(() => {
                btn.innerHTML = '<i class="fa-solid fa-copy mr-1"></i>Copia chiave';
            }, 2000);
        } catch {
            // fallback
            const range = document.createRange();
            range.selectNode(manualKey);
            window.getSelection()?.removeAllRanges();
            window.getSelection()?.addRange(range);
        }
    });

    // Verifica codice
    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        // Variabile code: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const code = document.getElementById('code').value.trim();
        if (code.length !== 6) return;

        setLoading(true);
        errorAlert.classList.add('hidden');

        try {
            // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
            const response = await fetch(`${API.baseUrl}/auth/2fa/enable`, {
                method: 'POST',
                headers: API.getAuthHeaders(),
                body: JSON.stringify({ code })
            });

            // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
            const data = await response.json().catch(() => ({}));

            if (response.ok) {
                successAlert.classList.remove('hidden');
                form.classList.add('hidden');
            } else {
                showError(data.error || 'Codice non valido. Riprova.');
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
