// Programmazione Page JavaScript - Film-centric v2

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
        // ignore errors, localStorage is still updated
      }
    }

    const cinema = allCinemas.find(c => Number(c.id) === Number(cinemaId));
    window.dispatchEvent(new CustomEvent('cinema:changed', {
      detail: { cinemaId, cinemaName: cinema?.nome || null }
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

// State
let currentTab = 'evidenza';
let currentSearch = '';
let currentCategoriaId = '';
let selectedCinemaId = null;
let allCategorie = [];
let allCinemas = [];
let userLocation = null;
let cinemasLoaded = false;
let modalSearchTerm = '';
let currentFilms = [];
let pendingOffertaId = null;
let pendingOfferta = null;
const FILMS_PAGE_SIZE = 20;
const CAROUSEL_PAGE_SIZE = 8;
let currentPage = 1;
let currentPagedResult = null;
let isLoadingMoreCarousel = false;

document.addEventListener('DOMContentLoaded', async () => {
  const params = new URLSearchParams(window.location.search);
  pendingOffertaId = params.get('offertaId') || sessionStorage.getItem('pending_offerta_id');
  if (pendingOffertaId) {
    sessionStorage.setItem('pending_offerta_id', pendingOffertaId);
  }

  selectedCinemaId = await CinemaManager.syncCinemaPreferito();

  setupTabs();
  setupSearch();
  setupCategoriaFilter();
  setupCinemaModal();
  setupCarouselControls();
  setupLoadMore();

  renderCinemaHeader();

  // Prioritize the visible content first.
  const filmsPromise = loadFilms();
  const categoriePromise = loadCategorie().then(populateCategoriaFilter);
  const cinemasPromise = loadCinemas().then(() => {
    cinemasLoaded = true;
    renderCinemaHeader();
  });

  requestUserLocationInBackground();
  await loadPendingOfferBanner();

  await Promise.all([
    filmsPromise,
    categoriePromise,
    cinemasPromise
  ]);
});

async function loadPendingOfferBanner() {
  const banner = document.getElementById('offerta-banner');
  const nameEl = document.getElementById('offerta-banner-name');
  if (!banner || !nameEl || !pendingOffertaId) return;

  const offers = normalizeCollection(await API.getOfferte());
  pendingOfferta = offers.find((offer) => String(offer.id) === String(pendingOffertaId)) || null;

  if (pendingOfferta) {
    nameEl.textContent = pendingOfferta.nome || 'Offerta selezionata';
    banner.classList.remove('hidden');
  }
}

function setupTabs() {
  document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active-tab'));
      btn.classList.add('active-tab');
      currentTab = btn.dataset.tab;
      currentPage = 1;
      loadFilms();
    });
  });
}

function setupSearch() {
  const input = document.getElementById('search-input');
  let debounceTimer;
  input?.addEventListener('input', (e) => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
      currentSearch = e.target.value.trim();
      currentPage = 1;
      loadFilms();
    }, 300);
  });
}

function setupCategoriaFilter() {
  const select = document.getElementById('categoria-filter');
  select?.addEventListener('change', (e) => {
    currentCategoriaId = e.target.value;
    currentPage = 1;
    loadFilms();
  });
}

function setupCarouselControls() {
  const prevBtn = document.getElementById('carousel-prev-btn');
  const nextBtn = document.getElementById('carousel-next-btn');

  prevBtn?.addEventListener('click', () => {
    const track = document.getElementById('films-carousel-track');
    if (!track) return;
    const card = track.querySelector('.programmazione-carousel-card');
    const step = card ? card.getBoundingClientRect().width + 24 : 320;
    track.scrollBy({ left: -step, behavior: 'smooth' });
  });

  nextBtn?.addEventListener('click', () => {
    const track = document.getElementById('films-carousel-track');
    if (!track) return;
    const card = track.querySelector('.programmazione-carousel-card');
    const step = card ? card.getBoundingClientRect().width + 24 : 320;
    track.scrollBy({ left: step, behavior: 'smooth' });
  });

  window.addEventListener('resize', () => {
    const track = document.getElementById('films-carousel-track');
    if (track) {
      updateCarouselUI(track, currentFilms.length);
    }
  });
}

function setupLoadMore() {
  const btn = document.getElementById('load-more-btn');
  btn?.addEventListener('click', async () => {
    if (currentTab !== 'tutti') return;
    if (!currentPagedResult?.hasNextPage) return;

    currentPage += 1;
    await loadFilms({ append: true });
  });
}

async function loadCategorie() {
  try {
    allCategorie = normalizeCollection(await API.getCategorie());
  } catch (error) {
    console.error('Errore caricamento categorie:', error);
  }
}

