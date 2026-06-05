// Programmazione Page JavaScript - Film-centric v2

const CinemaManager = {
  STORAGE_KEY: 'cb_selected_cinema',

  getLocalCinemaId() {
    // Variabile val: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
    // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const auth = getAuthSafe();
    if (!auth || !auth.isLoggedIn()) {
      return this.getLocalCinemaId();
    }

    try {
      // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const result = await API.getCinemaPreferito();
      // Variabile backendCinemaId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const backendCinemaId = result?.cinemaId ? parseInt(result.cinemaId, 10) : null;
      // Variabile localCinemaId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

    // Variabile auth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const auth = getAuthSafe();
    if (auth && auth.isLoggedIn()) {
      try {
        await API.setCinemaPreferito(cinemaId);
      } catch {
        // ignore errors, localStorage is still updated
      }
    }

    // Variabile cinema: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const cinema = allCinemas.find(c => Number(c.id) === Number(cinemaId));
    window.dispatchEvent(new CustomEvent('cinema:changed', {
      detail: { cinemaId, cinemaName: cinema?.nome || null }
    }));
  }
};

// Funzione getAuthSafe: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
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

// State
let currentTab = 'evidenza';
// Variabile currentSearch: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentSearch = '';
// Variabile currentCategoriaId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentCategoriaId = '';
// Variabile selectedCinemaId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let selectedCinemaId = null;
// Variabile allCategorie: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let allCategorie = [];
// Variabile allCinemas: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let allCinemas = [];
// Variabile userLocation: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let userLocation = null;
// Variabile cinemasLoaded: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let cinemasLoaded = false;
// Variabile modalSearchTerm: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let modalSearchTerm = '';
// Variabile currentFilms: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentFilms = [];
// Variabile pendingOffertaId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let pendingOffertaId = null;
// Variabile pendingOfferta: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let pendingOfferta = null;
// Variabile FILMS_PAGE_SIZE: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const FILMS_PAGE_SIZE = 20;
// Variabile CAROUSEL_PAGE_SIZE: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const CAROUSEL_PAGE_SIZE = 8;
// Variabile currentPage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentPage = 1;
// Variabile currentPagedResult: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentPagedResult = null;
// Variabile isLoadingMoreCarousel: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let isLoadingMoreCarousel = false;

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
  // Variabile categoriePromise: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const categoriePromise = loadCategorie().then(populateCategoriaFilter);
  // Variabile cinemasPromise: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione loadPendingOfferBanner: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadPendingOfferBanner() {
  // Variabile banner: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const banner = document.getElementById('offerta-banner');
  // Variabile nameEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nameEl = document.getElementById('offerta-banner-name');
  if (!banner || !nameEl || !pendingOffertaId) return;

  // Variabile offers: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const offers = normalizeCollection(await API.getOfferte());
  pendingOfferta = offers.find((offer) => String(offer.id) === String(pendingOffertaId)) || null;

  if (pendingOfferta) {
    nameEl.textContent = pendingOfferta.nome || 'Offerta selezionata';
    banner.classList.remove('hidden');
  }
}

// Funzione setupTabs: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupTabs() {
  document.querySelectorAll('.tab-btn').forEach(btn => {
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    btn.addEventListener('click', () => {
      document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active-tab'));
      btn.classList.add('active-tab');
      currentTab = btn.dataset.tab;
      currentPage = 1;
      loadFilms();
    });
  });
}

// Funzione setupSearch: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupSearch() {
  // Variabile input: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const input = document.getElementById('search-input');
  // Variabile debounceTimer: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let debounceTimer;
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  input?.addEventListener('input', (e) => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
      currentSearch = e.target.value.trim();
      currentPage = 1;
      loadFilms();
    }, 300);
  });
}

// Funzione setupCategoriaFilter: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupCategoriaFilter() {
  // Variabile select: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const select = document.getElementById('categoria-filter');
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  select?.addEventListener('change', (e) => {
    currentCategoriaId = e.target.value;
    currentPage = 1;
    loadFilms();
  });
}

