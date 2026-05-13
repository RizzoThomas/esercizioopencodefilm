// Scheda Film Page JavaScript

const CinemaManager = {
  STORAGE_KEY: 'cb_selected_cinema',

  getLocalCinemaId() {
    const val = localStorage.getItem(this.STORAGE_KEY);
    return val ? parseInt(val, 10) : null;
  },

  setLocalCinemaId(cinemaId) {
    if (cinemaId == null) {
      localStorage.removeItem(this.STORAGE_KEY);
    } else {
      localStorage.setItem(this.STORAGE_KEY, String(cinemaId));
    }
  },

  async syncCinemaPreferito() {
    const auth = getAuthSafe();
    if (!auth || !auth.isLoggedIn()) {
      return this.getLocalCinemaId();
    }

    try {
      const result = await API.getCinemaPreferito();
      const backendCinemaId = result?.cinemaId ? parseInt(result.cinemaId, 10) : null;
      const localCinemaId = this.getLocalCinemaId();

      if (backendCinemaId != null) {
        if (localCinemaId !== backendCinemaId) {
          this.setLocalCinemaId(backendCinemaId);
        }
        return backendCinemaId;
      }

      if (localCinemaId != null) {
        try {
          await API.setCinemaPreferito(localCinemaId);
        } catch {
          // ignore sync errors
        }
        return localCinemaId;
      }

      return null;
    } catch {
      return this.getLocalCinemaId();
    }
  },

  async setCinema(cinemaId) {
    this.setLocalCinemaId(cinemaId);

    const auth = getAuthSafe();
    if (auth && auth.isLoggedIn()) {
      try {
        await API.setCinemaPreferito(cinemaId);
      } catch {
        // ignore errors
      }
    }

    window.dispatchEvent(new CustomEvent('cinema:changed', {
      detail: { cinemaId }
    }));
  }
};

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
let filmId = null;
let selectedCinemaId = null;
let filmData = null;
let allCinemas = [];
let userLocation = null;
let dateRail = null;
let showCalendar = [];
let cinemasLoaded = false;
let modalSearchTerm = '';

document.addEventListener('DOMContentLoaded', async () => {
  const params = new URLSearchParams(window.location.search);
  filmId = params.get('id');
  const cinemaParam = params.get('cinema');

  if (!filmId) {
    showError('ID film non specificato');
    return;
  }

  selectedCinemaId = cinemaParam ? parseInt(cinemaParam, 10) : await CinemaManager.syncCinemaPreferito();

  setupCinemaModal();

  const filmPromise = loadFilm();
  const cinemasPromise = loadCinemas().then(() => {
    cinemasLoaded = true;
    renderCinemaInfo();
  });

  requestUserLocationInBackground();

  await Promise.all([filmPromise, cinemasPromise]);
});

async function loadCinemas() {
  try {
    const params = {};
    if (userLocation) {
      params.lat = userLocation.lat;
      params.lng = userLocation.lng;
    }
    allCinemas = normalizeCollection(await API.getProgrammazioneCinemas(params));
  } catch (error) {
    console.error('Errore caricamento cinema:', error);
  }
}

async function loadFilm() {
  showLoading();

  try {
    filmData = await API.getFilmScheda(filmId, selectedCinemaId);

    if (!filmData) {
      showError('Film non trovato');
      return;
    }

    renderFilm();
    setupDateRail();
    renderShows();
  } catch (error) {
    console.error('Errore caricamento film:', error);
    showError(error.message || 'Errore nel caricamento della scheda film');
  }
}