function populateCategoriaFilter() {
  const select = document.getElementById('categoria-filter');
  if (!select || !allCategorie.length) return;

  allCategorie.forEach(cat => {
    const option = document.createElement('option');
    option.value = String(cat.id);
    option.textContent = cat.nome;
    select.appendChild(option);
  });
}

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

function renderCinemaHeader() {
  const nameEl = document.getElementById('selected-cinema-name');
  const detailEl = document.getElementById('selected-cinema-detail');
  const noCinemaState = document.getElementById('no-cinema-state');
  const filmsGrid = document.getElementById('films-grid');
  const emptyState = document.getElementById('empty-state');

  if (selectedCinemaId == null) {
    if (nameEl) nameEl.textContent = 'Nessun cinema selezionato';
    if (detailEl) detailEl.classList.add('hidden');
    if (noCinemaState) noCinemaState.classList.remove('hidden');
    if (filmsGrid) filmsGrid.classList.add('hidden');
    if (emptyState) emptyState.classList.add('hidden');
    updateNavbarCinemaDisplay(null);
    return;
  }

  if (noCinemaState) noCinemaState.classList.add('hidden');
  if (filmsGrid) filmsGrid.classList.remove('hidden');

  const cinema = allCinemas.find(c => Number(c.id) === Number(selectedCinemaId));
  if (cinema) {
    if (nameEl) nameEl.textContent = cinema.nome;
    if (detailEl) {
      detailEl.textContent = `${cinema.citta} - ${cinema.indirizzo}`;
      detailEl.classList.remove('hidden');
    }
    updateNavbarCinemaDisplay(cinema.nome);
  } else {
    if (nameEl) nameEl.textContent = cinemasLoaded ? 'Cinema selezionato' : 'Caricamento cinema...';
    if (detailEl) {
      if (cinemasLoaded) {
        detailEl.classList.add('hidden');
      } else {
        detailEl.textContent = 'Aggiornamento dettagli in corso';
        detailEl.classList.remove('hidden');
      }
    }
    updateNavbarCinemaDisplay(null);
  }
}

function updateNavbarCinemaDisplay(cinemaName) {
  if (typeof window.updateNavbarCinema === 'function') {
    window.updateNavbarCinema(cinemaName);
  }
}

async function loadFilms(options = {}) {
  const grid = document.getElementById('films-grid');
  const emptyState = document.getElementById('empty-state');
  const noCinemaState = document.getElementById('no-cinema-state');
  const filmsHeader = document.getElementById('films-header');
  const loadMore = document.getElementById('films-load-more');
  const carouselControls = document.getElementById('carousel-controls');
  const append = options.append === true;

  if (selectedCinemaId == null) {
    if (grid) grid.classList.add('hidden');
    if (emptyState) emptyState.classList.add('hidden');
    if (noCinemaState) noCinemaState.classList.remove('hidden');
    if (filmsHeader) filmsHeader.classList.add('hidden');
    if (loadMore) loadMore.classList.add('hidden');
    if (carouselControls) carouselControls.classList.add('hidden');
    return;
  }

  if (noCinemaState) noCinemaState.classList.add('hidden');
  if (grid) grid.classList.remove('hidden');
  if (filmsHeader) filmsHeader.classList.remove('hidden');

  if (grid && !append) {
    grid.innerHTML = `
      <div class="col-span-full text-center py-16 text-body">
        <i class="fa-solid fa-spinner fa-spin text-4xl mb-4"></i>
        <p>Caricamento film...</p>
      </div>
    `;
  }

  try {
    const params = {
      tab: currentTab,
      cinemaId: selectedCinemaId,
      page: currentPage,
      pageSize: currentTab === 'tutti' ? FILMS_PAGE_SIZE : CAROUSEL_PAGE_SIZE
    };
    if (currentSearch) params.search = currentSearch;
    if (currentCategoriaId) params.categoriaId = parseInt(currentCategoriaId, 10);

    const result = await API.getProgrammazioneFilms(params);
    currentPagedResult = result;
    const films = normalizeCollection(result?.items);

    if (append) {
      currentFilms = [...currentFilms, ...films];
    } else {
      currentFilms = films;
    }

    renderFilms(currentFilms);
  } catch (error) {
    handleApiError(error);
    if (grid) {
      grid.innerHTML = `
        <div class="col-span-full text-center py-16 text-ferrari-semantic-warning">
          <i class="fa-solid fa-circle-exclamation text-4xl mb-4"></i>
          <p>Errore nel caricamento dei film</p>
        </div>
      `;
    }
  }
}

