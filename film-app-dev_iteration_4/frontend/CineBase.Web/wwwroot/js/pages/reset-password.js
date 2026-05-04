/**
 * Reset Password Page
 * Legge il token dall'URL e invia la nuova password.
 */
document.addEventListener('DOMContentLoaded', () => {
    const params = new URLSearchParams(window.location.search);
    const token = params.get('token');

    const form = document.getElementById('reset-form');
    const submitBtn = document.getElementById('submit-btn');
    const btnText = document.getElementById('btn-text');
    const btnLoader = document.getElementById('btn-loader');
    const errorAlert = document.getElementById('error-alert');
    const errorMsg = document.getElementById('error-message');
    const successAlert = document.getElementById('success-alert');
    const loginLink = document.getElementById('login-link');
    const loginLinkText = document.getElementById('login-link-text');
    const toggleBtn = document.getElementById('toggle-password');
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
        const isPassword = passwordInput.type === 'password';
        passwordInput.type = isPassword ? 'text' : 'password';
        toggleBtn.querySelector('i').className = isPassword ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye';
    });

    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const password = passwordInput.value.trim();
        if (password.length < 8) {
            showError('La password deve essere di almeno 8 caratteri.');
            return;
        }

        setLoading(true);
        errorAlert.classList.add('hidden');

        try {
            const response = await fetch(`${API.baseUrl}/auth/reset-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ token, newPassword: password })
            });

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
