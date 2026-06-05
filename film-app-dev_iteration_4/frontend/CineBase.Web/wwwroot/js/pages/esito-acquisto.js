// Variabile orderId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let orderId = null;
// Variabile ordine: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let ordine = null;
// Variabile pollingInterval: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let pollingInterval = null;
// Variabile returnFlags: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let returnFlags = { success: false, cancelled: false };

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  if (!Auth?.isLoggedIn?.()) {
    window.location.replace('/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search));
    return;
  }

  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const params = new URLSearchParams(window.location.search);
  orderId = parseInt(params.get('orderId'));
  returnFlags = {
    success: params.get('success') === 'true',
    cancelled: params.get('cancelled') === 'true'
  };

  if (!orderId) {
    showError('Parametro orderId mancante');
    return;
  }

  if (returnFlags.success) {
    // Non bloccare il rendering: riconcilia in background, il polling gestirà la convergenza
    reconcileAfterStripeReturn();
  }

  await loadOrdine();
});

// Funzione reconcileAfterStripeReturn: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function reconcileAfterStripeReturn() {
  try {
    await API.reconcileCheckoutSession(orderId);
  } catch {
    // Il webhook potrebbe non essere ancora arrivato; il polling sotto gestira la convergenza.
  }
}

// Funzione loadOrdine: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadOrdine() {
  try {
    ordine = await API.getOrdine(orderId);
    if (!ordine) {
      showError('Ordine non trovato');
      return;
    }
    renderEsito();
  } catch (error) {
    showError(error.message || 'Errore caricamento ordine');
  }
}

// Funzione renderEsito: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderEsito() {
  document.getElementById('loading-state').classList.add('hidden');
  document.getElementById('main-content').classList.remove('hidden');

  if (pollingInterval) {
    clearInterval(pollingInterval);
    pollingInterval = null;
  }

  renderHeader();
  renderOrderDetails();
  renderTickets();
  renderEmailStatus();
  setupActions();
}

// Funzione renderHeader: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderHeader() {
  // Variabile stato: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const stato = ordine.stato;
  // Variabile successHeader: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const successHeader = document.getElementById('success-header');
  // Variabile pendingHeader: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const pendingHeader = document.getElementById('pending-header');
  // Variabile failedHeader: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const failedHeader = document.getElementById('failed-header');

  successHeader.classList.add('hidden');
  pendingHeader.classList.add('hidden');
  failedHeader.classList.add('hidden');

  if (stato === 'Paid') {
    successHeader.classList.remove('hidden');
    // Force notification reload so badge updates immediately
    document.dispatchEvent(new CustomEvent('components:loaded'));
  } else if (stato === 'Pending' || stato === 'CheckoutInProgress') {
    pendingHeader.classList.remove('hidden');

    if (stato === 'CheckoutInProgress') {
      // Variabile h1: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const h1 = pendingHeader.querySelector('h1');
      const p = pendingHeader.querySelector('p');
      if (h1) h1.textContent = 'Verifica pagamento in corso...';
      if (p) p.textContent = 'Stiamo verificando il pagamento con Stripe';
    } else {
      // Variabile h1: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const h1 = pendingHeader.querySelector('h1');
      const p = pendingHeader.querySelector('p');
      if (h1) h1.textContent = 'Pagamento in elaborazione';
      if (p) p.textContent = 'Attendi la conferma del pagamento';
    }

    pollingInterval = setInterval(async () => {
      try {
        // Variabile updatedOrdine: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        let updatedOrdine;

        if (returnFlags.success) {
          // Variabile checkoutStatus: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const checkoutStatus = await API.reconcileCheckoutSession(orderId);
          updatedOrdine = checkoutStatus?.ordine || await API.getOrdine(orderId);
        } else {
          updatedOrdine = await API.getOrdine(orderId);
        }

        if (updatedOrdine && updatedOrdine.stato !== ordine.stato) {
          ordine = updatedOrdine;
          renderEsito();
        } else if (updatedOrdine && updatedOrdine.stato === 'Paid' && ordine.stato === 'Paid') {
          ordine = updatedOrdine;
          renderEsito();
          // Reload notifications on each polling tick while Paid (badge may need updating)
          document.dispatchEvent(new CustomEvent('components:loaded'));
        }
      } catch {
      }
    }, 3000);
  } else if (stato === 'Cancelled') {
    failedHeader.classList.remove('hidden');
    // Variabile h1: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const h1 = failedHeader.querySelector('h1');
    const p = failedHeader.querySelector('p');
    if (h1) h1.textContent = 'Pagamento annullato';
    if (p) p.textContent = 'L\'ordine e stato annullato. Puoi riprovare.';
  } else if (stato === 'Expired') {
    failedHeader.classList.remove('hidden');
    // Variabile h1: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const h1 = failedHeader.querySelector('h1');
    const p = failedHeader.querySelector('p');
    if (h1) h1.textContent = 'Ordine scaduto';
    if (p) p.textContent = 'Il tempo per completare il pagamento e scaduto.';
  } else {
    failedHeader.classList.remove('hidden');
  }
}