function renderFilms(films) {
  const grid = document.getElementById('films-grid');
  const emptyState = document.getElementById('empty-state');
  const titleEl = document.getElementById('films-section-title');
  const carouselControls = document.getElementById('carousel-controls');
  const carouselCounter = document.getElementById('carousel-counter');
  const loadMore = document.getElementById('films-load-more');

  if (!grid) return;

  if (titleEl) {
    titleEl.textContent = currentTab === 'evidenza'
      ? 'Film in evidenza'
      : currentTab === 'uscita'
        ? 'In uscita'
        : 'Tutti i film';
  }

  if (!films.length) {
    grid.innerHTML = '';
    if (emptyState) emptyState.classList.remove('hidden');
    if (carouselControls) carouselControls.classList.add('hidden');
    if (loadMore) loadMore.classList.add('hidden');
    return;
  }

  if (emptyState) emptyState.classList.add('hidden');

  if (currentTab === 'tutti') {
    renderFilmsGridWithLoadMore(films, currentPagedResult);
    if (carouselControls) {
      carouselControls.classList.add('hidden');
      carouselControls.classList.remove('flex');
    }
    return;
  }

  renderFilmsCarousel(films);
  if (loadMore) loadMore.classList.add('hidden');
  if (carouselControls) {
    carouselControls.classList.remove('hidden');
    carouselControls.classList.add('flex');
  }
  if (carouselCounter) {
    const visibleCount = Math.min(films.length, getCarouselVisibleEstimate());
    carouselCounter.textContent = `${visibleCount} / ${films.length}`;
  }
}

function renderFilmsGridWithLoadMore(films, pagedResult) {
  const grid = document.getElementById('films-grid');
  const loadMore = document.getElementById('films-load-more');

  grid.className = 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6';
  grid.innerHTML = films.map(renderFilmCard).join('');

  if (loadMore) {
    if (pagedResult?.hasNextPage) {
      loadMore.classList.remove('hidden');
    } else {
      loadMore.classList.add('hidden');
    }
  }
}

function renderFilmsCarousel(films) {
  const grid = document.getElementById('films-grid');

  grid.className = 'programmazione-carousel-shell';
  grid.innerHTML = `
    <div id="films-carousel-track" class="programmazione-carousel-track">
      ${films.map(film => `
        <div class="programmazione-carousel-card">
          ${renderFilmCard(film)}
        </div>
      `).join('')}
    </div>
  `;

  const track = document.getElementById('films-carousel-track');
  if (track) {
    const sync = () => updateCarouselUI(track, films.length);
    track.addEventListener('scroll', sync, { passive: true });
    track.addEventListener('scroll', handleCarouselInfiniteLoad, { passive: true });
    requestAnimationFrame(sync);
  }
}

function renderFilmCard(film) {
  const categorie = film.categorie || [];
  const categorieBadges = categorie.slice(0, 3).map(c =>
    `<span class="inline-block bg-canvas-elevated text-ink text-xs px-2 py-0.5 rounded-full">${c.nome}</span>`
  ).join('');

  let availabilityBadge;
  if (film.disponibileNelCinemaSelezionato) {
    const prossimoShow = film.prossimoShowNelCinemaSelezionato;
    availabilityBadge = `
      <div class="flex items-center gap-1 text-emerald-600 dark:text-emerald-400 text-xs font-medium">
        <i class="fa-solid fa-circle-check"></i>
        <span>Disponibile${prossimoShow ? ` - Prossimo: ${formatDateTimeLocal(prossimoShow)}` : ''}</span>
      </div>
    `;
  } else if (film.inUscita) {
    availabilityBadge = `
      <div class="flex items-center gap-1 text-amber-600 dark:text-amber-400 text-xs font-medium">
        <i class="fa-solid fa-clock"></i>
        <span>In uscita${film.dataRilascio ? ` - ${formatDateOnly(film.dataRilascio)}` : ''}</span>
      </div>
    `;
  } else {
    availabilityBadge = `
      <div class="flex items-center gap-1 text-body text-xs">
        <i class="fa-solid fa-circle-xmark"></i>
        <span>Non disponibile in questo cinema</span>
      </div>
    `;
  }

  return `
    <div class="card-ferrari overflow-hidden card-hover cursor-pointer group h-full" onclick="goToSchedaFilm(${film.id})">
      <div class="aspect-[2/3] bg-slate-700 relative overflow-hidden">
        <img src="${getCoverImage(film.copertinaPath)}"
              alt="${film.titolo}"
              class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
              loading="lazy"
              decoding="async"
              fetchpriority="low"
              referrerpolicy="no-referrer">
        <div class="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent"></div>
        <div class="absolute top-3 left-3 right-3 flex flex-wrap gap-1">
          ${categorieBadges}
        </div>
        <div class="absolute bottom-3 left-3 right-3">
          <span class="bg-ferrari-primary text-black text-xs font-bold px-2 py-1 rounded-full">${film.durata || '-'} min</span>
        </div>
      </div>
      <div class="p-4">
        <h3 class="text-ink font-semibold text-lg mb-2 line-clamp-2">${film.titolo}</h3>
        ${availabilityBadge}
      </div>
    </div>
  `;
}

