// Variabile MAX_SEATS: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const MAX_SEATS = 10;
// Variabile KEEP_ALIVE_INTERVAL: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const KEEP_ALIVE_INTERVAL = 60000;
// Variabile SEAT_POLL_INTERVAL: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const SEAT_POLL_INTERVAL = 15000;
// Variabile ZOOM_LEVELS: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const ZOOM_LEVELS = [0.6, 0.75, 0.85, 0.95, 1, 1.1, 1.2, 1.35, 1.5];
// Variabile DEFAULT_ZOOM_INDEX: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const DEFAULT_ZOOM_INDEX = 4;

// Variabile showId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let showId = null;
// Variabile seatMap: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let seatMap = null;
// Variabile holdToken: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let holdToken = null;
// Variabile holdExpiresAt: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let holdExpiresAt = null;
// Variabile selectedSeatIds: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let selectedSeatIds = new Set();
// Variabile countdownInterval: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let countdownInterval = null;
// Variabile keepAliveInterval: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let keepAliveInterval = null;
// Variabile pollInterval: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let pollInterval = null;
// Variabile zoomIndex: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let zoomIndex = DEFAULT_ZOOM_INDEX;
// Variabile offertaData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let offertaData = null; // Dati offerta se si acquista da un'offerta

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  if (!Auth?.isLoggedIn?.()) {
    window.location.replace('/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search));
    return;
  }

  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
      // Variabile offers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione loadSeatMap: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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

// Funzione renderShowInfo: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderShowInfo() {
  document.getElementById('show-film-title').textContent = seatMap.filmTitolo;
  document.getElementById('show-cinema').innerHTML = `<i class="fa-solid fa-location-dot mr-1"></i>${seatMap.cinemaNome}`;
  document.getElementById('show-sala').innerHTML = `<i class="fa-solid fa-door-open mr-1"></i>${seatMap.salaNome}`;

  // Variabile startDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const startDate = new Date(seatMap.startAtUtc);
  // Variabile options: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const options = { weekday: 'long', day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' };
  // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const dateStr = startDate.toLocaleDateString('it-IT', options);
  document.getElementById('show-datetime').innerHTML = `<i class="fa-regular fa-calendar mr-1"></i>${dateStr}`;

  // Variabile priceBase: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const priceBase = seatMap.prezzoBase || 0;
  // Variabile supplement: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const supplement = seatMap.supplementoSala || 0;
  // Variabile unitPrice: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const unitPrice = priceBase + supplement;
  document.getElementById('show-prezzo').textContent = `Prezzo: ${formatCurrency(unitPrice)}/posto`;
  document.getElementById('summary-unit-price').textContent = formatCurrency(unitPrice);
}

// Funzione renderSeatMap: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderSeatMap() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('seat-map');
  if (!container || !seatMap) return;

  // Variabile posti: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const posti = seatMap.posti || [];

  // Variabile grouped: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const grouped = buildSectorGroups(posti);
  // Variabile levelGroups: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const levelGroups = buildVisualLevels(grouped);

  // Variabile html: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    btn.addEventListener('click', () => toggleSeat(btn));
  });
}

// Funzione setupZoomControls: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupZoomControls() {
  // Variabile zoomOutBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const zoomOutBtn = document.getElementById('seat-zoom-out');
  // Variabile zoomInBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const zoomInBtn = document.getElementById('seat-zoom-in');
  // Variabile zoomResetBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const zoomResetBtn = document.getElementById('seat-zoom-reset');
  // Variabile wrapper: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const wrapper = document.getElementById('seat-map-container');
  if (!zoomOutBtn || !zoomInBtn || !zoomResetBtn || !wrapper) return;

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  zoomOutBtn.addEventListener('click', () => changeSeatMapZoom(-1));
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  zoomInBtn.addEventListener('click', () => changeSeatMapZoom(1));
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  zoomResetBtn.addEventListener('click', resetSeatMapZoom);

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  wrapper.addEventListener('wheel', handleSeatMapWheelZoom, { passive: false });

  updateZoomUi();
}

// Funzione changeSeatMapZoom: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function changeSeatMapZoom(direction) {
  // Variabile nextIndex: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nextIndex = Math.max(0, Math.min(ZOOM_LEVELS.length - 1, zoomIndex + direction));
  if (nextIndex === zoomIndex) return;
  zoomIndex = nextIndex;
  applySeatMapZoom();
  updateZoomUi();
}

// Funzione resetSeatMapZoom: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function resetSeatMapZoom() {
  zoomIndex = DEFAULT_ZOOM_INDEX;
  applySeatMapZoom();
  updateZoomUi();
}