function renderFilm() {
  hideLoading();

  const content = document.getElementById('film-content');
  if (content) content.classList.remove('hidden');

  // Cover
  const cover = document.getElementById('film-cover');
  if (cover) {
    cover.loading = 'eager';
    cover.decoding = 'async';
    cover.fetchPriority = 'high';
    cover.referrerPolicy = 'no-referrer';
    cover.src = getCoverImage(filmData.copertinaPath);
    cover.alt = filmData.titolo;
  }

  // Title
  const title = document.getElementById('film-title');
  if (title) title.textContent = filmData.titolo;

  // Duration
  const duration = document.getElementById('film-duration');
  if (duration) {
    const span = duration.querySelector('span');
    if (span) span.textContent = `${filmData.durata || '-'} min`;
  }

  // Release date
  const release = document.getElementById('film-release');
  if (release && filmData.dataRilascio) {
    release.classList.remove('hidden');
    const span = release.querySelector('span');
    if (span) span.textContent = formatDateOnly(filmData.dataRilascio);
  }

  // Categories
  const categories = document.getElementById('film-categories');
  if (categories) {
    const cats = filmData.categorie || [];
    categories.innerHTML = cats.map(c =>
      `<span class="inline-block bg-canvas-elevated text-ink text-xs px-2 py-0.5 rounded-full">${c.nome}</span>`
    ).join('');
  }

  // Director
  const director = document.getElementById('film-director');
  if (director) {
    const nome = filmData.registaNome || '';
    const cognome = filmData.registaCognome || '';
    if (nome || cognome) {
      director.classList.remove('hidden');
      const p = director.querySelector('p.font-medium');
      if (p) p.textContent = `${nome} ${cognome}`.trim();
    }
  }

  // Cast
  const cast = document.getElementById('film-cast');
  if (cast && filmData.castList && filmData.castList.length) {
    cast.classList.remove('hidden');
    const p = cast.querySelector('p.text-sm');
    if (p) p.textContent = filmData.castList.join(', ');
  }

  // Description
  const description = document.getElementById('film-description');
  if (description && filmData.descrizioneLunga) {
    description.classList.remove('hidden');
    const p = description.querySelector('p.line-clamp-4');
    if (p) p.textContent = filmData.descrizioneLunga;
  }

  // Go to shows button
  const goToShowsBtn = document.getElementById('go-to-shows-btn');
  if (goToShowsBtn) {
    goToShowsBtn.addEventListener('click', () => {
      const showsSection = document.getElementById('shows-section');
      if (showsSection) {
        showsSection.scrollIntoView({ behavior: 'smooth' });
      }
    });
  }

  // Cinema info
  renderCinemaInfo();

  // Store show calendar
  showCalendar = filmData.showCalendar || [];
}

function renderCinemaInfo() {
  const cinemaInfo = document.getElementById('cinema-info');
  const noCinemaWarning = document.getElementById('no-cinema-warning');
  const dateRailContainer = document.getElementById('date-rail-container');

  if (selectedCinemaId == null) {
    if (cinemaInfo) cinemaInfo.classList.add('hidden');
    if (noCinemaWarning) noCinemaWarning.classList.remove('hidden');
    if (dateRailContainer) dateRailContainer.classList.add('hidden');
    return;
  }

  if (noCinemaWarning) noCinemaWarning.classList.add('hidden');
  if (cinemaInfo) cinemaInfo.classList.remove('hidden');
  if (dateRailContainer) dateRailContainer.classList.remove('hidden');

  const cinema = allCinemas.find(c => Number(c.id) === Number(selectedCinemaId)) || filmData.cinemaSelezionato;
  if (cinema) {
    const nameEl = document.getElementById('cinema-name');
    const detailEl = document.getElementById('cinema-detail');
    if (nameEl) nameEl.textContent = cinema.nome;
    if (detailEl) detailEl.textContent = `${cinema.citta}${cinema.indirizzo ? ` - ${cinema.indirizzo}` : ''}`;
  }
}

