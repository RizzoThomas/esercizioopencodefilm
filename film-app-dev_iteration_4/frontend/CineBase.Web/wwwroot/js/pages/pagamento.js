// Variabile orderId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let orderId = null;
// Variabile ordine: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let ordine = null;
// Variabile creditoData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let creditoData = null;
// Variabile frontendConfig: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let frontendConfig = null;
// Variabile urlParams: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const urlParams = new URLSearchParams(window.location.search);
// Variabile offertaIdFromUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const offertaIdFromUrl = urlParams.get('offertaId');
// Variabile showIdFromUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const showIdFromUrl = urlParams.get('showId');
// Variabile abbonamentoIdFromUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const abbonamentoIdFromUrl = urlParams.get('abbonamentoId');
// Variabile stripeStatusFromUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const stripeStatusFromUrl = urlParams.get('stripe');
// Variabile paymentFlowMode: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let paymentFlowMode = 'order';

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  // Reset button state when page loads (fix for stuck "Elaborazione pagamento...")
  resetPayButton();
  
  if (!Auth?.isLoggedIn?.()) {
    window.location.replace('/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search));
    return;
  }

  if (abbonamentoIdFromUrl) {
    paymentFlowMode = 'abbonamento';
    await Promise.all([loadCredito(), loadFrontendConfig()]);
    if (stripeStatusFromUrl === 'success') {
      await finalizeAbbonamentoAfterStripe(abbonamentoIdFromUrl);
      return;
    }
    await loadAbbonamentoPayment(abbonamentoIdFromUrl);
    setupActions();
    return;
  }

  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const params = new URLSearchParams(window.location.search);
  orderId = parseInt(params.get('orderId'));

  if (!orderId) {
    showError('Parametro orderId mancante');
    return;
  }

  await Promise.all([loadOrdine(), loadCredito(), loadFrontendConfig()]);

  if (offertaIdFromUrl) {
    paymentFlowMode = 'offerta';
    await loadOffertaDiscount(offertaIdFromUrl);
  }

  if (!ordine) return;
  
  // Se l'ordine è bloccato in CheckoutInProgress (checkout abbandonato), prova a cancellarlo
  if (ordine.stato === 'CheckoutInProgress') {
    try {
      // Variabile checkoutStatus: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const checkoutStatus = await API.reconcileCheckoutSession(orderId);
      if (checkoutStatus?.ordine) ordine = checkoutStatus.ordine;
    } catch { /* ignore */ }
    
    // Se ancora CheckoutInProgress dopo reconcile, cancella l'ordine
    if (ordine.stato === 'CheckoutInProgress') {
      try {
        await API.cancelOrdine(orderId);
        ordine = await API.getOrdine(orderId);
      } catch { /* ignore */ }
    }
    
    // Redirect con parametro cancelled per la pagina di esito
    window.location.href = `/esito-acquisto.html?orderId=${ordine.id}&cancelled=true`;
    return;
  }

  if (ordine.stato !== 'Pending') {
    window.location.href = `/esito-acquisto.html?orderId=${ordine.id}`;
    return;
  }

  renderOrderSummary();
  setupPaymentOptions();
  setupActions();

  // Detect back-button return from Stripe: cancel abandoned checkout
  window.addEventListener('pageshow', async (e) => {
    if (paymentFlowMode !== 'order') return;
    if (e.persisted) {
      // Page restored from bfcache (user clicked back from Stripe)
      try { ordine = await API.getOrdine(orderId); } catch {}
      if (ordine?.stato === 'CheckoutInProgress' || ordine?.stato === 'Pending') {
        try { await API.cancelOrdine(orderId); } catch {}
        showToast('Pagamento non completato. Ordine annullato.', 'warning');
        setTimeout(() => {
          // Variabile backUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const backUrl = new URL('/acquista.html', window.location.origin);
          if (ordine?.showId) backUrl.searchParams.set('showId', ordine.showId);
          if (offertaIdFromUrl) backUrl.searchParams.set('offertaId', offertaIdFromUrl);
          window.location.href = backUrl.toString();
        }, 1500);
      }
    }
  });
});

