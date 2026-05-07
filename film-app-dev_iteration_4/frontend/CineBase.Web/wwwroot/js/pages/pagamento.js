let orderId = null;
let ordine = null;
let creditoData = null;
let frontendConfig = null;
const urlParams = new URLSearchParams(window.location.search);
const offertaIdFromUrl = urlParams.get('offertaId');
const showIdFromUrl = urlParams.get('showId');
const abbonamentoIdFromUrl = urlParams.get('abbonamentoId');
const stripeStatusFromUrl = urlParams.get('stripe');
let paymentFlowMode = 'order';

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
          const backUrl = new URL('/acquista.html', window.location.origin);
          if (ordine?.showId) backUrl.searchParams.set('showId', ordine.showId);
          if (offertaIdFromUrl) backUrl.searchParams.set('offertaId', offertaIdFromUrl);
          window.location.href = backUrl.toString();
        }, 1500);
      }
    }
  });
});

async function loadFrontendConfig() {
  try {
    frontendConfig = await API.getFrontendConfig();
  } catch {
    frontendConfig = null;
  }
}

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

async function loadCredito() {
  try {
    creditoData = await API.getCreditoMe();
  } catch {
    creditoData = { saldoAttuale: 0 };
  }
}

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

function hideOrderOnlyControls() {
  document.getElementById('option-misto')?.classList.add('hidden');
  document.getElementById('option-ticket')?.classList.add('hidden');
  document.getElementById('credit-slider-section')?.classList.add('hidden');
  document.getElementById('btn-cancel')?.classList.add('hidden');
  document.getElementById('order-summary-card')?.classList.add('hidden');
}