// Funzione handleSeatMapWheelZoom: gestisce un evento o una risposta utente. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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

// Funzione applySeatMapZoom: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function applySeatMapZoom() {
  // Variabile wrapper: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const wrapper = document.getElementById('seat-map-container');
  // Variabile layout: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const layout = document.querySelector('#seat-map .seat-map-layout');
  if (!wrapper || !layout) return;

  // Variabile zoom: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const zoom = ZOOM_LEVELS[zoomIndex];
  wrapper.style.setProperty('--seat-map-zoom', String(zoom));
  layout.style.transform = `scale(${zoom})`;
}

// Funzione updateZoomUi: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateZoomUi() {
  // Variabile zoomOutBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const zoomOutBtn = document.getElementById('seat-zoom-out');
  // Variabile zoomInBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const zoomInBtn = document.getElementById('seat-zoom-in');
  // Variabile zoomResetBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const zoomResetBtn = document.getElementById('seat-zoom-reset');
  // Variabile label: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const label = document.getElementById('seat-zoom-label');
  if (!zoomOutBtn || !zoomInBtn || !zoomResetBtn || !label) return;

  label.textContent = `${Math.round(ZOOM_LEVELS[zoomIndex] * 100)}%`;
  zoomOutBtn.disabled = zoomIndex === 0;
  zoomInBtn.disabled = zoomIndex === ZOOM_LEVELS.length - 1;
  zoomResetBtn.disabled = zoomIndex === DEFAULT_ZOOM_INDEX;
}

// Funzione buildSectorGroups: costruisce una struttura dati o una selezione ordinata per la UI. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function buildSectorGroups(posti) {
  // Variabile grouped: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const grouped = {};
  posti.forEach((p) => {
    if (!grouped[p.settore]) grouped[p.settore] = {};
    if (!grouped[p.settore][p.fila]) grouped[p.settore][p.fila] = [];
    grouped[p.settore][p.fila].push(p);
  });

  // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const result = {};
  Object.keys(grouped).forEach((settore) => {
    // Variabile rows: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione buildVisualLevels: costruisce una struttura dati o una selezione ordinata per la UI. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function buildVisualLevels(grouped) {
  // Variabile sectorNames: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const sectorNames = Object.keys(grouped);
  // Variabile normalized: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const normalized = sectorNames.map((name) => ({
    name,
    upper: name.toUpperCase(),
    rows: grouped[name]
  }));

  // Variabile access: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const access = normalized.filter((s) => s.upper.startsWith('ACCESS'));
  // Variabile galleria: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const galleria = normalized.filter((s) => s.upper.startsWith('GALLERIA'));
  // Variabile platea: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const platea = normalized.filter((s) => s.upper.startsWith('PLATEA'));
  // Variabile vip: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const vip = normalized.filter((s) => !access.includes(s) && !galleria.includes(s) && !platea.includes(s));

  // Variabile levels: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione sortSectorsForVisualOrder: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function sortSectorsForVisualOrder(sectors) {
  // Variabile/funzione rank: supporto non ovvio per stato, callback o logica della pagina.
  const rank = (name) => {
    // Variabile upper: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const upper = name.toUpperCase();
    if (upper.endsWith('-SX')) return 0;
    if (upper.endsWith('-CENTRO')) return 1;
    if (upper.endsWith('-DX')) return 2;
    return 3;
  };

  return [...sectors].sort((a, b) => rank(a.name) - rank(b.name));
}

// Funzione getColumnsClass: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getColumnsClass(count) {
  if (count <= 1) return 'seat-level-cols-1';
  if (count === 2) return 'seat-level-cols-2';
  return 'seat-level-cols-3';
}

// Funzione prettifySettoreName: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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

// Funzione renderSectorBlock: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderSectorBlock(name, rows, compact) {
  // Variabile maxSeatNumber: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const maxSeatNumber = Math.max(...rows.map((row) => Math.max(...row.seats.map((seat) => seat.numero))));
  // Variabile html: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
    // Variabile seatMapByNumber: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const seatMapByNumber = {};
    row.seats.forEach((seat) => { seatMapByNumber[seat.numero] = seat; });

    html += `<div class="seat-row"><span class="fila-label">F${row.fila}</span>`;
    for (let n = 1; n <= maxSeatNumber; n++) {
      const p = seatMapByNumber[n];
      if (!p) {
        html += '<span class="seat-placeholder"></span>';
        continue;
      }

      // Variabile statusClass: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const statusClass = getSeatStatusClass(p.stato, p.isWheelchair);
      // Variabile isSelected: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const isSelected = selectedSeatIds.has(p.salaPostoId);
      // Variabile finalClass: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const finalClass = isSelected ? 'seat-btn seat-selected' : `seat-btn ${statusClass}`;
      // Variabile disabled: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const disabled = p.stato === 2 || p.stato === 3;
      html += `<button class="${finalClass}" data-seat-id="${p.salaPostoId}" data-fila="${p.fila}" data-numero="${p.numero}" data-settore="${p.settore}" data-wheelchair="${p.isWheelchair}" ${disabled ? 'disabled' : ''} type="button" title="${name} - Fila ${p.fila}, Posto ${p.numero}${p.isWheelchair ? ' (disabile)' : ''}"></button>`;
    }
    html += '</div>';
  });

  html += '</div></div>';
  return html;
}

