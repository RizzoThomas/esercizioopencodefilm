// My Cinemas Page JavaScript

function getAuthSafe() {
  return typeof window !== 'undefined' && window.Auth ? window.Auth : null;
}

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

function formatTipoSalaLabel(tipoSala) {
  const normalized = String(tipoSala || '').trim().toUpperCase();
  if (normalized === 'TRED' || normalized === '3D') return '3D';
  if (normalized === 'DUED' || normalized === '2D') return '2D';
  if (normalized === 'ISENSE') return 'ISENSE';
  if (normalized === 'XL') return 'XL';
  return tipoSala || '';
}

function localDateKey(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

// State
let cinemaId = null;
let allCinemas = [];
let scheduleData = null;
let dateRail = null;
let userLocation = null;

document.addEventListener('DOMContentLoaded', async () => {
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
async function loadCinemaList() {
  showLoading();

  try {
    var params = {};
    if (userLocation) {
      params.lat = userLocation.lat;
      params.lng = userLocation.lng;
    }
    allCinemas = normalizeCollection(await API.getMyCinemas(params));

    hideLoading();
    const listView = document.getElementById('cinema-list-view');
    if (listView) listView.classList.remove('hidden');

    renderCinemaList();
  } catch (error) {
    console.error('Errore caricamento cinema:', error);
    showError(error.message || 'Errore nel caricamento dei cinema');
  }
}

function renderCinemaList() {
  const grid = document.getElementById('cinemas-grid');
  const noState = document.getElementById('no-cinemas-state');

  if (!grid) return;

  if (!allCinemas.length) {
    grid.innerHTML = '';
    if (noState) noState.classList.remove('hidden');
    return;
  }

  if (noState) noState.classList.add('hidden');

  grid.innerHTML = allCinemas.map(cinema => {
    const tipologie = (cinema.tipologieSalePresenti || []).map(t =>
      `<span class="inline-block bg-canvas-elevated text-ink text-xs px-2 py-0.5 rounded-full">${formatTipoSalaLabel(t)}</span>`
    ).join('');

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

function goToCinemaList() {
  window.location.href = '/my-cinemas.html';
}

function goToCinemaDetail(id) {
  window.location.href = `/my-cinemas.html?IdCinema=${id}`;
}

// Cinema Detail View
async function loadCinemaDetail() {
  showLoading();

  try {
    var cParams = {};
    if (userLocation) {
      cParams.lat = userLocation.lat;
      cParams.lng = userLocation.lng;
    }
    allCinemas = normalizeCollection(await API.getMyCinemas(cParams));

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const dateStr = localDateKey(today);
    scheduleData = await API.getCinemaSchedule(parseInt(cinemaId, 10), dateStr);

    if (!scheduleData) {
      showError('Cinema non trovato');
      return;
    }

    hideLoading();
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

function renderCinemaDetail() {
  const cinema = scheduleData.cinema;
  if (!cinema) return;

  const nameEl = document.getElementById('cinema-name');
  if (nameEl) nameEl.textContent = cinema.nome;

  const addressEl = document.getElementById('cinema-address');
  if (addressEl) {
    const span = addressEl.querySelector('span');
    if (span) span.textContent = `${cinema.citta}${cinema.indirizzo ? ` - ${cinema.indirizzo}` : ''}`;
  }

  const cinemaFromList = allCinemas.find(c => Number(c.id) === Number(cinema.id));
  const tipologie = (cinemaFromList?.tipologieSalePresenti || []).map(t =>
    `<span class="inline-block bg-canvas-elevated text-ink text-xs px-2 py-0.5 rounded-full">${t}</span>`
  ).join('');

  const tipologieEl = document.getElementById('cinema-tipologie');
  if (tipologieEl) tipologieEl.innerHTML = tipologie;
}

function setupDateRail() {
  const container = document.getElementById('date-rail-container');
  if (!container) return;

  dateRail = DateRail.create('date-rail-container', {
    days: 14,
    onDateSelected: async (date) => {
      await loadScheduleForDate(date);
    }
  });
}

async function loadScheduleForDate(date) {
  const dateStr = localDateKey(date);

  try {
    scheduleData = await API.getCinemaSchedule(parseInt(cinemaId, 10), dateStr);
    renderSchedule();
  } catch (error) {
    console.error('Errore caricamento programmazione:', error);
  }
}

function renderSchedule() {
  const container = document.getElementById('films-schedule');
  const noShowsState = document.getElementById('no-shows-state');

  if (!container) return;

  const films = scheduleData?.films || [];

  if (!films.length) {
    container.innerHTML = '';
    if (noShowsState) noShowsState.classList.remove('hidden');
    return;
  }

  if (noShowsState) noShowsState.classList.add('hidden');

  const tipoSalaOrder = ['2D', '3D', 'ISENSE', 'XL'];

  container.innerHTML = films.map(film => {
    const cover = getCoverImage(film.copertinaPath);
    const descrizione = film.descrizioneEstratto || '';

    const gruppiOrdinati = [...(film.gruppiPerTipoSala || [])].sort((a, b) => {
      const idxA = tipoSalaOrder.indexOf(a.tipoSala);
      const idxB = tipoSalaOrder.indexOf(b.tipoSala);
      return (idxA === -1 ? 999 : idxA) - (idxB === -1 ? 999 : idxB);
    });

    let showsHtml = '';

    gruppiOrdinati.forEach(gruppo => {
      const tipoSala = gruppo.tipoSala;
      const shows = gruppo.shows || [];

      if (shows.length === 0) return;

      const timeGroups = {};
      shows.forEach(show => {
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
        const showsAtTime = timeGroups[time];

        if (showsAtTime.length === 1) {
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
    btn.addEventListener('click', () => {
      const showId = btn.dataset.showId;
      handleShowClick(parseInt(showId, 10));
    });
  });
}

function applyAccessibilityFilters(container) {
  const activeFilters = [];
  if (accessibilityFilters.subtitles) activeFilters.push('subtitles');
  if (accessibilityFilters.audiodesc) activeFilters.push('audiodesc');

  const filmCards = container.querySelectorAll('.film-schedule-card');
  filmCards.forEach(card => {
    const buttons = card.querySelectorAll('.show-time-btn');
    let visibleCount = 0;

    buttons.forEach(btn => {
      if (activeFilters.length === 0) {
        btn.classList.remove('hidden');
        visibleCount++;
        return;
      }

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
      const visibleBtns = group.querySelectorAll('.show-time-btn:not(.hidden)');
      if (activeFilters.length > 0 && visibleBtns.length === 0) {
        group.classList.add('hidden');
      } else {
        group.classList.remove('hidden');
      }
    });
  });
}

async function requestUserLocationInBackground() {
  try {
    userLocation = await getUserLocation();
  } catch {
    // geolocation not available or denied
  }
}

function renderTimeButton(time, show, showSalaBadge = false) {
  const showId = show.showId;
  const salaNumero = show.salaNumeroProgressivo;

  let badges = '';
  if (showSalaBadge) {
    badges += `<span class="sala-badge">Sala ${salaNumero}</span>`;
  }

  // Accessibility badges
  const hasSubs = showHasSubtitles(show);
  const hasAD = showHasAudioDesc(show);
  if (hasSubs) {
    badges += `<span class="a11y-badge a11y-subs" title="Sottotitoli"><i class="fa-solid fa-closed-captioning"></i></span>`;
  }
  if (hasAD) {
    badges += `<span class="a11y-badge a11y-ad" title="Audio Descrizione"><i class="fa-solid fa-headphones"></i></span>`;
  }

  const a11yClasses = [];
  if (hasSubs) a11yClasses.push('a11y-subtitles');
  if (hasAD) a11yClasses.push('a11y-audiodesc');

  return `
    <button class="show-time-btn ${a11yClasses.join(' ')}" data-show-id="${showId}" type="button">
      ${time}${badges}
    </button>
  `;
}

function handleShowClick(showId) {
  const auth = getAuthSafe();
  if (auth && auth.isLoggedIn()) {
    window.location.href = `/acquista.html?showId=${showId}`;
  } else {
    const targetUrl = `/acquista.html?showId=${showId}`;
    window.location.href = `/login.html?redirect=${encodeURIComponent(targetUrl)}`;
  }
}

function getTipoSalaClass(tipoSala) {
  const normalized = (tipoSala || '').toUpperCase();
  if (normalized === 'TRED' || normalized === '3D') return 'tipo-sala-badge-3d';
  if (normalized === 'ISENSE') return 'tipo-sala-badge-isense';
  if (normalized === 'XL') return 'tipo-sala-badge-xl';
  return 'tipo-sala-badge-2d';
}

// Utilities
function showLoading() {
  const loading = document.getElementById('loading-state');
  const error = document.getElementById('error-state');
  const listView = document.getElementById('cinema-list-view');
  const detailView = document.getElementById('cinema-detail-view');
  if (loading) loading.classList.remove('hidden');
  if (error) error.classList.add('hidden');
  if (listView) listView.classList.add('hidden');
  if (detailView) detailView.classList.add('hidden');
}

function hideLoading() {
  const loading = document.getElementById('loading-state');
  if (loading) loading.classList.add('hidden');
}

function showError(message) {
  const loading = document.getElementById('loading-state');
  const error = document.getElementById('error-state');
  const listView = document.getElementById('cinema-list-view');
  const detailView = document.getElementById('cinema-detail-view');
  const msgEl = document.getElementById('error-message');
  if (loading) loading.classList.add('hidden');
  if (error) error.classList.remove('hidden');
  if (listView) listView.classList.add('hidden');
  if (detailView) detailView.classList.add('hidden');
  if (msgEl) msgEl.textContent = message;
}


function formatLocalTime(dateTimeStr) {
  if (!dateTimeStr) return '';
  const d = new Date(dateTimeStr);
  const hours = String(d.getHours()).padStart(2, '0');
  const minutes = String(d.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

let mapInstance = null;
let mapTimeout = null;

function openCinemaMap(cinemaId) {
  const cinema = allCinemas.find(c => Number(c.id) === Number(cinemaId));
  if (!cinema) return;

  const modal = document.getElementById('map-modal');
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
  const cinemaIndirizzo = cinema.indirizzo || '';
  const cinemaCitta = cinema.citta || '';
  const hasCoords = cinema.latitudine != null && !isNaN(cinema.latitudine)
    && cinema.longitudine != null && !isNaN(cinema.longitudine);

  // Initialize map after a short delay so the container is visible
  mapTimeout = setTimeout(async () => {
    mapTimeout = null;

    if (mapInstance) {
      mapInstance.remove();
      mapInstance = null;
    }

    let mapLat, mapLng;

    if (hasCoords) {
      mapLat = cinema.latitudine;
      mapLng = cinema.longitudine;
    } else {
      // Geocode address via free Nominatim API
      const query = [cinemaIndirizzo, cinemaCitta, 'Italia'].filter(Boolean).join(', ');
      try {
        const resp = await fetch(
          `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1`
        );
        const data = await resp.json();
        if (data.length > 0) {
          mapLat = parseFloat(data[0].lat);
          mapLng = parseFloat(data[0].lon);
        } else {
          // Try just the city
          const resp2 = await fetch(
            `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(cinemaCitta + ', Italia')}&limit=1`
          );
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

function closeMapModal() {
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

function showHasSubtitles(show) {
  // Deterministic: shows with even showId have subtitles (demo)
  return (show.showId % 2 === 0);
}

function showHasAudioDesc(show) {
  // Deterministic: shows divisible by 3 have audio description (demo)
  return (show.showId % 3 === 0);
}

function toggleAccessibilityFilter(type) {
  accessibilityFilters[type] = !accessibilityFilters[type];
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
function showAccessibilityFilters() {
  const filterBar = document.getElementById('accessibility-filters');
  if (filterBar) filterBar.classList.remove('hidden');
}

window.toggleAccessibilityFilter = toggleAccessibilityFilter;
