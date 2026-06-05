// Variabile userId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let userId = null;
// Variabile userData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let userData = null;
// Variabile movementsData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let movementsData = [];
// Variabile bigliettiData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let bigliettiData = [];
// Variabile ordiniData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let ordiniData = [];

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione loadUserData: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadUserData() {
  try {
    // Variabile utenti: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const utenti = normalizeCollection(await API.getUtenti());
    userData = utenti.find(u => u.id === userId);
    if (!userData) { showToast('Utente non trovato', 'danger'); return; }
    renderHeader();
    fillEditForm();
  } catch { showToast('Errore caricamento utente', 'danger'); }
}

// Funzione renderHeader: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderHeader() {
  // Variabile first: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const first = (userData.nome || 'U').charAt(0);
  // Variabile second: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const second = (userData.cognome || 'N').charAt(0);
  document.getElementById('user-avatar').textContent = `${first}${second}`.toUpperCase();
  document.getElementById('user-name').textContent = `${userData.nome || ''} ${userData.cognome || ''}`.trim() || 'Senza nome';
  document.getElementById('user-email').textContent = userData.email;
  document.getElementById('user-credito').textContent = formatCurrency(userData.creditoResiduo);
  document.getElementById('user-ruolo-badge').innerHTML = getRuoloBadge(userData.ruolo);
  document.getElementById('user-telefono').textContent = userData.telefono || '-';
  // Variabile date: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const date = new Date(userData.dataRegistrazione);
  document.getElementById('user-data').textContent = date.toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric' });
}

// Funzione fillEditForm: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function fillEditForm() {
  document.getElementById('edit-ruolo').value = userData.ruolo || 'User';
  document.getElementById('edit-credito').value = userData.creditoResiduo || 0;
}

// Funzione saveProfilo: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function saveProfilo() {
  // Variabile ruolo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const ruolo = document.getElementById('edit-ruolo').value;
  // Variabile credito: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const credito = parseFloat(document.getElementById('edit-credito').value);
  if (isNaN(credito) || credito < 0) { showToast('Credito non valido', 'danger'); return; }
  try {
    await API.updateRuolo(userId, { nuovoRuolo: ruolo });
    await API.updateCredito(userId, { nuovoCredito: credito });
    showToast('Utente aggiornato', 'success');
    await loadUserData();
  } catch (err) { handleApiError(err); }
}

// Funzione switchTab: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function switchTab(tabName) {
  document.querySelectorAll('.tab-btn').forEach(b => {
    b.classList.remove('active', 'border-ferrari-primary', 'text-ink');
    b.classList.add('border-transparent', 'text-body');
  });
  // Variabile btn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btn = document.querySelector(`[data-tab="${tabName}"]`);
  if (btn) { btn.classList.add('active', 'border-ferrari-primary', 'text-ink'); btn.classList.remove('border-transparent', 'text-body'); }
  document.querySelectorAll('.tab-content').forEach(t => t.classList.add('hidden'));
  document.getElementById(`tab-${tabName}`).classList.remove('hidden');
}