// Funzione setupCarouselControls: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupCarouselControls() {
  // Variabile prevBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const prevBtn = document.getElementById('carousel-prev-btn');
  // Variabile nextBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nextBtn = document.getElementById('carousel-next-btn');

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  prevBtn?.addEventListener('click', () => {
    // Variabile track: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const track = document.getElementById('films-carousel-track');
    if (!track) return;
    // Variabile card: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const card = track.querySelector('.programmazione-carousel-card');
    // Variabile step: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const step = card ? card.getBoundingClientRect().width + 24 : 320;
    track.scrollBy({ left: -step, behavior: 'smooth' });
  });

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  nextBtn?.addEventListener('click', () => {
    // Variabile track: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const track = document.getElementById('films-carousel-track');
    if (!track) return;
    // Variabile card: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const card = track.querySelector('.programmazione-carousel-card');
    // Variabile step: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const step = card ? card.getBoundingClientRect().width + 24 : 320;
    track.scrollBy({ left: step, behavior: 'smooth' });
  });

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  window.addEventListener('resize', () => {
    // Variabile track: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const track = document.getElementById('films-carousel-track');
    if (track) {
      updateCarouselUI(track, currentFilms.length);
    }
  });
}

// Funzione setupLoadMore: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupLoadMore() {
  // Variabile btn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const btn = document.getElementById('load-more-btn');
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  btn?.addEventListener('click', async () => {
    if (currentTab !== 'tutti') return;
    if (!currentPagedResult?.hasNextPage) return;

    currentPage += 1;
    await loadFilms({ append: true });
  });
}

// Funzione loadCategorie: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCategorie() {
  try {
    allCategorie = normalizeCollection(await API.getCategorie());
  } catch (error) {
    console.error('Errore caricamento categorie:', error);
  }
}

// Funzione populateCategoriaFilter: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function populateCategoriaFilter() {
  // Variabile select: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const select = document.getElementById('categoria-filter');
  if (!select || !allCategorie.length) return;

  allCategorie.forEach(cat => {
    // Variabile option: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const option = document.createElement('option');
    option.value = String(cat.id);
    option.textContent = cat.nome;
    select.appendChild(option);
  });
}

// Funzione loadCinemas: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCinemas() {
  try {
    // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione renderCinemaHeader: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderCinemaHeader() {
  // Variabile nameEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nameEl = document.getElementById('selected-cinema-name');
  // Variabile detailEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const detailEl = document.getElementById('selected-cinema-detail');
  // Variabile noCinemaState: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const noCinemaState = document.getElementById('no-cinema-state');
  // Variabile filmsGrid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const filmsGrid = document.getElementById('films-grid');
  // Variabile emptyState: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

  // Variabile cinema: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione updateNavbarCinemaDisplay: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateNavbarCinemaDisplay(cinemaName) {
  if (typeof window.updateNavbarCinema === 'function') {
    window.updateNavbarCinema(cinemaName);
  }
}

// Funzione loadFilms: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadFilms(options = {}) {
  // Variabile grid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const grid = document.getElementById('films-grid');
  // Variabile emptyState: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const emptyState = document.getElementById('empty-state');
  // Variabile noCinemaState: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const noCinemaState = document.getElementById('no-cinema-state');
  // Variabile filmsHeader: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const filmsHeader = document.getElementById('films-header');
  // Variabile loadMore: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const loadMore = document.getElementById('films-load-more');
  // Variabile carouselControls: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const carouselControls = document.getElementById('carousel-controls');
  // Variabile append: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
    // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const params = {
      tab: currentTab,
      cinemaId: selectedCinemaId,
      page: currentPage,
      pageSize: currentTab === 'tutti' ? FILMS_PAGE_SIZE : CAROUSEL_PAGE_SIZE
    };
    if (currentSearch) params.search = currentSearch;
    if (currentCategoriaId) params.categoriaId = parseInt(currentCategoriaId, 10);

    // Variabile result: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const result = await API.getProgrammazioneFilms(params);
    currentPagedResult = result;
    // Variabile films: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione renderFilms: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderFilms(films) {
  // Variabile grid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const grid = document.getElementById('films-grid');
  // Variabile emptyState: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const emptyState = document.getElementById('empty-state');
  // Variabile titleEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const titleEl = document.getElementById('films-section-title');
  // Variabile carouselControls: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const carouselControls = document.getElementById('carousel-controls');
  // Variabile carouselCounter: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const carouselCounter = document.getElementById('carousel-counter');
  // Variabile loadMore: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
    // Variabile visibleCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const visibleCount = Math.min(films.length, getCarouselVisibleEstimate());
    carouselCounter.textContent = `${visibleCount} / ${films.length}`;
  }
}

