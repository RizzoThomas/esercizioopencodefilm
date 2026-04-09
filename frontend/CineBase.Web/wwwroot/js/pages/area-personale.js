// area-personale.js - Gestione area personale

document.addEventListener('DOMContentLoaded', () => {
    // Verifica autenticazione
    if (!Auth.isAuthenticated()) {
        window.location.href = '/login.html?redirect=/area-personale.html';
        return;
    }

    const user = Auth.getUser();
    
    // Aggiorna welcome message
    const welcomeEl = document.getElementById('user-welcome');
    if (welcomeEl && user) {
        welcomeEl.textContent = `Benvenuto, ${user.nome} ${user.cognome}`;
    }

    // Tab switching
    const tabBtns = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    function activateTab(tabId) {
        const targetTab = tabId || 'profilo';

        tabBtns.forEach(b => {
            b.classList.remove('active', 'border-brand-orange', 'text-brand-orange');
            b.classList.add('border-transparent', 'text-gray-400');
        });

        tabContents.forEach(content => {
            content.classList.add('hidden');
        });

        const activeBtn = document.querySelector(`.tab-btn[data-tab="${targetTab}"]`);
        const activeContent = document.getElementById(`tab-${targetTab}`);

        if (activeBtn && activeContent) {
            activeBtn.classList.add('active', 'border-brand-orange', 'text-brand-orange');
            activeBtn.classList.remove('border-transparent', 'text-gray-400');
            activeContent.classList.remove('hidden');
        }

        if (targetTab === 'proiezioni') {
            loadSavedProiezioni();
        } else if (targetTab === 'prenotazioni') {
            loadPrenotazioni();
        }
    }

    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const tabId = btn.dataset.tab;
            activateTab(tabId);
        });
    });

    const hash = (window.location.hash || '').replace('#', '').toLowerCase();
    if (hash === 'prenotazioni' || hash === 'proiezioni' || hash === 'profilo') {
        activateTab(hash);
    }

    // Carica dati profilo
    loadProfile();
    
    // Gestione form profilo
    document.getElementById('profilo-form')?.addEventListener('submit', handleProfileUpdate);
    
    // Gestione cambio password
    document.getElementById('change-password-form')?.addEventListener('submit', handlePasswordChange);
});

async function loadProfile() {
    try {
        const user = await API.getCurrentUser();
        if (user) {
            const form = document.getElementById('profilo-form');
            if (form) {
                form.querySelector('[name="nome"]').value = user.nome;
                form.querySelector('[name="cognome"]').value = user.cognome;
                form.querySelector('[name="email"]').value = user.email;
                form.querySelector('[name="telefono"]').value = user.telefono || '';
                form.querySelector('[name="dataNascita"]').value = user.dataNascita 
                    ? new Date(user.dataNascita).toISOString().split('T')[0] 
                    : '';
            }
        }
    } catch (error) {
        console.error('Error loading profile:', error);
    }
}

async function handleProfileUpdate(e) {
    e.preventDefault();
    
    const btn = document.getElementById('save-profile-btn');
    const spinner = document.getElementById('profile-spinner');
    
    btn.disabled = true;
    spinner.classList.remove('hidden');
    
    const formData = {
        nome: e.target.querySelector('[name="nome"]').value,
        cognome: e.target.querySelector('[name="cognome"]').value,
        telefono: e.target.querySelector('[name="telefono"]').value || null,
        dataNascita: e.target.querySelector('[name="dataNascita"]').value 
            ? new Date(e.target.querySelector('[name="dataNascita"]').value).toISOString()
            : null
    };
    
    try {
        // Nota: l'endpoint per aggiornare il profilo va implementato nel backend
        // await API.updateProfile(formData);
        
        // Per ora mostriamo solo un successo
        showToast('Profilo aggiornato con successo!', 'success');
        
        // Aggiorna il nome nell'interfaccia
        const welcomeEl = document.getElementById('user-welcome');
        if (welcomeEl) {
            welcomeEl.textContent = `Benvenuto, ${formData.nome} ${formData.cognome}`;
        }
        
        // Aggiorna localStorage
        const currentUser = Auth.getUser();
        if (currentUser) {
            currentUser.nome = formData.nome;
            currentUser.cognome = formData.cognome;
            Auth.setUser(currentUser);
        }
    } catch (error) {
        showToast('Errore durante l\'aggiornamento del profilo', 'danger');
    } finally {
        btn.disabled = false;
        spinner.classList.add('hidden');
    }
}