function buildAcquistaUrl(showId) {
  const url = `/acquista.html?showId=${showId}`;
  if (!pendingOffertaId) return url;
  return `${url}&offertaId=${encodeURIComponent(pendingOffertaId)}`;
}

function getCarouselVisibleEstimate(track) {
  const carouselTrack = track || document.getElementById('films-carousel-track');
  if (!carouselTrack) return 1;
  const card = carouselTrack.querySelector('.programmazione-carousel-card');
  if (!card) return 1;
  const cardWidth = card.getBoundingClientRect().width;
  if (!cardWidth) return 1;
  const style = window.getComputedStyle(carouselTrack);
  const gap = parseFloat(style.columnGap || style.gap || '0') || 0;
  return Math.max(1, Math.floor((carouselTrack.clientWidth + gap) / (cardWidth + gap)));
}

function getCarouselCurrentIndex(track) {
  const card = track.querySelector('.programmazione-carousel-card');
  if (!card) return 0;
  const style = window.getComputedStyle(track);
  const gap = parseFloat(style.columnGap || style.gap || '0') || 0;
  const step = card.getBoundingClientRect().width + gap;
  if (!step) return 0;
  return Math.round(track.scrollLeft / step);
}

function updateCarouselUI(track, totalFilms) {
  const carouselCounter = document.getElementById('carousel-counter');
  const prevBtn = document.getElementById('carousel-prev-btn');
  const nextBtn = document.getElementById('carousel-next-btn');
  const carouselControls = document.getElementById('carousel-controls');

  if (!track || !carouselCounter || !prevBtn || !nextBtn || !carouselControls) return;

  const visibleCount = Math.min(totalFilms, getCarouselVisibleEstimate(track));
  const currentIndex = Math.min(getCarouselCurrentIndex(track), Math.max(0, totalFilms - visibleCount));
  const shownFrom = totalFilms === 0 ? 0 : currentIndex + 1;
  const shownUntil = Math.min(totalFilms, currentIndex + visibleCount);

  carouselCounter.textContent = `${shownFrom}-${shownUntil} / ${totalFilms}`;

  const canScroll = totalFilms > visibleCount;
  if (!canScroll) {
    carouselControls.classList.add('hidden');
    carouselControls.classList.remove('flex');
    return;
  }

  carouselControls.classList.remove('hidden');
  carouselControls.classList.add('flex');

  prevBtn.disabled = track.scrollLeft <= 4;
  nextBtn.disabled = track.scrollLeft + track.clientWidth >= track.scrollWidth - 4;
}

async function handleCarouselInfiniteLoad() {
  if (currentTab === 'tutti') return;
  if (isLoadingMoreCarousel) return;
  if (!currentPagedResult?.hasNextPage) return;

  const track = document.getElementById('films-carousel-track');
  if (!track) return;

  const remaining = track.scrollWidth - (track.scrollLeft + track.clientWidth);
  if (remaining > 240) return;

  isLoadingMoreCarousel = true;
  try {
    currentPage += 1;
    await loadFilms({ append: true });
  } finally {
    isLoadingMoreCarousel = false;
  }
}

function goToSchedaFilm(filmId) {
  const url = `/scheda-film.html?id=${filmId}${selectedCinemaId ? `&cinema=${selectedCinemaId}` : ''}`;
  window.location.href = url;
}


// Cinema Modal
function setupCinemaModal() {
  const modal = document.getElementById('cinema-modal');
  const closeBtn = document.getElementById('cinema-modal-close');
  const changeBtn = document.getElementById('change-cinema-btn');
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

  changeBtn?.addEventListener('click', openModal);
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
}

async function requestUserLocationInBackground() {
  try {
    userLocation = await getUserLocation();
    await loadCinemas();
    cinemasLoaded = true;
    renderCinemaHeader();

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

  renderCinemaHeader();
  loadFilms();
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

function formatDateTimeLocal(dateTimeStr) {
  if (!dateTimeStr) return '';
  const d = new Date(dateTimeStr);
  const day = String(d.getDate()).padStart(2, '0');
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const hours = String(d.getHours()).padStart(2, '0');
  const minutes = String(d.getMinutes()).padStart(2, '0');
  return `${day}/${month} ${hours}:${minutes}`;
}

function formatDateOnly(dateStr) {
  if (!dateStr) return '';
  if (typeof dateStr === 'string' && dateStr.includes('-')) {
    const parts = dateStr.split('-');
    return `${parts[2]}/${parts[1]}/${parts[0]}`;
  }
  return dateStr;
}

// Expose functions to window
window.goToSchedaFilm = goToSchedaFilm;
window.selectCinema = selectCinema;

// Update navbar when cinema changes
window.addEventListener('cinema:changed', (e) => {
  selectedCinemaId = e.detail?.cinemaId ?? selectedCinemaId;
  renderCinemaHeader();
});