// Funzione renderFilmsGridWithLoadMore: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderFilmsGridWithLoadMore(films, pagedResult) {
  // Variabile grid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const grid = document.getElementById('films-grid');
  // Variabile loadMore: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione renderFilmsCarousel: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderFilmsCarousel(films) {
  // Variabile grid: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

  // Variabile track: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const track = document.getElementById('films-carousel-track');
  if (track) {
    // Variabile/funzione sync: supporto non ovvio per stato, callback o logica della pagina.
    const sync = () => updateCarouselUI(track, films.length);
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    track.addEventListener('scroll', sync, { passive: true });
    // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
    track.addEventListener('scroll', handleCarouselInfiniteLoad, { passive: true });
    requestAnimationFrame(sync);
  }
}

// Funzione renderFilmCard: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderFilmCard(film) {
  // Variabile categorie: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const categorie = film.categorie || [];
  // Variabile categorieBadges: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const categorieBadges = categorie.slice(0, 3).map(c =>
    `<span class="inline-block bg-canvas-elevated text-ink text-xs px-2 py-0.5 rounded-full">${c.nome}</span>`
  ).join('');

  // Variabile availabilityBadge: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let availabilityBadge;
  if (film.disponibileNelCinemaSelezionato) {
    // Variabile prossimoShow: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione buildAcquistaUrl: costruisce una struttura dati o una selezione ordinata per la UI. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function buildAcquistaUrl(showId) {
  // Variabile params: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const params = new URLSearchParams(window.location.search);
  // Variabile offertaId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const offertaId = params.get('offertaId') || sessionStorage.getItem('pending_offerta_id');
  if (offertaId) {
    return '/pagamento.html?offertaId=' + encodeURIComponent(offertaId) + '&showId=' + encodeURIComponent(showId);
  }
  return '/acquista.html?showId=' + showId;
}

// Funzione getCarouselVisibleEstimate: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getCarouselVisibleEstimate(track) {
  // Variabile carouselTrack: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const carouselTrack = track || document.getElementById('films-carousel-track');
  if (!carouselTrack) return 1;
  // Variabile card: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const card = carouselTrack.querySelector('.programmazione-carousel-card');
  if (!card) return 1;
  // Variabile cardWidth: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cardWidth = card.getBoundingClientRect().width;
  if (!cardWidth) return 1;
  // Variabile style: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const style = window.getComputedStyle(carouselTrack);
  // Variabile gap: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const gap = parseFloat(style.columnGap || style.gap || '0') || 0;
  return Math.max(1, Math.floor((carouselTrack.clientWidth + gap) / (cardWidth + gap)));
}

// Funzione getCarouselCurrentIndex: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getCarouselCurrentIndex(track) {
  // Variabile card: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const card = track.querySelector('.programmazione-carousel-card');
  if (!card) return 0;
  // Variabile style: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const style = window.getComputedStyle(track);
  // Variabile gap: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const gap = parseFloat(style.columnGap || style.gap || '0') || 0;
  // Variabile step: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const step = card.getBoundingClientRect().width + gap;
  if (!step) return 0;
  return Math.round(track.scrollLeft / step);
}

// Funzione updateCarouselUI: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateCarouselUI(track, totalFilms) {
  // Variabile carouselCounter: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const carouselCounter = document.getElementById('carousel-counter');
  // Variabile prevBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const prevBtn = document.getElementById('carousel-prev-btn');
  // Variabile nextBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nextBtn = document.getElementById('carousel-next-btn');
  // Variabile carouselControls: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const carouselControls = document.getElementById('carousel-controls');

  if (!track || !carouselCounter || !prevBtn || !nextBtn || !carouselControls) return;

  // Variabile visibleCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const visibleCount = Math.min(totalFilms, getCarouselVisibleEstimate(track));
  // Variabile currentIndex: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const currentIndex = Math.min(getCarouselCurrentIndex(track), Math.max(0, totalFilms - visibleCount));
  // Variabile shownFrom: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const shownFrom = totalFilms === 0 ? 0 : currentIndex + 1;
  // Variabile shownUntil: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const shownUntil = Math.min(totalFilms, currentIndex + visibleCount);

  carouselCounter.textContent = `${shownFrom}-${shownUntil} / ${totalFilms}`;

  // Variabile canScroll: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione handleCarouselInfiniteLoad: gestisce un evento o una risposta utente. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function handleCarouselInfiniteLoad() {
  if (currentTab === 'tutti') return;
  if (isLoadingMoreCarousel) return;
  if (!currentPagedResult?.hasNextPage) return;

  // Variabile track: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const track = document.getElementById('films-carousel-track');
  if (!track) return;

  // Variabile remaining: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione goToSchedaFilm: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToSchedaFilm(filmId) {
  // Variabile url: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const url = `/scheda-film.html?id=${filmId}${selectedCinemaId ? `&cinema=${selectedCinemaId}` : ''}`;
  window.location.href = url;
}