// Funzione getSeatStatusClass: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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

// Funzione toggleSeat: commuta uno stato visivo o funzionale tra due modalità. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function toggleSeat(btn) {
  // Variabile seatId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

  // Variabile newSelected: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const newSelected = new Set([...selectedSeatIds, seatId]);
  // Variabile seatIdsArray: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const seatIdsArray = Array.from(newSelected);

  try {
    // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione getOriginalStatus: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getOriginalStatus(seatId) {
  if (!seatMap || !seatMap.posti) return 0;
  // Variabile posto: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const posto = seatMap.posti.find(p => p.salaPostoId === seatId);
  return posto ? posto.stato : 0;
}

// Funzione refreshHoldSeats: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function refreshHoldSeats() {
  if (!holdToken) return;

  try {
    // Variabile seatIdsArray: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const seatIdsArray = Array.from(selectedSeatIds);
    // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione releaseCurrentHold: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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

// Funzione refreshSeatMap: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function refreshSeatMap() {
  try {
    seatMap = await API.getSeatMap(showId);
    renderSeatMap();
  } catch {
    // ignore
  }
}

// Funzione updateSummary: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateSummary() {
  // Variabile list: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const list = document.getElementById('selected-seats-list');
  // Variabile countEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const countEl = document.getElementById('summary-count');
  // Variabile totalEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totalEl = document.getElementById('summary-total');
  // Variabile btnContinue: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnContinue = document.getElementById('btn-continue');
  // Variabile countdownCard: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const countdownCard = document.getElementById('countdown-card');
  // Variabile offertaRow: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const offertaRow = document.getElementById('summary-offerta-row');
  // Variabile offertaLabel: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const offertaLabel = document.getElementById('summary-offerta-label');
  // Variabile offertaAmount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const offertaAmount = document.getElementById('summary-offerta-amount');

  // Variabile selected: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const selected = getSelectedSeats();
  countEl.textContent = selected.length;

  // Variabile priceBase: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const priceBase = seatMap?.prezzoBase || 0;
  // Variabile supplement: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const supplement = seatMap?.supplementoSala || 0;
  // Variabile unitPrice: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const unitPrice = priceBase + supplement;
  // Variabile rawTotal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const rawTotal = selected.length * unitPrice;

  // Mostra info offerta se presente
  if (offertaData && offertaRow && offertaAmount && offertaLabel) {
    // Variabile offerPrice: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const offerPrice = offertaData.prezzo || rawTotal;
    // Variabile offerSeats: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const offerSeats = offertaData.numeroBiglietti || 0;
    // Variabile extra: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const extra = Math.max(0, selected.length - offerSeats);
    // Variabile extraCost: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const extraCost = extra * unitPrice;
    // Variabile finalTotal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const finalTotal = offerPrice + extraCost;

    if (extra > 0) {
      offertaLabel.textContent = `Offerta "${offertaData.nome}" (${offerSeats} posti)`;
      offertaAmount.innerHTML = `${formatCurrency(offerPrice)} <span class="text-body text-xs">+ ${extra} extra</span>`;
    } else {
      offertaLabel.textContent = `Offerta "${offertaData.nome}"`;
      offertaAmount.textContent = formatCurrency(offerPrice);
    }
    offertaRow.classList.remove('hidden');
    totalEl.textContent = formatCurrency(finalTotal);
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

// Funzione getSelectedSeats: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getSelectedSeats() {
  if (!seatMap || !seatMap.posti) return [];
  return seatMap.posti.filter(p => selectedSeatIds.has(p.salaPostoId));
}

// Funzione startCountdown: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function startCountdown() {
  stopCountdown();
  if (!holdExpiresAt) return;

  document.getElementById('countdown-card').classList.remove('hidden');
  updateCountdownDisplay();

  countdownInterval = setInterval(() => {
    updateCountdownDisplay();
  }, 1000);
}