// Funzione renderOrderDetails: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderOrderDetails() {
  document.getElementById('order-code').textContent = ordine.codiceOrdine;

  // Variabile startDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const startDate = new Date(ordine.startAtUtc);
  // Variabile dateOptions: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const dateOptions = { weekday: 'short', day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' };
  // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const dateStr = startDate.toLocaleDateString('it-IT', dateOptions);

  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('order-details');
  container.innerHTML = `
    <div class="flex justify-between text-sm">
      <span class="text-body">Film</span>
      <span class="font-medium text-ink">${ordine.filmTitolo}</span>
    </div>
    <div class="flex justify-between text-sm">
      <span class="text-body">Cinema</span>
      <span class="font-medium text-ink">${ordine.cinemaNome}</span>
    </div>
    <div class="flex justify-between text-sm">
      <span class="text-body">Sala</span>
      <span class="font-medium text-ink">${ordine.salaNome}</span>
    </div>
    <div class="flex justify-between text-sm">
      <span class="text-body">Data e ora</span>
      <span class="font-medium text-ink">${dateStr}</span>
    </div>
    <div class="flex justify-between text-sm">
      <span class="text-body">Numero biglietti</span>
      <span class="font-medium text-ink">${ordine.numeroBiglietti}</span>
    </div>
    <div class="flex justify-between text-sm">
      <span class="text-body">Stato</span>
      <span class="font-medium">${getStatoBadge(ordine.stato)}</span>
    </div>
  `;

  document.getElementById('detail-importo-carta').textContent = formatCurrency(ordine.importoCarta || 0);
  document.getElementById('detail-importo-credito').textContent = formatCurrency(ordine.importoCredito || 0);
  document.getElementById('detail-totale').textContent = formatCurrency(ordine.totaleLordo);
}

// Funzione renderTickets: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderTickets() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('tickets-list');
  // Variabile tickets: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tickets = ordine.biglietti || [];

  document.getElementById('tickets-count').textContent = `${tickets.length} bigliett${tickets.length === 1 ? 'o' : 'i'}`;

  if (!tickets.length) {
    container.innerHTML = `<p class="text-sm text-body text-center py-4">Nessun biglietto emesso</p>`;
    return;
  }

  container.innerHTML = tickets.map(t => `
    <div class="flex items-center justify-between p-3 border border-hairline bg-canvas">
      <div class="flex items-center gap-3">
        <div class="flex-shrink-0 w-10 h-10 bg-ferrari-primary/15 flex items-center justify-center">
          <i class="fa-solid fa-ticket text-ferrari-primary"></i>
        </div>
        <div>
          <p class="font-semibold text-ink text-sm">
            ${t.settore} - Fila ${t.fila}, Posto ${t.numero}
          </p>
          <p class="text-xs text-body font-mono">${t.codiceBiglietto}</p>
        </div>
      </div>
      <div class="text-right">
        <p class="font-semibold text-ferrari-primary text-sm">${formatCurrency(t.prezzoTotale)}</p>
        <p class="text-xs ${t.stato === 'Issued' ? 'text-emerald-500' : t.stato === 'Validated' ? 'text-blue-500' : 'text-body'}">${getStatoBiglietto(t.stato)}</p>
      </div>
    </div>
  `).join('');
}

