// Proiezioni Page JavaScript
let allCinemas = [];
// Variabile allFilms: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let allFilms = [];
// Variabile currentPage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentPage = 1;
// Variabile pageSize: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const pageSize = 10;
// Variabile totalPages: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let totalPages = 1;
// Variabile totalProiezioniCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let totalProiezioniCount = 0;
// Variabile currentSearch: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentSearch = '';

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  bindSearch();
  await Promise.all([loadCinemasList(), loadFilmsList()]);
  populateSelects();
  await loadProiezioni();
  setupFormSubmit();
});

// Funzione normalizePaged: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizePaged(data) {
  if (Array.isArray(data) || Array.isArray(data?.$values)) {
    // Variabile items: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const items = normalizeCollection(data);
    return {
      items,
      page: 1,
      pageSize: items.length || pageSize,
      totalCount: items.length,
      totalPages: 1
    };
  }

  // Variabile items: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const items = normalizeCollection(data?.items ?? data?.Items ?? []);
  // Variabile resolvedPage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const resolvedPage = Number(data?.page ?? data?.Page ?? 1);
  // Variabile resolvedPageSize: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const resolvedPageSize = Number(data?.pageSize ?? data?.PageSize ?? pageSize);
  // Variabile resolvedTotalCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const resolvedTotalCount = Number(data?.totalCount ?? data?.TotalCount ?? items.length);
  // Variabile resolvedTotalPages: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const resolvedTotalPages = Number(data?.totalPages ?? data?.TotalPages ?? 1);

  return {
    items,
    page: Number.isFinite(resolvedPage) && resolvedPage > 0 ? resolvedPage : 1,
    pageSize: Number.isFinite(resolvedPageSize) && resolvedPageSize > 0 ? resolvedPageSize : pageSize,
    totalCount: Number.isFinite(resolvedTotalCount) && resolvedTotalCount >= 0 ? resolvedTotalCount : items.length,
    totalPages: Number.isFinite(resolvedTotalPages) && resolvedTotalPages > 0 ? resolvedTotalPages : 1
  };
}

// Funzione loadProiezioni: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadProiezioni() {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('proiezioni-table-body');
  if (!tableBody) return;
  
  try {
    // Variabile response: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const response = await API.getProiezioni({
      page: currentPage,
      pageSize,
      search: currentSearch || undefined
    });

    // Variabile paged: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const paged = normalizePaged(response);
    totalPages = paged.totalPages;
    totalProiezioniCount = paged.totalCount;
    currentPage = paged.page;

    renderProiezioni(paged.items);
    updateStats(totalProiezioniCount);
    renderPagination(paged.items.length);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-ferrari-semantic-warning">Errore nel caricamento delle proiezioni</td></tr>';
    renderPagination(0);
  }
}

// Funzione loadCinemasList: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCinemasList() {
  try {
    allCinemas = normalizeCollection(await API.getCinemas());
  } catch (error) {
    console.error('Error loading cinemas:', error);
  }
}

// Funzione loadFilmsList: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadFilmsList() {
  try {
    allFilms = normalizeCollection(await API.getFilms());
  } catch (error) {
    console.error('Error loading films:', error);
  }
}

// Funzione populateSelects: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function populateSelects() {
  populateSelect('cinema-select', allCinemas, 'id', ['nome'], 'Seleziona Cinema');
  populateSelect('film-select', allFilms, 'id', ['titolo'], 'Seleziona Film');
}