// Funzione loadFrontendConfig: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadFrontendConfig() {
  try {
    frontendConfig = await API.getFrontendConfig();
  } catch {
    frontendConfig = null;
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
  } catch (error) {
    showError(error.message || 'Errore caricamento ordine');
  }
}

// Funzione loadCredito: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCredito() {
  try {
    creditoData = await API.getCreditoMe();
  } catch {
    creditoData = { saldoAttuale: 0 };
  }
}

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

// Funzione hideOrderOnlyControls: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function hideOrderOnlyControls() {
  document.getElementById('option-misto')?.classList.add('hidden');
  document.getElementById('option-ticket')?.classList.add('hidden');
  document.getElementById('credit-slider-section')?.classList.add('hidden');
  document.getElementById('btn-cancel')?.classList.add('hidden');
  document.getElementById('order-summary-card')?.classList.add('hidden');
}

// Funzione loadOffertaDiscount: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadOffertaDiscount(offertaId) {
  try {
    // Variabile offers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const offers = normalizeCollection(await API.getOfferte());
    // Variabile offerta: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const offerta = offers.find((offer) => String(offer.id) === String(offertaId)) || null;
    if (!offerta) throw new Error('Offerta non trovata');

    hideOrderOnlyControls();
    document.getElementById('offerta-banner')?.classList.remove('hidden');
    document.getElementById('offerta-banner-name').textContent = offerta.nome || 'Offerta';
    document.getElementById('offerta-banner-desc').textContent = offerta.descrizione || '';

    // Salva il totale originale prima di applicare lo sconto
    const totaleOriginale = ordine?.totaleLordo || 0;
    ordine = ordine || {};
    ordine.totaleOriginale = totaleOriginale;
    ordine.totaleLordo = offerta.prezzo;
    ordine.offertaId = offertaId;
    window._offertaData = { id: offertaId, nome: offerta.nome, prezzo: offerta.prezzo, totaleOriginale: totaleOriginale };

    document.getElementById('order-total').textContent = formatCurrency(offerta.prezzo);

    updatePayButtonText();
  } catch (e) {
    showError(e?.message || 'Offerta non trovata');
  }
}

// Funzione finalizeAbbonamentoAfterStripe: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function finalizeAbbonamentoAfterStripe(abbonamentoId) {
  // Variabile marker: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const marker = `abbonamento-finalized-${abbonamentoId}`;
  if (sessionStorage.getItem(marker)) {
    window.location.href = '/profilo.html';
    return;
  }

  try {
    // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
    const res = await fetch((window.API_BASE_URL || 'http://localhost:5000') + '/abbonamenti/' + abbonamentoId + '/attiva', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + Auth.getAccessToken()
      },
      body: JSON.stringify({ metodoPagamento: 'carta', autoRinnovo: true })
    });

    if (!res.ok) {
      // Variabile err: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Attivazione abbonamento fallita');
    }

    sessionStorage.setItem(marker, '1');
    showToast('Abbonamento attivato!', 'success');
    setTimeout(() => window.location.href = '/profilo.html', 1200);
  } catch (e) {
    showError(e?.message || 'Attivazione abbonamento fallita');
  }
}

