// Registi Page JavaScript
let allFilms = [];
// Variabile currentPage: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentPage = 1;
// Variabile pageSize: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
const pageSize = 10;
// Variabile totalPages: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let totalPages = 1;
// Variabile totalRegistiCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let totalRegistiCount = 0;
// Variabile currentSearch: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
let currentSearch = '';

// Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
document.addEventListener('DOMContentLoaded', async () => {
  bindSearch();
  setupFormSubmit();
  await loadRegisti();
});

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

// Funzione loadRegisti: carica i dati iniziali o aggiorna il contenuto visibile della pagina. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function loadRegisti() {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('registi-table-body');
  if (!tableBody) return;

  try {
    const [registiResponse, filmsResponse] = await Promise.all([
      API.getRegisti({
        page: currentPage,
        pageSize,
        search: currentSearch || undefined
      }),
      API.getFilms()
    ]);

    // Variabile paged: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const paged = normalizePaged(registiResponse);
    totalPages = paged.totalPages;
    totalRegistiCount = paged.totalCount;
    currentPage = paged.page;

    allFilms = normalizeCollection(filmsResponse);
    renderRegisti(paged.items);
    updateStats(totalRegistiCount, paged.items);
    renderPagination(paged.items.length);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="6" class="px-6 py-4 text-center text-ferrari-semantic-warning">Errore nel caricamento dei registi</td></tr>';
    renderPagination(0);
  }
}

// Funzione renderRegisti: costruisce markup o componenti UI a partire dai dati in ingresso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function renderRegisti(registi) {
  // Variabile tableBody: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const tableBody = document.getElementById('registi-table-body');
  if (!tableBody) return;

  if (!registi.length) {
    tableBody.innerHTML = '<tr><td colspan="6" class="px-6 py-4 text-center text-body">Nessun regista trovato</td></tr>';
    return;
  }

  // Variabile filmCountByRegista: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const filmCountByRegista = {};
  allFilms.forEach(film => {
    if (film.registaId || film.registaId === 0) {
      // Variabile key: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
      const key = String(film.registaId);
      filmCountByRegista[key] = (filmCountByRegista[key] || 0) + 1;
    }
  });

  tableBody.innerHTML = registi.map(regista => {
    // Variabile filmCount: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const filmCount = filmCountByRegista[String(regista.id)] || 0;
    return `
      <tr class="row-hover">
        <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${regista.id}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-ink">${regista.nome}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-ink">${regista.cognome}</td>
        <td class="px-6 py-4 whitespace-nowrap">
          <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium chip-active">
            ${regista.nazionalita || '-'}
          </span>
        </td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-body">${filmCount}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
          <button onclick="editRegista(${regista.id})" class="text-ferrari-primary hover:text-ferrari-primary-hover mr-3">
            <i class="fa-solid fa-pencil"></i>
          </button>
          <button onclick="deleteRegista(${regista.id}, '${escapeHtml(`${regista.nome} ${regista.cognome}`)}')" class="text-red-600 hover:text-red-900">
            <i class="fa-solid fa-trash"></i>
          </button>
        </td>
      </tr>
    `;
  }).join('');
}

// Funzione updateStats: aggiorna lo stato o il DOM in base ai dati correnti. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
function updateStats(totalCount, visibleItems) {
  // Variabile totalRegistiEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totalRegistiEl = document.getElementById('total-registi');
  if (totalRegistiEl) totalRegistiEl.textContent = String(totalCount);

  // Variabile nationalities: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const nationalities = new Set(visibleItems.map(r => r.nazionalita).filter(Boolean));
  // Variabile totalNationalitiesEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totalNationalitiesEl = document.getElementById('total-nationalities');
  if (totalNationalitiesEl) totalNationalitiesEl.textContent = String(nationalities.size);

  // Variabile totalFilmsAssociatedEl: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
  const totalFilmsAssociatedEl = document.getElementById('total-films-associated');
  if (totalFilmsAssociatedEl) totalFilmsAssociatedEl.textContent = String(allFilms.length);
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
    await loadRegisti();
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

  if (totalRegistiCount === 0 || serverItemsCount === 0) {
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
  const endItem = Math.min(currentPage * pageSize, totalRegistiCount);

  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalRegistiCount} registi`;
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
  await loadRegisti();
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
  const form = document.getElementById('regista-form');
  if (!form) return;

  // Listener evento: si attiva quando scatta l'evento sulla pagina e aggiorna la UI o lo stato.
  form.addEventListener('submit', async (e) => {
    e.preventDefault();

    // Variabile data: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const data = serializeForm('regista-form');
    // Variabile editId: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const editId = form.dataset.editId;

    try {
      if (editId) {
        await API.updateRegista(editId, data);
        showToast('Regista aggiornato con successo');
      } else {
        await API.createRegista(data);
        showToast('Regista creato con successo');
      }

      closeModal('regista-modal');
      await loadRegisti();
    } catch (error) {
      handleApiError(error);
    }
  });
}

// Funzione editRegista: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function editRegista(id) {
  try {
    // Variabile regista: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const regista = await API.getRegista(id);
    openModal('regista-modal', 'Modifica Regista');

    // Variabile form: mantiene stato, riferimenti DOM o configurazione usata dalla logica della pagina.
    const form = document.getElementById('regista-form');
    form.dataset.editId = id;

    form.querySelector('[name="nome"]').value = regista.nome || '';
    form.querySelector('[name="cognome"]').value = regista.cognome || '';
    form.querySelector('[name="nazionalita"]').value = regista.nazionalita || '';
  } catch (error) {
    handleApiError(error);
  }
}

// Funzione deleteRegista: gestisce la logica prevista e restituisce il risultato atteso. Parametri: quelli definiti nella firma. Ritorno: valore o Promise previsto.
async function deleteRegista(id, name) {
  openDeleteModal(name, async () => {
    try {
      await API.deleteRegista(id);
      showToast('Regista eliminato con successo');
      await loadRegisti();
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
