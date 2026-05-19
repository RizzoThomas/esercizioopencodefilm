// My Cinemas Page JavaScript

// Funzione getAuthSafe: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
function getAuthSafe() {
  return typeof window !== 'undefined' && window.Auth ? window.Auth : null;
}

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

// Funzione formatTipoSalaLabel: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function formatTipoSalaLabel(tipoSala) {
  // Variabile normalized: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const normalized = String(tipoSala || '').trim().toUpperCase();
  if (normalized === 'TRED' || normalized === '3D') return '3D';
  if (normalized === 'DUED' || normalized === '2D') return '2D';
  if (normalized === 'ISENSE') return 'ISENSE';
  if (normalized === 'XL') return 'XL';
  return tipoSala || '';
}

// Funzione localDateKey: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function localDateKey(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

// State
let cinemaId = null;
// Variabile allCinemas: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let allCinemas = [];
// Variabile scheduleData: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let scheduleData = null;
// Variabile dateRail: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let dateRail = null;
// Variabile userLocation: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let userLocation = null;

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const params = new URLSearchParams(window.location.search);
  cinemaId = params.get('IdCinema');

  // Wait for geolocation with timeout
  await Promise.race([
    getUserLocation().then(loc => { userLocation = loc; }).catch(() => {}),
    new Promise(r => setTimeout(r, 3000))
  ]);

  if (cinemaId) {
    await loadCinemaDetail();
  } else {
    await loadCinemaList();
  }
});

// Cinema List View
// Funzione loadCinemaList: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
async function loadCinemaList() {
  showLoading();

  try {
    // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var params = {};
    if (userLocation) {
      params.lat = userLocation.lat;
      params.lng = userLocation.lng;
    }
    allCinemas = normalizeCollection(await API.getMyCinemas(params));

    hideLoading();
    // Variabile listView: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const listView = document.getElementById('cinema-list-view');
    if (listView) listView.classList.remove('hidden');

    renderCinemaList();
  } catch (error) {
    console.error('Errore caricamento cinema:', error);
    showError(error.message || 'Errore nel caricamento dei cinema');
  }
}

// Funzione renderCinemaList: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderCinemaList() {
  // Variabile grid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const grid = document.getElementById('cinemas-grid');
  // Variabile noState: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const noState = document.getElementById('no-cinemas-state');

  if (!grid) return;

  if (!allCinemas.length) {
    grid.innerHTML = '';
    if (noState) noState.classList.remove('hidden');
    return;
  }

  if (noState) noState.classList.add('hidden');

  grid.innerHTML = allCinemas.map(cinema => {
    // Variabile tipologie: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const tipologie = (cinema.tipologieSalePresenti || []).map(t =>
      `<span class="inline-block bg-canvas-elevated text-ink text-xs px-2 py-0.5 rounded-full">${formatTipoSalaLabel(t)}</span>`
    ).join('');

    // Variabile distance: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const distance = cinema.distanzaKm != null ? `${cinema.distanzaKm.toFixed(1)} km` : '';

    return `
      <div class="card-ferrari p-5 card-hover cursor-pointer group" onclick="goToCinemaDetail(${cinema.id})">
        <div class="flex items-start gap-3 mb-3">
          <i class="fa-solid fa-film text-ferrari-primary text-xl mt-1"></i>
          <div class="flex-1 min-w-0">
            <h3 class="font-semibold text-lg text-ink group-hover:text-ferrari-primary transition-colors truncate">${cinema.nome}</h3>
            <p class="text-sm text-body cursor-pointer hover:text-ferrari-primary transition-colors" onclick="event.stopPropagation(); openCinemaMap(${cinema.id})">
              <i class="fa-solid fa-location-dot mr-1"></i>${cinema.citta}${cinema.indirizzo ? ` - ${cinema.indirizzo}` : ''}
              <i class="fa-solid fa-map ml-1 text-xs"></i>
            </p>
            ${distance ? `<p class="text-xs text-body mt-1"><i class="fa-solid fa-location-crosshairs mr-1"></i>${distance}</p>` : ''}
          </div>
        </div>
        ${tipologie ? `<div class="flex flex-wrap gap-1 mt-3 pt-3 border-t border-hairline/20">${tipologie}</div>` : ''}
      </div>
    `;
  }).join('');
}

// Funzione goToCinemaList: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToCinemaList() {
  window.location.href = '/my-cinemas.html';
}

// Funzione goToCinemaDetail: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToCinemaDetail(id) {
  window.location.href = `/my-cinemas.html?IdCinema=${id}`;
}

