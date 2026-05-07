const MAX_SEATS = 10;
const KEEP_ALIVE_INTERVAL = 60000;
const SEAT_POLL_INTERVAL = 15000;
const ZOOM_LEVELS = [0.6, 0.75, 0.85, 0.95, 1, 1.1, 1.2, 1.35, 1.5];
const DEFAULT_ZOOM_INDEX = 4;

let showId = null;
let seatMap = null;
let holdToken = null;
let holdExpiresAt = null;
let selectedSeatIds = new Set();
let countdownInterval = null;
let keepAliveInterval = null;
let pollInterval = null;
let zoomIndex = DEFAULT_ZOOM_INDEX;
let offertaData = null; // Dati offerta se si acquista da un'offerta

document.addEventListener('DOMContentLoaded', async () => {
  if (!Auth?.isLoggedIn?.()) {
    window.location.replace('/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search));
    return;
  }

  const params = new URLSearchParams(window.location.search);
  showId = parseInt(params.get('showId'));

  if (!showId) {
    showError('Parametro showId mancante');
    return;
  }

  // Carica dati offerta se presente
  const offertaId = params.get('offertaId');
  if (offertaId) {
    try {
      const offers = Array.isArray(await API.getOfferte())
        ? await API.getOfferte()
        : (await API.getOfferte())?.$values || (await API.getOfferte())?.items || [];
      offertaData = offers.find(o => String(o.id) === String(offertaId)) || null;
    } catch { /* non bloccare il flusso */ }
  }

  await loadSeatMap();
  setupActions();
  setupZoomControls();
});

async function loadSeatMap() {
  showLoading();
  try {
    seatMap = await API.getSeatMap(showId);
    renderShowInfo();
    renderSeatMap();
    if (seatMap.scadeAtUtc) {
      holdExpiresAt = new Date(seatMap.scadeAtUtc);
      startCountdown();
      startKeepAlive();
    }
    hideLoading();
    document.getElementById('main-content').classList.remove('hidden');
  } catch (error) {
    showError(error.message || 'Errore caricamento piantina');
  }
}

function renderShowInfo() {
  document.getElementById('show-film-title').textContent = seatMap.filmTitolo;
  document.getElementById('show-cinema').innerHTML = `<i class="fa-solid fa-location-dot mr-1"></i>${seatMap.cinemaNome}`;
  document.getElementById('show-sala').innerHTML = `<i class="fa-solid fa-door-open mr-1"></i>${seatMap.salaNome}`;

  const startDate = new Date(seatMap.startAtUtc);
  const options = { weekday: 'long', day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' };
  const dateStr = startDate.toLocaleDateString('it-IT', options);
  document.getElementById('show-datetime').innerHTML = `<i class="fa-regular fa-calendar mr-1"></i>${dateStr}`;

  const priceBase = seatMap.prezzoBase || 0;
  const supplement = seatMap.supplementoSala || 0;
  const unitPrice = priceBase + supplement;
  document.getElementById('show-prezzo').textContent = `Prezzo: ${formatCurrency(unitPrice)}/posto`;
  document.getElementById('summary-unit-price').textContent = formatCurrency(unitPrice);
}

function renderSeatMap() {
  const container = document.getElementById('seat-map');
  if (!container || !seatMap) return;

  const posti = seatMap.posti || [];

  const grouped = buildSectorGroups(posti);
  const levelGroups = buildVisualLevels(grouped);

  let html = '<div class="seat-map-layout">';

  levelGroups.forEach(level => {
    html += `
      <section class="seat-level-block">
        <div class="seat-level-header">
          <span class="seat-level-title">${level.label}</span>
        </div>
        <div class="seat-level-sectors ${level.columnsClass}">
    `;

    level.sectors.forEach(sector => {
      html += renderSectorBlock(sector.name, sector.rows, sector.compact);
    });

    html += `
        </div>
      </section>
    `;
  });

  html += '</div>';

  container.innerHTML = html;
  applySeatMapZoom();

  container.querySelectorAll('.seat-btn:not([disabled])').forEach(btn => {
    btn.addEventListener('click', () => toggleSeat(btn));
  });
}

function setupZoomControls() {
  const zoomOutBtn = document.getElementById('seat-zoom-out');
  const zoomInBtn = document.getElementById('seat-zoom-in');
  const zoomResetBtn = document.getElementById('seat-zoom-reset');
  const wrapper = document.getElementById('seat-map-container');
  if (!zoomOutBtn || !zoomInBtn || !zoomResetBtn || !wrapper) return;

  zoomOutBtn.addEventListener('click', () => changeSeatMapZoom(-1));
  zoomInBtn.addEventListener('click', () => changeSeatMapZoom(1));
  zoomResetBtn.addEventListener('click', resetSeatMapZoom);

  wrapper.addEventListener('wheel', handleSeatMapWheelZoom, { passive: false });

  updateZoomUi();
}

function changeSeatMapZoom(direction) {
  const nextIndex = Math.max(0, Math.min(ZOOM_LEVELS.length - 1, zoomIndex + direction));
  if (nextIndex === zoomIndex) return;
  zoomIndex = nextIndex;
  applySeatMapZoom();
  updateZoomUi();
}

function resetSeatMapZoom() {
  zoomIndex = DEFAULT_ZOOM_INDEX;
  applySeatMapZoom();
  updateZoomUi();
}

function handleSeatMapWheelZoom(event) {
  if (window.innerWidth < 1024) return;
  if (!event.ctrlKey) return;

  event.preventDefault();
  if (event.deltaY < 0) {
    changeSeatMapZoom(1);
  } else if (event.deltaY > 0) {
    changeSeatMapZoom(-1);
  }
}

function applySeatMapZoom() {
  const wrapper = document.getElementById('seat-map-container');
  const layout = document.querySelector('#seat-map .seat-map-layout');
  if (!wrapper || !layout) return;

  const zoom = ZOOM_LEVELS[zoomIndex];
  wrapper.style.setProperty('--seat-map-zoom', String(zoom));
  layout.style.transform = `scale(${zoom})`;
}

function updateZoomUi() {
  const zoomOutBtn = document.getElementById('seat-zoom-out');
  const zoomInBtn = document.getElementById('seat-zoom-in');
  const zoomResetBtn = document.getElementById('seat-zoom-reset');
  const label = document.getElementById('seat-zoom-label');
  if (!zoomOutBtn || !zoomInBtn || !zoomResetBtn || !label) return;

  label.textContent = `${Math.round(ZOOM_LEVELS[zoomIndex] * 100)}%`;
  zoomOutBtn.disabled = zoomIndex === 0;
  zoomInBtn.disabled = zoomIndex === ZOOM_LEVELS.length - 1;
  zoomResetBtn.disabled = zoomIndex === DEFAULT_ZOOM_INDEX;
}

function buildSectorGroups(posti) {
  const grouped = {};
  posti.forEach((p) => {
    if (!grouped[p.settore]) grouped[p.settore] = {};
    if (!grouped[p.settore][p.fila]) grouped[p.settore][p.fila] = [];
    grouped[p.settore][p.fila].push(p);
  });

  const result = {};
  Object.keys(grouped).forEach((settore) => {
    const rows = Object.keys(grouped[settore])
      .sort((a, b) => Number(a) - Number(b))
      .map((fila) => ({
        fila: Number(fila),
        seats: grouped[settore][fila].sort((a, b) => a.numero - b.numero)
      }));

    result[settore] = rows;
  });

  return result;
}

function buildVisualLevels(grouped) {
  const sectorNames = Object.keys(grouped);
  const normalized = sectorNames.map((name) => ({
    name,
    upper: name.toUpperCase(),
    rows: grouped[name]
  }));

  const access = normalized.filter((s) => s.upper.startsWith('ACCESS'));
  const galleria = normalized.filter((s) => s.upper.startsWith('GALLERIA'));
  const platea = normalized.filter((s) => s.upper.startsWith('PLATEA'));
  const vip = normalized.filter((s) => !access.includes(s) && !galleria.includes(s) && !platea.includes(s));

  const levels = [];

  if (access.length) {
    levels.push({
      label: 'Accessibilità',
      columnsClass: access.length > 1 ? 'seat-level-cols-2' : 'seat-level-cols-1',
      sectors: access.map((s) => ({ name: prettifySettoreName(s.name), rows: s.rows, compact: true }))
    });
  }

  if (galleria.length) {
    levels.push({
      label: 'Galleria',
      columnsClass: getColumnsClass(galleria.length),
      sectors: sortSectorsForVisualOrder(galleria).map((s) => ({ name: prettifySettoreName(s.name), rows: s.rows, compact: false }))
    });
  }

  if (platea.length) {
    levels.push({
      label: 'Platea',
      columnsClass: getColumnsClass(platea.length),
      sectors: sortSectorsForVisualOrder(platea).map((s) => ({ name: prettifySettoreName(s.name), rows: s.rows, compact: false }))
    });
  }

  if (vip.length) {
    levels.push({
      label: 'Altri settori',
      columnsClass: getColumnsClass(vip.length),
      sectors: vip.map((s) => ({ name: prettifySettoreName(s.name), rows: s.rows, compact: false }))
    });
  }

  return levels;
}

function sortSectorsForVisualOrder(sectors) {
  const rank = (name) => {
    const upper = name.toUpperCase();
    if (upper.endsWith('-SX')) return 0;
    if (upper.endsWith('-CENTRO')) return 1;
    if (upper.endsWith('-DX')) return 2;
    return 3;
  };

  return [...sectors].sort((a, b) => rank(a.name) - rank(b.name));
}

function getColumnsClass(count) {
  if (count <= 1) return 'seat-level-cols-1';
  if (count === 2) return 'seat-level-cols-2';
  return 'seat-level-cols-3';
}

function prettifySettoreName(name) {
  return String(name || '')
    .toUpperCase()
    .replace(/-/g, ' ')
    .replace(/\bSX\b/g, 'Sinistra')
    .replace(/\bDX\b/g, 'Destra')
    .replace(/\bCENTRO\b/g, 'Centro')
    .replace(/\bVIP\b/g, 'Vip')
    .replace(/\bACCESS\b/g, 'Access');
}

function renderSectorBlock(name, rows, compact) {
  const maxSeatNumber = Math.max(...rows.map((row) => Math.max(...row.seats.map((seat) => seat.numero))));
  let html = `
    <div class="seat-sector-card ${compact ? 'seat-sector-card-compact' : ''}">
      <div class="seat-sector-name">${name}</div>
      <div class="seat-sector-grid">
        <div class="seat-row seat-row-numbers">
          <span class="fila-label fila-label-header"></span>
  `;

  for (let n = 1; n <= maxSeatNumber; n++) {
    html += `<span class="seat-num-label">${n}</span>`;
  }

  html += '</div>';

  rows.forEach((row) => {
    const seatMapByNumber = {};
    row.seats.forEach((seat) => { seatMapByNumber[seat.numero] = seat; });

    html += `<div class="seat-row"><span class="fila-label">F${row.fila}</span>`;
    for (let n = 1; n <= maxSeatNumber; n++) {
      const p = seatMapByNumber[n];
      if (!p) {
        html += '<span class="seat-placeholder"></span>';
        continue;
      }

      const statusClass = getSeatStatusClass(p.stato, p.isWheelchair);
      const isSelected = selectedSeatIds.has(p.salaPostoId);
      const finalClass = isSelected ? 'seat-btn seat-selected' : `seat-btn ${statusClass}`;
      const disabled = p.stato === 2 || p.stato === 3;
      html += `<button class="${finalClass}" data-seat-id="${p.salaPostoId}" data-fila="${p.fila}" data-numero="${p.numero}" data-settore="${p.settore}" data-wheelchair="${p.isWheelchair}" ${disabled ? 'disabled' : ''} type="button" title="${name} - Fila ${p.fila}, Posto ${p.numero}${p.isWheelchair ? ' (disabile)' : ''}"></button>`;
    }
    html += '</div>';
  });

  html += '</div></div>';
  return html;
}

function getSeatStatusClass(stato, isWheelchair) {
  if (isWheelchair) return 'seat-wheelchair';
  switch (stato) {
    case 0: return 'seat-available';
    case 1: return 'seat-held-other';
    case 2: return 'seat-held-me';
    case 3: return 'seat-sold';
    default: return 'seat-available';
  }
}

async function toggleSeat(btn) {
  const seatId = parseInt(btn.dataset.seatId);

  if (selectedSeatIds.has(seatId)) {
    selectedSeatIds.delete(seatId);
    btn.classList.remove('seat-selected');
    btn.classList.add(getSeatStatusClass(getOriginalStatus(seatId), btn.dataset.wheelchair === 'true'));
    updateSummary();

    if (selectedSeatIds.size === 0 && holdToken) {
      await releaseCurrentHold();
    } else if (holdToken) {
      await refreshHoldSeats();
    }
    return;
  }

  if (selectedSeatIds.size >= MAX_SEATS) {
    showToast(`Massimo ${MAX_SEATS} posti per ordine`, 'warning');
    return;
  }

  const newSelected = new Set([...selectedSeatIds, seatId]);
  const seatIdsArray = Array.from(newSelected);

  try {
    const result = await API.createHold(showId, seatIdsArray);

    if (result.conflitti && result.conflitti.length > 0) {
      showToast('Alcuni posti non sono più disponibili', 'warning');
    }

    holdToken = result.holdToken;
    holdExpiresAt = new Date(result.scadeAtUtc);

    selectedSeatIds = new Set(result.salaPostoIds);

    startCountdown();
    startKeepAlive();
    renderSeatMap();
    updateSummary();
    startPolling();
  } catch (error) {
    if (error.status === 409) {
      showToast('Posto non più disponibile. La piantina verrà aggiornata.', 'warning');
      await refreshSeatMap();
    } else {
      handleApiError(error);
    }
  }
}

function getOriginalStatus(seatId) {
  if (!seatMap || !seatMap.posti) return 0;
  const posto = seatMap.posti.find(p => p.salaPostoId === seatId);
  return posto ? posto.stato : 0;
}

async function refreshHoldSeats() {
  if (!holdToken) return;

  try {
    const seatIdsArray = Array.from(selectedSeatIds);
    const result = await API.createHold(showId, seatIdsArray);

    holdToken = result.holdToken;
    holdExpiresAt = new Date(result.scadeAtUtc);
    selectedSeatIds = new Set(result.salaPostoIds);

    renderSeatMap();
    updateSummary();
  } catch (error) {
    if (error.status === 409) {
      showToast('Conflitto sui posti. La piantina verrà aggiornata.', 'warning');
      selectedSeatIds.clear();
      holdToken = null;
      holdExpiresAt = null;
      stopCountdown();
      stopKeepAlive();
      stopPolling();
      await refreshSeatMap();
    }
  }
}

async function releaseCurrentHold() {
  if (!holdToken) return;
  try {
    await API.releaseHold(holdToken);
  } catch {
    // ignore
  }
  holdToken = null;
  holdExpiresAt = null;
  selectedSeatIds.clear();
  stopCountdown();
  stopKeepAlive();
  stopPolling();
  renderSeatMap();
  updateSummary();
}

async function refreshSeatMap() {
  try {
    seatMap = await API.getSeatMap(showId);
    renderSeatMap();
  } catch {
    // ignore
  }
}

function updateSummary() {
  const list = document.getElementById('selected-seats-list');
  const countEl = document.getElementById('summary-count');
  const totalEl = document.getElementById('summary-total');
  const btnContinue = document.getElementById('btn-continue');
  const countdownCard = document.getElementById('countdown-card');
  const offertaRow = document.getElementById('summary-offerta-row');
  const offertaLabel = document.getElementById('summary-offerta-label');
  const offertaAmount = document.getElementById('summary-offerta-amount');

  const selected = getSelectedSeats();
  countEl.textContent = selected.length;

  const priceBase = seatMap?.prezzoBase || 0;
  const supplement = seatMap?.supplementoSala || 0;
  const unitPrice = priceBase + supplement;
  const rawTotal = selected.length * unitPrice;

  // Mostra info offerta se presente
  if (offertaData && offertaRow && offertaAmount && offertaLabel) {
    const offerPrice = offertaData.prezzo || rawTotal;
    offertaLabel.textContent = `Offerta "${offertaData.nome}"`;
    offertaAmount.textContent = formatCurrency(offerPrice);
    offertaRow.classList.remove('hidden');
    totalEl.textContent = formatCurrency(offerPrice);
  } else {
    if (offertaRow) offertaRow.classList.add('hidden');
    totalEl.textContent = formatCurrency(rawTotal);
  }

  if (selected.length > 0) {
    list.innerHTML = selected.map(s =>
      `<div class="flex items-center justify-between py-1.5 px-2 bg-canvas-elevated text-sm">
        <span class="text-ink">
          <span class="text-body text-xs">${s.settore}</span>
          Fila ${s.fila}, Posto ${s.numero}
          ${s.isWheelchair ? ' <i class="fa-solid fa-wheelchair text-xs text-body"></i>' : ''}
        </span>
        <span class="text-ferrari-primary font-semibold">${formatCurrency(unitPrice)}</span>
      </div>`
    ).join('');
    btnContinue.disabled = false;
    btnContinue.classList.remove('opacity-50', 'cursor-not-allowed');
    countdownCard.classList.remove('hidden');
  } else {
    list.innerHTML = `<p class="text-sm text-body text-center py-4">Seleziona almeno un posto dalla piantina</p>`;
    btnContinue.disabled = true;
    btnContinue.classList.add('opacity-50', 'cursor-not-allowed');
    countdownCard.classList.add('hidden');
  }
}

function getSelectedSeats() {
  if (!seatMap || !seatMap.posti) return [];
  return seatMap.posti.filter(p => selectedSeatIds.has(p.salaPostoId));
}

function startCountdown() {
  stopCountdown();
  if (!holdExpiresAt) return;

  document.getElementById('countdown-card').classList.remove('hidden');
  updateCountdownDisplay();

  countdownInterval = setInterval(() => {
    updateCountdownDisplay();
  }, 1000);
}

function updateCountdownDisplay() {
  if (!holdExpiresAt) return;

  const now = new Date();
  const diff = holdExpiresAt - now;

  if (diff <= 0) {
    document.getElementById('countdown-timer').textContent = '00:00';
    stopCountdown();
    stopKeepAlive();
    stopPolling();
    showToast('Tempo scaduto! I posti sono stati rilasciati.', 'warning');
    selectedSeatIds.clear();
    holdToken = null;
    holdExpiresAt = null;
    refreshSeatMap();
    updateSummary();
    return;
  }

  const minutes = Math.floor(diff / 60000);
  const seconds = Math.floor((diff % 60000) / 1000);
  document.getElementById('countdown-timer').textContent =
    `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

function stopCountdown() {
  if (countdownInterval) {
    clearInterval(countdownInterval);
    countdownInterval = null;
  }
}

function startKeepAlive() {
  stopKeepAlive();
  keepAliveInterval = setInterval(async () => {
    if (!holdToken) return;
    try {
      const result = await API.refreshHold(holdToken);
      holdExpiresAt = new Date(result.scadeAtUtc);
    } catch {
      // hold may have expired
    }
  }, KEEP_ALIVE_INTERVAL);
}

function stopKeepAlive() {
  if (keepAliveInterval) {
    clearInterval(keepAliveInterval);
    keepAliveInterval = null;
  }
}

function startPolling() {
  stopPolling();
  pollInterval = setInterval(async () => {
    try {
      seatMap = await API.getSeatMap(showId);
      renderSeatMap();
    } catch {
      // ignore
    }
  }, SEAT_POLL_INTERVAL);
}

function stopPolling() {
  if (pollInterval) {
    clearInterval(pollInterval);
    pollInterval = null;
  }
}

function setupActions() {
  const btnContinue = document.getElementById('btn-continue');
  const btnBack = document.getElementById('btn-back');

  btnContinue.addEventListener('click', async () => {
    if (selectedSeatIds.size === 0 || !holdToken) {
      showToast('Seleziona almeno un posto', 'warning');
      return;
    }

    // Controllo offerta: se l'offerta include più biglietti di quelli selezionati, avvisa
    if (offertaData && offertaData.numeroBiglietti && selectedSeatIds.size < offertaData.numeroBiglietti) {
      const mancanti = offertaData.numeroBiglietti - selectedSeatIds.size;
      const conferma = confirm(
        `L'offerta "${offertaData.nome}" include ${offertaData.numeroBiglietti} biglietti, ma hai selezionato solo ${selectedSeatIds.size} posto/i.\n\n` +
        `Proseguendo perderai ${mancanti} biglietto/i incluso/i nell'offerta.\n\nVuoi continuare comunque?`
      );
      if (!conferma) return;
    }

    btnContinue.disabled = true;
    btnContinue.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Creazione ordine...';

    try {
      const idempotencyKey = `order-${holdToken}-${Date.now()}`;
      const ordine = await API.createOrdine(holdToken, idempotencyKey);

      stopPolling();
      stopKeepAlive();
      stopCountdown();

      const params = new URLSearchParams(window.location.search);
      const offertaId = params.get('offertaId');
      let url = '/pagamento.html?orderId=' + encodeURIComponent(ordine.id);
      if (offertaId) url += '&offertaId=' + encodeURIComponent(offertaId);
      window.location.href = url;
    } catch (error) {
      handleApiError(error);
      btnContinue.disabled = false;
      btnContinue.innerHTML = '<i class="fa-solid fa-credit-card mr-2"></i>Continua al pagamento';
    }
  });

  btnBack.addEventListener('click', async () => {
    if (holdToken && selectedSeatIds.size > 0) {
      try {
        await API.releaseHold(holdToken);
      } catch {
        // ignore
      }
    }
    window.history.back();
  });
}

function showLoading() {
  document.getElementById('loading-state').classList.remove('hidden');
  document.getElementById('error-state').classList.add('hidden');
  document.getElementById('main-content').classList.add('hidden');
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

window.addEventListener('beforeunload', async () => {
  // Clear all intervals
  if (countdownInterval) clearInterval(countdownInterval);
  if (keepAliveInterval) clearInterval(keepAliveInterval);
  if (pollInterval) clearInterval(pollInterval);

  if (holdToken && selectedSeatIds.size > 0) {
    const token = holdToken;
    try {
      navigator.sendBeacon(
        `${API_BASE_URL}/checkout/holds/${encodeURIComponent(token)}`,
        JSON.stringify({})
      );
    } catch {
      // best effort
    }
  }
});
