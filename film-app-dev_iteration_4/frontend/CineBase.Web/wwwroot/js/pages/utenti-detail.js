let userId = null;
let userData = null;
let movementsData = [];
let bigliettiData = [];
let ordiniData = [];

document.addEventListener('DOMContentLoaded', async () => {
  const params = new URLSearchParams(window.location.search);
  userId = parseInt(params.get('id'));
  if (!userId) {
    showToast('ID utente mancante', 'danger');
    return;
  }
  await loadUserData();
  await loadMovements();
  await loadBiglietti();
  await loadOrdini();
});

async function loadUserData() {
  try {
    const utenti = normalizeCollection(await API.getUtenti());
    userData = utenti.find(u => u.id === userId);
    if (!userData) { showToast('Utente non trovato', 'danger'); return; }
    renderHeader();
    fillEditForm();
  } catch { showToast('Errore caricamento utente', 'danger'); }
}

function renderHeader() {
  const first = (userData.nome || 'U').charAt(0);
  const second = (userData.cognome || 'N').charAt(0);
  document.getElementById('user-avatar').textContent = `${first}${second}`.toUpperCase();
  document.getElementById('user-name').textContent = `${userData.nome || ''} ${userData.cognome || ''}`.trim() || 'Senza nome';
  document.getElementById('user-email').textContent = userData.email;
  document.getElementById('user-credito').textContent = formatCurrency(userData.creditoResiduo);
  document.getElementById('user-ruolo-badge').innerHTML = getRuoloBadge(userData.ruolo);
  document.getElementById('user-telefono').textContent = userData.telefono || '-';
  const date = new Date(userData.dataRegistrazione);
  document.getElementById('user-data').textContent = date.toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric' });
}

function fillEditForm() {
  document.getElementById('edit-ruolo').value = userData.ruolo || 'User';
  document.getElementById('edit-credito').value = userData.creditoResiduo || 0;
}

async function saveProfilo() {
  const ruolo = document.getElementById('edit-ruolo').value;
  const credito = parseFloat(document.getElementById('edit-credito').value);
  if (isNaN(credito) || credito < 0) { showToast('Credito non valido', 'danger'); return; }
  try {
    await API.updateRuolo(userId, { nuovoRuolo: ruolo });
    await API.updateCredito(userId, { nuovoCredito: credito });
    showToast('Utente aggiornato', 'success');
    await loadUserData();
  } catch (err) { handleApiError(err); }
}

function switchTab(tabName) {
  document.querySelectorAll('.tab-btn').forEach(b => {
    b.classList.remove('active', 'border-ferrari-primary', 'text-ink');
    b.classList.add('border-transparent', 'text-body');
  });
  const btn = document.querySelector(`[data-tab="${tabName}"]`);
  if (btn) { btn.classList.add('active', 'border-ferrari-primary', 'text-ink'); btn.classList.remove('border-transparent', 'text-body'); }
  document.querySelectorAll('.tab-content').forEach(t => t.classList.add('hidden'));
  document.getElementById(`tab-${tabName}`).classList.remove('hidden');
}

async function loadMovements() {
  const container = document.getElementById('movimenti-list');
  try {
    const data = await apiFetch(`/credito/utente/${userId}`);
    movementsData = normalizeCollection(data?.movimenti) || normalizeCollection(data) || [];
    if (!movementsData.length) {
      container.innerHTML = '<p class="text-body text-center py-4">Nessun movimento credito</p>';
      return;
    }
    container.innerHTML = movementsData.map(m => {
      const date = new Date(m.createdAtUtc);
      const isPositive = m.tipo === 'TopUp' || m.tipo === 'Refund';
      const color = isPositive ? 'text-emerald-500' : 'text-red-400';
      const sign = isPositive ? '+' : '';
      let label, detail;
      switch (m.tipo) {
        case 'TopUp': label = 'Ricarica credito'; detail = m.note || `Transazione ${m.id}`; break;
        case 'DebitOrder': label = 'Acquisto biglietti'; detail = m.filmTitolo || m.codiceOrdine || `Ordine ${m.ordineId}`; break;
        case 'Refund': label = 'Rimborso'; detail = m.filmTitolo || m.note || ''; break;
        default: label = m.tipo || 'Movimento'; detail = m.note || '';
      }
      return `<div class="flex items-start justify-between p-3 border border-hairline"><div><span class="text-ink font-medium text-sm">${label}</span>${detail ? `<p class="text-xs text-body">${detail}</p>` : ''}<p class="text-xs text-body mt-0.5">${date.toLocaleDateString('it-IT', { day:'numeric', month:'short', hour:'2-digit', minute:'2-digit' })}</p></div><span class="${color} font-semibold text-sm whitespace-nowrap">${sign}${formatCurrency(Math.abs(m.importo))}</span></div>`;
    }).join('');
  } catch {
    container.innerHTML = '<p class="text-body text-center py-4">Nessun movimento o endpoint non disponibile</p>';
  }
}