// Cinema Detail View
// Funzione loadCinemaDetail: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
async function loadCinemaDetail() {
  showLoading();

  try {
    // Variabile cParams: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    var cParams = {};
    if (userLocation) {
      cParams.lat = userLocation.lat;
      cParams.lng = userLocation.lng;
    }
    allCinemas = normalizeCollection(await API.getMyCinemas(cParams));

    // Variabile today: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const dateStr = localDateKey(today);
    scheduleData = await API.getCinemaSchedule(parseInt(cinemaId, 10), dateStr);

    if (!scheduleData) {
      showError('Cinema non trovato');
      return;
    }

    hideLoading();
    // Variabile detailView: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const detailView = document.getElementById('cinema-detail-view');
    if (detailView) detailView.classList.remove('hidden');

    renderCinemaDetail();
    setupDateRail();
    renderSchedule();
  } catch (error) {
    console.error('Errore caricamento dettaglio cinema:', error);
    showError(error.message || 'Errore nel caricamento del cinema');
  }
}

// Funzione renderCinemaDetail: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderCinemaDetail() {
  // Variabile cinema: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cinema = scheduleData.cinema;
  if (!cinema) return;

  // Variabile nameEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nameEl = document.getElementById('cinema-name');
  if (nameEl) nameEl.textContent = cinema.nome;

  // Variabile addressEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const addressEl = document.getElementById('cinema-address');
  if (addressEl) {
    // Variabile span: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const span = addressEl.querySelector('span');
    if (span) span.textContent = `${cinema.citta}${cinema.indirizzo ? ` - ${cinema.indirizzo}` : ''}`;
  }

  // Variabile cinemaFromList: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cinemaFromList = allCinemas.find(c => Number(c.id) === Number(cinema.id));
  // Variabile tipologie: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tipologie = (cinemaFromList?.tipologieSalePresenti || []).map(t =>
    `<span class="inline-block bg-canvas-elevated text-ink text-xs px-2 py-0.5 rounded-full">${t}</span>`
  ).join('');

  // Variabile tipologieEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tipologieEl = document.getElementById('cinema-tipologie');
  if (tipologieEl) tipologieEl.innerHTML = tipologie;
}

// Funzione setupDateRail: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupDateRail() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('date-rail-container');
  if (!container) return;

  dateRail = DateRail.create('date-rail-container', {
    days: 14,
    onDateSelected: async (date) => {
      await loadScheduleForDate(date);
    }
  });
}

// Funzione loadScheduleForDate: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadScheduleForDate(date) {
  // Variabile dateStr: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const dateStr = localDateKey(date);

  try {
    scheduleData = await API.getCinemaSchedule(parseInt(cinemaId, 10), dateStr);
    renderSchedule();
  } catch (error) {
    console.error('Errore caricamento programmazione:', error);
  }
}

