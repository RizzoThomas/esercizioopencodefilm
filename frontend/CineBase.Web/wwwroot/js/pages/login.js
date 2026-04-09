// login.js - Gestione login

document.addEventListener('DOMContentLoaded', () => {
    // Se l'utente è già autenticato, redirect
    if (Auth.isAuthenticated()) {
        Router.redirectAfterLogin();
        return;
    }

    const form = document.getElementById('login-form');
    const errorMessage = document.getElementById('error-message');
    const submitBtn = document.getElementById('submit-btn');
    const btnText = document.getElementById('btn-text');
    const btnSpinner = document.getElementById('btn-spinner');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        // Nascondi messaggio errore precedente
        errorMessage.classList.add('hidden');
        
        // Disabilita bottone e mostra spinner
        submitBtn.disabled = true;
        btnText.textContent = 'Accesso in corso...';
        btnSpinner.classList.remove('hidden');

    const email = form.querySelector('[name="email"]').value;
    const password = form.querySelector('[name="password"]').value;

    // Basic validation
    if (!email || !password) {
      errorMessage.textContent = 'Email e password sono obbligatorie';
      errorMessage.classList.remove('hidden');
      submitBtn.disabled = false;
      btnText.textContent = 'Accedi';
      btnSpinner.classList.add('hidden');
      return;
    }

        try {
            await Auth.login(email, password);
            
            // Login successo - redirect
            showToast('Login effettuato con successo!', 'success');
            setTimeout(() => {
                Router.redirectAfterLogin();
            }, 500);
        } catch (error) {
            console.error('Login error:', error);
            
            // Mostra errore
            let message = 'Errore durante il login';
            if (error.status === 401) {
                message = 'Email o password non validi';
            } else if (error.message) {
                message = error.message;
            }
            
            errorMessage.textContent = message;
            errorMessage.classList.remove('hidden');
        } finally {
            // Riabilita bottone
            submitBtn.disabled = false;
            btnText.textContent = 'Accedi';
            btnSpinner.classList.add('hidden');
        }
    });
});