async function loadBiglietti() {
  const container = document.getElementById('biglietti-list');
  try {
    const data = await apiFetch(`/admin/utenti/${userId}/biglietti`);
    bigliettiData = normalizeCollection(data) || [];
    if (!bigliettiData.length) {
      container.innerHTML = '<p class="text-body text-center py-4">Nessun biglietto</p>';
      return;
    }
    container.innerHTML = bigliettiData.slice(0, 20).map(b => {
      const date = new Date(b.startAtUtc);
      const statoClass = {Issued:'text-emerald-500',Validated:'text-blue-500',Cancelled:'text-red-400'}[b.stato] || 'text-body';
      return `<div class="flex items-center justify-between p-3 border border-hairline"><div><p class="text-ink text-sm font-medium">${b.filmTitolo || '-'}</p><p class="text-xs text-body">${b.cinemaNome || ''} - ${b.salaNome || ''} | ${date.toLocaleDateString('it-IT',{day:'numeric',month:'short',hour:'2-digit',minute:'2-digit'})}</p><p class="text-xs text-body">${b.settore || ''} Fila ${b.fila || '-'} Posto ${b.numero || '-'} | <span class="${statoClass}">${b.stato || '-'}</span></p></div><span class="text-ferrari-primary font-semibold text-sm">${formatCurrency(b.prezzoTotale)}</span></div>`;
    }).join('');
  } catch {
    container.innerHTML = '<p class="text-body text-center py-4">Endpoint biglietti non disponibile</p>';
  }
}

async function loadOrdini() {
  const container = document.getElementById('ordini-list');
  try {
    const data = await apiFetch(`/admin/utenti/${userId}/ordini`);
    ordiniData = normalizeCollection(data) || [];
    if (!ordiniData.length) {
      container.innerHTML = '<p class="text-body text-center py-4">Nessun ordine</p>';
      return;
    }
    container.innerHTML = ordiniData.slice(0, 20).map(o => {
      const date = new Date(o.startAtUtc || o.createdAtUtc);
      return `<div class="flex items-center justify-between p-3 border border-hairline"><div><p class="text-ink text-sm font-medium">${o.filmTitolo || '-'}</p><p class="text-xs text-body">${o.cinemaNome || ''} - ${o.salaNome || ''} | ${date.toLocaleDateString('it-IT',{day:'numeric',month:'short',hour:'2-digit',minute:'2-digit'})}</p><p class="text-xs text-body">${o.numeroBiglietti || 0} biglietti | <span class="${o.stato==='Paid'?'text-emerald-500':'text-amber-500'}">${o.stato || '-'}</span> | Cod: ${o.codiceOrdine || '-'}</p></div><span class="text-ferrari-primary font-semibold text-sm">${formatCurrency(o.totaleLordo)}</span></div>`;
    }).join('');
  } catch {
    container.innerHTML = '<p class="text-body text-center py-4">Endpoint ordini non disponibile</p>';
  }
}

// --- AI CHATBOT ---
const AI_KEYWORDS = {
  'credito': 'Il credito può essere ricaricato dalla sezione Profilo. Come admin, puoi modificare il credito nella tab Profilo di questa pagina.',
  'biglietto': 'I biglietti vengono emessi dopo il pagamento. Vedi la tab Biglietti per tutti i biglietti di questo utente.',
  'ordine': 'Gli ordini sono visibili nella tab Ordini. Ogni ordine ha uno stato (Paid, Pending, Cancelled).',
  'ruolo': 'I ruoli disponibili sono User, PowerUser e Admin. Puoi cambiarli nella tab Profilo.',
  'registra': 'La registrazione avviene da /registrazione.html. L\'utente deve inserire email, nome, cognome e password.',
  'password': 'La password non è visibile agli admin. L\'utente può reimpostarla solo tramite la funzione "Password dimenticata".',
  'bug': 'BUG_DETECTED',
  'problema': 'Se riscontri un problema tecnico, ti consiglio di usare la parola "bug" per aprire il form di segnalazione.',
  'errore': 'Se hai trovato un errore, scrivi "bug" per segnalarlo in modo dettagliato.',
  'aiuto': 'Posso aiutarti con informazioni su: credito, biglietti, ordini, ruoli, registrazione. Scrivi "bug" per segnalare un problema.',
  'default': 'Non ho informazioni specifiche su questo argomento. Prova a chiedere di: credito, biglietti, ordini, ruoli, registrazione. Scrivi "bug" per segnalare un problema.'
};