// Funzione renderSchedule: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderSchedule() {
  // Variabile container: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const container = document.getElementById('films-schedule');
  // Variabile noShowsState: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const noShowsState = document.getElementById('no-shows-state');

  if (!container) return;

  // Variabile films: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const films = scheduleData?.films || [];

  if (!films.length) {
    container.innerHTML = '';
    if (noShowsState) noShowsState.classList.remove('hidden');
    return;
  }

  if (noShowsState) noShowsState.classList.add('hidden');

  // Variabile tipoSalaOrder: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tipoSalaOrder = ['2D', '3D', 'ISENSE', 'XL'];

  container.innerHTML = films.map(film => {
    // Variabile cover: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const cover = getCoverImage(film.copertinaPath);
    // Variabile descrizione: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const descrizione = film.descrizioneEstratto || '';

    // Variabile gruppiOrdinati: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const gruppiOrdinati = [...(film.gruppiPerTipoSala || [])].sort((a, b) => {
      // Variabile idxA: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const idxA = tipoSalaOrder.indexOf(a.tipoSala);
      // Variabile idxB: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const idxB = tipoSalaOrder.indexOf(b.tipoSala);
      return (idxA === -1 ? 999 : idxA) - (idxB === -1 ? 999 : idxB);
    });

    // Variabile showsHtml: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let showsHtml = '';

    gruppiOrdinati.forEach(gruppo => {
      // Variabile tipoSala: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const tipoSala = gruppo.tipoSala;
      // Variabile shows: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const shows = gruppo.shows || [];

      if (shows.length === 0) return;

      // Variabile timeGroups: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const timeGroups = {};
      shows.forEach(show => {
        // Variabile time: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const time = formatLocalTime(show.startAtUtc);
        if (!timeGroups[time]) {
          timeGroups[time] = [];
        }
        timeGroups[time].push(show);
      });

      showsHtml += `
        <div class="mb-3">
          <div class="tipo-sala-header">
            <span class="tipo-sala-badge ${getTipoSalaClass(tipoSala)}">${formatTipoSalaLabel(tipoSala)}</span>
          </div>
          <div class="flex flex-wrap gap-2">
      `;

      Object.keys(timeGroups).sort().forEach(time => {
        // Variabile showsAtTime: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const showsAtTime = timeGroups[time];

        if (showsAtTime.length === 1) {
          // Variabile show: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const show = showsAtTime[0];
          showsHtml += renderTimeButton(time, show);
        } else {
          showsAtTime.forEach(show => {
            showsHtml += renderTimeButton(time, show, true);
          });
        }
      });

      showsHtml += `
          </div>
        </div>
      `;
    });

    return `
      <div class="film-schedule-card">
        <img src="${cover}" alt="${film.titolo}" class="film-schedule-cover" loading="lazy" decoding="async" fetchpriority="low" referrerpolicy="no-referrer">
        <div class="flex-1 min-w-0">
          <h3 class="font-semibold text-lg text-ink mb-1">${film.titolo}</h3>
          ${descrizione ? `<p class="text-sm text-body line-clamp-2 mb-3">${descrizione}</p>` : ''}
          ${showsHtml}
        </div>
      </div>
    `;
  }).join('');

  // Show accessibility filters
  showAccessibilityFilters();

  // Apply accessibility filters
  applyAccessibilityFilters(container);

  container.querySelectorAll('.show-time-btn').forEach(btn => {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    btn.addEventListener('click', () => {
      // Variabile showId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const showId = btn.dataset.showId;
      handleShowClick(parseInt(showId, 10));
    });
  });
}

// Funzione applyAccessibilityFilters: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function applyAccessibilityFilters(container) {
  // Variabile activeFilters: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const activeFilters = [];
  if (accessibilityFilters.subtitles) activeFilters.push('subtitles');
  if (accessibilityFilters.audiodesc) activeFilters.push('audiodesc');

  // Variabile filmCards: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const filmCards = container.querySelectorAll('.film-schedule-card');
  filmCards.forEach(card => {
    // Variabile buttons: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const buttons = card.querySelectorAll('.show-time-btn');
    // Variabile visibleCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let visibleCount = 0;

    buttons.forEach(btn => {
      if (activeFilters.length === 0) {
        btn.classList.remove('hidden');
        visibleCount++;
        return;
      }

      // Variabile match: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      let match = true;
      if (accessibilityFilters.subtitles && !btn.classList.contains('a11y-subtitles')) match = false;
      if (accessibilityFilters.audiodesc && !btn.classList.contains('a11y-audiodesc')) match = false;

      if (match) {
        btn.classList.remove('hidden');
        visibleCount++;
      } else {
        btn.classList.add('hidden');
      }
    });

    // Hide entire film card if no shows match
    if (activeFilters.length > 0 && visibleCount === 0) {
      card.classList.add('hidden');
    } else {
      card.classList.remove('hidden');
    }

    // Hide empty tipo-sala groups
    card.querySelectorAll('.mb-3').forEach(group => {
      // Variabile visibleBtns: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const visibleBtns = group.querySelectorAll('.show-time-btn:not(.hidden)');
      if (activeFilters.length > 0 && visibleBtns.length === 0) {
        group.classList.add('hidden');
      } else {
        group.classList.remove('hidden');
      }
    });
  });
}

// Funzione requestUserLocationInBackground: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function requestUserLocationInBackground() {
  try {
    userLocation = await getUserLocation();
  } catch {
    // geolocation not available or denied
  }
}

// Funzione renderTimeButton: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderTimeButton(time, show, showSalaBadge = false) {
  // Variabile showId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const showId = show.showId;
  // Variabile salaNumero: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const salaNumero = show.salaNumeroProgressivo;

  // Variabile badges: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let badges = '';
  if (showSalaBadge) {
    badges += `<span class="sala-badge">Sala ${salaNumero}</span>`;
  }

  // Accessibility badges
  const hasSubs = showHasSubtitles(show);
  // Variabile hasAD: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const hasAD = showHasAudioDesc(show);
  if (hasSubs) {
    badges += `<span class="a11y-badge a11y-subs" title="Sottotitoli"><i class="fa-solid fa-closed-captioning"></i></span>`;
  }
  if (hasAD) {
    badges += `<span class="a11y-badge a11y-ad" title="Audio Descrizione"><i class="fa-solid fa-headphones"></i></span>`;
  }

  // Variabile a11yClasses: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const a11yClasses = [];
  if (hasSubs) a11yClasses.push('a11y-subtitles');
  if (hasAD) a11yClasses.push('a11y-audiodesc');

  return `
    <button class="show-time-btn ${a11yClasses.join(' ')}" data-show-id="${showId}" type="button">
      ${time}${badges}
    </button>
  `;
}

