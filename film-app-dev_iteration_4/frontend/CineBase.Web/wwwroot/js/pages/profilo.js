// Variabile profiloData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let profiloData = null;
// Variabile creditoData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let creditoData = null;
// Variabile cinemaPreferito: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let cinemaPreferito = null;
// Variabile userSubscriptionData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let userSubscriptionData = null;

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  if (!Auth?.isLoggedIn?.()) {
    window.location.replace('/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search));
    return;
  }

  // Check return from Stripe topup
  const topupParams = new URLSearchParams(window.location.search);
  if (topupParams.get('topup') === 'success') {
    // Variabile sessionId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let sessionId = topupParams.get('session_id') || sessionStorage.getItem('pendingTopupSessionId');
    sessionStorage.removeItem('pendingTopupSessionId');
    if (sessionId) {
      try { await API.reconcileTopup(sessionId); } catch { /* non-critical topup reconciliation */ }
    }
    showToast('Ricarica credito effettuata con successo!', 'success');
    // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const url = new URL(window.location.href);
    url.searchParams.delete('topup');
    url.searchParams.delete('session_id');
    window.history.replaceState({}, '', url);
  } else if (topupParams.get('topup') === 'cancelled') {
    showToast('Ricarica annullata', 'info');
    // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const url = new URL(window.location.href);
    url.searchParams.delete('topup');
    window.history.replaceState({}, '', url);
  }

  await Promise.all([
    loadProfilo(),
    loadCredito(),
    loadCinemaPreferito(),
    loadUserSubscription(),
    loadUserVouchers(),
    loadOrdini(),
    loadBiglietti(),
    loadPrenotazioniLegacy(),
    caricaStato2FA(),
    loadAccountSecurity()
  ]);

  setupProfiloForm();
});

// Funzione loadUserSubscription: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadUserSubscription() {
  try {
    // Variabile sub: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const sub = await API.getUserSubscription();
    if (!sub) return;

    userSubscriptionData = sub;
    // Variabile section: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const section = document.getElementById('subscription-section');
    if (!section) return;
    section.classList.remove('hidden');
    // Variabile content: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const content = document.getElementById('subscription-content');
    if (!content) return;

    content.innerHTML = `
      <div class="p-4 bg-canvas-elevated border border-hairline">
        <div class="flex justify-between items-start mb-3">
          <div>
            <h3 class="text-xl font-bold text-ink">${sub.abbonamentoNome}</h3>
            <p class="text-sm text-body">${sub.abbonamentoTipo} · Dal ${new Date(sub.dataInizio).toLocaleDateString('it-IT')}</p>
          </div>
          <span class="px-3 py-1 text-xs font-semibold ${sub.stato === 'attivo' ? 'bg-ferrari-primary/15 text-ferrari-primary' : 'bg-ferrari-semantic-warning/15 text-ferrari-semantic-warning'} uppercase tracking-wider">${sub.stato}</span>
        </div>
        <div class="space-y-1.5 text-sm text-ink/90 mb-4">
          <div><i class="fa-solid fa-ticket mr-2 text-ferrari-primary"></i>${sub.numeroBigliettiPerMese} biglietti/mese</div>
          ${sub.includePopcornPerMese > 0 ? `<div><i class="fa-solid fa-popcorn mr-2 text-ferrari-primary"></i>${sub.includePopcornPerMese} popcorn/mese</div>` : ''}
          <div><i class="fa-solid fa-calendar mr-2 text-ferrari-primary"></i>Scade: ${new Date(sub.dataScadenza).toLocaleDateString('it-IT')}</div>
        </div>
        <div class="flex gap-3 flex-wrap">
          <button onclick="toggleAutoRenew()" class="btn-outline text-xs px-3 py-1.5">
            <i class="fa-solid fa-rotate mr-1"></i>Rinnovo auto: ${sub.autoRinnovo ? 'ON' : 'OFF'}
          </button>
          <button onclick="fermaAbbonamento()" class="btn-outline text-xs px-3 py-1.5 text-ferrari-semantic-warning border-ferrari-semantic-warning hover:bg-ferrari-semantic-warning/10">
            <i class="fa-solid fa-stop mr-1"></i>Ferma Abbonamento
          </button>
        </div>
      </div>`;
  } catch (e) {
    console.error('subscription load err', e);
  }
}

