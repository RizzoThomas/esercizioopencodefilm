/**
 * Forgot Password Page
 */
document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('forgot-form');
    const submitBtn = document.getElementById('submit-btn');
    const btnText = document.getElementById('btn-text');
    const btnLoader = document.getElementById('btn-loader');
    const errorAlert = document.getElementById('error-alert');
    const errorMsg = document.getElementById('error-message');
    const successAlert = document.getElementById('success-alert');
    const successMsg = document.getElementById('success-message');

    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const email = document.getElementById('email').value.trim();
        if (!email) return;

        setLoading(true);
        hideAlerts();

        try {
            const response = await fetch(`${API.baseUrl}/auth/forgot-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });

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

    function setLoading(loading) {
        btnText.classList.toggle('hidden', loading);
        btnLoader.classList.toggle('hidden', !loading);
        submitBtn.disabled = loading;
    }

    function hideAlerts() {
        errorAlert.classList.add('hidden');
        successAlert.classList.add('hidden');
    }

    function showError(msg) {
        errorMsg.textContent = msg;
        errorAlert.classList.remove('hidden');
    }
});