// Funzione handleShowClick: gestisce un evento o una risposta utente. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function handleShowClick(showId) {
  // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const auth = getAuthSafe();
  if (auth && auth.isLoggedIn()) {
    window.location.href = `/acquista.html?showId=${showId}`;
  } else {
    // Variabile targetUrl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const targetUrl = `/acquista.html?showId=${showId}`;
    window.location.href = `/login.html?redirect=${encodeURIComponent(targetUrl)}`;
  }
}

// Funzione getTipoSalaClass: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getTipoSalaClass(tipoSala) {
  // Variabile normalized: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const normalized = (tipoSala || '').toUpperCase();
  if (normalized === 'TRED' || normalized === '3D') return 'tipo-sala-badge-3d';
  if (normalized === 'ISENSE') return 'tipo-sala-badge-isense';
  if (normalized === 'XL') return 'tipo-sala-badge-xl';
  return 'tipo-sala-badge-2d';
}

// Utilities
// Funzione showLoading: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
function showLoading() {
  // Variabile loading: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const loading = document.getElementById('loading-state');
  // Variabile error: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const error = document.getElementById('error-state');
  // Variabile listView: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const listView = document.getElementById('cinema-list-view');
  // Variabile detailView: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const detailView = document.getElementById('cinema-detail-view');
  if (loading) loading.classList.remove('hidden');
  if (error) error.classList.add('hidden');
  if (listView) listView.classList.add('hidden');
  if (detailView) detailView.classList.add('hidden');
}

// Funzione hideLoading: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function hideLoading() {
  // Variabile loading: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const loading = document.getElementById('loading-state');
  if (loading) loading.classList.add('hidden');
}

// Funzione showError: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function showError(message) {
  // Variabile loading: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const loading = document.getElementById('loading-state');
  // Variabile error: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const error = document.getElementById('error-state');
  // Variabile listView: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const listView = document.getElementById('cinema-list-view');
  // Variabile detailView: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const detailView = document.getElementById('cinema-detail-view');
  // Variabile msgEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const msgEl = document.getElementById('error-message');
  if (loading) loading.classList.add('hidden');
  if (error) error.classList.remove('hidden');
  if (listView) listView.classList.add('hidden');
  if (detailView) detailView.classList.add('hidden');
  if (msgEl) msgEl.textContent = message;
}


// Funzione formatLocalTime: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function formatLocalTime(dateTimeStr) {
  if (!dateTimeStr) return '';
  const d = new Date(dateTimeStr);
  // Variabile hours: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const hours = String(d.getHours()).padStart(2, '0');
  // Variabile minutes: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const minutes = String(d.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

// Variabile mapInstance: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let mapInstance = null;
// Variabile mapTimeout: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let mapTimeout = null;