// Funzione loadUserVouchers: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadUserVouchers() {
  try {
    // Variabile vouchers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const vouchers = await API.getUserVouchers();
    if (!vouchers?.length) return;

    // Variabile section: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const section = document.getElementById('vouchers-section');
    if (!section) return;
    section.classList.remove('hidden');
    // Variabile content: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const content = document.getElementById('vouchers-content');
    if (!content) return;

    content.innerHTML = vouchers.map(v => `
      <div class="p-4 bg-canvas-elevated rounded-lg border border-hairline">
        <p class="text-xs text-body uppercase tracking-wider mb-1">Codice</p>
        <p class="text-lg font-mono font-bold text-ferrari-primary mb-2">${v.codice}</p>
        <div class="flex justify-between text-sm">
          <span class="text-ink font-semibold">${Number(v.importo || v.saldoResiduo || 0).toFixed(2)} €</span>
          <span class="${v.stato === 'attivo' ? 'text-green-400' : 'text-red-400'} text-xs font-semibold uppercase">${v.stato}</span>
        </div>
        ${v.dataScadenza ? `<p class="text-xs text-body/60 mt-1">Scade: ${new Date(v.dataScadenza).toLocaleDateString('it-IT')}</p>` : ''}
      </div>`).join('');
  } catch (e) {
    console.error('voucher load err', e);
  }
}

// Funzione toggleAutoRenew: commuta uno stato visivo o funzionale tra due modalità. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function toggleAutoRenew() {
  try {
    // Variabile autoRinnovo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const autoRinnovo = !userSubscriptionData?.autoRinnovo;
    // Variabile res: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const res = await API.toggleAutoRenew(autoRinnovo);
    if (res) {
      await loadUserSubscription();
      showToast('Auto-rinnovo aggiornato', 'success');
    }
  } catch (e) {
    showToast('Errore', 'danger');
  }
}

// Funzione fermaAbbonamento: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function fermaAbbonamento() {
  if (!confirm('Sei sicuro di voler fermare questo abbonamento? Non verra\' piu\' rinnovato e perderai i benefici alla scadenza.')) return;
  try {
    await API.cancelSubscription();
    await loadUserSubscription();
    showToast('Abbonamento fermato. Resta attivo fino alla scadenza.', 'success');
  } catch (e) {
    showToast('Errore durante la disattivazione', 'danger');
  }
}

// Funzione loadProfilo: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadProfilo() {
  try {
    profiloData = await API.getProfilo();
    fillProfiloForm();
  } catch (error) {
    handleApiError(error);
  }
}

// Funzione fillProfiloForm: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function fillProfiloForm() {
  if (!profiloData) return;
  document.getElementById('profilo-email').value = profiloData.email || '';
  document.getElementById('profilo-nome').value = profiloData.nome || '';
  document.getElementById('profilo-cognome').value = profiloData.cognome || '';
  document.getElementById('profilo-telefono').value = profiloData.telefono || '';
}

// Funzione setupProfiloForm: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupProfiloForm() {
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById('profilo-form');
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = {
      nome: document.getElementById('profilo-nome').value.trim(),
      cognome: document.getElementById('profilo-cognome').value.trim(),
      telefono: document.getElementById('profilo-telefono').value.trim() || null
    };

    try {
      profiloData = await API.updateProfilo(data);
      fillProfiloForm();
      // Variabile user: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const user = Auth.getUser();
      if (user) {
        user.nome = profiloData.nome;
        user.cognome = profiloData.cognome;
        Auth.saveUser(user);
      }
      showToast('Profilo aggiornato con successo');
      if (typeof window.updateAuthUI === 'function') window.updateAuthUI();
      // Variabile savedEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const savedEl = document.getElementById('profilo-saved');
      if (savedEl) {
        savedEl.classList.remove('hidden');
        setTimeout(() => savedEl.classList.add('hidden'), 2000);
      }
    } catch (error) {
      handleApiError(error);
    }
  });
}

// Funzione loadCinemaPreferito: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCinemaPreferito() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('cinema-preferito-content');
  try {
    // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const result = await API.getCinemaPreferito();
    cinemaPreferito = result;

    if (!result || !result.cinemaId) {
      container.innerHTML = `
        <div class="text-center py-4">
          <p class="text-sm text-body mb-3">Nessun cinema preferito impostato</p>
          <a href="/my-cinemas.html" class="btn-primary">
            <i class="fa-solid fa-location-dot mr-1"></i>Scegli cinema
          </a>
        </div>`;
      return;
    }

    // Variabile cinema: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const cinema = result.cinema;
    container.innerHTML = `
      <div class="flex items-start gap-4">
        <div class="flex-shrink-0 w-12 h-12 bg-ferrari-primary/20 flex items-center justify-center">
          <i class="fa-solid fa-location-dot text-ferrari-primary text-xl"></i>
        </div>
        <div class="flex-1 min-w-0">
          <h3 class="font-semibold text-ink truncate">${cinema.nome}</h3>
          <p class="text-sm text-body">${cinema.citta}${cinema.indirizzo ? ` - ${cinema.indirizzo}` : ''}</p>
          ${cinema.telefono ? `<p class="text-xs text-body mt-1"><i class="fa-solid fa-phone mr-1"></i>${cinema.telefono}</p>` : ''}
        </div>
        <a href="/my-cinemas.html" class="btn-tertiary text-xs" title="Cambia cinema preferito">
          <i class="fa-solid fa-pen"></i>
        </a>
      </div>`;
  } catch {
    container.innerHTML = `<p class="text-sm text-body">Errore caricamento cinema preferito</p>`;
  }
}