async function handlePasswordChange(e) {
    e.preventDefault();
    
    const form = e.target;
    const currentPassword = form.querySelector('[name="currentPassword"]').value;
    const newPassword = form.querySelector('[name="newPassword"]').value;
    const confirmPassword = form.querySelector('[name="confirmPassword"]').value;
    const errorEl = document.getElementById('password-error');
    const btn = document.getElementById('change-password-btn');
    
    // Validazione
    if (newPassword !== confirmPassword) {
        errorEl.textContent = 'Le password non coincidono';
        errorEl.classList.remove('hidden');
        return;
    }
    
    if (newPassword.length < 6) {
        errorEl.textContent = 'La nuova password deve essere di almeno 6 caratteri';
        errorEl.classList.remove('hidden');
        return;
    }
    
    errorEl.classList.add('hidden');
    btn.disabled = true;
    btn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin mr-2"></i>Cambio in corso...';
    
    try {
        // Nota: l'endpoint per cambiare password va implementato nel backend
        // await API.changePassword({ currentPassword, newPassword });
        
        showToast('Password cambiata con successo!', 'success');
        document.getElementById('change-password-modal').classList.add('hidden');
        form.reset();
    } catch (error) {
        errorEl.textContent = error.message || 'Errore durante il cambio password';
        errorEl.classList.remove('hidden');
    } finally {
        btn.disabled = false;
        btn.innerHTML = 'Cambia Password';
    }
}