let bugMode = false;

function toggleAIPanel() {
  document.getElementById('ai-panel').classList.toggle('hidden');
}

function sendAIMessage() {
  const input = document.getElementById('ai-input');
  const msg = input.value.trim();
  if (!msg) return;

  const msgs = document.getElementById('ai-messages');
  msgs.innerHTML += `<div class="text-right"><span class="inline-block bg-ferrari-primary/20 text-ink px-3 py-1.5 rounded text-sm">${escapeHtml(msg)}</span></div>`;
  input.value = '';

  if (bugMode) {
    msgs.innerHTML += `<div class="text-body p-2 bg-white/5 rounded text-sm"><i class="fa-solid fa-bug mr-1 text-amber-500"></i>Per favore completa il form di segnalazione bug qui sopra. Assicurati di descrivere nel dettaglio cosa è successo.</div>`;
    return;
  }

  const lower = msg.toLowerCase();
  let response;

  if (lower.includes('bug')) {
    bugMode = true;
    document.getElementById('ai-bug-form').classList.remove('hidden');
    response = '<i class="fa-solid fa-bug mr-1 text-amber-500"></i>Ho rilevato una segnalazione di bug. Per favore compila il form qui sopra con <b>titolo</b> e <b>descrizione dettagliata</b>. Più dettagli fornisci, più facilmente potremo risolvere il problema.';
  } else {
    response = AI_KEYWORDS['default'];
    for (const [key, val] of Object.entries(AI_KEYWORDS)) {
      if (key === 'default') continue;
      if (lower.includes(key)) { response = val; break; }
    }
  }

  setTimeout(() => {
    msgs.innerHTML += `<div class="text-body p-2 bg-white/5 rounded text-sm">${response}</div>`;
    msgs.scrollTop = msgs.scrollHeight;
  }, 400);
}

function cancelBug() {
  bugMode = false;
  document.getElementById('ai-bug-form').classList.add('hidden');
  document.getElementById('bug-titolo').value = '';
  document.getElementById('bug-descrizione').value = '';
  const msgs = document.getElementById('ai-messages');
  msgs.innerHTML += `<div class="text-body p-2 bg-white/5 rounded text-sm">Segnalazione bug annullata. Come posso aiutarti?</div>`;
}

async function submitBug() {
  const titolo = document.getElementById('bug-titolo').value.trim();
  const descrizione = document.getElementById('bug-descrizione').value.trim();

  if (!titolo) { showToast('Inserisci un titolo per il bug', 'warning'); return; }
  if (!descrizione) { showToast('Inserisci una descrizione dettagliata', 'warning'); return; }

  const btn = document.getElementById('btn-submit-bug');
  btn.disabled = true;
  btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-1"></i>Invio...';

  try {
    await API.createSegnalazione({ titolo, descrizione, emailUtente: userData.email, userId });
    showToast('Segnalazione inviata con successo', 'success');
    cancelBug();
    const msgs = document.getElementById('ai-messages');
    msgs.innerHTML += `<div class="text-emerald-500 p-2 bg-white/5 rounded text-sm"><i class="fa-solid fa-check-circle mr-1"></i>Segnalazione inviata! La trovi nella dashboard admin sotto <b>Segnalazioni</b>.</div>`;
  } catch (err) {
    showToast('Errore invio segnalazione', 'danger');
    btn.disabled = false;
    btn.innerHTML = '<i class="fa-solid fa-paper-plane mr-1"></i>Invia Segnalazione';
  }
}

function getRuoloBadge(ruolo) {
  switch (ruolo) {
    case 'Admin': return '<span class="inline-flex gap-1 px-2 py-0.5 text-xs font-semibold bg-ferrari-primary/15 text-ferrari-primary"><i class="fa-solid fa-crown"></i>Admin</span>';
    case 'PowerUser': return '<span class="inline-flex gap-1 px-2 py-0.5 text-xs font-semibold bg-amber-500/15 text-amber-500"><i class="fa-solid fa-bolt"></i>PowerUser</span>';
    default: return '<span class="inline-flex gap-1 px-2 py-0.5 text-xs font-semibold bg-body/15 text-body"><i class="fa-solid fa-user"></i>User</span>';
  }
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}