// Funzione loadCredito: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCredito() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('credito-content');
  try {
    creditoData = await API.getCreditoMe();

    // Variabile saldo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const saldo = creditoData.saldoAttuale || 0;
    // Variabile movimenti: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const movimenti = creditoData.movimenti || [];

    // Variabile html: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let html = `
      <div class="flex items-center justify-between mb-4">
        <div>
          <p class="text-sm text-body">Saldo disponibile</p>
          <p class="text-2xl font-bold text-ferrari-primary">${formatCurrency(saldo)}</p>
        </div>
        <div class="w-12 h-12 bg-ferrari-primary/20 flex items-center justify-center">
          <i class="fa-solid fa-wallet text-ferrari-primary text-xl"></i>
        </div>
      </div>`;

    if (movimenti.length > 0) {
      // Variabile recentMov: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const recentMov = movimenti.slice(0, 5);
      html += `<div class="pt-3 mt-3">
        <p class="text-xs font-semibold text-body uppercase tracking-wider mb-2">Ultimi movimenti</p>
        <div class="space-y-2">`;

      recentMov.forEach(m => {
        // Variabile isPositive: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const isPositive = m.tipo === 'TopUp' || m.tipo === 'Refund';
        // Variabile sign: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const sign = isPositive ? '+' : '';
        // Variabile date: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const date = new Date(m.createdAtUtc);
        // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const dateStr = date.toLocaleDateString('it-IT', { day: 'numeric', month: 'short' });
        // Variabile timeStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const timeStr = date.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });

        // Variabile icon: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        let icon, color, label, detail;
        if (m.tipo === 'TopUp') {
          icon = 'fa-arrow-down';
          color = 'text-emerald-500';
          label = 'Ricarica';
          detail = m.note ? `<span class="text-xs text-body">${m.note}</span>` : '';
        } else if (m.tipo === 'DebitOrder') {
          icon = 'fa-arrow-up';
          color = 'text-red-400';
          label = 'Acquisto film';
          detail = m.filmTitolo
            ? `<span class="text-xs text-body">${m.filmTitolo} · ${new Date(m.showStartAtUtc).toLocaleDateString('it-IT', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}</span>`
            : (m.codiceOrdine ? `<span class="text-xs text-body">${m.codiceOrdine}</span>` : '');
        } else if (m.tipo === 'Refund') {
          icon = 'fa-rotate-left';
          color = 'text-emerald-500';
          label = 'Rimborso';
          detail = m.filmTitolo
            ? `<span class="text-xs text-body">${m.filmTitolo}</span>`
            : '';
        } else {
          icon = 'fa-minus';
          color = 'text-body';
          label = 'Rettifica';
          detail = m.note ? `<span class="text-xs text-body">${m.note}</span>` : '';
        }

        html += `
          <div class="flex items-start justify-between text-sm">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <i class="fa-solid ${icon} ${color} text-xs mt-0.5"></i>
                <span class="text-ink font-medium">${label}</span>
              </div>
              ${detail ? `<div class="ml-5 mt-0.5">${detail}</div>` : ''}
              <p class="text-xs text-body mt-0.5 ml-5">${dateStr} alle ${timeStr}</p>
            </div>
            <span class="${color} font-semibold whitespace-nowrap">${sign}${formatCurrency(Math.abs(m.importo))}</span>
          </div>`;
      });

      html += `</div></div>`;
    }

    html += `<button onclick="openTopupModal()" class="topup-launch-btn btn-primary w-full mt-3 py-2 text-sm">
      <i class="fa-solid fa-plus mr-1"></i>Ricarica credito
    </button>`;

    container.innerHTML = html;
  } catch {
    container.innerHTML = `<p class="text-sm text-body">Errore caricamento credito</p>`;
  }
}

// Variabile allOrdini: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let allOrdini = [];
// Variabile ORDINI_PER_PAGE: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const ORDINI_PER_PAGE = 5;
// Variabile ordiniShown: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let ordiniShown = ORDINI_PER_PAGE;

// Funzione loadOrdini: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadOrdini() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('ordini-list');
  try {
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = await API.getOrdini();
    allOrdini = normalizeCollection(data);

    if (!allOrdini.length) {
      container.innerHTML = `
        <div class="text-center py-8 text-body">
          <i class="fa-solid fa-receipt text-4xl mb-3 opacity-40"></i>
          <p class="font-medium">Nessun ordine</p>
          <p class="text-sm mt-1">I tuoi ordini appariranno qui</p>
        </div>`;
      return;
    }

    ordiniShown = ORDINI_PER_PAGE;
    renderOrdini();
  } catch {
    container.innerHTML = `<p class="text-sm text-ferrari-semantic-warning text-center py-4">Errore caricamento ordini</p>`;
  }
}

// Funzione renderOrdini: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderOrdini() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('ordini-list');
  // Variabile visible: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const visible = allOrdini.slice(0, ordiniShown);

  container.innerHTML = visible.map(o => {
    // Variabile startDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const startDate = new Date(o.startAtUtc);
    // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const dateStr = startDate.toLocaleDateString('it-IT', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
    // Variabile statoBadge: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const statoBadge = getStatoBadge(o.stato);

    return `
      <div class="border border-hairline p-4 mb-3 hover:bg-canvas-elevated/50 transition-colors">
        <div class="flex justify-between items-start">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1">
              <h3 class="font-semibold text-ink truncate">${o.filmTitolo}</h3>
              ${statoBadge}
            </div>
            <p class="text-sm text-body">
              <i class="fa-solid fa-location-dot mr-1"></i>${o.cinemaNome} - ${o.salaNome}
            </p>
            <p class="text-sm text-body">
              <i class="fa-regular fa-calendar mr-1"></i>${dateStr}
            </p>
            <div class="flex flex-wrap gap-3 mt-2 text-sm">
              <span class="text-body">
                <i class="fa-solid fa-ticket mr-1"></i>${o.numeroBiglietti} bigliett${o.numeroBiglietti === 1 ? 'o' : 'i'}
              </span>
              <span class="text-ferrari-primary font-semibold">${formatCurrency(o.totaleLordo)}</span>
            </div>
            <p class="text-xs text-body mt-1 font-mono">${o.codiceOrdine}</p>
          </div>
          <div class="flex flex-col gap-1 ml-2 flex-shrink-0">
            ${o.stato === 'Paid' ? `<button onclick="downloadPdf(${o.id})" class="btn-tertiary text-xs" title="Scarica PDF"><i class="fa-solid fa-file-pdf mr-1"></i>PDF</button>` : ''}
            <a href="/esito-acquisto.html?orderId=${o.id}" class="btn-tertiary text-xs" title="Dettagli"><i class="fa-solid fa-eye mr-1"></i>Dettagli</a>
          </div>
        </div>
      </div>`;
  }).join('');

  if (ordiniShown < allOrdini.length) {
    // Variabile remaining: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const remaining = allOrdini.length - ordiniShown;
    container.innerHTML += `
      <div class="text-center mt-3">
        <button onclick="caricaAltriOrdini()" class="btn-outline text-sm">
          <i class="fa-solid fa-chevron-down mr-1"></i>Carica di più (${remaining} rimasti)
        </button>
      </div>`;
  }
}

// Funzione caricaAltriOrdini: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function caricaAltriOrdini() {
  ordiniShown += ORDINI_PER_PAGE;
  renderOrdini();
}

// Variabile allBiglietti: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let allBiglietti = [];
// Variabile BIGLIETTI_PER_PAGE: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const BIGLIETTI_PER_PAGE = 5;
// Variabile bigliettiShown: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let bigliettiShown = BIGLIETTI_PER_PAGE;

// Funzione loadBiglietti: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadBiglietti() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('biglietti-list');
  try {
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = await API.getBiglietti();
    allBiglietti = normalizeCollection(data);

    if (!allBiglietti.length) {
      container.innerHTML = `
        <div class="text-center py-8 text-body">
          <i class="fa-solid fa-ticket text-4xl mb-3 opacity-40"></i>
          <p class="font-medium">Nessun biglietto</p>
          <p class="text-sm mt-1">I tuoi biglietti appariranno qui dopo l'acquisto</p>
        </div>`;
      return;
    }

    bigliettiShown = BIGLIETTI_PER_PAGE;
    renderBiglietti();
  } catch {
    container.innerHTML = `<p class="text-sm text-ferrari-semantic-warning text-center py-4">Errore caricamento biglietti</p>`;
  }
}

// Funzione renderBiglietti: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderBiglietti() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('biglietti-list');
  // Variabile visible: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const visible = allBiglietti.slice(0, bigliettiShown);

  container.innerHTML = visible.map(b => {
    // Variabile startDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const startDate = new Date(b.startAtUtc);
    // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const dateStr = startDate.toLocaleDateString('it-IT', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
    // Variabile statoClass: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const statoClass = b.stato === 'Issued' ? 'text-emerald-500' : b.stato === 'Validated' ? 'text-blue-500' : 'text-body';
    // Variabile statoLabel: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const statoLabel = b.stato === 'Issued' ? 'Emesso' : b.stato === 'Validated' ? 'Validato' : b.stato === 'Cancelled' ? 'Annullato' : b.stato;

    return `
      <div class="border border-hairline p-4 mb-3 hover:bg-canvas-elevated/50 transition-colors">
        <div class="flex justify-between items-start">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1">
              <h3 class="font-semibold text-ink truncate">${b.filmTitolo}</h3>
              <span class="${statoClass} text-xs font-semibold">${statoLabel}</span>
            </div>
            <p class="text-sm text-body">
              <i class="fa-solid fa-location-dot mr-1"></i>${b.cinemaNome} - ${b.salaNome}
            </p>
            <p class="text-sm text-body">
              <i class="fa-regular fa-calendar mr-1"></i>${dateStr}
            </p>
            <div class="flex flex-wrap gap-3 mt-2 text-sm">
              <span class="text-body">
                <i class="fa-solid fa-chair mr-1"></i>${b.settore} - Fila ${b.fila}, Posto ${b.numero}
              </span>
              <span class="text-ferrari-primary font-semibold">${formatCurrency(b.prezzoTotale)}</span>
            </div>
            <p class="text-xs text-body mt-1 font-mono">${b.codiceBiglietto}</p>
            ${b.validatoAtUtc ? `<p class="text-xs text-blue-500 mt-1"><i class="fa-solid fa-check mr-1"></i>Validato il ${new Date(b.validatoAtUtc).toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</p>` : ''}
          </div>
          <div class="flex flex-col gap-1 ml-2 flex-shrink-0">
            <button onclick="visualizzaBiglietto('${b.codiceBiglietto}')" class="btn-tertiary text-xs" title="Visualizza dettagli">
              <i class="fa-solid fa-eye mr-1"></i>Visualizza
            </button>
            ${b.stato === 'Issued' || b.stato === 'Validated' ? `<button onclick="downloadPdf(${b.ordineId})" class="btn-tertiary text-xs" title="Scarica PDF"><i class="fa-solid fa-file-pdf mr-1"></i>PDF</button>` : ''}
          </div>
        </div>
      </div>`;
  }).join('');

  if (bigliettiShown < allBiglietti.length) {
    // Variabile remaining: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const remaining = allBiglietti.length - bigliettiShown;
    container.innerHTML += `
      <div class="text-center mt-3">
        <button onclick="caricaAltriBiglietti()" class="btn-outline text-sm">
          <i class="fa-solid fa-chevron-down mr-1"></i>Carica di più (${remaining} rimasti)
        </button>
      </div>`;
  }
}

// Funzione caricaAltriBiglietti: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function caricaAltriBiglietti() {
  bigliettiShown += BIGLIETTI_PER_PAGE;
  renderBiglietti();
}

// Funzione visualizzaBiglietto: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function visualizzaBiglietto(codiceBiglietto) {
  const b = allBiglietti.find(t => t.codiceBiglietto === codiceBiglietto);
  if (!b) return;

  // Variabile dettaglioHtml: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let dettaglioHtml = '';
  try {
    // Variabile detail: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const detail = await API.getBiglietto(b.id);
    dettaglioHtml = buildBigliettoDetailHtml(detail);
  } catch {
    dettaglioHtml = buildBigliettoDetailHtml(b);
  }

  // Variabile modal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const modal = document.getElementById('biglietto-modal');
  // Variabile content: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const content = document.getElementById('biglietto-modal-content');
  if (modal && content) {
    content.innerHTML = dettaglioHtml;
    modal.classList.remove('hidden');
  }
}

// Funzione buildBigliettoDetailHtml: costruisce una struttura dati o una selezione ordinata per la UI. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function buildBigliettoDetailHtml(b) {
  // Variabile startDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const startDate = new Date(b.startAtUtc);
  // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const dateStr = startDate.toLocaleDateString('it-IT', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  // Variabile statoClass: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const statoClass = b.stato === 'Issued' ? 'text-emerald-500' : b.stato === 'Validated' ? 'text-blue-500' : 'text-body';
  // Variabile statoLabel: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const statoLabel = b.stato === 'Issued' ? 'Emesso' : b.stato === 'Validated' ? 'Validato' : b.stato === 'Cancelled' ? 'Annullato' : b.stato;

  return `
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <h3 class="text-xl font-bold text-ink">${b.filmTitolo}</h3>
        <span class="${statoClass} text-sm font-semibold">${statoLabel}</span>
      </div>
      <div class="grid grid-cols-2 gap-3 text-sm">
        <div>
          <p class="text-body">Cinema</p>
          <p class="text-ink font-medium">${b.cinemaNome}</p>
        </div>
        <div>
          <p class="text-body">Sala</p>
          <p class="text-ink font-medium">${b.salaNome}</p>
        </div>
        <div>
          <p class="text-body">Data e ora</p>
          <p class="text-ink font-medium">${dateStr}</p>
        </div>
        <div>
          <p class="text-body">Posto</p>
          <p class="text-ink font-medium">${b.settore} - Fila ${b.fila}, Posto ${b.numero}</p>
        </div>
        <div>
          <p class="text-body">Prezzo</p>
          <p class="text-ferrari-primary font-bold">${formatCurrency(b.prezzoTotale)}</p>
        </div>
        <div>
          <p class="text-body">Codice</p>
          <p class="text-ink font-mono text-xs">${b.codiceBiglietto}</p>
        </div>
      </div>
      ${b.validatoAtUtc ? `<div class="border-t border-hairline pt-3"><p class="text-sm text-blue-500"><i class="fa-solid fa-check-circle mr-1"></i>Validato il ${new Date(b.validatoAtUtc).toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</p></div>` : ''}
      <div class="border-t border-hairline pt-3 flex gap-2">
        <button onclick="chiudiBigliettoModal()" class="btn-outline flex-1 text-sm">Chiudi</button>
        ${b.stato === 'Issued' || b.stato === 'Validated' ? `<button onclick="downloadPdf(${b.ordineId})" class="btn-primary flex-1 text-sm"><i class="fa-solid fa-file-pdf mr-1"></i>Scarica PDF</button>` : ''}
      </div>
    </div>`;
}

// Funzione chiudiBigliettoModal: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function chiudiBigliettoModal() {
  // Variabile modal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const modal = document.getElementById('biglietto-modal');
  if (modal) modal.classList.add('hidden');
}

// Funzione loadPrenotazioniLegacy: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadPrenotazioniLegacy() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('prenotazioni-list');
  try {
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = await API.getPrenotazioni();
    // Variabile prenotazioni: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const prenotazioni = normalizeCollection(data);

    if (!prenotazioni.length) {
      container.innerHTML = `
        <div class="text-center py-4 text-body">
          <p class="text-sm">Nessuna prenotazione legacy</p>
        </div>`;
      return;
    }

    container.innerHTML = prenotazioni.map(p => {
      // Variabile oraDisplay: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const oraDisplay = formatTime(p.oraProiezione);
      return `
        <div class="border border-hairline p-3 mb-2">
          <div class="flex justify-between items-start">
            <div class="flex-1 min-w-0">
              <h3 class="font-semibold text-ink text-sm truncate">${p.titoloFilm || 'Film'}</h3>
              <p class="text-xs text-body">${p.nomeCinema || 'Cinema'} - ${formatDate(p.dataProiezione)}${oraDisplay ? ' alle ' + oraDisplay : ''}</p>
              <p class="text-xs text-body">${p.numeroPosti} post${p.numeroPosti > 1 ? 'i' : 'o'}</p>
            </div>
            <button onclick="window.deletePrenotazione(${p.id})" class="text-ferrari-semantic-warning hover:text-red-400 p-1 ml-2" title="Annulla">
              <i class="fa-solid fa-trash-can text-xs"></i>
            </button>
          </div>
        </div>`;
    }).join('');
  } catch {
    container.innerHTML = `<p class="text-sm text-body text-center py-4">Errore caricamento prenotazioni</p>`;
  }
}

// Funzione deletePrenotazione: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function deletePrenotazione(id) {
  if (!confirm('Sei sicuro di voler annullare questa prenotazione?')) return;
  try {
    await API.deletePrenotazione(id);
    showToast('Prenotazione annullata');
    await loadPrenotazioniLegacy();
  } catch (error) {
    handleApiError(error);
  }
}

// Funzione downloadPdf: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function downloadPdf(orderId) {
  try {
    // Variabile blob: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const blob = await API.getOrdinePdf(orderId);
    // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `biglietti.pdf`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  } catch {
    showToast('Errore nel download del PDF', 'danger');
  }
}

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

// Funzione getStatoBadge: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getStatoBadge(stato) {
  switch (stato) {
    case 'Paid':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-500"><i class="fa-solid fa-check text-[10px]"></i>Pagato</span>';
    case 'Pending':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-500/15 text-amber-500"><i class="fa-solid fa-clock text-[10px]"></i>In attesa</span>';
    case 'Failed':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-red-500/15 text-red-500"><i class="fa-solid fa-xmark text-[10px]"></i>Fallito</span>';
    case 'Cancelled':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-body/15 text-body"><i class="fa-solid fa-ban text-[10px]"></i>Annullato</span>';
    case 'Expired':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-body/15 text-body"><i class="fa-solid fa-hourglass-end text-[10px]"></i>Scaduto</span>';
    default:
      return `<span class="text-xs">${stato}</span>`;
  }
}

// --- Top-up (ricarica credito) ---
// Funzione reconcileTopup: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
async function reconcileTopup(sessionId) {
  try {
    await API.reconcileTopup(sessionId);
  } catch { /* non-critical reconciliation retry */ }
}

// Variabile selectedTopupAmount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let selectedTopupAmount = 0;

// Funzione openTopupModal: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function openTopupModal() {
  selectedTopupAmount = 0;
  document.getElementById('selected-topup-amount').textContent = '0,00 €';
  document.getElementById('custom-topup-amount').value = '';
  document.getElementById('btn-topup-pay').disabled = true;
  document.querySelectorAll('.topup-amount-btn').forEach(b => b.classList.remove('selected'));
  document.getElementById('topup-modal').classList.remove('hidden');
}

// Funzione closeTopupModal: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function closeTopupModal() {
  document.getElementById('topup-modal')?.classList.add('hidden');
}

// Funzione selectTopupAmount: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function selectTopupAmount(amount) {
  selectedTopupAmount = amount;
  document.querySelectorAll('.topup-amount-btn').forEach(b => b.classList.remove('selected'));
  document.querySelector(`.topup-amount-btn[data-amount="${amount}"]`)?.classList.add('selected');
  document.getElementById('custom-topup-amount').value = '';
  document.getElementById('selected-topup-amount').textContent = formatCurrency(amount);
  document.getElementById('btn-topup-pay').disabled = false;
}

// Funzione onCustomTopupChange: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function onCustomTopupChange() {
  document.querySelectorAll('.topup-amount-btn').forEach(b => b.classList.remove('selected'));
  // Variabile val: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const val = parseFloat(document.getElementById('custom-topup-amount').value);
  if (val && val > 0) {
    selectedTopupAmount = val;
    document.getElementById('selected-topup-amount').textContent = formatCurrency(val);
    document.getElementById('btn-topup-pay').disabled = false;
  } else {
    selectedTopupAmount = 0;
    document.getElementById('selected-topup-amount').textContent = '0,00 €';
    document.getElementById('btn-topup-pay').disabled = true;
  }
}

// Funzione payTopup: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function payTopup() {
  if (selectedTopupAmount <= 0) return;
  // Variabile btn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btn = document.getElementById('btn-topup-pay');
  btn.disabled = true;
  btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-1"></i>Reindirizzamento...';
  try {
    // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const result = await API.createTopupStripeSession(selectedTopupAmount);
    if (result?.stripeCheckoutUrl) {
      if (result.stripeCheckoutSessionId) {
        sessionStorage.setItem('pendingTopupSessionId', result.stripeCheckoutSessionId);
      }
      window.location.href = result.stripeCheckoutUrl;
    } else {
      showToast('Errore nella creazione del pagamento', 'danger');
      btn.disabled = false;
      btn.innerHTML = '<i class="fa-solid fa-plus mr-1"></i>Aggiungi Credito';
    }
  } catch (error) {
    handleApiError(error);
    btn.disabled = false;
    btn.innerHTML = '<i class="fa-solid fa-plus mr-1"></i>Aggiungi Credito';
  }
}

// Funzione caricaStato2FA: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function caricaStato2FA() {
  // Variabile statusBadge: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const statusBadge = document.getElementById('2fa-status-badge');
  // Variabile enableBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const enableBtn = document.getElementById('2fa-enable-btn');
  // Variabile disableBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const disableBtn = document.getElementById('2fa-disable-btn');

  try {
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    const response = await fetch(`${API.baseUrl}/auth/me`, {
      headers: API.getAuthHeaders()
    });
    if (!response.ok) return;
    // Variabile user: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const user = await response.json();
    // Variabile twoFactorEnabled: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const twoFactorEnabled = user?.twoFactorEnabled === true;

    if (statusBadge) {
      statusBadge.textContent = twoFactorEnabled ? 'ATTIVO' : 'NON ATTIVO';
      statusBadge.style.background = twoFactorEnabled
        ? 'rgba(16, 185, 129, 0.15)'
        : 'rgba(255, 255, 255, 0.1)';
    }
    if (enableBtn) enableBtn.classList.toggle('hidden', twoFactorEnabled);
    if (disableBtn) disableBtn.classList.toggle('hidden', !twoFactorEnabled);
  } catch {
    // Ignora errori — la sezione sicurezza è opzionale
  }
}

// Funzione disable2FA: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function disable2FA() {
  if (!confirm('Sei sicuro di voler disattivare l\'autenticazione a due fattori?')) return;

  try {
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    const response = await fetch(`${API.baseUrl}/auth/2fa/disable`, {
      method: 'POST',
      headers: API.getAuthHeaders()
    });

    if (response.ok) {
      showToast('2FA disattivato con successo.', 'success');
      caricaStato2FA();
    } else {
      showToast('Errore durante la disattivazione 2FA.', 'error');
    }
  } catch {
    showToast('Impossibile connettersi al server.', 'error');
  }
}

window.disable2FA = disable2FA;
window.deletePrenotazione = deletePrenotazione;
window.visualizzaBiglietto = visualizzaBiglietto;
window.chiudiBigliettoModal = chiudiBigliettoModal;
window.caricaAltriBiglietti = caricaAltriBiglietti;
window.caricaAltriOrdini = caricaAltriOrdini;
window.submitChangePassword = submitChangePassword;
window.submitSetPasswordRequest = submitSetPasswordRequest;

// ─── Account Security (Iteration 5) ────────────────────────────────

// Funzione loadAccountSecurity: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
async function loadAccountSecurity() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var container = document.getElementById('account-security-content');
  if (!container || !Auth.isLoggedIn()) return;

  try {
    // Variabile security: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var security = await Auth.getAccountSecurity();
    renderAccountSecurity(container, security);
  } catch (err) {
    container.innerHTML = '<p class="text-body text-sm">Impossibile caricare le impostazioni di sicurezza.</p>';
  }
}

// Funzione renderAccountSecurity: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderAccountSecurity(container, security) {
  // Variabile html: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var html = '';

  // Provider collegati
  if (security.linkedProviders && security.linkedProviders.length > 0) {
    html += '<div class="mb-4"><p class="text-sm font-medium text-ink mb-2">Provider collegati:</p>';
    security.linkedProviders.forEach(function(p) {
      // Variabile icon: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      var icon = p.provider === 'Google' ? 'fa-google' : p.provider === 'Microsoft' ? 'fa-microsoft' : 'fa-facebook';
      html += '<span class="inline-flex items-center gap-2 mr-3 mb-2 px-3 py-1 text-xs bg-white/5 border border-hairline"><i class="fa-brands ' + icon + '"></i>' + p.name + '</span>';
    });
    html += '</div>';
  }

  // Stato password
  if (security.hasLocalPassword) {
    // Variabile lastChanged: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var lastChanged = security.passwordChangedAtUtc
      ? new Date(security.passwordChangedAtUtc).toLocaleDateString('it-IT')
      : 'mai';

    html += '<div class="mb-4 p-3 bg-emerald-500/10 border border-emerald-500/30">';
    html += '<p class="text-sm font-medium text-emerald-400"><i class="fa-solid fa-circle-check mr-1"></i>Password locale attiva</p>';
    html += '<p class="text-xs text-body mt-1">Ultimo cambio: ' + lastChanged + '</p>';
    html += '</div>';

    // Form cambio password
    html += '<div class="mb-4"><p class="text-sm font-medium text-ink mb-3">Cambia password:</p>';
    html += '<form onsubmit="submitChangePassword(event)" class="space-y-3">';
    html += '<input type="password" id="sec-current-pwd" class="input-ferrari w-full px-3 py-2 text-sm" placeholder="Password attuale" required>';
    html += '<input type="password" id="sec-new-pwd" class="input-ferrari w-full px-3 py-2 text-sm" placeholder="Nuova password (min. 8 caratteri)" required minlength="8">';
    html += '<button type="submit" class="btn-primary text-sm py-2 px-4"><i class="fa-solid fa-key mr-2"></i>Cambia Password</button>';
    html += '</form></div>';
  } else {
    html += '<div class="mb-4 p-3 bg-amber-500/10 border border-amber-500/30">';
    html += '<p class="text-sm font-medium text-amber-400"><i class="fa-solid fa-triangle-exclamation mr-1"></i>Nessuna password locale</p>';
    html += '<p class="text-xs text-body mt-1">Accedi solo tramite social. Imposta una password per maggiore sicurezza.</p>';
    html += '</div>';

    html += '<button onclick="submitSetPasswordRequest()" id="btn-setup-pwd" class="btn-primary text-sm py-2 px-4">';
    html += '<i class="fa-solid fa-envelope mr-2"></i>Invia link per impostare password';
    html += '</button>';
  }

  container.innerHTML = html;
}

// Funzione submitChangePassword: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function submitChangePassword(event) {
  event.preventDefault();
  // Variabile currentPwd: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var currentPwd = document.getElementById('sec-current-pwd').value;
  // Variabile newPwd: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var newPwd = document.getElementById('sec-new-pwd').value;

  if (newPwd.length < 8) {
    showToast('La password deve essere di almeno 8 caratteri.', 'error');
    return;
  }

  try {
    await Auth.changePassword(currentPwd, newPwd);
    showToast('Password cambiata con successo. Effettua nuovamente il login sugli altri dispositivi.', 'success');
    document.getElementById('sec-current-pwd').value = '';
    document.getElementById('sec-new-pwd').value = '';
    // Ricarica stato sicurezza
    loadAccountSecurity();
  } catch (err) {
    showToast(err.message || 'Errore durante il cambio password.', 'error');
  }
}

// Funzione submitSetPasswordRequest: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function submitSetPasswordRequest() {
  // Variabile btn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  var btn = document.getElementById('btn-setup-pwd');
  if (btn) { btn.disabled = true; btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Invio in corso...'; }

  try {
    await Auth.requestSetPassword();
    showToast('Email inviata! Controlla la tua casella di posta per impostare la password.', 'success');
  } catch (err) {
    showToast(err.message || 'Errore durante la richiesta.', 'error');
  } finally {
    if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fa-solid fa-envelope mr-2"></i>Invia link per impostare password'; }
  }
}

