// register.js - Gestione registrazione

document.addEventListener('DOMContentLoaded', () => {
    // Se l'utente è già autenticato, redirect
    if (Auth.isAuthenticated()) {
        Router.redirectAfterLogin();
        return;
    }

    const form = document.getElementById('register-form');
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
        btnText.textContent = 'Registrazione in corso...';
        btnSpinner.classList.remove('hidden');

        const formData = {
            email: form.querySelector('[name="email"]').value,
            password: form.querySelector('[name="password"]').value,
            nome: form.querySelector('[name="nome"]').value,
            cognome: form.querySelector('[name="cognome"]').value,
            telefono: form.querySelector('[name="telefono"]').value || null,
            dataNascita: form.querySelector('[name="dataNascita"]').value 
                ? new Date(form.querySelector('[name="dataNascita"]').value).toISOString()
                : null
        };

        try {
            await API.register(formData);
            
            // Registrazione successo - redirect a login
            showToast('Registrazione completata! Effettua il login.', 'success');
            setTimeout(() => {
                window.location.href = '/login.html';
            }, 1000);
        } catch (error) {
            console.error('Register error:', error);
            
            // Mostra errore
            let message = 'Errore durante la registrazione';
            if (error.status === 400 && error.message) {
                message = error.message;
            } else if (error.message) {
                message = error.message;
            }
            
            errorMessage.textContent = message;
            errorMessage.classList.remove('hidden');
        } finally {
            // Riabilita bottone
            submitBtn.disabled = false;
            btnText.textContent = 'Registrati';
            btnSpinner.classList.add('hidden');
        }
    });
});