function setupDateRail() {
  const container = document.getElementById('date-rail-container');
  if (!container) return;

  // Calculate how many days of shows we have
  let days = 14;
  if (showCalendar.length > 0) {
    const firstDate = new Date(showCalendar[0].data + 'T00:00:00');
    const lastDate = new Date(showCalendar[showCalendar.length - 1].data + 'T00:00:00');
    const span = Math.ceil((lastDate - firstDate) / (1000 * 60 * 60 * 24)) + 1;
    days = Math.max(span, 7);
  }

  dateRail = DateRail.create('date-rail-container', {
    days: Math.min(days, 30),
    onDateSelected: () => {
      renderShows();
    }
  });
}

function renderShows() {
  const showsSection = document.getElementById('shows-section');
  const noShowsState = document.getElementById('no-shows-state');
  const container = document.getElementById('shows-container');

  if (!container || selectedCinemaId == null) {
    if (showsSection) showsSection.classList.add('hidden');
    if (noShowsState) noShowsState.classList.add('hidden');
    return;
  }

  const selectedDate = dateRail?.getSelectedDate();
  if (!selectedDate) {
    if (showsSection) showsSection.classList.add('hidden');
    if (noShowsState) noShowsState.classList.add('hidden');
    return;
  }

  const selectedKey = localDateKey(selectedDate);

  // Match backend DateOnly string directly (backend returns "YYYY-MM-DD" as local date)
  const dayGroup = showCalendar.find(g => g.data === selectedKey);

  if (!dayGroup || !dayGroup.gruppiPerTipoSala || dayGroup.gruppiPerTipoSala.length === 0) {
    if (showsSection) showsSection.classList.add('hidden');
    if (noShowsState) noShowsState.classList.remove('hidden');
    return;
  }

  if (noShowsState) noShowsState.classList.add('hidden');
  if (showsSection) showsSection.classList.remove('hidden');

  const tipoSalaOrder = ['2D', '3D', 'ISENSE', 'XL'];
  const gruppiOrdinati = [...dayGroup.gruppiPerTipoSala].sort((a, b) => {
    const idxA = tipoSalaOrder.indexOf(a.tipoSala);
    const idxB = tipoSalaOrder.indexOf(b.tipoSala);
    return (idxA === -1 ? 999 : idxA) - (idxB === -1 ? 999 : idxB);
  });

  let html = '';

  gruppiOrdinati.forEach(gruppo => {
    const tipoSala = gruppo.tipoSala;
    const shows = gruppo.shows || [];

    if (shows.length === 0) return;

    // Group by local time, aggregate sala badges for same-time shows
    const timeGroups = {};
    shows.forEach(show => {
      const time = formatLocalTime(show.startAtUtc);
      if (!timeGroups[time]) {
        timeGroups[time] = [];
      }
      timeGroups[time].push(show);
    });

    html += `
      <div class="mb-6">
        <div class="tipo-sala-header">
          <span class="tipo-sala-badge ${getTipoSalaClass(tipoSala)}">${formatTipoSalaLabel(tipoSala)}</span>
        </div>
        <div class="flex flex-wrap gap-2">
    `;

    Object.keys(timeGroups).sort().forEach(time => {
      const showsAtTime = timeGroups[time];

      if (showsAtTime.length === 1) {
        const show = showsAtTime[0];
        html += renderTimeButton(time, show);
      } else {
        showsAtTime.forEach(show => {
          html += renderTimeButton(time, show, true);
        });
      }
    });

    html += `
        </div>
      </div>
    `;
  });

  container.innerHTML = html;

  container.querySelectorAll('.show-time-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const showId = btn.dataset.showId;
      handleShowClick(parseInt(showId, 10));
    });
  });
}