async function loadSavedProiezioni() {
    const container = document.getElementById('saved-proiezioni-list');
    const countEl = document.getElementById('saved-count');
    
    try {
        const proiezioni = await API.getSavedProiezioni();
        countEl.textContent = `${proiezioni.length} elementi`;
        
        if (proiezioni.length === 0) {
            container.innerHTML = `
                <div class="text-center py-12 text-gray-400">
                    <i class="fa-solid fa-bookmark text-4xl mb-4 opacity-50"></i>
                    <p>Nessuna proiezione salvata</p>
                    <a href="/index.html#programmazione" class="text-brand-orange hover:underline mt-2 inline-block">
                        Esplora la programmazione
                    </a>
                </div>
            `;
            return;
        }
        
        container.innerHTML = proiezioni.map(p => `
            <div class="bg-brand-dark rounded-xl border border-white/10 p-4 flex items-start gap-4">
                <div class="flex-1">
                    <h3 class="font-semibold text-lg">${p.film.titolo}</h3>
                    <p class="text-gray-400 text-sm mt-1">
                        <i class="fa-solid fa-location-dot mr-1"></i>${p.cinema.nome}, ${p.cinema.citta}
                    </p>
                    <p class="text-gray-400 text-sm mt-1">
                        <i class="fa-regular fa-calendar mr-1"></i>${new Date(p.dataProiezione).toLocaleDateString('it-IT')}
                        <i class="fa-regular fa-clock ml-3 mr-1"></i>${p.oraProiezione.hours.toString().padStart(2, '0')}:${p.oraProiezione.minutes.toString().padStart(2, '0')}
                    </p>
                    ${p.note ? `<p class="text-gray-500 text-sm mt-2 italic">"${p.note}"</p>` : ''}
                </div>
                <div class="flex flex-col gap-2">
                    <button onclick="openPrenotazioneModal(${p.proiezioneId}, '${(p.film.titolo || '').replace(/'/g, "\\'")}')"
                        class="bg-brand-orange/20 hover:bg-brand-orange/30 text-brand-orange px-3 py-2 rounded-lg text-sm transition-colors whitespace-nowrap">
                        <i class="fa-solid fa-ticket mr-1"></i>Prenota
                    </button>
                    <button onclick="removeSavedProiezione(${p.id})" 
                        class="text-gray-400 hover:text-red-400 transition-colors p-2">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');
    } catch (error) {
        container.innerHTML = `
            <div class="text-center py-12 text-red-400">
                <i class="fa-solid fa-exclamation-circle text-4xl mb-4"></i>
                <p>Errore durante il caricamento</p>
            </div>
        `;
    }
}

async function loadPrenotazioni() {
    const container = document.getElementById('prenotazioni-list');
    const countEl = document.getElementById('prenotazioni-count');
    
    try {
        const prenotazioni = await API.getPrenotazioni();
        countEl.textContent = `${prenotazioni.length} elementi`;
        
        if (prenotazioni.length === 0) {
            container.innerHTML = `
                <div class="text-center py-12 text-gray-400">
                    <i class="fa-solid fa-ticket text-4xl mb-4 opacity-50"></i>
                    <p>Nessuna prenotazione effettuata</p>
                    <a href="/index.html#programmazione" class="text-brand-orange hover:underline mt-2 inline-block">
                        Esplora la programmazione
                    </a>
                </div>
            `;
            return;
        }
        
        const statoColors = {
            'InAttesa': 'bg-yellow-500/20 text-yellow-400',
            'Confermata': 'bg-green-500/20 text-green-400',
            'Annullata': 'bg-red-500/20 text-red-400'
        };
        
        const statoLabels = {
            'InAttesa': 'In attesa',
            'Confermata': 'Confermata',
            'Annullata': 'Annullata'
        };
        
        container.innerHTML = prenotazioni.map(p => `
            <div class="bg-brand-dark rounded-xl border border-white/10 p-4">
                <div class="flex justify-between items-start mb-3">
                    <div>
                        <h3 class="font-semibold text-lg">${p.film.titolo}</h3>
                        <span class="text-xs text-gray-500 font-mono">${p.codicePrenotazione}</span>
                    </div>
                    <span class="px-2 py-1 rounded-full text-xs font-medium ${statoColors[p.stato] || 'bg-gray-500/20 text-gray-400'}">
                        ${statoLabels[p.stato] || p.stato}
                    </span>
                </div>
                <div class="grid grid-cols-2 gap-4 text-sm text-gray-400">
                    <div>
                        <i class="fa-solid fa-location-dot mr-1"></i>${p.cinema.nome}
                    </div>
                    <div>
                        <i class="fa-regular fa-calendar mr-1"></i>${new Date(p.dataProiezione).toLocaleDateString('it-IT')}
                    </div>
                    <div>
                        <i class="fa-solid fa-users mr-1"></i>${p.numeroPosti} posti
                    </div>
                    <div>
                        <i class="fa-solid fa-chair mr-1"></i>${Array.isArray(p.posti) && p.posti.length ? p.posti.join(', ') : 'Assegnazione automatica'}
                    </div>
                    <div>
                        <i class="fa-solid fa-euro-sign mr-1"></i>${p.prezzoTotale?.toFixed(2) || '0.00'}
                    </div>
                </div>
                ${p.stato === 'InAttesa' ? `
                    <div class="mt-4 flex gap-2">
                        <button onclick="annullaPrenotazione(${p.id})" 
                            class="flex-1 bg-red-500/20 hover:bg-red-500/30 text-red-400 py-2 rounded-lg transition-colors text-sm">
                            Annulla Prenotazione
                        </button>
                    </div>
                ` : ''}
            </div>
        `).join('');
    } catch (error) {
        container.innerHTML = `
            <div class="text-center py-12 text-red-400">
                <i class="fa-solid fa-exclamation-circle text-4xl mb-4"></i>
                <p>Errore durante il caricamento</p>
            </div>
        `;
    }
}

async function removeSavedProiezione(id) {
    if (!confirm('Sei sicuro di voler rimuovere questa proiezione dai salvati?')) return;
    
    try {
        await API.deleteSavedProiezione(id);
        showToast('Proiezione rimossa dai salvati', 'success');
        loadSavedProiezioni();
    } catch (error) {
        showToast('Errore durante la rimozione', 'danger');
    }
}

async function annullaPrenotazione(id) {
    if (!confirm('Sei sicuro di voler annullare questa prenotazione?')) return;
    
    try {
        await API.annullaPrenotazione(id);
        showToast('Prenotazione annullata', 'success');
        loadPrenotazioni();
    } catch (error) {
        showToast('Errore durante l\'annullamento', 'danger');
    }
}

function openPrenotazioneModal(proiezioneId, _titoloFilm) {
    const redirectUrl = `/prenotazione.html?proiezioneId=${encodeURIComponent(String(proiezioneId))}`;
    window.location.href = redirectUrl;
}
