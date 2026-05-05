let orderId = null;
let ordine = null;
let creditoData = null;
let frontendConfig = null;

document.addEventListener('DOMContentLoaded', async () => {
  // Reset button state when page loads (fix for stuck "Elaborazione pagamento...")
  resetPayButton();
  
  if (!Auth?.isLoggedIn?.()) {
    window.location.replace('/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search));
    return;
  }

  const params = new URLSearchParams(window.location.search);
  orderId = parseInt(params.get('orderId'));

  if (!orderId) {
    showError('Parametro orderId mancante');
    return;
  }

  await Promise.all([loadOrdine(), loadCredito(), loadFrontendConfig()]);

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
  const saldo = creditoData?.saldoAttuale || 0;
  const totale = ordine?.totaleLordo || 0;

  sliderSection.classList.add('hidden');
  stripeInfoSection.classList.add('hidden');

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

  btnCancel.addEventListener('click', async () => {
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

  try {
    const method = document.querySelector('input[name="payment-method"]:checked')?.value || 'carta';
    let importoCreditoRichiesto = null;

    if (method === 'credito') {
      importoCreditoRichiesto = ordine.totaleLordo;
    } else if (method === 'misto') {
      const slider = document.getElementById('credit-slider');
      importoCreditoRichiesto = parseFloat(slider.value);
    }

    if (method === 'credito' && (creditoData?.saldoAttuale || 0) >= ordine.totaleLordo) {
      const metodoPagamento = 'Credito';
      const idempotencyKey = `pay-${orderId}-${Date.now()}`;
      const result = await API.payOrdine(orderId, metodoPagamento, importoCreditoRichiesto, idempotencyKey);

      if (result.statoPagamento === 'Paid' || result.ordine?.stato === 'Paid') {
        window.location.href = `/esito-acquisto.html?orderId=${orderId}&success=true`;
      } else {
        showToast(result.messaggio || 'Pagamento in elaborazione', 'info');
        setTimeout(() => {
          window.location.href = `/esito-acquisto.html?orderId=${orderId}`;
        }, 2000);
      }
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
    handleApiError(error);
    // On payment failure (especially 409 Conflict), the order state
    // might be corrupted. Redirect to order detail so user can cancel.
    if (error?.status === 409) {
      showToast('Ordine non più pagabile. Verrai reindirizzato ai dettagli.', 'warning');
      setTimeout(() => {
        window.location.href = `/esito-acquisto.html?orderId=${orderId}`;
      }, 2000);
      return;
    }
    btnPay.disabled = false;
    btnPay.innerHTML = '<i class="fa-solid fa-lock mr-2"></i><span id="pay-button-text">Riprova pagamento</span>';
    updatePayButtonText();
  }
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