// Funzione loadMovements: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadMovements() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('movimenti-list');
  try {
    // Try admin endpoint first, fallback to credito/me
    const email = userData?.email || '';
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = await API.getUtenteMovimenti(email);
    movementsData = normalizeCollection(data) || [];
    if (!movementsData.length) {
      container.innerHTML = '<p class="text-body text-center py-4">Nessun movimento credito</p>';
      return;
    }
    container.innerHTML = movementsData.map(m => {
      // Variabile date: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const date = new Date(m.createdAtUtc || m.data);
      // Variabile importo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const importo = m.importo || m.amount || 0;
      // Variabile isPositive: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const isPositive = importo > 0 || (m.tipo === 'TopUp' || m.tipo === 'Refund');
      // Variabile color: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const color = isPositive ? 'text-emerald-500' : 'text-red-400';
      // Variabile sign: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const sign = isPositive ? '+' : '';
      // Variabile label: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      let label, detail;
      switch (m.tipo) {
        case 'TopUp': label = 'Ricarica credito'; detail = m.note || `Transazione ${m.id}`; break;
        case 'DebitOrder': label = 'Acquisto biglietti'; detail = m.filmTitolo || m.codiceOrdine || `Ordine ${m.ordineId}`; break;
        case 'Refund': label = 'Rimborso'; detail = m.filmTitolo || m.note || ''; break;
        default: label = m.tipo || m.descrizione || 'Movimento'; detail = m.note || m.descrizione || '';
      }
      // Variabile importoDisplay: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const importoDisplay = importo ? `${sign}${formatCurrency(Math.abs(importo))}` : '';
      return `<div class="flex items-start justify-between p-3 border border-hairline"><div><span class="text-ink font-medium text-sm">${label}</span>${detail ? `<p class="text-xs text-body">${detail}</p>` : ''}<p class="text-xs text-body mt-0.5">${date.toLocaleDateString('it-IT', { day:'numeric', month:'short', hour:'2-digit', minute:'2-digit' })}</p></div>${importoDisplay ? `<span class="${color} font-semibold text-sm whitespace-nowrap">${importoDisplay}</span>` : ''}</div>`;
    }).join('');
  } catch {
    container.innerHTML = '<p class="text-body text-center py-4">Nessun movimento credito trovato</p>';
  }
}

// Funzione loadBiglietti: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadBiglietti() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('biglietti-list');
  try {
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = await API.getUtenteBiglietti(userId);
    bigliettiData = normalizeCollection(data) || [];
    if (!bigliettiData.length) {
      container.innerHTML = '<p class="text-body text-center py-4">Nessun biglietto</p>';
      return;
    }
    container.innerHTML = bigliettiData.slice(0, 20).map(b => {
      // Variabile date: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const date = new Date(b.startAtUtc || b.createdAtUtc);
      // Variabile statoClass: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const statoClass = {Issued:'text-emerald-500',Validated:'text-blue-500',Cancelled:'text-red-400'}[b.stato] || 'text-body';
      // Variabile prezzo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const prezzo = b.prezzoTotale || b.prezzo || 0;
      return `<div class="flex items-center justify-between p-3 border border-hairline"><div><p class="text-ink text-sm font-medium">${b.filmTitolo || '-'}</p><p class="text-xs text-body">${b.cinemaNome || ''} - ${b.salaNome || ''} | ${date.toLocaleDateString('it-IT',{day:'numeric',month:'short',hour:'2-digit',minute:'2-digit'})}</p><p class="text-xs text-body">${b.settore || ''} Fila ${b.fila || '-'} Posto ${b.numero || '-'} | <span class="${statoClass}">${b.stato || '-'}</span></p></div><span class="text-ferrari-primary font-semibold text-sm">${formatCurrency(prezzo)}</span></div>`;
    }).join('');
  } catch {
    container.innerHTML = '<p class="text-body text-center py-4">Nessun biglietto trovato</p>';
  }
}

// Funzione loadOrdini: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadOrdini() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('ordini-list');
  try {
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = await API.getUtenteOrdini(userId);
    ordiniData = normalizeCollection(data) || [];
    if (!ordiniData.length) {
      container.innerHTML = '<p class="text-body text-center py-4">Nessun ordine</p>';
      return;
    }
    container.innerHTML = ordiniData.slice(0, 20).map(o => {
      // Variabile date: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const date = new Date(o.startAtUtc || o.createdAtUtc);
      // Variabile totale: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const totale = o.totaleLordo || o.importoTotale || 0;
      return `<div class="flex items-center justify-between p-3 border border-hairline"><div><p class="text-ink text-sm font-medium">${o.filmTitolo || '-'}</p><p class="text-xs text-body">${o.cinemaNome || ''} - ${o.salaNome || ''} | ${date.toLocaleDateString('it-IT',{day:'numeric',month:'short',hour:'2-digit',minute:'2-digit'})}</p><p class="text-xs text-body">${o.numeroBiglietti || 0} biglietti | <span class="${o.stato==='Paid'?'text-emerald-500':'text-amber-500'}">${o.stato || '-'}</span> | Cod: ${o.codiceOrdine || '-'}</p></div><span class="text-ferrari-primary font-semibold text-sm">${formatCurrency(totale)}</span></div>`;
    }).join('');
  } catch {
    container.innerHTML = '<p class="text-body text-center py-4">Nessun ordine trovato</p>';
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

// Variabile bugMode: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let bugMode = false;

// Funzione toggleAIPanel: commuta uno stato visivo o funzionale tra due modalità. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function toggleAIPanel() {
  document.getElementById('ai-panel').classList.toggle('hidden');
}