// Funzione loadAbbonamentoPayment: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadAbbonamentoPayment(abbonamentoId) {
  try {
    // Variabile abbonamenti: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const abbonamenti = normalizeCollection(await API.getAbbonamenti());
    // Variabile abb: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const abb = abbonamenti.find((item) => String(item.id) === String(abbonamentoId)) || null;
    if (!abb) throw new Error('Abbonamento non trovato');

    hideOrderOnlyControls();
    document.getElementById('abbonamento-banner')?.classList.remove('hidden');
    document.getElementById('abbonamento-banner').classList.remove('hidden');
    document.getElementById('abbonamento-banner-name').textContent = abb.nome || 'Abbonamento';
    document.getElementById('abbonamento-banner-desc').textContent = abb.descrizione || '';

    hideLoading();
    document.getElementById('main-content').classList.remove('hidden');

    // Mostra il credito disponibile (non veniva mostrato nel flusso abbonamento)
    document.getElementById('credit-balance').textContent = formatCurrency(creditoData?.saldoAttuale || 0);

    // Variabile prezzo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const prezzo = abb.prezzoAnnuale || abb.prezzo || 0;
    document.getElementById('order-summary').innerHTML = `
      <div class="flex justify-between text-sm">
        <span class="text-body">Tipo</span>
        <span class="font-medium text-ink">${abb.tipo || '-'}</span>
      </div>
      <div class="flex justify-between text-sm">
        <span class="text-body">Biglietti/mese</span>
        <span class="font-medium text-ink">${abb.numeroBigliettiPerMese || 0}</span>
      </div>
    `;

    document.getElementById('order-total').textContent = formatCurrency(prezzo);
    window._abbonamentoData = { id: abbonamentoId, prezzo, tipo: abb.tipo, nome: abb.nome };
    ordine = { totaleLordo: prezzo, showId: 0 };

    setupPaymentOptions();
    onPaymentMethodChange(document.querySelector('input[name="payment-method"]:checked')?.value || 'carta');
  } catch (e) {
    showError(e?.message || 'Abbonamento non trovato');
  }
}

