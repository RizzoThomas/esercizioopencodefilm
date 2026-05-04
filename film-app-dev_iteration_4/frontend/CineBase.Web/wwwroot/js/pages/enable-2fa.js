/**
 * Enable 2FA Page
 * Carica QR code da backend, verifica codice e attiva 2FA.
 */
document.addEventListener('DOMContentLoaded', () => {
    const loadingState = document.getElementById('loading-state');
    const setupContent = document.getElementById('setup-content');
    const qrImage = document.getElementById('qr-image');
    const manualKey = document.getElementById('manual-key');
    const errorAlert = document.getElementById('error-alert');
    const errorMsg = document.getElementById('error-message');
    const successAlert = document.getElementById('success-alert');
    const form = document.getElementById('verify-form');
    const submitBtn = document.getElementById('submit-btn');
    const btnText = document.getElementById('btn-text');
    const btnLoader = document.getElementById('btn-loader');

    // Carica setup 2FA
    loadSetup();

    async function loadSetup() {
        try {
            const response = await fetch(`${API.baseUrl}/auth/2fa/setup`, {
                method: 'POST',
                headers: API.getAuthHeaders()
            });

            if (!response.ok) {
                const data = await response.json().catch(() => ({}));
                showError(data.error || 'Errore durante il caricamento del setup 2FA.');
                loadingState.classList.add('hidden');
                return;
            }

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
        const key = manualKey.textContent.replace(/\s/g, '');
        try {
            await navigator.clipboard.writeText(key);
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
        const code = document.getElementById('code').value.trim();
        if (code.length !== 6) return;

        setLoading(true);
        errorAlert.classList.add('hidden');

        try {
            const response = await fetch(`${API.baseUrl}/auth/2fa/enable`, {
                method: 'POST',
                headers: API.getAuthHeaders(),
                body: JSON.stringify({ code })
            });

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

    function setLoading(loading) {
        btnText.classList.toggle('hidden', loading);
        btnLoader.classList.toggle('hidden', !loading);
        submitBtn.disabled = loading;
    }

    function showError(msg) {
        errorMsg.textContent = msg;
        errorAlert.classList.remove('hidden');
    }
});
