// Cinemas Page JavaScript
let currentPage = 1;
// Variabile pageSize: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const pageSize = 10;
// Variabile totalPages: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let totalPages = 1;
// Variabile totalCinemasCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let totalCinemasCount = 0;
// Variabile currentSearch: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentSearch = '';

// Funzione normalizeCollection: normalizza il valore in ingresso per confronti stabili. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

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

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  bindSearch();
  setupFormSubmit();
  await loadCinemas();
});

// Funzione loadCinemas: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadCinemas() {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('cinemas-table-body');
  if (!tableBody) return;

  try {
    // Variabile response: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const response = await API.getCinemas({
      page: currentPage,
      pageSize,
      search: currentSearch || undefined
    });

    // Variabile paged: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const paged = normalizePaged(response);
    totalPages = paged.totalPages;
    totalCinemasCount = paged.totalCount;
    currentPage = paged.page;

    renderCinemas(paged.items);
    updateStats(totalCinemasCount);
    renderPagination(paged.items.length);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-4 text-center text-ferrari-semantic-warning">Errore nel caricamento dei cinema</td></tr>';
    renderPagination(0);
  }
}

// Funzione renderCinemas: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderCinemas(cinemas) {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('cinemas-table-body');
  if (!tableBody) return;

  if (!cinemas.length) {
    tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-4 text-center text-body">Nessun cinema trovato</td></tr>';
    return;
  }

  tableBody.innerHTML = cinemas.map(cinema => `
    <tr class="row-hover">
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${cinema.id}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-ink">${cinema.nome}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${cinema.indirizzo || '-'}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${cinema.citta || '-'}</td>
      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button onclick="editCinema(${cinema.id})" class="text-ferrari-primary hover:text-ferrari-primary-hover mr-3">
          <i class="fa-solid fa-pencil"></i>
        </button>
        <button onclick="deleteCinema(${cinema.id}, '${escapeHtml(cinema.nome || '')}')" class="text-red-600 hover:text-red-900">
          <i class="fa-solid fa-trash"></i>
        </button>
      </td>
    </tr>
  `).join('');
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

  if (totalCinemasCount === 0 || serverItemsCount === 0) {
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
  const endItem = Math.min(currentPage * pageSize, totalCinemasCount);

  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalCinemasCount} cinema`;
  pageIndicator.textContent = `Pagina ${currentPage} di ${totalPages}`;

  firstBtn.disabled = currentPage <= 1;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  lastBtn.disabled = currentPage >= totalPages;
}

// Funzione updateStats: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateStats(totalCount) {
  // Variabile totalCinemasEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totalCinemasEl = document.getElementById('total-cinemas');
  if (totalCinemasEl) totalCinemasEl.textContent = String(totalCount);
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
    await loadCinemas();
  });
}

// Funzione goToPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function goToPage(page) {
  if (page < 1 || page > totalPages || page === currentPage) return;
  currentPage = page;
  await loadCinemas();
}

// Funzione goToFirstPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToFirstPage() {
  return goToPage(1);
}

// Funzione goToPrevPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToPrevPage() {
  return goToPage(currentPage - 1);
}

// Funzione goToNextPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToNextPage() {
  return goToPage(currentPage + 1);
}

// Funzione goToLastPage: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function goToLastPage() {
  return goToPage(totalPages);
}

// Funzione escapeHtml: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function escapeHtml(text) {
  // Variabile div: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Funzione setupFormSubmit: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function setupFormSubmit() {
  // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const form = document.getElementById('cinema-form');
  if (!form) return;

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  form.addEventListener('submit', async (e) => {
    e.preventDefault();

    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = serializeForm('cinema-form');
    delete data.telefono;
    // Variabile editId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const editId = form.dataset.editId;

    try {
      if (editId) {
        await API.updateCinema(editId, data);
        showToast('Cinema aggiornato con successo');
      } else {
        await API.createCinema(data);
        showToast('Cinema creato con successo');
      }

      closeModal('cinema-modal');
      await loadCinemas();
    } catch (error) {
      handleApiError(error);
    }
  });
}

// Funzione editCinema: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function editCinema(id) {
  try {
    // Variabile cinema: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const cinema = await API.getCinema(id);

    if (!cinema) {
      showToast('Cinema non trovato', 'danger');
      return;
    }

    openModal('cinema-modal', 'Modifica Cinema');

    // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const form = document.getElementById('cinema-form');
    if (!form) return;

    form.dataset.editId = id;

    // Variabile nomeInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const nomeInput = form.querySelector('[name="nome"]');
    // Variabile indirizzoInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const indirizzoInput = form.querySelector('[name="indirizzo"]');
    // Variabile cittaInput: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const cittaInput = form.querySelector('[name="citta"]');

    if (nomeInput) nomeInput.value = cinema.nome || '';
    if (indirizzoInput) indirizzoInput.value = cinema.indirizzo || '';
    if (cittaInput) cittaInput.value = cinema.citta || '';
  } catch (error) {
    handleApiError(error);
  }
}

// Funzione deleteCinema: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function deleteCinema(id, name) {
  openDeleteModal(name, async () => {
    try {
      await API.deleteCinema(id);
      showToast('Cinema eliminato con successo');
      await loadCinemas();
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