// Funzione renderOrderSummary: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderOrderSummary() {
  hideLoading();
  document.getElementById('main-content').classList.remove('hidden');

  document.getElementById('credit-balance').textContent = formatCurrency(creditoData?.saldoAttuale || 0);

  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('order-summary');
  // Variabile startDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const startDate = new Date(ordine.startAtUtc);
  // Variabile dateOptions: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const dateOptions = { weekday: 'short', day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' };
  // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const dateStr = startDate.toLocaleDateString('it-IT', dateOptions);

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
      <span class="text-body">Codice ordine</span>
      <span class="font-mono text-ink">${ordine.codiceOrdine}</span>
    </div>
  `;

  if (window._offertaData) {
    // Variabile risparmio: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const risparmio = (window._offertaData.totaleOriginale || ordine.totaleOriginale || 0) - (window._offertaData.prezzo || 0);
    container.insertAdjacentHTML('beforeend', `
      <div class="border-t border-hairline pt-3 mt-3">
        <div class="flex justify-between text-sm text-body line-through">
          <span>Prezzo pieno</span>
          <span>${formatCurrency(window._offertaData.totaleOriginale || ordine.totaleOriginale || 0)}</span>
        </div>
        <div class="flex justify-between text-sm text-ferrari-primary font-medium">
          <span>Offerta "${window._offertaData.nome}"</span>
          <span>−${formatCurrency(risparmio)}</span>
        </div>
      </div>
    `);
  }

  document.getElementById('order-total').textContent = formatCurrency(ordine.totaleLordo);
  updatePayButtonText();
}

// Funzione setupPaymentOptions: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupPaymentOptions() {
  // Variabile saldo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const saldo = creditoData?.saldoAttuale || 0;
  // Variabile totale: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totale = ordine?.totaleLordo || 0;

  // Variabile optionCredito: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const optionCredito = document.getElementById('option-credito');
  // Variabile optionMisto: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const optionMisto = document.getElementById('option-misto');
  // Variabile creditOnlyDesc: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const creditOnlyDesc = document.getElementById('credit-only-desc');

  if (saldo < totale) {
    optionCredito.querySelector('input').disabled = true;
    optionCredito.classList.add('opacity-50', 'cursor-not-allowed');
    creditOnlyDesc.textContent = `Credito insufficiente (disponibili ${formatCurrency(saldo)})`;
  }

  if (saldo <= 0) {
    optionMisto.querySelector('input').disabled = true;
    optionMisto.classList.add('opacity-50', 'cursor-not-allowed');
  }

  // Variabile slider: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const slider = document.getElementById('credit-slider');
  slider.max = Math.min(saldo, Math.max(0, totale - 0.01));
  slider.value = 0;

  document.querySelectorAll('input[name="payment-method"]').forEach(radio => {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    radio.addEventListener('change', () => {
      onPaymentMethodChange(radio.value);
    });
  });

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  slider.addEventListener('input', () => {
    updateSplitDisplay();
  });
}

// Funzione onPaymentMethodChange: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function onPaymentMethodChange(method) {
  // Variabile stripeInfoSection: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const stripeInfoSection = document.getElementById('stripe-info-section');
  // Variabile sliderSection: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const sliderSection = document.getElementById('credit-slider-section');
  // Variabile ticketCodeSection: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const ticketCodeSection = document.getElementById('ticket-code-section');
  // Variabile saldo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const saldo = creditoData?.saldoAttuale || 0;
  // Variabile totale: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totale = ordine?.totaleLordo || 0;

  sliderSection.classList.add('hidden');
  stripeInfoSection.classList.add('hidden');
  ticketCodeSection?.classList.add('hidden');

  switch (method) {
    case 'carta':
      stripeInfoSection.classList.remove('hidden');
      break;
    case 'credito':
      break;
    case 'misto':
      sliderSection.classList.remove('hidden');
      stripeInfoSection.classList.remove('hidden');
      // Variabile slider: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const slider = document.getElementById('credit-slider');
      slider.max = Math.min(saldo, Math.max(0, totale - 0.01));
      slider.value = Math.min(saldo, Math.max(0, totale - 0.01));
      updateSplitDisplay();
      break;
    case 'ticket':
      ticketCodeSection?.classList.remove('hidden');
      break;
  }

  updatePayButtonText();
}

// Funzione updateSplitDisplay: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateSplitDisplay() {
  // Variabile slider: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const slider = document.getElementById('credit-slider');
  // Variabile creditAmount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const creditAmount = parseFloat(slider.value);
  // Variabile totale: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totale = ordine?.totaleLordo || 0;
  // Variabile cardAmount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cardAmount = totale - creditAmount;

  document.getElementById('credit-amount-label').textContent = `Credito: ${formatCurrency(creditAmount)}`;
  document.getElementById('card-amount-label').textContent = `Carta: ${formatCurrency(cardAmount)}`;

  updatePayButtonText();
}

// Funzione updatePayButtonText: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updatePayButtonText() {
  // Variabile method: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const method = document.querySelector('input[name="payment-method"]:checked')?.value || 'carta';
  // Variabile totale: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totale = ordine?.totaleLordo || 0;
  // Variabile amount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let amount = totale;

  if (method === 'credito') {
    amount = totale;
  } else if (method === 'misto') {
    // Variabile slider: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const slider = document.getElementById('credit-slider');
    // Variabile creditUsed: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const creditUsed = parseFloat(slider?.value || 0);
    amount = totale - creditUsed;
  }

  // Variabile btnText: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnText = document.getElementById('pay-button-text');
  if (method === 'credito') {
    btnText.textContent = `Paga ${formatCurrency(totale)} con credito`;
  } else if (method === 'misto' && amount <= 0) {
    btnText.textContent = `Paga ${formatCurrency(totale)} con credito`;
  } else if (method === 'ticket') {
    btnText.textContent = 'Riscatta voucher';
  } else {
    btnText.textContent = `Paga ${formatCurrency(amount)} con carta`;
  }
}

// Funzione setupActions: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupActions() {
  // Variabile btnPay: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnPay = document.getElementById('btn-pay');
  // Variabile btnCancel: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnCancel = document.getElementById('btn-cancel');

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  btnPay.addEventListener('click', async () => {
    await handlePayment();
  });

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  btnCancel?.addEventListener('click', async () => {
    btnCancel.disabled = true;
    btnCancel.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Annullamento...';

    try {
      await API.cancelOrdine(orderId);
      window.location.href = `/acquista.html?showId=${ordine?.showId}`;
    } catch (error) {
      handleApiError(error);
      btnCancel.disabled = false;
      btnCancel.innerHTML = '<i class="fa-solid fa-arrow-left mr-2"></i>Annulla e torna ai posti';
    }
  });
}

// Funzione handlePayment: gestisce un evento o una risposta utente. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function handlePayment() {
  // Variabile btnPay: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnPay = document.getElementById('btn-pay');
  btnPay.disabled = true;
  btnPay.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Elaborazione pagamento...';

  // Variabile method: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let method = 'carta'; // hoisted for catch-block access

  try {
    // Refresh order state — prevent paying an already-paid order
    try { ordine = await API.getOrdine(orderId); } catch {}
    if (ordine?.stato === 'Paid') {
      window.location.href = `/esito-acquisto.html?orderId=${orderId}&success=true`;
      return;
    }

    // Se c'è un'offerta, usa il prezzo scontato (non il totale pieno dell'ordine)
    if (window._offertaData && ordine) {
      ordine.totaleLordo = window._offertaData.prezzo;
    }

    if (window._abbonamentoData) {
      // Variabile method: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const method = document.querySelector('input[name="payment-method"]:checked')?.value || 'carta';
      if (method === 'credito') {
        try {
          // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
          const res = await fetch((window.API_BASE_URL || 'http://localhost:5000') + '/abbonamenti/' + window._abbonamentoData.id + '/attiva', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Authorization': 'Bearer ' + Auth.getAccessToken()
            },
            body: JSON.stringify({ metodoPagamento: 'credito', autoRinnovo: true })
          });

          if (res.ok) {
            showToast('Abbonamento attivato!', 'success');
            setTimeout(() => window.location.href = '/profilo.html', 1500);
            return;
          }

          // Variabile err: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const err = await res.json().catch(() => ({}));
          showToast(res.status === 409 ? 'Hai già un abbonamento attivo. Controlla il tuo profilo.' : (err.message || err.title || 'Errore attivazione'), 'danger');
        } catch (e) {
          showToast('Errore di rete', 'danger');
        }

        btnPay.disabled = false;
        btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Riprova pagamento</span>';
        return;
      }

      // Variabile idempotencyKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const idempotencyKey = `abbonamento-${window._abbonamentoData.id}-${Date.now()}`;
      // Variabile session: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const session = await API.createAbbonamentoStripeCheckoutSession(window._abbonamentoData.id, idempotencyKey);

      if (session?.stripeCheckoutUrl) {
        window.location.href = session.stripeCheckoutUrl;
        return;
      }

      try {
        // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
        const res = await fetch((window.API_BASE_URL || 'http://localhost:5000') + '/abbonamenti/' + window._abbonamentoData.id + '/attiva', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + Auth.getAccessToken()
          },
          body: JSON.stringify({ metodoPagamento: 'carta', autoRinnovo: true })
        });

        if (res.ok) {
          showToast('Abbonamento attivato!', 'success');
          setTimeout(() => window.location.href = '/profilo.html', 1500);
          return;
        }

        // Variabile err: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const err = await res.json().catch(() => ({}));
        showToast(res.status === 409 ? 'Hai già un abbonamento attivo. Controlla il tuo profilo.' : (err.message || err.title || 'Errore attivazione'), 'danger');
      } catch (e) {
        showToast('Errore di rete', 'danger');
      }

      btnPay.disabled = false;
      btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Riprova pagamento</span>';
      return;
    }

    method = document.querySelector('input[name="payment-method"]:checked')?.value || 'carta';
    // Variabile importoCreditoRichiesto: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let importoCreditoRichiesto = null;
    // Variabile offertaId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const offertaId = window._offertaData?.id || null;

    if (method === 'credito') {
      importoCreditoRichiesto = ordine.totaleLordo;
    } else if (method === 'misto') {
      // Variabile slider: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const slider = document.getElementById('credit-slider');
      importoCreditoRichiesto = parseFloat(slider.value);
    }

    if (method === 'credito') {
      // Variabile saldo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const saldo = creditoData?.saldoAttuale || 0;
      if (saldo < ordine.totaleLordo) {
        showToast('Credito insufficiente. Hai ' + formatCurrency(saldo) + ', servono ' + formatCurrency(ordine.totaleLordo) + '.', 'danger');
        btnPay.disabled = false;
        btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Paga</span>';
        return;
      }

      // Variabile metodoPagamento: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const metodoPagamento = 'Credito';
      // Variabile idempotencyKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const idempotencyKey = 'pay-' + orderId + '-' + Date.now();
      console.log('[PAGAMENTO] Paying with credit, orderId=' + orderId);
      // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const result = await API.payOrdine(orderId, metodoPagamento, importoCreditoRichiesto, idempotencyKey, null, offertaId);
      console.log('[PAGAMENTO] Credit payment result:', result);
      window.location.replace('/esito-acquisto.html?orderId=' + orderId + '&success=true');
      return;
    }

    if (method === 'ticket') {
      // Variabile codiceVoucher: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const codiceVoucher = document.getElementById('ticket-code-input')?.value?.trim();
      if (!codiceVoucher) {
        showToast('Inserisci il codice del voucher.', 'warning');
        btnPay.disabled = false;
        btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Paga</span>';
        return;
      }
      // Variabile metodoPagamento: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const metodoPagamento = 'Ticket';
      // Variabile idempotencyKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const idempotencyKey = 'pay-' + orderId + '-' + Date.now();
      console.log('[PAGAMENTO] Paying with ticket, orderId=' + orderId + ' code=' + codiceVoucher);
      // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const result = await API.payOrdine(orderId, metodoPagamento, null, idempotencyKey, codiceVoucher, offertaId);
      console.log('[PAGAMENTO] Ticket payment result:', result);
      window.location.replace('/esito-acquisto.html?orderId=' + orderId + '&success=true');
      return;
    }

    if (method === 'misto' && importoCreditoRichiesto > 0 && (creditoData?.saldoAttuale || 0) >= importoCreditoRichiesto) {
      // Variabile idempotencyKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const idempotencyKey = `checkout-${orderId}-${Date.now()}`;
      // Variabile session: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const session = await API.createStripeCheckoutSession(orderId, {
        metodoPagamento: 'Misto',
        importoCreditoRichiesto,
        offertaId: offertaId || undefined
      }, idempotencyKey);

      if (session?.stripeCheckoutUrl) {
        window.location.href = session.stripeCheckoutUrl;
        return;
      }

      showToast('Errore nella creazione della sessione Stripe Checkout', 'danger');
      btnPay.disabled = false;
      btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Riprova pagamento</span>';
      return;
    }

    // Variabile idempotencyKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const idempotencyKey = `checkout-${orderId}-${Date.now()}`;
    // Variabile session: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const session = await API.createStripeCheckoutSession(orderId, {
      metodoPagamento: 'Carta',
      offertaId: offertaId || undefined
    }, idempotencyKey);

    if (session?.stripeCheckoutUrl) {
      window.location.href = session.stripeCheckoutUrl;
    } else {
      showToast('Errore nella creazione della sessione di pagamento', 'danger');
      btnPay.disabled = false;
      btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Riprova pagamento</span>';
    }
  } catch (error) {
    // Credit payment: backend transaction may have succeeded even if response errored.
    // Poll order status before redirecting anywhere.
    if (method === 'credito' && error?.status === 409) {
      showToast('Verifica stato pagamento...', 'warning');
      // Variabile paid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const paid = await pollOrderPaid(orderId, 4, 1500);
      if (paid) {
        window.location.href = `/esito-acquisto.html?orderId=${orderId}&success=true`;
        return;
      }
      if (paymentFlowMode === 'abbonamento') {
        showToast('Hai già un abbonamento attivo. Controlla il tuo profilo.', 'danger');
      } else {
        showToast('I posti selezionati non sono pi\u00f9 disponibili. Torna alla selezione posti.', 'warning');
      }
      setTimeout(function() {
        if (paymentFlowMode === 'abbonamento') return;
        // Variabile backUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const backUrl = new URL('/acquista.html', window.location.origin);
        if (ordine?.showId) backUrl.searchParams.set('showId', ordine.showId);
        if (offertaIdFromUrl) backUrl.searchParams.set('offertaId', offertaIdFromUrl);
        window.location.href = backUrl.toString();
      }, 2500);
      return;
    }

    // 409 for non-credit payments: order not in payable state (seats expired, etc.)
    if (error?.status === 409) {
      if (paymentFlowMode === 'abbonamento') {
        showToast('Hai già un abbonamento attivo. Controlla il tuo profilo.', 'danger');
      } else {
        showToast('I posti selezionati non sono pi\u00f9 disponibili. Torna alla selezione posti.', 'warning');
      }
      setTimeout(function() {
        if (paymentFlowMode === 'abbonamento') return;
        // Variabile backUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const backUrl = new URL('/acquista.html', window.location.origin);
        if (ordine?.showId) backUrl.searchParams.set('showId', ordine.showId);
        if (offertaIdFromUrl) backUrl.searchParams.set('offertaId', offertaIdFromUrl);
        window.location.href = backUrl.toString();
      }, 2500);
      return;
    }

    // Network/server error: payment might have succeeded on backend
    // (MySQL connection errors can cause this). Poll order status.
    btnPay.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Verifica pagamento...';
    // Variabile paid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const paid = await pollOrderPaid(orderId, 4, 1500);
    if (paid) {
      window.location.href = `/esito-acquisto.html?orderId=${orderId}&success=true`;
      return;
    }

    handleApiError(error);
    btnPay.disabled = false;
    btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Riprova pagamento</span>';
    updatePayButtonText();
  }
}

// Funzione pollOrderPaid: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function pollOrderPaid(orderId, attempts, delayMs) {
  for (let i = 0; i < attempts; i++) {
    await new Promise(r => setTimeout(r, delayMs));
    try {
      // Variabile ordine: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const ordine = await API.getOrdine(orderId);
      if (ordine?.stato === 'Paid') return true;
    } catch { /* keep polling */ }
  }
  return false;
}

// Funzione getStripePublishableKey: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getStripePublishableKey() {
  // Variabile configKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const configKey = frontendConfig?.stripePublishableKey;
  if (configKey) return configKey;
  return '';
}

// Funzione hideLoading: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function hideLoading() {
  document.getElementById('loading-state').classList.add('hidden');
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

// Reset button state when returning from Stripe via browser back
window.addEventListener('pageshow', (event) => {
  if (paymentFlowMode !== 'order') return;
  if (event.persisted) {
    resetPayButton();
    if (orderId) {
      Promise.all([loadOrdine(), loadCredito()]).then(() => {
        if (ordine && ordine.stato === 'Pending') {
          renderOrderSummary();
          setupPaymentOptions();
          setupActions();
        } else if (ordine) {
          window.location.href = `/esito-acquisto.html?orderId=${ordine.id}`;
        }
      }).catch(() => {});
    }
  }
});

// Funzione resetPayButton: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function resetPayButton() {
  // Variabile btn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btn = document.getElementById('btn-pay');
  if (btn) {
    btn.disabled = false;
    btn.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Paga ora</span>';
  }
}