async function loadOffertaDiscount(offertaId) {
  try {
    const offers = normalizeCollection(await API.getOfferte());
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

async function finalizeAbbonamentoAfterStripe(abbonamentoId) {
  const marker = `abbonamento-finalized-${abbonamentoId}`;
  if (sessionStorage.getItem(marker)) {
    window.location.href = '/profilo.html';
    return;
  }

  try {
    const res = await fetch((window.API_BASE_URL || 'http://localhost:5000') + '/abbonamenti/' + abbonamentoId + '/attiva', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + Auth.getAccessToken()
      },
      body: JSON.stringify({ metodoPagamento: 'carta', autoRinnovo: true })
    });

    if (!res.ok) {
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

async function loadAbbonamentoPayment(abbonamentoId) {
  try {
    const abbonamenti = normalizeCollection(await API.getAbbonamenti());
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

function renderOrderSummary() {
  hideLoading();
  document.getElementById('main-content').classList.remove('hidden');

  document.getElementById('credit-balance').textContent = formatCurrency(creditoData?.saldoAttuale || 0);

  const container = document.getElementById('order-summary');
  const startDate = new Date(ordine.startAtUtc);
  const dateOptions = { weekday: 'short', day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' };
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

function setupPaymentOptions() {
  const saldo = creditoData?.saldoAttuale || 0;
  const totale = ordine?.totaleLordo || 0;

  const optionCredito = document.getElementById('option-credito');
  const optionMisto = document.getElementById('option-misto');
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

  const slider = document.getElementById('credit-slider');
  slider.max = Math.min(saldo, Math.max(0, totale - 0.01));
  slider.value = 0;

  document.querySelectorAll('input[name="payment-method"]').forEach(radio => {
    radio.addEventListener('change', () => {
      onPaymentMethodChange(radio.value);
    });
  });

  slider.addEventListener('input', () => {
    updateSplitDisplay();
  });
}

function onPaymentMethodChange(method) {
  const stripeInfoSection = document.getElementById('stripe-info-section');
  const sliderSection = document.getElementById('credit-slider-section');
  const ticketCodeSection = document.getElementById('ticket-code-section');
  const saldo = creditoData?.saldoAttuale || 0;
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

function updateSplitDisplay() {
  const slider = document.getElementById('credit-slider');
  const creditAmount = parseFloat(slider.value);
  const totale = ordine?.totaleLordo || 0;
  const cardAmount = totale - creditAmount;

  document.getElementById('credit-amount-label').textContent = `Credito: ${formatCurrency(creditAmount)}`;
  document.getElementById('card-amount-label').textContent = `Carta: ${formatCurrency(cardAmount)}`;

  updatePayButtonText();
}

function updatePayButtonText() {
  const method = document.querySelector('input[name="payment-method"]:checked')?.value || 'carta';
  const totale = ordine?.totaleLordo || 0;
  let amount = totale;

  if (method === 'credito') {
    amount = totale;
  } else if (method === 'misto') {
    const slider = document.getElementById('credit-slider');
    const creditUsed = parseFloat(slider?.value || 0);
    amount = totale - creditUsed;
  }

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

function setupActions() {
  const btnPay = document.getElementById('btn-pay');
  const btnCancel = document.getElementById('btn-cancel');

  btnPay.addEventListener('click', async () => {
    await handlePayment();
  });

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

async function handlePayment() {
  const btnPay = document.getElementById('btn-pay');
  btnPay.disabled = true;
  btnPay.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Elaborazione pagamento...';

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
      const method = document.querySelector('input[name="payment-method"]:checked')?.value || 'carta';
      if (method === 'credito') {
        try {
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

          const err = await res.json().catch(() => ({}));
          showToast(res.status === 409 ? 'Hai già un abbonamento attivo. Controlla il tuo profilo.' : (err.message || err.title || 'Errore attivazione'), 'danger');
        } catch (e) {
          showToast('Errore di rete', 'danger');
        }

        btnPay.disabled = false;
        btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Riprova pagamento</span>';
        return;
      }

      const idempotencyKey = `abbonamento-${window._abbonamentoData.id}-${Date.now()}`;
      const session = await API.createAbbonamentoStripeCheckoutSession(window._abbonamentoData.id, idempotencyKey);

      if (session?.stripeCheckoutUrl) {
        window.location.href = session.stripeCheckoutUrl;
        return;
      }

      try {
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
    let importoCreditoRichiesto = null;
    const offertaId = window._offertaData?.id || null;

    if (method === 'credito') {
      importoCreditoRichiesto = ordine.totaleLordo;
    } else if (method === 'misto') {
      const slider = document.getElementById('credit-slider');
      importoCreditoRichiesto = parseFloat(slider.value);
    }

    if (method === 'credito') {
      const saldo = creditoData?.saldoAttuale || 0;
      if (saldo < ordine.totaleLordo) {
        showToast('Credito insufficiente. Hai ' + formatCurrency(saldo) + ', servono ' + formatCurrency(ordine.totaleLordo) + '.', 'danger');
        btnPay.disabled = false;
        btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Paga</span>';
        return;
      }

      const metodoPagamento = 'Credito';
      const idempotencyKey = 'pay-' + orderId + '-' + Date.now();
      console.log('[PAGAMENTO] Paying with credit, orderId=' + orderId);
      const result = await API.payOrdine(orderId, metodoPagamento, importoCreditoRichiesto, idempotencyKey, null, offertaId);
      console.log('[PAGAMENTO] Credit payment result:', result);
      window.location.replace('/esito-acquisto.html?orderId=' + orderId + '&success=true');
      return;
    }

    if (method === 'ticket') {
      const codiceVoucher = document.getElementById('ticket-code-input')?.value?.trim();
      if (!codiceVoucher) {
        showToast('Inserisci il codice del voucher.', 'warning');
        btnPay.disabled = false;
        btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Paga</span>';
        return;
      }
      const metodoPagamento = 'Ticket';
      const idempotencyKey = 'pay-' + orderId + '-' + Date.now();
      console.log('[PAGAMENTO] Paying with ticket, orderId=' + orderId + ' code=' + codiceVoucher);
      const result = await API.payOrdine(orderId, metodoPagamento, null, idempotencyKey, codiceVoucher, offertaId);
      console.log('[PAGAMENTO] Ticket payment result:', result);
      window.location.replace('/esito-acquisto.html?orderId=' + orderId + '&success=true');
      return;
    }

    if (method === 'misto' && importoCreditoRichiesto > 0 && (creditoData?.saldoAttuale || 0) >= importoCreditoRichiesto) {
      const idempotencyKey = `checkout-${orderId}-${Date.now()}`;
      const session = await API.createStripeCheckoutSession(orderId, {
        metodoPagamento: 'Misto',
        importoCreditoRichiesto
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

    const idempotencyKey = `checkout-${orderId}-${Date.now()}`;
    const session = await API.createStripeCheckoutSession(orderId, {
      metodoPagamento: 'Carta'
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

async function pollOrderPaid(orderId, attempts, delayMs) {
  for (let i = 0; i < attempts; i++) {
    await new Promise(r => setTimeout(r, delayMs));
    try {
      const ordine = await API.getOrdine(orderId);
      if (ordine?.stato === 'Paid') return true;
    } catch { /* keep polling */ }
  }
  return false;
}

function getStripePublishableKey() {
  const configKey = frontendConfig?.stripePublishableKey;
  if (configKey) return configKey;
  return '';
}

function hideLoading() {
  document.getElementById('loading-state').classList.add('hidden');
}

function showError(message) {
  document.getElementById('loading-state').classList.add('hidden');
  document.getElementById('error-state').classList.remove('hidden');
  document.getElementById('main-content').classList.add('hidden');
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

function resetPayButton() {
  const btn = document.getElementById('btn-pay');
  if (btn) {
    btn.disabled = false;
    btn.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Paga ora</span>';
  }
}