// Cinema Modal
// Funzione setupCinemaModal: descrive l'azione eseguita, i parametri in ingresso e il valore restituito.
function setupCinemaModal() {
  // Variabile modal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const modal = document.getElementById('cinema-modal');
  // Variabile closeBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const closeBtn = document.getElementById('cinema-modal-close');
  // Variabile changeBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const changeBtn = document.getElementById('change-cinema-btn');
  // Variabile ctaBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const ctaBtn = document.getElementById('select-cinema-cta');
  // Variabile searchInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const searchInput = document.getElementById('cinema-search-input');

  // Variabile/funzione openModal: supporto non ovvio per stato, callback o logica della pagina.
  const openModal = () => {
    if (modal) {
      modal.classList.remove('hidden');
      document.body.style.overflow = 'hidden';
      if (!cinemasLoaded) {
        // Variabile list: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

  // Variabile/funzione closeModal: supporto non ovvio per stato, callback o logica della pagina.
  const closeModal = () => {
    if (modal) {
      modal.classList.add('hidden');
      document.body.style.overflow = '';
    }
  };

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  changeBtn?.addEventListener('click', openModal);
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  ctaBtn?.addEventListener('click', openModal);
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  closeBtn?.addEventListener('click', closeModal);

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  modal?.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
  });

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && modal && !modal.classList.contains('hidden')) {
      closeModal();
    }
  });

  // Variabile debounceTimer: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let debounceTimer;
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
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

// Funzione requestUserLocationInBackground: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function requestUserLocationInBackground() {
  try {
    userLocation = await getUserLocation();
    await loadCinemas();
    cinemasLoaded = true;
    renderCinemaHeader();

    // Variabile modal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const modal = document.getElementById('cinema-modal');
    if (modal && !modal.classList.contains('hidden')) {
      renderCinemaList(modalSearchTerm);
    }
  } catch {
    // geolocation not available or denied
  }
}

// Funzione renderCinemaList: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderCinemaList(search = '') {
  // Variabile list: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const list = document.getElementById('cinema-list');
  if (!list) return;

  // Variabile cinemas: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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
    // Variabile isSelected: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const isSelected = Number(cinema.id) === Number(selectedCinemaId);
    // Variabile distance: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const distance = cinema.distanzaKm != null ? `${cinema.distanzaKm.toFixed(1)} km` : '';
    // Variabile tipologie: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
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

// Funzione selectCinema: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function selectCinema(cinemaId) {
  CinemaManager.setCinema(cinemaId);
  selectedCinemaId = cinemaId;

  // Variabile modal: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const modal = document.getElementById('cinema-modal');
  if (modal) {
    modal.classList.add('hidden');
    document.body.style.overflow = '';
  }

  renderCinemaHeader();
  loadFilms();
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

// Funzione formatDateTimeLocal: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function formatDateTimeLocal(dateTimeStr) {
  if (!dateTimeStr) return '';
  const d = new Date(dateTimeStr);
  // Variabile day: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const day = String(d.getDate()).padStart(2, '0');
  // Variabile month: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const month = String(d.getMonth() + 1).padStart(2, '0');
  // Variabile hours: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const hours = String(d.getHours()).padStart(2, '0');
  // Variabile minutes: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const minutes = String(d.getMinutes()).padStart(2, '0');
  return `${day}/${month} ${hours}:${minutes}`;
}

// Funzione formatDateOnly: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function formatDateOnly(dateStr) {
  if (!dateStr) return '';
  if (typeof dateStr === 'string' && dateStr.includes('-')) {
    // Variabile parts: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const parts = dateStr.split('-');
    return `${parts[2]}/${parts[1]}/${parts[0]}`;
  }
  return dateStr;
}

// Expose functions to window
window.goToSchedaFilm = goToSchedaFilm;
window.selectCinema = selectCinema;

// Update navbar when cinema changes
// Listener evento: si attiva quando scatta l'evento e aggiorna UI o stato.
window.addEventListener('cinema:changed', (e) => {
  selectedCinemaId = e.detail?.cinemaId ?? selectedCinemaId;
  renderCinemaHeader();
});