function renderTimeButton(time, show, showSalaBadge = false) {
  const showId = show.showId;
  const salaNumero = show.salaNumeroProgressivo;

  let badgeHtml = '';
  if (showSalaBadge) {
    badgeHtml = `<span class="sala-badge">Sala ${salaNumero}</span>`;
  }

  return `
    <button class="show-time-btn" data-show-id="${showId}" type="button">
      ${time}${badgeHtml}
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

// Cinema Modal
function setupCinemaModal() {
  const modal = document.getElementById('cinema-modal');
  const closeBtn = document.getElementById('cinema-modal-close');
  const ctaBtn = document.getElementById('select-cinema-cta');
  const searchInput = document.getElementById('cinema-search-input');

  const openModal = () => {
    if (modal) {
      modal.classList.remove('hidden');
      document.body.style.overflow = 'hidden';
      if (!cinemasLoaded) {
        const list = document.getElementById('cinema-list');
        if (list) {
          list.innerHTML = `
            <div class="text-center py-8 text-body">
              <i class="fa-solid fa-spinner fa-spin text-2xl mb-2"></i>
              <p>Caricamento cinema...</p>
            </div>
          `;
        }
      } else {
        renderCinemaList(modalSearchTerm);
      }
    }
  };

  const closeModal = () => {
    if (modal) {
      modal.classList.add('hidden');
      document.body.style.overflow = '';
    }
  };

  ctaBtn?.addEventListener('click', openModal);
  closeBtn?.addEventListener('click', closeModal);

  modal?.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && modal && !modal.classList.contains('hidden')) {
      closeModal();
    }
  });

  let debounceTimer;
  searchInput?.addEventListener('input', (e) => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
      modalSearchTerm = e.target.value.trim();
      if (cinemasLoaded) {
        renderCinemaList(modalSearchTerm);
      }
    }, 200);
  });

  window.addEventListener('cinema:changed', (e) => {
    selectedCinemaId = e.detail?.cinemaId ?? selectedCinemaId;
    renderCinemaInfo();
    loadFilm();
  });
}

async function requestUserLocationInBackground() {
  try {
    userLocation = await getUserLocation();
    await loadCinemas();
    cinemasLoaded = true;
    renderCinemaInfo();

    const modal = document.getElementById('cinema-modal');
    if (modal && !modal.classList.contains('hidden')) {
      renderCinemaList(modalSearchTerm);
    }
  } catch {
    // geolocation not available or denied
  }
}

function renderCinemaList(search = '') {
  const list = document.getElementById('cinema-list');
  if (!list) return;

  let cinemas = [...allCinemas];

  if (search) {
    const s = search.toLowerCase();
    cinemas = cinemas.filter(c =>
      (c.nome && c.nome.toLowerCase().includes(s)) ||
      (c.citta && c.citta.toLowerCase().includes(s)) ||
      (c.indirizzo && c.indirizzo.toLowerCase().includes(s))
    );
  }

  if (!cinemas.length) {
    list.innerHTML = `
      <div class="text-center py-8 text-body">
        <i class="fa-solid fa-film text-3xl mb-2"></i>
        <p>Nessun cinema trovato</p>
      </div>
    `;
    return;
  }

  list.innerHTML = cinemas.map(cinema => {
    const isSelected = Number(cinema.id) === Number(selectedCinemaId);
    const distance = cinema.distanzaKm != null ? `${cinema.distanzaKm.toFixed(1)} km` : '';
    const tipologie = (cinema.tipologieSalePresenti || []).slice(0, 4).map(t =>
      `<span class="inline-block bg-canvas-elevated text-ink text-xs px-2 py-0.5 rounded-full">${formatTipoSalaLabel(t)}</span>`
    ).join('');

    return `
      <button onclick="selectCinema(${cinema.id})"
        class="w-full text-left p-4 border transition-colors ${isSelected
          ? 'border-ferrari-primary bg-canvas-elevated'
          : 'border-hairline/20 hover:border-ferrari-primary/50 hover:bg-canvas'
        }">
        <div class="flex items-start justify-between gap-3">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1">
              <h3 class="font-semibold text-ink truncate">${cinema.nome}</h3>
              ${isSelected ? '<i class="fa-solid fa-circle-check text-ferrari-primary"></i>' : ''}
            </div>
            <p class="text-sm text-body">${cinema.citta}${cinema.indirizzo ? ` - ${cinema.indirizzo}` : ''}</p>
            ${distance ? `<p class="text-xs text-body mt-1"><i class="fa-solid fa-location-dot mr-1"></i>${distance}</p>` : ''}
            ${tipologie ? `<div class="flex flex-wrap gap-1 mt-2">${tipologie}</div>` : ''}
          </div>
        </div>
      </button>
    `;
  }).join('');
}

function selectCinema(cinemaId) {
  CinemaManager.setCinema(cinemaId);
  selectedCinemaId = cinemaId;

  const modal = document.getElementById('cinema-modal');
  if (modal) {
    modal.classList.add('hidden');
    document.body.style.overflow = '';
  }

  renderCinemaInfo();
  loadFilm();
}

// Utilities
function showLoading() {
  const loading = document.getElementById('loading-state');
  const error = document.getElementById('error-state');
  const content = document.getElementById('film-content');
  if (loading) loading.classList.remove('hidden');
  if (error) error.classList.add('hidden');
  if (content) content.classList.add('hidden');
}

function hideLoading() {
  const loading = document.getElementById('loading-state');
  if (loading) loading.classList.add('hidden');
}

function showError(message) {
  const loading = document.getElementById('loading-state');
  const error = document.getElementById('error-state');
  const content = document.getElementById('film-content');
  const msgEl = document.getElementById('error-message');
  if (loading) loading.classList.add('hidden');
  if (error) error.classList.remove('hidden');
  if (content) content.classList.add('hidden');
  if (msgEl) msgEl.textContent = message;
}


function formatLocalTime(dateTimeStr) {
  if (!dateTimeStr) return '';
  const d = new Date(dateTimeStr);
  const hours = String(d.getHours()).padStart(2, '0');
  const minutes = String(d.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

function formatDateOnly(dateStr) {
  if (!dateStr) return '';
  if (typeof dateStr === 'string' && dateStr.includes('-')) {
    const parts = dateStr.split('-');
    return `${parts[2]}/${parts[1]}/${parts[0]}`;
  }
  return dateStr;
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

window.selectCinema = selectCinema;

// ─── Watchlist ──────────────────────────────────────────
let isWatchlistSaved = false;

async function checkWatchlistStatus() {
  const auth = getAuthSafe();
  if (!auth || !auth.isLoggedIn()) return;

  try {
    const result = await API.checkWatchlist(parseInt(filmId, 10));
    isWatchlistSaved = result.isSaved;
    updateWatchlistIcon();
  } catch {
    // ignore - user not logged in or error
  }
}

function updateWatchlistIcon() {
  const icon = document.getElementById('watchlist-icon');
  if (!icon) return;
  if (isWatchlistSaved) {
    icon.className = 'fa-solid fa-bookmark text-ferrari-primary';
  } else {
    icon.className = 'fa-regular fa-bookmark';
  }
}

async function toggleWatchlist() {
  const auth = getAuthSafe();
  if (!auth || !auth.isLoggedIn()) {
    window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.href);
    return;
  }

  try {
    if (isWatchlistSaved) {
      await API.removeFromWatchlist(parseInt(filmId, 10));
      isWatchlistSaved = false;
      showToast('Film rimosso dalla watchlist');
    } else {
      await API.addToWatchlist(parseInt(filmId, 10));
      isWatchlistSaved = true;
      showToast('Film salvato nella watchlist!');
    }
    updateWatchlistIcon();
  } catch {
    showToast('Errore, riprova', 'danger');
  }
}

// Check watchlist status after film loads
const _origLoadFilm = loadFilm;
loadFilm = async function() {
  await _origLoadFilm();
  // Wait a tick for DOM to settle, then check watchlist
  setTimeout(async () => {
    if (document.getElementById('watchlist-icon')) {
      await checkWatchlistStatus();
    }
  }, 100);
};

window.toggleWatchlist = toggleWatchlist;