// Funzione sendAIMessage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function sendAIMessage() {
  // Variabile input: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const input = document.getElementById('ai-input');
  // Variabile msg: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const msg = input.value.trim();
  if (!msg) return;

  // Variabile msgs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const msgs = document.getElementById('ai-messages');
  msgs.innerHTML += `<div class="text-right"><span class="inline-block bg-ferrari-primary/20 text-ink px-3 py-1.5 rounded text-sm">${escapeHtml(msg)}</span></div>`;
  input.value = '';

  if (bugMode) {
    msgs.innerHTML += `<div class="text-body p-2 bg-white/5 rounded text-sm"><i class="fa-solid fa-bug mr-1 text-amber-500"></i>Per favore completa il form di segnalazione bug qui sopra. Assicurati di descrivere nel dettaglio cosa è successo.</div>`;
    return;
  }

  // Variabile lower: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const lower = msg.toLowerCase();
  // Variabile response: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione cancelBug: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function cancelBug() {
  bugMode = false;
  document.getElementById('ai-bug-form').classList.add('hidden');
  document.getElementById('bug-titolo').value = '';
  document.getElementById('bug-descrizione').value = '';
  // Variabile msgs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const msgs = document.getElementById('ai-messages');
  msgs.innerHTML += `<div class="text-body p-2 bg-white/5 rounded text-sm">Segnalazione bug annullata. Come posso aiutarti?</div>`;
}

// Funzione submitBug: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function submitBug() {
  // Variabile titolo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const titolo = document.getElementById('bug-titolo').value.trim();
  // Variabile descrizione: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const descrizione = document.getElementById('bug-descrizione').value.trim();

  if (!titolo) { showToast('Inserisci un titolo per il bug', 'warning'); return; }
  if (!descrizione) { showToast('Inserisci una descrizione dettagliata', 'warning'); return; }

  // Variabile btn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btn = document.getElementById('btn-submit-bug');
  btn.disabled = true;
  btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-1"></i>Invio...';

  try {
    await API.createSegnalazione({ titolo, descrizione, emailUtente: userData.email, userId });
    showToast('Segnalazione inviata con successo', 'success');
    cancelBug();
    // Variabile msgs: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const msgs = document.getElementById('ai-messages');
    msgs.innerHTML += `<div class="text-emerald-500 p-2 bg-white/5 rounded text-sm"><i class="fa-solid fa-check-circle mr-1"></i>Segnalazione inviata! La trovi nella dashboard admin sotto <b>Segnalazioni</b>.</div>`;
  } catch (err) {
    showToast('Errore invio segnalazione', 'danger');
    btn.disabled = false;
    btn.innerHTML = '<i class="fa-solid fa-paper-plane mr-1"></i>Invia Segnalazione';
  }
}

// Funzione getRuoloBadge: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getRuoloBadge(ruolo) {
  switch (ruolo) {
    case 'Admin': return '<span class="inline-flex gap-1 px-2 py-0.5 text-xs font-semibold bg-ferrari-primary/15 text-ferrari-primary"><i class="fa-solid fa-crown"></i>Admin</span>';
    case 'PowerUser': return '<span class="inline-flex gap-1 px-2 py-0.5 text-xs font-semibold bg-amber-500/15 text-amber-500"><i class="fa-solid fa-bolt"></i>PowerUser</span>';
    default: return '<span class="inline-flex gap-1 px-2 py-0.5 text-xs font-semibold bg-body/15 text-body"><i class="fa-solid fa-user"></i>User</span>';
  }
}

// Funzione escapeHtml: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function escapeHtml(text) {
  // Variabile div: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}