// Funzione renderProiezioni: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderProiezioni(proiezioni) {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('proiezioni-table-body');
  if (!tableBody) return;
  
  if (!proiezioni.length) {
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-body">Nessuna proiezione trovata</td></tr>';
    return;
  }
  
  tableBody.innerHTML = proiezioni.map(proiezione => `
    <tr class="row-hover">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${proiezione.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-ink">${getCinemaLabel(proiezione)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-ink">${getFilmLabel(proiezione)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${formatDate(proiezione.data)}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${formatTime(proiezione.ora)}</td>
      <td class="px-6 py-4 whitespace-nowrap">
        ${renderProiezioneStatus(proiezione)}
      </td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editProiezione(${proiezione.id})" class="text-ferrari-primary hover:text-ferrari-primary-hover mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteProiezione(${proiezione.id}, '${getFilmTitle(proiezione.filmId)}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
}

// Funzione getStatusMeta: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getStatusMeta(proiezione) {
  // Variabile now: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const now = new Date();
  // Variabile today: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

  // Variabile proiezioneDate: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  let proiezioneDate = null;
  if (proiezione.data) {
    // Variabile datePart: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const datePart = String(proiezione.data).split('T')[0];
    const [year, month, day] = datePart.split('-').map(Number);
    if (year && month && day) {
      proiezioneDate = new Date(year, month - 1, day);
    }
  }

  // Variabile isPast: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const isPast = proiezioneDate && proiezioneDate < today;
  return {
    className: isPast ? 'chip-past' : 'chip-active',
    text: isPast ? 'Passata' : 'In programma'
  };
}

// Funzione renderProiezioneStatus: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderProiezioneStatus(proiezione) {
  // Variabile status: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const status = getStatusMeta(proiezione);
  return `<span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${status.className}">${status.text}</span>`;
}

// Funzione getCinemaLabel: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getCinemaLabel(proiezione) {
  // Variabile cinemaName: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cinemaName = proiezione?.cinema?.nome || getCinemaName(proiezione.cinemaId);
  if (cinemaName.startsWith('ID ')) {
    return `<span class="text-body text-[11px] font-normal opacity-70">${cinemaName}</span>`;
  }
  return `${cinemaName} <span class="ml-1 text-[11px] font-normal text-body opacity-70">(ID ${proiezione.cinemaId})</span>`;
}

// Funzione getFilmLabel: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getFilmLabel(proiezione) {
  // Variabile filmTitle: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const filmTitle = proiezione?.film?.titolo || getFilmTitle(proiezione.filmId);
  if (filmTitle.startsWith('ID ')) {
    return `<span class="text-body text-[11px] font-normal opacity-70">${filmTitle}</span>`;
  }
  return `${filmTitle} <span class="ml-1 text-[11px] font-normal text-body opacity-70">(ID ${proiezione.filmId})</span>`;
}

// Funzione getCinemaName: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getCinemaName(cinemaId) {
  // Variabile cinema: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const cinema = allCinemas.find(c => Number(c.id) === Number(cinemaId));
  return cinema ? cinema.nome : `ID ${cinemaId}`;
}

// Funzione getFilmTitle: recupera un valore derivato e lo restituisce al chiamante. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function getFilmTitle(filmId) {
  // Variabile film: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const film = allFilms.find(f => Number(f.id) === Number(filmId));
  return film ? film.titolo : `ID ${filmId}`;
}

// Funzione updateStats: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateStats(totalCount) {
  // Variabile totalProiezioniEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totalProiezioniEl = document.getElementById('total-proiezioni');
  if (totalProiezioniEl) totalProiezioniEl.textContent = String(totalCount);
}

// Funzione bindSearch: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function bindSearch() {
  // Variabile searchInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const searchInput = document.getElementById('search-input');
  if (!searchInput) return;

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  searchInput.addEventListener('input', async (e) => {
    currentSearch = (e.target.value || '').trim();
    currentPage = 1;
    await loadProiezioni();
  });
}