// Funzione updateCountdownDisplay: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateCountdownDisplay() {
  if (!holdExpiresAt) return;

  // Variabile now: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const now = new Date();
  // Variabile diff: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

  // Variabile minutes: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const minutes = Math.floor(diff / 60000);
  // Variabile seconds: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const seconds = Math.floor((diff % 60000) / 1000);
  document.getElementById('countdown-timer').textContent =
    `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

// Funzione stopCountdown: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function stopCountdown() {
  if (countdownInterval) {
    clearInterval(countdownInterval);
    countdownInterval = null;
  }
}

// Funzione startKeepAlive: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function startKeepAlive() {
  stopKeepAlive();
  keepAliveInterval = setInterval(async () => {
    if (!holdToken) return;
    try {
      // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const result = await API.refreshHold(holdToken);
      holdExpiresAt = new Date(result.scadeAtUtc);
    } catch {
      // hold may have expired
    }
  }, KEEP_ALIVE_INTERVAL);
}

// Funzione stopKeepAlive: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function stopKeepAlive() {
  if (keepAliveInterval) {
    clearInterval(keepAliveInterval);
    keepAliveInterval = null;
  }
}

// Funzione startPolling: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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

// Funzione stopPolling: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function stopPolling() {
  if (pollInterval) {
    clearInterval(pollInterval);
    pollInterval = null;
  }
}

// Funzione setupActions: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupActions() {
  // Variabile btnContinue: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnContinue = document.getElementById('btn-continue');
  // Variabile btnBack: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btnBack = document.getElementById('btn-back');

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  btnContinue.addEventListener('click', async () => {
    if (selectedSeatIds.size === 0 || !holdToken) {
      showToast('Seleziona almeno un posto', 'warning');
      return;
    }

    // Controllo offerta: se l'offerta include più biglietti di quelli selezionati, blocca
    if (offertaData && offertaData.numeroBiglietti && selectedSeatIds.size < offertaData.numeroBiglietti) {
      // Variabile mancanti: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const mancanti = offertaData.numeroBiglietti - selectedSeatIds.size;
      showToast(`L'offerta "${offertaData.nome}" richiede almeno ${offertaData.numeroBiglietti} posti. Selezionane altri ${mancanti}.`, 'warning');
      return;
    }

    // Controllo offerta: se si selezionano più posti dell'offerta, avvisa del costo extra
    if (offertaData && offertaData.numeroBiglietti && selectedSeatIds.size > offertaData.numeroBiglietti) {
      // Variabile extra: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const extra = selectedSeatIds.size - offertaData.numeroBiglietti;
      // Variabile prezzoUnitario: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const prezzoUnitario = (seatMap?.prezzoBase || 0) + (seatMap?.supplementoSala || 0);
      // Variabile costoExtra: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const costoExtra = extra * prezzoUnitario;
      // Variabile conferma: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const conferma = confirm(
        `L'offerta "${offertaData.nome}" include ${offertaData.numeroBiglietti} posti, ma ne hai selezionati ${selectedSeatIds.size}.\n\n` +
        `${extra} posto/i extra ti costeranno ${formatCurrency(costoExtra)} al prezzo pieno di ${formatCurrency(prezzoUnitario)} ciascuno.\n\nVuoi continuare?`
      );
      if (!conferma) return;
    }

    btnContinue.disabled = true;
    btnContinue.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Creazione ordine...';

    try {
      // Variabile idempotencyKey: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const idempotencyKey = `order-${holdToken}-${Date.now()}`;
      // Variabile ordine: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const ordine = await API.createOrdine(holdToken, idempotencyKey);

      stopPolling();
      stopKeepAlive();
      stopCountdown();

      // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const params = new URLSearchParams(window.location.search);
      // Variabile offertaId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const offertaId = params.get('offertaId');
      // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      let url = '/pagamento.html?orderId=' + encodeURIComponent(ordine.id);
      if (offertaId) url += '&offertaId=' + encodeURIComponent(offertaId);
      window.location.href = url;
    } catch (error) {
      handleApiError(error);
      btnContinue.disabled = false;
      btnContinue.innerHTML = '<i class="fa-solid fa-credit-card mr-2"></i>Continua al pagamento';
    }
  });

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
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

// Funzione showLoading: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function showLoading() {
  document.getElementById('loading-state').classList.remove('hidden');
  document.getElementById('error-state').classList.add('hidden');
  document.getElementById('main-content').classList.add('hidden');
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

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
window.addEventListener('beforeunload', async () => {
  // Clear all intervals
  if (countdownInterval) clearInterval(countdownInterval);
  if (keepAliveInterval) clearInterval(keepAliveInterval);
  if (pollInterval) clearInterval(pollInterval);

  if (holdToken && selectedSeatIds.size > 0) {
    // Variabile token: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