// Funzione renderEmailStatus: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderEmailStatus() {
  // Variabile card: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const card = document.getElementById('email-status-card');
  // Variabile icon: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const icon = document.getElementById('email-status-icon');
  // Variabile text: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const text = document.getElementById('email-status-text');
  // Variabile detail: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const detail = document.getElementById('email-status-detail');

  if (ordine.stato !== 'Paid') {
    card.classList.add('hidden');
    return;
  }

  card.classList.remove('hidden');

  if (ordine.ticketEmailSentAtUtc) {
    // Variabile sentDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const sentDate = new Date(ordine.ticketEmailSentAtUtc);
    icon.className = 'fa-solid fa-circle-check text-emerald-500 text-lg';
    text.textContent = 'Email di conferma inviata';
    detail.textContent = `Inviata il ${sentDate.toLocaleDateString('it-IT', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' })}`;
  } else if (ordine.ticketEmailLastError) {
    icon.className = 'fa-solid fa-triangle-exclamation text-amber-500 text-lg';
    text.textContent = 'Invio email non riuscito';
    detail.textContent = 'Puoi scaricare il PDF dei biglietti qui sotto';
  } else {
    icon.className = 'fa-solid fa-paper-plane text-blue-500 text-lg';
    text.textContent = 'Email in fase di invio';
    detail.textContent = 'Riceverai i biglietti via email a breve';
  }
}

// Funzione setupActions: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupActions() {
  // Variabile btnPdf: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnPdf = document.getElementById('btn-download-pdf');
  btnPdf.onclick = async () => {
    btnPdf.disabled = true;
    btnPdf.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Download in corso...';

    try {
      // Variabile blob: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const blob = await API.getOrdinePdf(orderId);
      // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `biglietti-${ordine.codiceOrdine}.pdf`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (error) {
      showToast('Errore nel download del PDF', 'danger');
    } finally {
      btnPdf.disabled = false;
      btnPdf.innerHTML = '<i class="fa-solid fa-file-pdf mr-2"></i>Scarica PDF';
    }
  };
}

// Funzione getStatoBadge: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getStatoBadge(stato) {
  switch (stato) {
    case 'Paid':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-500"><i class="fa-solid fa-check text-[10px]"></i>Pagato</span>';
    case 'Pending':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-500/15 text-amber-500"><i class="fa-solid fa-clock text-[10px]"></i>In attesa</span>';
    case 'CheckoutInProgress':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-blue-500/15 text-blue-500"><i class="fa-solid fa-spinner fa-spin text-[10px]"></i>Checkout in corso</span>';
    case 'Failed':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-red-500/15 text-red-500"><i class="fa-solid fa-xmark text-[10px]"></i>Fallito</span>';
    case 'Cancelled':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-canvas-elevated/50 text-body"><i class="fa-solid fa-ban text-[10px]"></i>Annullato</span>';
    case 'Expired':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-canvas-elevated/50 text-body"><i class="fa-solid fa-hourglass-end text-[10px]"></i>Scaduto</span>';
    default:
      return stato;
  }
}

// Funzione getStatoBiglietto: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getStatoBiglietto(stato) {
  switch (stato) {
    case 'Issued': return 'Emesso';
    case 'Validated': return 'Validato';
    case 'Cancelled': return 'Annullato';
    default: return stato;
  }
}

// Funzione showError: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function showError(message) {
  document.getElementById('loading-state').classList.add('hidden');
  document.getElementById('error-state').classList.remove('hidden');
  document.getElementById('main-content').classList.add('hidden');
  // Variabile msgEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const msgEl = document.getElementById('error-message');
  if (msgEl) msgEl.textContent = message;
}

// Cleanup intervals on page unload
// Listener evento: si attiva quando scatta l'evento e aggiorna UI o stato.
window.addEventListener('beforeunload', () => {
  if (pollingInterval) clearInterval(pollingInterval);
});