// Funzione renderPagination: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderPagination(serverItemsCount) {
  // Variabile paginationInfo: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const paginationInfo = document.getElementById('pagination-info');
  // Variabile pageIndicator: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const pageIndicator = document.getElementById('page-indicator');
  // Variabile firstBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const firstBtn = document.getElementById('pagination-first');
  // Variabile prevBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const prevBtn = document.getElementById('pagination-prev');
  // Variabile nextBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nextBtn = document.getElementById('pagination-next');
  // Variabile lastBtn: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const lastBtn = document.getElementById('pagination-last');

  if (!paginationInfo || !pageIndicator || !firstBtn || !prevBtn || !nextBtn || !lastBtn) return;

  if (totalProiezioniCount === 0 || serverItemsCount === 0) {
    paginationInfo.textContent = 'Nessun risultato';
    pageIndicator.textContent = 'Pagina 1 di 1';
    firstBtn.disabled = true;
    prevBtn.disabled = true;
    nextBtn.disabled = true;
    lastBtn.disabled = true;
    return;
  }

  // Variabile startItem: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const startItem = ((currentPage - 1) * pageSize) + 1;
  // Variabile endItem: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const endItem = Math.min(currentPage * pageSize, totalProiezioniCount);

  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalProiezioniCount} proiezioni`;
  pageIndicator.textContent = `Pagina ${currentPage} di ${totalPages}`;

  firstBtn.disabled = currentPage <= 1;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  lastBtn.disabled = currentPage >= totalPages;
}

// Funzione goToPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function goToPage(page) {
  if (page < 1 || page > totalPages || page === currentPage) return;
  currentPage = page;
  await loadProiezioni();
}

// Funzione goToFirstPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToFirstPage() {
  goToPage(1);
}

// Funzione goToPrevPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToPrevPage() {
  goToPage(currentPage - 1);
}

// Funzione goToNextPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToNextPage() {
  goToPage(currentPage + 1);
}

// Funzione goToLastPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToLastPage() {
  goToPage(totalPages);
}

// Funzione setupFormSubmit: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupFormSubmit() {
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById('proiezione-form');
  if (!form) return;
  
  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = serializeForm('proiezione-form');
    delete data.postiTotali;
    if (data.cinemaId) data.cinemaId = Number(data.cinemaId);
    if (data.filmId) data.filmId = Number(data.filmId);

    if (data.data && data.ora) {
      data.ora = `${data.data}T${data.ora}:00`;
    }

    // Variabile editId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const editId = form.dataset.editId;
    
    try {
      if (editId) {
        await API.updateProiezione(editId, data);
        showToast('Proiezione aggiornata con successo');
      } else {
        await API.createProiezione(data);
        showToast('Proiezione creata con successo');
      }
      
      closeModal('proiezione-modal');
      loadProiezioni();
    } catch (error) {
      handleApiError(error);
    }
  });
}

// Funzione editProiezione: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function editProiezione(id) {
  try {
    // Variabile proiezione: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const proiezione = await API.getProiezione(id);
    
    if (!proiezione) {
      showToast('Proiezione non trovata', 'danger');
      return;
    }
    
    openModal('proiezione-modal', 'Modifica Proiezione');

    // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const form = document.getElementById('proiezione-form');
    if (!form) return;
    
    form.dataset.editId = id;

    // Variabile cinemaIdInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const cinemaIdInput = form.querySelector('[name="cinemaId"]');
    // Variabile filmIdInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const filmIdInput = form.querySelector('[name="filmId"]');
    // Variabile dataInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const dataInput = form.querySelector('[name="data"]');
    // Variabile oraInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const oraInput = form.querySelector('[name="ora"]');

    if (cinemaIdInput) cinemaIdInput.value = proiezione.cinemaId || '';
    if (filmIdInput) filmIdInput.value = proiezione.filmId || '';
    if (dataInput) dataInput.value = formatDateForInput(proiezione.data);
    if (oraInput) oraInput.value = formatTime(proiezione.ora);
  } catch (error) {
    console.error('Error in editProiezione:', error);
    handleApiError(error);
  }
}

// Funzione deleteProiezione: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function deleteProiezione(id, title) {
  openDeleteModal(title, async () => {
    try {
      await API.deleteProiezione(id);
      showToast('Proiezione eliminata con successo');
      loadProiezioni();
    } catch (error) {
      handleApiError(error);
    }
  });
}

window.goToPage = goToPage;
window.goToFirstPage = goToFirstPage;
window.goToPrevPage = goToPrevPage;
window.goToNextPage = goToNextPage;
window.goToLastPage = goToLastPage;