// Funzione openCinemaMap: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function openCinemaMap(cinemaId) {
  // Variabile cinema: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cinema = allCinemas.find(c => Number(c.id) === Number(cinemaId));
  if (!cinema) return;

  // Variabile modal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const modal = document.getElementById('map-modal');
  // Variabile nameEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nameEl = document.getElementById('map-cinema-name');
  if (!modal || !nameEl) return;

  // Kill any pending map init from previous call
  if (mapTimeout) {
    clearTimeout(mapTimeout);
    mapTimeout = null;
  }

  nameEl.textContent = `${cinema.nome} - ${cinema.citta}${cinema.indirizzo ? `, ${cinema.indirizzo}` : ''}`;
  modal.classList.remove('hidden');

  // Capture cinema refs by value to avoid stale closure
  const cinemaNome = cinema.nome;
  // Variabile cinemaIndirizzo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cinemaIndirizzo = cinema.indirizzo || '';
  // Variabile cinemaCitta: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cinemaCitta = cinema.citta || '';
  // Variabile hasCoords: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const hasCoords = cinema.latitudine != null && !isNaN(cinema.latitudine)
    && cinema.longitudine != null && !isNaN(cinema.longitudine);

  // Initialize map after a short delay so the container is visible
  mapTimeout = setTimeout(async () => {
    mapTimeout = null;

    if (mapInstance) {
      mapInstance.remove();
      mapInstance = null;
    }

    // Variabile mapLat: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    let mapLat, mapLng;

    if (hasCoords) {
      mapLat = cinema.latitudine;
      mapLng = cinema.longitudine;
    } else {
      // Geocode address via free Nominatim API
      const query = [cinemaIndirizzo, cinemaCitta, 'Italia'].filter(Boolean).join(', ');
      try {
        // Chiamata API: contatta il backend con i dati previsti e usa la risposta per aggiornare l'interfaccia.
        const resp = await fetch(
          `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1`
        );
        // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
        const data = await resp.json();
        if (data.length > 0) {
          mapLat = parseFloat(data[0].lat);
          mapLng = parseFloat(data[0].lon);
        } else {
          // Try just the city
// Chiamata API: contatta l'endpoint indicato, invia i dati richiesti e legge la risposta.
          const resp2 = await fetch(
            `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(cinemaCitta + ', Italia')}&limit=1`
          );
          // Variabile data2: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
          const data2 = await resp2.json();
          if (data2.length > 0) {
            mapLat = parseFloat(data2[0].lat);
            mapLng = parseFloat(data2[0].lon);
          } else {
            // Last fallback: Italy center
            mapLat = 41.8719;
            mapLng = 12.5674;
          }
        }
      } catch {
        mapLat = 41.8719;
        mapLng = 12.5674;
      }
    }

    mapInstance = L.map('map-container', {
      attributionControl: true,
      zoomControl: true
    }).setView([mapLat, mapLng], 15);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      maxZoom: 19
    }).addTo(mapInstance);

    L.marker([mapLat, mapLng])
      .addTo(mapInstance)
      .bindPopup(`<b>${cinemaNome}</b><br>${cinemaIndirizzo || cinemaCitta}`)
      .openPopup();
  }, 150);
}

// Funzione closeMapModal: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function closeMapModal() {
  // Variabile modal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const modal = document.getElementById('map-modal');
  if (modal) modal.classList.add('hidden');

  if (mapTimeout) {
    clearTimeout(mapTimeout);
    mapTimeout = null;
  }

  if (mapInstance) {
    mapInstance.remove();
    mapInstance = null;
  }
}

// Funzione getUserLocation: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getUserLocation() {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject(new Error('Geolocation not available'));
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        resolve({
          lat: position.coords.latitude,
          lng: position.coords.longitude
        });
      },
      () => {
        reject(new Error('Geolocation permission denied'));
      },
      { enableHighAccuracy: false, timeout: 5000, maximumAge: 300000 }
    );
  });
}

window.goToCinemaList = goToCinemaList;
window.goToCinemaDetail = goToCinemaDetail;
window.openCinemaMap = openCinemaMap;
window.closeMapModal = closeMapModal;

// ─── Accessibility Filters ──────────────────────────────
let accessibilityFilters = { subtitles: false, audiodesc: false };

// Funzione showHasSubtitles: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function showHasSubtitles(show) {
  // Deterministic: shows with even showId have subtitles (demo)
  return (show.showId % 2 === 0);
}

// Funzione showHasAudioDesc: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function showHasAudioDesc(show) {
  // Deterministic: shows divisible by 3 have audio description (demo)
  return (show.showId % 3 === 0);
}

// Funzione toggleAccessibilityFilter: commuta uno stato visivo o funzionale tra due modalità. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function toggleAccessibilityFilter(type) {
  accessibilityFilters[type] = !accessibilityFilters[type];
  // Variabile btn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btn = document.getElementById(type === 'subtitles' ? 'filter-subtitles' : 'filter-audiodesc');
  if (btn) {
    if (accessibilityFilters[type]) {
      btn.classList.add('bg-ferrari-primary', 'text-white', 'border-ferrari-primary');
      btn.classList.remove('text-body', 'border-hairline');
      btn.dataset.active = 'true';
    } else {
      btn.classList.remove('bg-ferrari-primary', 'text-white', 'border-ferrari-primary');
      btn.classList.add('text-body', 'border-hairline');
      btn.dataset.active = 'false';
    }
  }
  renderSchedule();
}

// Show accessibility filters when schedule is loaded
// Funzione showAccessibilityFilters: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
function showAccessibilityFilters() {
  // Variabile filterBar: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const filterBar = document.getElementById('accessibility-filters');
  if (filterBar) filterBar.classList.remove('hidden');
}

window.toggleAccessibilityFilter = toggleAccessibilityFilter;
